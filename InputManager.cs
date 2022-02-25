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
        private int currentStep, tileSize;
        private readonly Form1 form;
        private bool inputHasChanged, sizeHasChanged;
        private Dictionary<string, Tuple<Color[], Tile, int>> tileCache;

        private XElement xRoot;

        private TilePropagator dbPropagator;
        private TileModel dbModel;

        public InputManager(Form1 formIn) {
            tileSize = 0;
            tileCache = new Dictionary<string, Tuple<Color[], Tile, int>>();
            form = formIn;
            currentStep = 0;
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

                        bool isCached = false;

                        foreach (PatternArray patternArray in patternList) {
                            isCached = form.addPattern(patternArray, currentColors.ToList());
                            if (isCached) {
                                break;
                            }
                        }

                        if (!isCached) {
                            form.bitMaps.saveCache();
                        }
                    } else {
                        dbModel = new AdjacentModel();
                        xRoot = XDocument.Load($"samples/{form.getSelectedInput()}/data.xml").Root;

                        tileSize = xRoot.get("size", 16);

                        List<double> simpleWeights = new();

                        if (xRoot == null) {
                            return (new Bitmap(0, 0), true);
                        }

                        TileRotationBuilder builder = new(4, true);
                        tileCache = new Dictionary<string, Tuple<Color[], Tile, int>>();
                        Dictionary<int, TileSymmetry> symmetries = new();

                        bool isCached = false;

                        foreach (XElement xTile in xRoot.Element("tiles")?.elements("tile")!) {
                            string tileName = xTile.get<string>("name");

                            char sym = xTile.get("symmetry", 'X');

                            TileSymmetry symmetry = TileSymmetry.X;
                            int cardinality;
                            switch (sym) {
                                case 'L':
                                    symmetry = TileSymmetry.L;
                                    cardinality = 4;
                                    break;
                                case 'T':
                                    symmetry = TileSymmetry.T;
                                    cardinality = 4;
                                    break;
                                case 'I':
                                    symmetry = TileSymmetry.I;
                                    cardinality = 2;
                                    break;
                                case '\\':
                                    symmetry = TileSymmetry.SLASH;
                                    cardinality = 2;
                                    break;
                                case 'F':
                                    symmetry = TileSymmetry.F;
                                    cardinality = 8;
                                    break;
                                default:
                                    cardinality = 1;
                                    break;
                            }

                            Bitmap bitmap = new($"samples/{form.getSelectedInput()}/{tileName}.png");
                            Color[] cur = imTile((x, y) => bitmap.GetPixel(x, y), tileSize);
                            int val = tileCache.Count;
                            tileCache.Add(tileName, new Tuple<Color[], Tile, int>(cur, new Tile(val), val));

                            symmetries[val] = symmetry;

                            for (int t = 1; t < cardinality; t++) {
                                string name = $"{tileName} {t}";
                                int myIdx = tileCache.Count;
                                tileCache.Add(name, t <= 3
                                    ? new Tuple<Color[], Tile, int>(rotate(cur.ToArray(), tileSize),
                                        new Tile(myIdx), val)
                                    : new Tuple<Color[], Tile, int>(reflect(cur.ToArray(), tileSize),
                                        new Tile(myIdx), val));
                            }

                            if (!isCached) {
                                isCached = form.addPattern(bitmap);
                            }

                            for (int t = 0; t < cardinality; t++) {
                                simpleWeights.Add(xTile.get("weight", 1.0));
                            }
                        }

                        if (!isCached) {
                            form.bitMaps.saveCache();
                        }

                        for (int i = 0; i < tileCache.Count; i++) {
                            builder.setTreatment(tileCache.ElementAt(i).Value.Item2, TileRotationTreatment.GENERATED);
                            int refIdx = tileCache.ElementAt(i).Value.Item3;
                            builder.addSymmetry(tileCache.ElementAt(i).Value.Item2, symmetries[refIdx]);
                        }

                        TileRotation rotations = builder.build();

                        dbModel = new AdjacentModel(DirectionSet.cartesian2d);

                        for (int i = 0; i < tileCache.Count; i++) {
                            double refWeight = simpleWeights[tileCache.ElementAt(i).Value.Item3];
                            Tile refTile = tileCache.ElementAt(i).Value.Item2;
                            ((AdjacentModel) dbModel).setFrequency(refTile, refWeight, rotations);
                        }

                        foreach (XElement xNeighbor in xRoot.Element("neighbors").Elements("neighbor")) {
                            string left = xNeighbor.get<string>("left");
                            string right = xNeighbor.get<string>("right");

                            Tile leftTile = tileCache[left].Item2;
                            Tile rightTile = tileCache[right].Item2;

                            ((AdjacentModel) dbModel).addAdjacency(new[] {leftTile}, new[] {rightTile},
                                Direction.X_PLUS, new TileRotation(4, true));
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

        public int getCurrentStep() {
            return currentStep;
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
                    currentStep++;
                }
            }

            Console.WriteLine(@"Stepping forward took " + sw.ElapsedMilliseconds + @"ms.");
            sw.Restart();

            Bitmap outputBitmap;
            if (form.isOverlappingModel()) {
                outputBitmap = new Bitmap(form.getOutputWidth(), form.getOutputHeight());
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.DarkGray);
                    }
                }
            } else {
                outputBitmap = new Bitmap(form.getOutputWidth() * tileSize, form.getOutputHeight() * tileSize);
                Console.WriteLine("SIMPLE");
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        int value = dbOutput.get(x, y);
                        Color[] outputPattern = value >= 0
                            ? tileCache.ElementAt(value).Value.Item1
                            : Enumerable.Repeat(Color.DarkGray, tileSize * tileSize).ToArray();

                        for (int yy = 0; yy < tileSize; yy++) {
                            for (int xx = 0; xx < tileSize; xx++) {
                                Color cur = outputPattern[xx * tileSize + yy];
                                outputBitmap.SetPixel(x * tileSize + xx, y * tileSize + yy, cur);
                            }
                        }
                    }
                }
            }

            Console.WriteLine(@"Bitmap took " + sw.ElapsedMilliseconds + @"ms.");
            return (outputBitmap, dbStatus == Resolution.DECIDED);
        }

        public Bitmap stepBackWfc(int steps) {
            Stopwatch sw = new();
            sw.Restart();
            for (int i = 0; i < steps; i++) {
                dbPropagator.doBacktrack();
            }

            Console.WriteLine(@"Stepping back took " + sw.ElapsedMilliseconds + @"ms.");
            sw.Restart();

            Bitmap outputBitmap;
            if (form.isOverlappingModel()) {
                outputBitmap = new Bitmap(form.getOutputWidth(), form.getOutputHeight());
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.DarkGray);
                    }
                }
            } else {
                outputBitmap = new Bitmap(form.getOutputWidth() * tileSize, form.getOutputHeight() * tileSize);
                Console.WriteLine("SIMPLE");
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        int value = dbOutput.get(x, y);
                        Color[] outputPattern = value >= 0
                            ? tileCache.ElementAt(value).Value.Item1
                            : Enumerable.Repeat(Color.DarkGray, tileSize * tileSize).ToArray();
                        for (int yy = 0; yy < tileSize; yy++) {
                            for (int xx = 0; xx < tileSize; xx++) {
                                Color cur = outputPattern[xx * tileSize + yy];
                                outputBitmap.SetPixel(x * tileSize + xx, y * tileSize + yy, cur);
                            }
                        }
                    }
                }
            }

            Console.WriteLine(@"Bitmap took " + sw.ElapsedMilliseconds + @"ms.");

            return outputBitmap;
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