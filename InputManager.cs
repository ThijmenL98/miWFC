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
using WFC4All.enums;
using WFC4All.Properties;

namespace WFC4All {
    public class InputManager {
        private readonly XDocument xDoc;
        private Bitmap currentBitmap;
        private int currentStep, tileSize;
        private readonly Form1 form;
        private bool inputHasChanged;
        private Dictionary<int, Tuple<Color[], Tile>> tileCache;

        private XElement xRoot;

        private TilePropagator dbPropagator;
        private TileModel dbModel;
        private ITopoArray<Tile> tiles;

        private static SelectionHeuristic currentSelectionHeuristic;
        private static PatternHeuristic currentPatternHeuristic;

        private Bitmap latestOutput;

        public InputManager(Form1 formIn) {
            tileSize = 0;
            tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
            form = formIn;
            currentStep = 0;
            xDoc = XDocument.Parse(Resources.samples);
            currentBitmap = null;
            inputHasChanged = true;
            dbModel = null;
            dbPropagator = null;
            currentSelectionHeuristic = SelectionHeuristic.ENTROPY;
            currentPatternHeuristic = PatternHeuristic.WEIGHTED;
            latestOutput = new Bitmap(1, 1);
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
                bool inputPaddingEnabled, outputPaddingEnabled;

                if (form.getSelectedCategory().Equals("Textures")) {
                    inputPaddingEnabled = form.isOverlappingModel() && form.inputPaddingEnabled();
                    outputPaddingEnabled = inputPaddingEnabled;
                } else {
                    inputPaddingEnabled = form.getSelectedCategory().Equals("Worlds Top-Down")
                        || form.getSelectedCategory().Equals("Worlds Side-View")
                        || form.getSelectedInput().Equals("Font")
                        || form.getSelectedCategory().Equals("Knots") && !form.getSelectedInput().Equals("Nested")
                                                                      && !form.getSelectedInput().Equals("NotKnot");
                    outputPaddingEnabled = form.getSelectedCategory().Equals("Worlds Side-View")
                        || form.getSelectedInput().Equals("Font")
                        || form.getSelectedCategory().Equals("Knots") && !form.getSelectedInput().Equals("Nested");
                }
                
                if (inputHasChanged) {
                    form.displayLoading(true);
                    currentBitmap = getImage(form.getSelectedInput());

                    for (int i = 0; i < form.bitMaps.getPatternCount(); i++) {
                        foreach (Control item in form.patternPanel.Controls) {
                            if (item.Name.Contains("patternPB_")) {
                                Thread.CurrentThread.IsBackground = true;
                                form.patternPanel.Controls.Remove(item);
                                break;
                            }
                        }
                    }

                    form.bitMaps.reset();

                    if (form.isOverlappingModel()) {
                        ITopoArray<Color> dbSample
                            = TopoArray.create(imageToColourArray(currentBitmap),
                                inputPaddingEnabled); //TODO Input Padding
                        tiles = dbSample.toTiles();
                        dbModel = new OverlappingModel(form.getSelectedOverlapTileDimension());
                        bool hasRotations = form.getSelectedCategory().Equals("Worlds Top-Down")
                            || form.getSelectedCategory().Equals("Knots") || form.getSelectedCategory().Equals("Knots")
                            || form.getSelectedInput().Equals("Mazelike");
                        List<PatternArray> patternList
                            = ((OverlappingModel) dbModel).addSample(tiles, new TileRotation(hasRotations ? 4 : 1, false));

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

                        tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
                        bool isCached = false;

                        foreach (XElement xTile in xRoot.Element("tiles")?.elements("tile")!) {
                            Bitmap bitmap = new($"samples/{form.getSelectedInput()}/{xTile.get<string>("name")}.png");
                            int cardinality = xTile.get("symmetry", 'X') switch {
                                'L' => 4,
                                'T' => 4,
                                'I' => 2,
                                '\\' => 2,
                                'F' => 8,
                                _ => 1
                            };

                            Color[] cur = imTile((x, y) => bitmap.GetPixel(x, y), tileSize);
                            int val = tileCache.Count;
                            Tile curTile = new(val);
                            tileCache.Add(val, new Tuple<Color[], Tile>(cur, curTile));

                            for (int t = 1; t < cardinality; t++) {
                                int myIdx = tileCache.Count;
                                Color[] curCard = t <= 3
                                    ? rotate(tileCache[val + t - 1].Item1.ToArray(), tileSize)
                                    : reflect(tileCache[val + t - 4].Item1.ToArray(), tileSize);
                                tileCache.Add(myIdx, new Tuple<Color[], Tile>(curCard, new Tile(myIdx)));
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
                            double refWeight = simpleWeights[i];
                            Tile refTile = tileCache.ElementAt(i).Value.Item2;
                            ((AdjacentModel) dbModel).setFrequency(refTile, refWeight);
                        }

                        int[][] values = new int[50][];

                        int j = 0;
                        foreach (XElement xTile in xRoot.Element("rows")?.elements("row")!) {
                            string[] row = xTile.Value.Split(',');
                            values[j] = new int[50];
                            for (int k = 0; k < 50; k++) {
                                values[j][k] = int.Parse(row[k]);
                            }

                            j++;
                        }

                        ITopoArray<int> sample = TopoArray.create(values, false);
                        dbModel = new AdjacentModel(sample.toTiles());
                    }

#if (DEBUG)
                    Console.WriteLine(@$"Init took {sw.ElapsedMilliseconds}ms.");
#endif
                    sw.Restart();
                }

                //TODO Output Padding
                GridTopology dbTopology = new(form.getOutputWidth(), form.getOutputHeight(), outputPaddingEnabled);
                int curSeed = Environment.TickCount;
                dbPropagator = new TilePropagator(dbModel, dbTopology, new TilePropagatorOptions {
                    BackTrackDepth = -1,
                    RandomDouble = new Random(curSeed).NextDouble,
                }, (int) currentSelectionHeuristic, (int) currentPatternHeuristic);
#if (DEBUG)
                Console.WriteLine(@$"Assigning took {sw.ElapsedMilliseconds}ms.");
#endif
                sw.Restart();

                if (form.isOverlappingModel()) {
                    if ("flowers".Equals(form.getSelectedInput().ToLower())) {
                        // Set the bottom last 2 rows to be the ground tile
                        dbPropagator?.select(0, form.getOutputHeight() - 1, 0,
                            new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                        dbPropagator?.select(0, form.getOutputHeight() - 2, 0,
                            new Tile(currentColors.ElementAt(currentColors.Count - 1)));

                        // And ban it elsewhere
                        for (int y = 0; y < form.getOutputHeight() - 2; y++) {
                            dbPropagator?.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                        }
                    }

                    if ("skyline".Equals(form.getSelectedInput().ToLower())) {
                        // Set the bottom last row to be the ground tile
                        dbPropagator?.select(0, form.getOutputHeight() - 1, 0,
                            new Tile(currentColors.ElementAt(currentColors.Count - 1)));

                        // And ban it elsewhere
                        for (int y = 0; y < form.getOutputHeight() - 1; y++) {
                            dbPropagator?.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                        }
                    }
                }

                inputHasChanged = false;
            }

            form.displayLoading(false);

            bool decided;
            (latestOutput, decided) = runWfcDB(steps);
            return (latestOutput, decided);
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

#if (DEBUG)
            Console.WriteLine(@$"Stepping forward took {sw.ElapsedMilliseconds}ms.");
#endif
            sw.Restart();

            Bitmap outputBitmap;
            if (form.isOverlappingModel()) {
                outputBitmap = new Bitmap(form.getOutputWidth(), form.getOutputHeight());
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        Color cur = dbOutput.get(x, y); // TODO Check if colour is made from multiple?
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            } else {
                outputBitmap = new Bitmap(form.getOutputWidth() * tileSize, form.getOutputHeight() * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        int value = dbOutput.get(x, y);
                        Color[] outputPattern = value >= 0
                            ? tileCache.ElementAt(value).Value.Item1
                            : Enumerable.Repeat(Color.Silver, tileSize * tileSize).ToArray();

                        for (int yy = 0; yy < tileSize; yy++) {
                            for (int xx = 0; xx < tileSize; xx++) {
                                Color cur = outputPattern[yy * tileSize + xx];
                                outputBitmap.SetPixel(x * tileSize + xx, y * tileSize + yy,
                                    cur);
                            }
                        }
                    }
                }
            }

#if (DEBUG)
            Console.WriteLine(@$"Bitmap took {sw.ElapsedMilliseconds}ms. {dbStatus}");
#endif
            return (outputBitmap, dbStatus == Resolution.DECIDED);
        }

        public Bitmap stepBackWfc(int steps) {
            Stopwatch sw = new();
            sw.Restart();
            for (int i = 0; i < steps; i++) {
                dbPropagator.doBacktrack();
            }

#if (DEBUG)
            Console.WriteLine(@$"Stepping back took {sw.ElapsedMilliseconds}ms.");
#endif
            sw.Restart();

            Bitmap outputBitmap;
            if (form.isOverlappingModel()) {
                outputBitmap = new Bitmap(form.getOutputWidth(), form.getOutputHeight());
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            } else {
                outputBitmap = new Bitmap(form.getOutputWidth() * tileSize, form.getOutputHeight() * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < form.getOutputHeight(); y++) {
                    for (int x = 0; x < form.getOutputWidth(); x++) {
                        int value = dbOutput.get(x, y);
                        Color[] outputPattern = value >= 0
                            ? tileCache.ElementAt(value).Value.Item1
                            : Enumerable.Repeat(Color.Silver, tileSize * tileSize).ToArray();
                        for (int yy = 0; yy < tileSize; yy++) {
                            for (int xx = 0; xx < tileSize; xx++) {
                                Color cur = outputPattern[yy * tileSize + xx];
                                outputBitmap.SetPixel(x * tileSize + xx, y * tileSize + yy, cur);
                            }
                        }
                    }
                }
            }

#if (DEBUG)
            Console.WriteLine(@$"Bitmap took {sw.ElapsedMilliseconds}ms.");
#endif

            return outputBitmap;
        }

        public Bitmap resizePixels(PictureBox pictureBox, Bitmap bitmap, int padding, Color borderColor,
            bool drawLines) {
            int w2 = pictureBox.Width, h2 = pictureBox.Height, w1 = bitmap.Width, h1 = bitmap.Height;

            Bitmap outputBM = new(pictureBox.Width, pictureBox.Height);
            double xRatio = w1 / (double) (w2 - padding * 2);
            double yRatio = h1 / (double) (h2 - padding * 2);

            try {
                for (int i = 0; i < h2 - padding; i++) {
                    int py = (int) Math.Floor(i * yRatio);
                    for (int j = 0; j < w2 - padding; j++) {
                        int px = (int) Math.Floor(j * xRatio);
                        Color nextC;
                        if (px >= bitmap.Width || py >= bitmap.Height) {
                            nextC = borderColor;
                        } else if (drawLines && form.isOverlappingModel() && w1 != 2 &&
                            (i % ((h2 - padding) / h1) == 0 && i != 0 ||
                                j % ((w2 - padding) / w1) == 0 && j != 0)) {
                            Color c1 = Color.Gray;
                            Color c2 = bitmap.GetPixel(px, py);
                            nextC = Color.FromArgb((c1.A + c2.A) / 2, (c1.R + c2.R) / 2, (c1.G + c2.G) / 2,
                                (c1.B + c2.B) / 2);
                        } else {
                            nextC = bitmap.GetPixel(px, py);
                        }

                        outputBM.SetPixel(j + padding - padding, i + padding - padding, nextC);
                    }
                }
            } catch (DivideByZeroException) { }

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

        public Bitmap resizeBitmap(Bitmap source, float scale) {
            int width, height;
            if (form.isOverlappingModel()) {
                width = source.Width * (int) scale;
                height = source.Height * (int) scale;
            } else {
                width = (int) (source.Width * scale);
                height = (int) (source.Height * scale);
            }

            Bitmap bmp = new(width, height);

            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.DarkGray);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, width, height));

            Pen semiTransPen = new(Color.FromArgb(100, 20, 20, 20), 1);

            if (form.isOverlappingModel()) {
                for (int i = 0; i < source.Width; i++) {
                    // Vertical gridlines
                    g.DrawLine(semiTransPen, i * (int) scale, 0, i * (int) scale, source.Height * (int) scale);
                }

                for (int i = 0; i < source.Height; i++) {
                    // Horizontal gridlines
                    g.DrawLine(semiTransPen, 0, i * (int) scale, source.Width * (int) scale, i * (int) scale);
                }
            }

            g.Save();

            return bmp;
        }

        /*
         * Getters
         */

        public int getCurrentStep() {
            return currentStep;
        }

        public Bitmap getLatestOutput() {
            return latestOutput;
        }

        public bool isCollapsed() {
            return dbPropagator.Status == Resolution.DECIDED;
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public (object[], int) getImagePatternDimensions(string imageName) {
            IEnumerable<XElement> xElements = xDoc.Root.elements("overlapping", "simpletiled");
            IEnumerable<int> matchingElements = xElements.Where(x =>
                x.get<string>("name") == imageName).Select(t =>
                t.get("patternSize", 3));


            List<object> patternDimensionsList = new();
            int j = 0;
            for (int i = 2; i < 6; i++) {
                if (i >= 4 && !matchingElements.Contains(5) && !matchingElements.Contains(4)) {
                    break;
                }

                patternDimensionsList.Add("  " + i);
                if (j == 0 && matchingElements.Contains(i)) {
                    j = i;
                }
            }

            return (patternDimensionsList.ToArray(), j - 2);
        }

        public string[] getImages(string modelType, string category) {
            List<string> images = new();
            if (xDoc.Root != null) {
                images = xDoc.Root.Elements(modelType)
                    .Where(xElement => xElement.get<string>("category").Equals(category))
                    .Select(xElement => xElement.get<string>("name")).ToList();
            }

            images.Sort();

            return images.Distinct().ToArray();
        }

        public static Bitmap getImage(string name) {
            return new Bitmap($"samples/{name}.png");
        }

        //TODO Maybe re-enable
        // private bool transformationIsEnabled(int i) {
        //     if (i == 0) {
        //         return true;
        //     }
        //
        //     PictureBox currentPB = form.pbs[i - 1];
        //     return currentPB.BackColor.Equals(Color.LawnGreen);
        // }

        /*
         * Setters
         */

        public void setInputChanged(string source) {
            if (!form.isChangingModels) {
#if (DEBUG)
                Console.WriteLine(@$"Input changed on {source}");
#endif
                inputHasChanged = true;
            }
        }

        public static void setSelectionHeuristic(SelectionHeuristic selectionHeuristic) {
            currentSelectionHeuristic = selectionHeuristic;
        }

        public static void setPatternHeuristic(PatternHeuristic patternHeuristic) {
            currentPatternHeuristic = patternHeuristic;
        }

        /*
         * Pattern Adaptation Simple
         */

        private static Color[] imTile(Func<int, int, Color> f, int tilesize) {
            Color[] result = new Color[tilesize * tilesize];
            for (int y = 0; y < tilesize; y++) {
                for (int x = 0; x < tilesize; x++) {
                    result[x + y * tilesize] = f(x, y);
                }
            }

            return result;
        }

        private static Color[] rotate(IReadOnlyList<Color> array, int tilesize) {
            return imTile((x, y) => array[tilesize - 1 - y + x * tilesize], tilesize);
        }

        private static Color[] reflect(IReadOnlyList<Color> array, int tilesize) {
            return imTile((x, y) => array[tilesize - 1 - x + y * tilesize], tilesize);
        }

        public static string[] getCategories(string modelType) {
            return modelType.Equals("overlapping")
                ? new[] {"Textures", "Shapes", "Knots", "Fonts", "Worlds Side-View", "Worlds Top-Down"}
                : new[] {"Worlds Top-Down", "Textures"};
        }
    }
}