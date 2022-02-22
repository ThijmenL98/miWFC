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
            xDoc = XDocument.Load("samples.xml");
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
            Console.WriteLine();
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            if (reset || dbPropagator == null) {
                bool selectedPeriodicity = form.isOverlappingModel() && form.getPeriodicEnabled();
                if (inputHasChanged) {
                    currentBitmap = getImage(form.getSelectedInput());
                    ITopoArray<Color> dbSample
                        = TopoArray.create(imageToColourArray(currentBitmap), selectedPeriodicity);
                    ITopoArray<Tile> tiles = dbSample.toTiles();

                    if (form.isOverlappingModel()) {
                        dbModel = new OverlappingModel(form.getSelectedOverlapTileDimension());
                        List<PatternArray> patternList = ((OverlappingModel) dbModel).addSample(tiles);
                        int total = form.bitMaps.getPatternCount();
                        form.bitMaps.reset();

                        for (int i = 0; i < total; i++) {
                            foreach (Control item in form.patternPanel.Controls) {
                                if (item.Name == "patternPB_" + i) {
                                    Thread.CurrentThread.IsBackground = true;
                                    form.patternPanel.Controls.Remove(item);
                                    break;
                                }
                            }
                        }

                        foreach (PatternArray patternArray in patternList) {
                            form.addPattern(patternArray, currentColors.ToList());
                        }
                    } else {
                        //TODO
                        dbModel = new AdjacentModel();
                        ((AdjacentModel) dbModel).addSample(tiles);

                        Console.WriteLine(tiles.get(0));
                    }

                    Console.WriteLine(@"Init took " + sw.ElapsedMilliseconds + @"ms.");
                    sw.Restart();
                    sizeHasChanged = true;
                }

                if (sizeHasChanged) {
                    GridTopology dbTopology = new GridTopology(form.getOutputWidth(), form.getOutputHeight(),
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
            Stopwatch sw = new Stopwatch();
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

            ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
            Bitmap outputBitmap = new Bitmap(form.getOutputWidth(), form.getOutputHeight());
            for (int y = 0; y < form.getOutputHeight(); y++) {
                for (int x = 0; x < form.getOutputWidth(); x++) {
                    Color cur = dbOutput.get(x, y);
                    outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.DarkGray);
                }
            }
            
            Console.WriteLine(@"Bitmap took " + sw.ElapsedMilliseconds + @"ms.");
            return (outputBitmap, dbStatus == Resolution.DECIDED);
        }

        public static Bitmap resizePixels(PictureBox pictureBox, Bitmap bitmap, int w1, int h1, int w2, int h2,
            int padding) {
            int marginX = (int) Math.Floor((pictureBox.Width - w2) / 2d) + padding;
            int marginY = (int) Math.Floor((pictureBox.Height - h2) / 2d) + padding;

            Bitmap outputBM = new Bitmap(pictureBox.Width, pictureBox.Height);
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

            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.Clear(Color.DarkGray);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                g.DrawImage(source, new Rectangle(0, 0, width, height));
                g.Save();
            }

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

            List<object> patternDimensionsList = new List<object>();
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
            List<string> images = new List<string>();
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

        public XElement getSimpleXRoot() {
            return xRoot;
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
            Console.WriteLine(@"Input changed on " + source);
            inputHasChanged = true;
        }

        public void setSizeChanged() {
            sizeHasChanged = true;
        }
    }
}