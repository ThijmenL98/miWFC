using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WFC4All {
    public class InputManager {
        private readonly XDocument xDoc;
        private Bitmap currentBitmap;
        private List<Color> overlappingColors;
        private List<Color[]> simpleColors;
        private byte[,] sample;
        private Dictionary<long, int> weightsDictionary;
        private List<long> ordering;
        private int groundPatternIdx, lastDim, tileSize;
        private readonly Form1 form;
        private bool inputHasChanged;
        private string curFile;
        private List<double> simpleWeights;
        private List<int[]> simpleActions;
        private List<string> tileNames;
        private Dictionary<string, int> firstOccurrences;

        private XElement xRoot;

        public InputManager(Form1 formIn) {
            form = formIn;
            xDoc = XDocument.Load("samples.xml");
            currentBitmap = null;
            overlappingColors = new List<Color>();
            simpleColors = new List<Color[]>();
            weightsDictionary = new Dictionary<long, int>();
            ordering = new List<long>();
            inputHasChanged = true;
            groundPatternIdx = 0;
            curFile = "";
        }

        /*
         * Functionality
         */

        public Bitmap runWfc() {
            Stopwatch sw = Stopwatch.StartNew();

            Random random = new Random();

            XElement xElem = xDoc.Root.Elements("overlapping", "simpletiled")
                .Where(x => x.Get<string>("name") == form.getSelectedInput()).ElementAtOrDefault(0);

            if ((xElem != null && xElem.Name == "overlapping") || lastDim != form.getSelectedOverlapTileDimension()) {
                extractPatterns(lastDim != form.getSelectedOverlapTileDimension(),
                    xElem != null && xElem.Name == "overlapping");
                lastDim = form.getSelectedOverlapTileDimension();
                Console.WriteLine($@"Pattern Extraction = {sw.ElapsedMilliseconds}ms.");
                sw.Restart();
            }

            Model model;
            string name = xElem.Get<string>("name");

            string heuristicString = xElem.Get<string>("heuristic");
            Model.Heuristic heuristic = heuristicString == "Scanline"
                ? Model.Heuristic.SCANLINE
                : heuristicString == "MRV"
                    ? Model.Heuristic.MRV
                    : Model.Heuristic.ENTROPY;

            if (xElem != null && xElem.Name == "overlapping") {
                int groundPattern = 0;

                if (name.Equals("Skyline") || name.Equals("Flowers") || name.Equals("Platformer") ||
                    name.Equals("Skyline2")) {
                    groundPattern = getGroundPatternIdx();
                }

                model = new OverlappingModel(form.getSelectedOverlapTileDimension(), form.getOutputWidth(),
                    form.getOutputHeight(), form.getPeriodicEnabled(), groundPattern, heuristic, form, this);
            } else {
                model = new SimpleTiledModel(form.getOutputWidth(), form.getOutputHeight(), form.getPeriodicEnabled(),
                    heuristic, form, this);
            }

            while (sw.ElapsedMilliseconds < 3000) {
                int seed = random.Next();
                bool success = model.Run(seed, -1); //Setting Limit to e.g. x, causes it to stop after adding x tiles

                if (success) {
                    Console.WriteLine($@"Algorithm = {sw.ElapsedMilliseconds}ms.");
                    return model.Graphics();
                }
            }

            Console.WriteLine($@"Algorithm = {sw.ElapsedMilliseconds}ms.");
            return null;
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
                        nextC = Color.LawnGreen;
                    } else {
                        nextC = bitmap.GetPixel(px, py);
                    }

                    outputBM.SetPixel(j + marginX - padding, i + marginY - padding, nextC);
                }
            }

            return outputBM;
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

        public void extractPatterns(bool force, bool overlapping) {
            if (!inputHasChanged && !force) {
                return;
            }

            form.setPatternLabelVisible();

            bool fileChanged = false;
            if (!curFile.Equals(form.getSelectedInput())) {
                curFile = form.getSelectedInput();
                fileChanged = true;
            }

            currentBitmap = getImage(form.getSelectedInput());

            if (fileChanged) {
                int total = form.bitMaps.getPatternCount();
                for (int i = 0; i < total; i++) {
                    foreach (Control item in form.patternPanel.Controls) {
                        if (item.Name == "patternPB_" + i) {
                            Thread.CurrentThread.IsBackground = true;
                            form.patternPanel.Controls.Remove(item);
                            break;
                        }
                    }
                }
            }

            bool periodicInput = form.getPeriodicEnabled();
            int overlapTileDimension = form.getSelectedOverlapTileDimension();
            int inputWidth = currentBitmap.Width, inputHeight = currentBitmap.Height;

            if (overlapping) {
                sample = new byte[inputWidth, inputHeight];
                overlappingColors = new List<Color>();
                for (int y = 0; y < inputHeight; y++) {
                    for (int x = 0; x < inputWidth; x++) {
                        Color color = currentBitmap.GetPixel(x, y);

                        int colorIndex = overlappingColors.TakeWhile(c => c != color).Count();

                        if (colorIndex == overlappingColors.Count) {
                            overlappingColors.Add(color);
                        }

                        sample[x, y] = (byte) colorIndex;
                    }
                }

                int colorsCount = overlappingColors.Count;

                long index(IReadOnlyList<byte> inputPattern) {
                    long result = 0, power = 1;
                    for (int pixelIdx = 0; pixelIdx < inputPattern.Count; pixelIdx++) {
                        result += inputPattern[inputPattern.Count - 1 - pixelIdx] * power;
                        power *= colorsCount;
                    }

                    return result;
                }

                weightsDictionary = new Dictionary<long, int>();
                ordering = new List<long>();

                Stopwatch sw = new Stopwatch();
                for (int y = 0; y < (periodicInput ? inputHeight : inputHeight - overlapTileDimension + 1); y++) {
                    for (int x = 0; x < (periodicInput ? inputWidth : inputWidth - overlapTileDimension + 1); x++) {
                        byte[][] patternSymmetry = new byte[8][];

                        patternSymmetry[0] = patternFromSample(x, y);
                        patternSymmetry[1] = reflect(patternSymmetry[0]); // pattern flipped over y axis once
                        patternSymmetry[2] = rotate(patternSymmetry[0]); // pattern rotated CW once
                        patternSymmetry[3] = reflect(patternSymmetry[2]); // pattern rotated CW once, then flipped
                        patternSymmetry[4] = rotate(patternSymmetry[2]); // pattern rotated CW twice
                        patternSymmetry[5] = reflect(patternSymmetry[4]); // pattern rotated CW twice, then flipped
                        patternSymmetry[6] = rotate(patternSymmetry[4]); // pattern rotated CW thrice
                        patternSymmetry[7] = reflect(patternSymmetry[6]); // pattern rotated CW thrice, then flipped

                        for (int i = 0; i < 8; i++) {
                            if (i == 0 || transformationIsEnabled(i)) {
                                long idx = index(patternSymmetry[i]);

                                if (weightsDictionary.ContainsKey(idx)) {
                                    weightsDictionary[idx]++;
                                } else {
                                    weightsDictionary.Add(idx, 1);
                                    ordering.Add(idx);

                                    //TODO check if pattern is similar to others (flipped)
                                    Thread.CurrentThread.IsBackground = true;
                                    form.addPattern(patternSymmetry[0], overlappingColors, overlapTileDimension,
                                        weightsDictionary.Count - 1, i == 0 && fileChanged);
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Transformation extraction: " + sw.ElapsedMilliseconds + "ms.");

                groundPatternIdx = form.bitMaps.getFloorIndex(weightsDictionary.Count);
            } else {
                xRoot = XDocument.Load($"samples/{curFile}/data.xml").Root;
                tileSize = xRoot.Get("size", 16);
                bool unique = xRoot.Get("unique", false);
                simpleColors = new List<Color[]>();
                tileNames = new List<string>();
                simpleActions = new List<int[]>();
                simpleWeights = new List<double>();
                firstOccurrences = new Dictionary<string, int>();

                if (xRoot == null) {
                    return;
                }

                int curPattern = 0;

                foreach (XElement xTile in xRoot.Element("tiles")?.Elements("tile")) {
                    string tileName = xTile.Get<string>("name");

                    Func<int, int> a, b;
                    int cardinality;

                    char sym = xTile.Get("symmetry", 'X');
                    switch (sym) {
                        case 'L':
                            cardinality = 4;
                            a = i => (i + 1) % 4;
                            b = i => i % 2 == 0 ? i + 1 : i - 1;
                            break;
                        case 'T':
                            cardinality = 4;
                            a = i => (i + 1) % 4;
                            b = i => i % 2 == 0 ? i : 4 - i;
                            break;
                        case 'I':
                            cardinality = 2;
                            a = i => 1 - i;
                            b = i => i;
                            break;
                        case '\\':
                            cardinality = 2;
                            a = i => 1 - i;
                            b = i => 1 - i;
                            break;
                        case 'F':
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

                    int actionCount = simpleActions.Count;
                    firstOccurrences.Add(tileName, actionCount);

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

                    if (unique) {
                        Bitmap bitmap = null;
                        for (int t = 0; t < cardinality; t++) {
                            bitmap = new Bitmap($"samples/{curFile}/{tileName} {t}.png");
                            simpleColors.Add(tile((x, y) => bitmap.GetPixel(x, y), tileSize));
                            tileNames.Add($"{tileName} {t}");
                            curPattern++;
                        }

                        if (bitmap != null) {
                            form.addPattern(bitmap, curPattern);
                        }
                    } else {
                        Bitmap bitmap = new Bitmap($"samples/{curFile}/{tileName}.png");
                        simpleColors.Add(tile((x, y) => bitmap.GetPixel(x, y), tileSize));
                        tileNames.Add($"{tileName} 0");

                        for (int t = 1; t < cardinality; t++) {
                            if (t <= 3) {
                                simpleColors.Add(rotate(simpleColors[actionCount + t - 1], tileSize));
                            }

                            if (t >= 4) {
                                simpleColors.Add(reflect(simpleColors[actionCount + t - 4], tileSize));
                            }

                            tileNames.Add($"{tileName} {t}");

                            curPattern++;
                        }

                        form.addPattern(bitmap, curPattern);
                    }

                    for (int t = 0; t < cardinality; t++) {
                        simpleWeights.Add(xTile.Get("weight", 1.0));
                    }
                }
            }

            inputHasChanged = false;
        }

        /*
         * Getters
         */

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public (object[], int) getImagePatternDimensions(string imageName) {
            IEnumerable<XElement> xElements = xDoc.Root.Elements("overlapping", "simpletiled");
            IEnumerable<int> matchingElements = xElements.Where(x =>
                x.Get<string>("name") == imageName).Select(t =>
                t.Get("N", 3));

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
                images = xDoc.Root.Elements(modelType).Select(xElement => xElement.Get<string>("name"))
                    .ToList();
            }

            images.Sort();

            return images.Distinct().ToArray();
        }

        public static Bitmap getImage(string name) {
            return new Bitmap($"samples/{name}.png");
        }

        public List<Color> getOverlapColors() {
            return overlappingColors;
        }

        public List<Color[]> getSimpleColors() {
            return simpleColors;
        }

        public Dictionary<long, int> getOverlappingWeights() {
            return weightsDictionary;
        }

        public List<double> getSimpleWeights() {
            return simpleWeights;
        }

        public int getTileSize() {
            return tileSize;
        }

        public List<int[]> getActions() {
            return simpleActions;
        }

        public Dictionary<string, int> getFirstOccurences() {
            return firstOccurrences;
        }

        public string getSimpleTileName(int index) {
            return tileNames[index];
        }

        public XElement getSimpleXRoot() {
            return xRoot;
        }

        public List<long> getOrdering() {
            return ordering;
        }

        private int getGroundPatternIdx() {
            return groundPatternIdx;
        }

        private bool transformationIsEnabled(int i) {
            PictureBox currentPB = form.pbs[i - 1];
            return currentPB.BackColor.Equals(Color.LawnGreen);
        }

        /*
         * Setters
         */

        public void setInputChanged() {
            inputHasChanged = true;
        }

        /*
         * Pattern Adaptation Overlapping
         */

        private byte[] patternFromSample(int x, int y) {
            currentBitmap = getImage(form.getSelectedInput());
            int inputWidth = currentBitmap.Width, inputHeight = currentBitmap.Height;
            return pattern((dx, dy) => sample[(x + dx) % inputWidth, (y + dy) % inputHeight]);
        }

        private byte[] pattern(Func<int, int, byte> f) {
            int overlapTileDimension = form.getSelectedOverlapTileDimension();
            byte[] result = new byte[overlapTileDimension * overlapTileDimension];
            for (int y = 0; y < overlapTileDimension; y++) {
                for (int x = 0; x < overlapTileDimension; x++) {
                    result[x + y * overlapTileDimension] = f(x, y);
                }
            }

            return result;
        }

        private byte[] rotate(IReadOnlyList<byte> inputPattern) {
            int overlapTileDimension = form.getSelectedOverlapTileDimension();
            return pattern((x, y) => inputPattern[overlapTileDimension - 1 - y + x * overlapTileDimension]);
        }

        private byte[] reflect(IReadOnlyList<byte> inputPattern) {
            int overlapTileDimension = form.getSelectedOverlapTileDimension();
            return pattern((x, y) => inputPattern[overlapTileDimension - 1 - x + y * overlapTileDimension]);
        }

        /*
         * Pattern Adaptation Simple
         */

        private Color[] tile(Func<int, int, Color> f, int tilesize) {
            Color[] result = new Color[tilesize * tilesize];
            for (int y = 0; y < tilesize; y++) {
                for (int x = 0; x < tilesize; x++) {
                    result[x + y * tilesize] = f(x, y);
                }
            }

            return result;
        }

        private Color[] rotate(IReadOnlyList<Color> array, int tilesize) {
            return tile((x, y) => array[tilesize - 1 - y + x * tilesize], tilesize);
        }

        private Color[] reflect(IReadOnlyList<Color> array, int tilesize) {
            return tile((x, y) => array[tilesize - 1 - x + y * tilesize], tilesize);
        }
    }
}