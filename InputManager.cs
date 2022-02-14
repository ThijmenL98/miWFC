using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private List<Color> colors;
        private byte[,] sample;
        private Dictionary<long, int> weightsDictionary;
        private List<long> ordering;
        private int groundPatternIdx;
        private readonly Form1 form;

        private bool inputHasChanged;

        public InputManager(Form1 formIn) {
            form = formIn;
            xDoc = XDocument.Load("samples.xml");
            currentBitmap = null;
            colors = new List<Color>();
            weightsDictionary = new Dictionary<long, int>();
            ordering = new List<long>();
            inputHasChanged = true;
            groundPatternIdx = 0;
        }

        private int lastDim = 0;

        public Bitmap runWfc() {
            Stopwatch sw = Stopwatch.StartNew();

            Random random = new Random();

            XElement xElem = xDoc.Root.Elements("overlapping", "simpletiled")
                .Where(x => x.Get<string>("name") == form.getSelectedInput()).ElementAtOrDefault(0);

            if ((xElem != null && xElem.Name == "overlapping") || lastDim != form.getSelectedOverlapTileDimension()) {
                extractPatterns(lastDim != form.getSelectedOverlapTileDimension(),
                    xElem != null && xElem.Name == "overlapping");
                lastDim = form.getSelectedOverlapTileDimension();
                Console.WriteLine($"Pattern Extraction = {sw.ElapsedMilliseconds}ms.");
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
                model = new SimpleTiledModel(name, form.getOutputWidth(), form.getOutputHeight(),
                    form.getPeriodicEnabled(), heuristic, form);
            }

            while (sw.ElapsedMilliseconds < 3000) {
                int seed = random.Next();
                bool success = model.Run(seed, -1); //Setting Limit to e.g. x, causes it to stop after adding x tiles

                if (success) {
                    Console.WriteLine($"Algorithm = {sw.ElapsedMilliseconds}ms.");
                    return model.Graphics();
                }
            }

            Console.WriteLine($"Algorithm = {sw.ElapsedMilliseconds}ms.");
            return null;
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

        private static string currentImage = "";

        public static Bitmap getImage(string name) {
            currentImage = name;
            return new Bitmap($"samples/{name}.png");
        }

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
                for (int j = 0; j < w2 - padding; j++) {
                    int px = (int) Math.Floor(j * xRatio);
                    int py = (int) Math.Floor(i * yRatio);
                    outputBM.SetPixel(j + marginX - padding, i + marginY - padding, bitmap.GetPixel(px, py));
                }
            }

            return outputBM;
        }

        public static Bitmap resizeBitmap(Bitmap source, float scale) {
            // Figure out the new size.
            int width = (int) (source.Width * scale);
            int height = (int) (source.Height * scale);

            // Create the new bitmap.
            // Note that Bitmap has a resize constructor, but you can't control the quality.
            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.Clear(Color.DarkGray);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(source, new Rectangle(0, 0, width, height));
                g.Save();
            }

            return bmp;
        }

        public List<Color> getCurrentColors() {
            return colors;
        }

        public Dictionary<long, int> getWeightsDictionary() {
            return weightsDictionary;
        }

        public List<long> getOrdering() {
            return ordering;
        }

        private int getGroundPatternIdx() {
            return groundPatternIdx;
        }

        public void setInputChanged() {
            inputHasChanged = true;
        }

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

        private bool symmetryIsEnabled(int i) {
            PictureBox currentPB = form.pbs[i - 1];
            return currentPB.BackColor.Equals(Color.LawnGreen);
        }

        private byte[] rotate(IReadOnlyList<byte> inputPattern) {
            int overlapTileDimension = form.getSelectedOverlapTileDimension();
            return pattern((x, y) => inputPattern[overlapTileDimension - 1 - y + x * overlapTileDimension]);
        }

        private byte[] reflect(IReadOnlyList<byte> inputPattern) {
            int overlapTileDimension = form.getSelectedOverlapTileDimension();
            return pattern((x, y) => inputPattern[overlapTileDimension - 1 - x + y * overlapTileDimension]);
        }

        private String curFile = "";

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

                colors = new List<Color>();

                for (int y = 0; y < inputHeight; y++) {
                    for (int x = 0; x < inputWidth; x++) {
                        Color color = currentBitmap.GetPixel(x, y);

                        int colorIndex = colors.TakeWhile(c => c != color).Count();

                        if (colorIndex == colors.Count) {
                            colors.Add(color);
                        }

                        sample[x, y] = (byte) colorIndex;
                    }
                }

                int colorsCount = colors.Count;

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
                            if (i == 0 || symmetryIsEnabled(i)) {
                                long idx = index(patternSymmetry[i]);
                                if (weightsDictionary.ContainsKey(idx)) {
                                    weightsDictionary[idx]++;
                                } else {
                                    weightsDictionary.Add(idx, 1);
                                    ordering.Add(idx);

                                    //TODO check if pattern is similar to others (flipped)
                                    Thread.CurrentThread.IsBackground = true;
                                    form.addPattern(patternSymmetry[0], colors, overlapTileDimension,
                                        weightsDictionary.Count - 1, i == 0 && fileChanged);
                                }
                            }
                        }
                    }
                }

                groundPatternIdx = form.bitMaps.getFloorIndex(weightsDictionary.Count);
            } else {
                //TODO
                
            }

            inputHasChanged = false;
        }
    }
}