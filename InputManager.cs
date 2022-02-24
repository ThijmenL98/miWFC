using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using WFC4All.DeBroglie;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;

namespace WFC4All {
    public class InputManager {
        private readonly XDocument xDoc;
        private Bitmap currentBitmap;
        private readonly int groundPatternIdx;
        private readonly Form1 form;
        private bool inputHasChanged, sizeHasChanged;

        private XElement xRoot;

        private TilePropagator dbPropagator;
        private TileModel dbModel;

        public InputManager(Form1 formIn) {
            form = formIn;
            xDoc = XDocument.Load("./samples.xml");
            currentBitmap = null;
            inputHasChanged = true;
            sizeHasChanged = true;
            groundPatternIdx = 0;
            dbModel = null;
            dbPropagator = null;
        }

        /*
         * Functionality
         */

        public (Bitmap, bool) initAndRunWfcDB(bool reset, int steps) {
            if (form.isChangingModels) {
                return (new Bitmap(1, 1), true);
            }
            
            Stopwatch sw = new();
            sw.Restart();
            
            if (reset || dbPropagator == null) {
                bool selectedPeriodicity = form.isOverlappingModel() && form.getPeriodicEnabled();
                if (inputHasChanged) {
                    currentBitmap = getImage(form.getSelectedInput());
                    
                    for (int i = 0; i < form.bitMaps.getPatternCount(); i++) {
                        foreach (Control item in form.patternPanel.Controls) {
                            if (item.Name == "patternPB_" + i) {
                                Thread.CurrentThread.IsBackground = true;
                                form.patternPanel.Controls.Remove(item);
                                break;
                            }
                        }
                    }
                    form.bitMaps.reset();

                    if (form.isOverlappingModel()) {
                        ITopoArray<Color> dbSample
                            = TopoArray.create(imageToColourArray(currentBitmap), selectedPeriodicity);
                        ITopoArray<Tile> tiles = dbSample.toTiles();
                        dbModel = new OverlappingModel(form.getSelectedOverlapTileDimension());
                        List<PatternArray> patternList = ((OverlappingModel) dbModel).addSample(tiles);

                        foreach (PatternArray patternArray in patternList) {
                            form.addPattern(patternArray, currentColors.ToList());
                        }
                    } else {
                        dbModel = new AdjacentModel();
                        xRoot = XDocument.Load($"samples/{form.getSelectedInput()}/data.xml").Root;

                        int tileSize = xRoot.get("size", 16);

                        List<Color[]> simpleColors = new();
                        List<int[]> simpleActions = new();
                        List<double> simpleWeights = new();

                        if (xRoot == null) {
                            return (new Bitmap(0, 0), true);
                        }

                        TileRotationBuilder builder = new(4, true);
                        List<Tile> tiles = new();

                        foreach (XElement xTile in xRoot.Element("tiles")?.elements("tile")!) {
                            string tileName = xTile.get<string>("name");

                            Func<int, int> a, b;
                            int cardinality;

                            char sym = xTile.get("symmetry", 'X');
                            TileSymmetry symmetry = TileSymmetry.X;
                            switch (sym) {
                                case 'L':
                                    symmetry = TileSymmetry.L;
                                    cardinality = 4;
                                    a = i => (i + 1) % 4;
                                    b = i => i % 2 == 0 ? i + 1 : i - 1;
                                    break;
                                case 'T':
                                    symmetry = TileSymmetry.T;
                                    cardinality = 4;
                                    a = i => (i + 1) % 4;
                                    b = i => i % 2 == 0 ? i : 4 - i;
                                    break;
                                case 'I':
                                    symmetry = TileSymmetry.I;
                                    cardinality = 2;
                                    a = i => 1 - i;
                                    b = i => i;
                                    break;
                                case '\\':
                                    symmetry = TileSymmetry.SLASH;
                                    cardinality = 2;
                                    a = i => 1 - i;
                                    b = i => 1 - i;
                                    break;
                                case 'F':
                                    symmetry = TileSymmetry.F;
                                    cardinality = 8;
                                    a = i => i < 4 ? (i + 1) % 4 : 4 + (i - 1) % 4;
                                    b = i => i < 4 ? i + 4 : i - 4;
                                    break;
                                default:
                                    cardinality = 1;
                                    a = i => i;
                                    b = i => i;
                                    break;
                            }

                            Tile tile = new(tileName);
                            tiles.Add(tile);
                            builder.setTreatment(tile, TileRotationTreatment.GENERATED);
                            builder.addSymmetry(tile, symmetry);

                            int actionCount = simpleActions.Count;

                            int[][] map = new int[cardinality][];
                            for (int t = 0; t < cardinality; t++) {
                                map[t] = new int[8];

                                map[t][0] = t;
                                map[t][1] = a(t);
                                map[t][2] = a(a(t));
                                map[t][3] = a(a(a(t)));
                                map[t][4] = b(t);
                                map[t][5] = b(a(t));
                                map[t][6] = b(a(a(t)));
                                map[t][7] = b(a(a(a(t))));

                                for (int s = 0; s < 8; s++) {
                                    map[t][s] += actionCount;
                                }

                                simpleActions.Add(map[t]);
                            }

                            Bitmap bitmap = new($"samples/{form.getSelectedInput()}/{tileName}.png");
                            simpleColors.Add(imTile((x, y) => bitmap.GetPixel(x, y), tileSize));

                            for (int t = 1; t < cardinality; t++) {
                                simpleColors.Add(t <= 3
                                    ? rotate(simpleColors[actionCount + t - 1], tileSize)
                                    : reflect(simpleColors[actionCount + t - 4], tileSize));
                            }

                            form.addPattern(bitmap);

                            for (int t = 0; t < cardinality; t++) {
                                simpleWeights.Add(xTile.get("weight", 1.0));
                            }
                        }
                        
                        TileRotation rotations = builder.build();
                        dbModel = new AdjacentModel(DirectionSet.cartesian2d);
                        foreach ((Tile tile, int i) in tiles.Select((value, i) => ( value, i ))) {
                            ((AdjacentModel) dbModel).setFrequency(tile, simpleWeights[i], rotations);
                        }
                    }

                    Console.WriteLine(@"Init took " + sw.ElapsedMilliseconds + @"ms.");
                    sw.Restart();
                    sizeHasChanged = true;
                }

                if (sizeHasChanged) {
                    GridTopology dbTopology = new(form.getOutputWidth(), form.getOutputHeight(),
                        selectedPeriodicity);
                    dbPropagator = new TilePropagator(dbModel, dbTopology, new TilePropagatorOptions {
                        BackTrackDepth = -1,
                    });
                    Console.WriteLine(@"Assigning took " + sw.ElapsedMilliseconds + @"ms.");
                    sw.Restart();
                } else {
                    dbPropagator?.clear();
                    Console.WriteLine(@"Clearing took " + sw.ElapsedMilliseconds + @"ms.");
                }

                inputHasChanged = false;
                sizeHasChanged = false;
            }

            return runWfcDB(steps);
        }

        private (Bitmap, bool) runWfcDB(int steps) {
            Stopwatch sw = new();
            sw.Restart();
            Resolution dbStatus = Resolution.UNDECIDED;
            if (steps == -1) {
                dbStatus = dbPropagator.run();
            } else {
                for (int i = 0; i < steps; i++) {
                    dbStatus = dbPropagator.step();
                }
            }

            Console.WriteLine(@"Stepping took " + sw.ElapsedMilliseconds + @"ms.");
            sw.Restart();

            Bitmap outputBitmap = new(form.getOutputWidth(), form.getOutputHeight());
            if (form.isOverlappingModel()) {
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.DarkGray);
                    }
                }
            } else {
                Console.WriteLine("SIMPLE");
                ITopoArray<object> dbOutput = dbPropagator.toValueArray<object>();
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        //Console.WriteLine(dbOutput.get(x,y) == null);
                    }
                }
            }

            Console.WriteLine(@"Bitmap took " + sw.ElapsedMilliseconds + @"ms.");
            return (outputBitmap, dbStatus == Resolution.DECIDED);
        }

        public static Bitmap resizePixels(PictureBox pictureBox, Bitmap bitmap, int w1, int h1, int w2, int h2,
            int padding) {
            int marginX = (int) Math.Floor((pictureBox.Width - w2) / 2d) + padding;
            int marginY = (int) Math.Floor((pictureBox.Height - h2) / 2d) + padding;

            Bitmap outputBM = new(pictureBox.Width, pictureBox.Height);
            double xRatio = w1 / (double) (w2 - padding * 2);
            double yRatio = h1 / (double) (h2 - padding * 2);

            for (int x = 0; x < pictureBox.Width - padding; x++) {
                for (int y = 0; y < pictureBox.Height - padding; y++) {
                    if (y <= marginY || x <= marginX || y >= pictureBox.Height - marginY ||
                        x >= pictureBox.Width - marginX) {
                        outputBM.SetPixel(x, y, Color.DarkGray);
                    } else {
                        // Skip ahead horizontally
                        y = pictureBox.Height - marginY - 1;
                    }
                }
            }

            for (int i = 0; i < h2 - padding; i++) {
                int py = (int) Math.Floor(i * yRatio);
                for (int j = 0; j < w2 - padding; j++) {
                    int px = (int) Math.Floor(j * xRatio);
                    Color nextC;
                    if (px >= bitmap.Width || py >= bitmap.Height) {
                        nextC = Color.Transparent;
                    } else {
                        nextC = bitmap.GetPixel(px, py);
                    }

                    outputBM.SetPixel(j + marginX - padding, i + marginY - padding, nextC);
                }
            }

            return outputBM;
        }

        private static HashSet<Color> currentColors;

        private static Color[][] imageToColourArray(Bitmap bmp) {
            int width = bmp.Width;
            int height = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            byte[] bytes = new byte[height * data.Stride];
            try {
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            } finally {
                bmp.UnlockBits(data);
            }

            Color[][] result = new Color[height][];
            currentColors = new HashSet<Color>();
            for (int y = 0; y < height; ++y) {
                result[y] = new Color[width];
                for (int x = 0; x < width; ++x) {
                    int offset = y * data.Stride + x * 3;
                    Color c = Color.FromArgb(255, bytes[offset + 2], bytes[offset + 1], bytes[offset + 0]);
                    result[y][x] = c;
                    currentColors.Add(c);
                }
            }

            return result;
        }

        public static Bitmap resizeBitmap(Bitmap source, float scale) {
            int width = (int) (source.Width * scale);
            int height = (int) (source.Height * scale);

            Bitmap bmp = new(width, height);

            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.DarkGray);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, width, height));
            g.Save();

            return bmp;
        }

        /*
         * Getters
         */

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public (object[], int) getImagePatternDimensions(string imageName) {
            IEnumerable<XElement> xElements = xDoc.Root.elements("overlapping", "simpletiled");
            IEnumerable<int> matchingElements = xElements.Where(x =>
                x.get<string>("name") == imageName).Select(t =>
                t.get("N", 3));

            List<object> patternDimensionsList = new();
            int j = 0;
            for (int i = 2; i < 6; i++) {
                string append = matchingElements.Contains(i) ? " (recommended)" : "";
                patternDimensionsList.Add("  " + i + append);
                if (j == 0 && matchingElements.Contains(i)) {
                    j = i;
                }
            }

            return (patternDimensionsList.ToArray(), j - 2);
        }

        public string[] getImages(string modelType) {
            List<string> images = new();
            if (xDoc.Root != null) {
                images = xDoc.Root.Elements(modelType).Select(xElement => xElement.get<string>("name"))
                    .ToList();
            }

            images.Sort();

            return images.Distinct().ToArray();
        }

        public static Bitmap getImage(string name) {
            return new Bitmap($"samples/{name}.png");
        }

        private bool transformationIsEnabled(int i) {
            if (i == 0) {
                return true;
            }

            PictureBox currentPB = form.pbs[i - 1];
            return currentPB.BackColor.Equals(Color.LawnGreen);
        }

        /*
         * Setters
         */

        public void setInputChanged(string source) {
            if (!form.isChangingModels) {
                Console.WriteLine(@"Input changed on " + source);
                inputHasChanged = true;
            }
        }

        public void setSizeChanged() {
            sizeHasChanged = true;
        }


        /*
         * Pattern Adaptation Simple
         */

        private Color[] imTile(Func<int, int, Color> f, int tilesize) {
            Color[] result = new Color[tilesize * tilesize];
            for (int y = 0; y < tilesize; y++) {
                for (int x = 0; x < tilesize; x++) {
                    result[x + y * tilesize] = f(x, y);
                }
            }

            return result;
        }

        private Color[] rotate(IReadOnlyList<Color> array, int tilesize) {
            return imTile((x, y) => array[tilesize - 1 - y + x * tilesize], tilesize);
        }

        private Color[] reflect(IReadOnlyList<Color> array, int tilesize) {
            return imTile((x, y) => array[tilesize - 1 - x + y * tilesize], tilesize);
        }
    }
}