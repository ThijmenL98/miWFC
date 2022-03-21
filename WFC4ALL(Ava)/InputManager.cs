using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using WFC4ALL.ContentControls;
using WFC4All.DeBroglie;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using Image = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace WFC4All {
    public class InputManager {
        private readonly XDocument xDoc;
        private Bitmap currentBitmap;
        private int currentStep, tileSize;
        private bool inputHasChanged;
        private Dictionary<int, Tuple<Color[], Tile>> tileCache;

        private XElement xRoot;

        private TilePropagator dbPropagator;
        private TileModel dbModel;
        private ITopoArray<Tile> tiles;

        private Bitmap latestOutput;

        private readonly MainWindowViewModel mainWindowVM;
        private readonly MainWindow mainWindow;

        private readonly Avalonia.Media.Imaging.Bitmap noResultFoundBM;
        private readonly Stack<int> savePoints;

        private readonly InputControl inputControl;

        private DispatcherTimer timer = new();

        private bool _isChangingModels;

        public InputManager(MainWindowViewModel mainWindowVM, MainWindow mainWindow) {
            this.mainWindowVM = mainWindowVM;
            this.mainWindow = mainWindow;

            _isChangingModels = false;

            inputControl = mainWindow.getInputControl();

            noResultFoundBM = new Avalonia.Media.Imaging.Bitmap("Assets/NoResultFound.png");
            savePoints = new Stack<int>();
            savePoints.Push(0);

            tileSize = 0;
            tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
            currentStep = 0;
            xDoc = XDocument.Load("./Assets/samples.xml");
            currentBitmap = null;
            inputHasChanged = true;
            dbModel = null;
            dbPropagator = null;
            latestOutput = new Bitmap(1, 1);

            mainWindow.getInputControl().setCategories(getCategories("overlapping"));

            string[] inputImageDataSource = getImages("overlapping", "Textures"); // or "simpletiled"
            mainWindow.getInputControl().setInputImages(inputImageDataSource);

            (int[] patternSizeDataSource, int i) = getImagePatternDimensions(inputImageDataSource[0]);
            mainWindow.getInputControl().setPatternSizes(patternSizeDataSource, i);

            restartSolution();
            updateInstantCollapse(1);
        }

        /*
         * Functionality
         */

        public void showPopUp() {
            mainWindowVM.PopupVisible = true;
        }

        public void hidePopUp() {
            mainWindowVM.PopupVisible = false;
        }

        public bool popUpOpened() {
            return mainWindowVM.PopupVisible;
        }
        
        public void updateCategories(string[]? values, int idx = 0) {
            mainWindow.getInputControl().setCategories(values, idx);
        }

        public void updateInputImages(string[]? values, int idx = 0) {
            mainWindow.getInputControl().setInputImages(values, idx);
        }

        public void updatePatternSizes(int[]? values, int idx = 0) {
            mainWindow.getInputControl().setPatternSizes(values, idx);
        }

        public void setModelChanging(bool isChanging) {
            _isChangingModels = isChanging;
        }

        public void restartSolution() {
            if (_isChangingModels) {
                return;
            }

            try {
                int stepAmount = mainWindowVM.StepAmount;
                (Bitmap result2, bool _) = initAndRunWfcDB(true, stepAmount == 100 ? -1 : stepAmount);
                Avalonia.Media.Imaging.Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void advanceStep() {
            try {
                (Bitmap result2, bool finished) = initAndRunWfcDB(false, mainWindowVM.StepAmount);
                if (finished) {
                    return;
                }

                Avalonia.Media.Imaging.Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void revertStep() {
            try {
                Bitmap result2 = stepBackWfc(mainWindowVM.StepAmount);
                Avalonia.Media.Imaging.Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;

                int curStep = getCurrentStep();
                if (curStep < savePoints.Peek()) {
                    savePoints.Pop();
                }
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void placeMarker() {
            int curStep = getCurrentStep();
            while (true) {
                if (savePoints.Count != 0 && curStep < savePoints.Peek()) {
                    savePoints.Pop();
                } else {
                    savePoints.Push(curStep);
                    return;
                }
            }
        }

        public void loadMarker() {
            int prevTimePoint = savePoints.Count == 0 ? 0 : savePoints.Peek();
            if (getCurrentStep() == prevTimePoint && savePoints.Count != 0) {
                savePoints.Pop();
                prevTimePoint = savePoints.Count == 0 ? 0 : savePoints.Peek();
            }

            int stepsToRevert = getCurrentStep() - prevTimePoint;
            setCurrentStep(prevTimePoint);
            try {
                Bitmap result2 = stepBackWfc(stepsToRevert);
                Avalonia.Media.Imaging.Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public async void export() {
            SaveFileDialog sfd = new() {
                Title = @"Select export location & file name",
                DefaultExtension = "jpg",
                Filters = new List<FileDialogFilter> {
                    new() {
                        Extensions = new List<string> {"jpg"},
                        Name = "JPeg Image"
                    }
                }
            };

            string? settingsFileName = await sfd.ShowAsync(new Window());
            if (settingsFileName != null) {
                getLatestOutput().Save(settingsFileName, ImageFormat.Jpeg);
            }
        }

        public void animate() {
            if (isCollapsed()) {
                restartSolution();
            }

            bool startAnimation = mainWindowVM.IsPlaying;

            if (startAnimation) {
                lock (timer) {
                    timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                    timer.Tick += animationAnimationTimerTick!;
                    timer.Start();
                }
            } else {
                lock (timer) {
                    timer.Stop();
                    timer = new DispatcherTimer();
                }
            }
        }

        private void animationAnimationTimerTick(object sender, EventArgs e) {
            lock (timer) {
                /* only work when this is no reentry while we are already working */
                if (timer.IsEnabled) {
                    timer.Stop();
                    timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                    try {
                        (Bitmap result2, bool finished) = initAndRunWfcDB(false, mainWindowVM.StepAmount);
                        Avalonia.Media.Imaging.Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                        mainWindowVM.OutputImage = avaloniaBitmap;
                        if (finished) {
                            mainWindowVM.IsPlaying = false;
                            return;
                        }
                    } catch (Exception exception) {
#if (DEBUG)
                        Trace.WriteLine(exception);
#endif
                        mainWindowVM.OutputImage = noResultFoundBM;
                    }

                    timer.Start();
                }
            }
        }

        private Avalonia.Media.Imaging.Bitmap ConvertToAvaloniaBitmap(Image? bitmap) {
            if (bitmap == null) {
                return new Avalonia.Media.Imaging.Bitmap("");
            }

            Bitmap bitmapTmp = new(bitmap);
            BitmapData? bitmapData = bitmapTmp.LockBits(new Rectangle(0, 0, bitmapTmp.Width, bitmapTmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Avalonia.Media.Imaging.Bitmap bitmap1 = new(
                Avalonia.Platform.PixelFormat.Bgra8888, AlphaFormat.Premul,
                bitmapData.Scan0,
                new PixelSize(bitmapData.Width, bitmapData.Height),
                new Vector(96, 96),
                bitmapData.Stride);
            bitmapTmp.UnlockBits(bitmapData);
            bitmapTmp.Dispose();
            return bitmap1;
        }

        private (Bitmap, bool) initAndRunWfcDB(bool reset, int steps) {
            if (_isChangingModels) {
                return (new Bitmap(1, 1), true);
            }

            Stopwatch sw = new();
            sw.Restart();

            if (reset) {
                bool inputPaddingEnabled, outputPaddingEnabled;
                string category = inputControl.getCategory();
                string inputImage = inputControl.getInputImage();

                if (category.Equals("Textures")) {
                    inputPaddingEnabled = isOverlappingModel() && mainWindowVM.PaddingEnabled;
                    outputPaddingEnabled = inputPaddingEnabled;
                } else {
                    inputPaddingEnabled = category.Equals("Worlds Top-Down") || category.Equals("Worlds Side-View")
                                                                             || category.Equals("Font") ||
                                                                             category.Equals("Knots") &&
                                                                             !inputImage.Equals("Nested")
                                                                             && !inputImage.Equals("NotKnot");
                    outputPaddingEnabled = category.Equals("Worlds Side-View") || inputImage.Equals("Font")
                                                                               || category.Equals("Knots")
                                                                               && !inputImage.Equals("Nested");
                }

                if (inputHasChanged) {
                    // form.displayLoading(true);
                    currentBitmap = getImage(inputControl.getInputImage());
                    mainWindowVM.Tiles.Clear();
                    resetPatterns();

                    if (isOverlappingModel()) {
                        ITopoArray<Color> dbSample
                            = TopoArray.create(imageToColourArray(currentBitmap),
                                inputPaddingEnabled);
                        tiles = dbSample.toTiles();
                        dbModel = new OverlappingModel(inputControl.getPatternSize());

                        bool hasRotations = inputControl.getCategory().Equals("Worlds Top-Down")
                                            || category.Equals("Knots") || category.Equals("Knots") ||
                                            inputImage.Equals("Mazelike");
                        (List<PatternArray>? patternList, List<double>? patternWeights)
                            = ((OverlappingModel) dbModel).addSample(tiles,
                                new TileRotation(hasRotations ? 4 : 1, false));

                        for (int i = 0; i < patternList.Count; i++) {
                            addPattern(patternList[i], patternWeights[i]);
                        }
                    } else {
                        dbModel = new AdjacentModel();
                        xRoot = XDocument.Load($"samples/{inputImage}/data.xml").Root ?? new XElement("");

                        tileSize = int.Parse(xRoot.Attribute("size")?.Value ?? "16");

                        List<double> simpleWeights = new();

                        tileCache = new Dictionary<int, Tuple<Color[], Tile>>();

                        foreach (XElement xTile in xRoot.Element("tiles")?.Elements("tile")!) {
                            Bitmap bitmap = new($"samples/{inputImage}/{xTile.Attribute("name")?.Value}.png");
                            int cardinality = char.Parse(xTile.Attribute("symmetry")?.Value ?? "X") switch {
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

                            double tileWeight = double.Parse(xTile.Attribute("weight")?.Value ?? "1.0");
                            mainWindowVM.Tiles.Add(new TileViewModel(ConvertToAvaloniaBitmap(bitmap), tileWeight));

                            for (int t = 0; t < cardinality; t++) {
                                simpleWeights.Add(tileWeight);
                            }
                        }

                        for (int i = 0; i < tileCache.Count; i++) {
                            double refWeight = simpleWeights[i];
                            Tile refTile = tileCache.ElementAt(i).Value.Item2;
                            ((AdjacentModel) dbModel).setFrequency(refTile, refWeight);
                        }

                        int[][] values = new int[50][];

                        int j = 0;
                        foreach (XElement xTile in xRoot.Element("rows")?.Elements("row")!) {
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
                    Trace.WriteLine(@$"Init took {sw.ElapsedMilliseconds}ms.");
#endif
                    sw.Restart();
                }

                int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;

                GridTopology dbTopology = new(outputWidth, outputHeight, outputPaddingEnabled);
                int curSeed = Environment.TickCount;
                dbPropagator = new TilePropagator(dbModel, dbTopology, new TilePropagatorOptions {
                    BackTrackDepth = -1,
                    RandomDouble = new Random(curSeed).NextDouble,
                });
#if (DEBUG)
                Trace.WriteLine(@$"Assigning took {sw.ElapsedMilliseconds}ms.");
#endif
                sw.Restart();

                if (isOverlappingModel()) {
                    switch (inputImage.ToLower()) {
                        case "flowers": {
                            // Set the bottom last 2 rows to be the ground tile
                            dbPropagator.@select(0, outputHeight - 1, 0,
                                new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                            dbPropagator.@select(0, outputHeight - 2, 0,
                                new Tile(currentColors.ElementAt(currentColors.Count - 1)));

                            // And ban it elsewhere
                            for (int y = 0; y < outputHeight - 2; y++) {
                                dbPropagator?.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                            }

                            break;
                        }
                        case "skyline": {
                            // Set the bottom last row to be the ground tile
                            dbPropagator.@select(0, outputHeight - 1, 0,
                                new Tile(currentColors.ElementAt(currentColors.Count - 1)));

                            // And ban it elsewhere
                            for (int y = 0; y < outputHeight - 1; y++) {
                                dbPropagator.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                            }

                            break;
                        }
                    }
                }

                inputHasChanged = false;
            }

            //TODO
            //form.displayLoading(false);

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
            Trace.WriteLine(@$"Stepping forward took {sw.ElapsedMilliseconds}ms.");
#endif
            sw.Restart();

            Bitmap outputBitmap;
            int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;

            if (isOverlappingModel()) {
                outputBitmap = new Bitmap(outputWidth, outputHeight);
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < outputHeight; y++) {
                    for (int x = 0; x < outputWidth; x++) {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            } else {
                outputBitmap = new Bitmap(outputWidth * tileSize, outputHeight * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < outputHeight; y++) {
                    for (int x = 0; x < outputWidth; x++) {
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
            Trace.WriteLine(@$"Bitmap took {sw.ElapsedMilliseconds}ms. {dbStatus}");
#endif
            return (outputBitmap, dbStatus == Resolution.DECIDED);
        }

        private Bitmap stepBackWfc(int steps) {
            Stopwatch sw = new();
            sw.Restart();
            for (int i = 0; i < steps; i++) {
                dbPropagator.doBacktrack();
            }

#if (DEBUG)
            Trace.WriteLine(@$"Stepping back took {sw.ElapsedMilliseconds}ms.");
#endif
            sw.Restart();

            Bitmap outputBitmap;
            int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;

            if (isOverlappingModel()) {
                outputBitmap = new Bitmap(outputWidth, outputHeight);
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < outputHeight; y++) {
                    for (int x = 0; x < outputWidth; x++) {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            } else {
                outputBitmap = new Bitmap(outputWidth * tileSize, outputHeight * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < outputHeight; y++) {
                    for (int x = 0; x < outputWidth; x++) {
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
            Trace.WriteLine(@$"Bitmap took {sw.ElapsedMilliseconds}ms.");
#endif

            return outputBitmap;
        }

        private HashSet<ImageR> curBitmaps = new();
        private Dictionary<int, List<Bitmap>> similarityMap = new();
        private int patternCount;

        private void resetPatterns() {
            similarityMap = new Dictionary<int, List<Bitmap>>();
            curBitmaps = new HashSet<ImageR>();
            patternCount = 0;
        }

        private void addPattern(PatternArray colors, double weight) {
            int n = colors.Height;
            Bitmap pattern = new(n, n);

            Dictionary<Point, Color> data = new();

            for (int x = 0; x < n; x++) {
                for (int y = 0; y < n; y++) {
                    Color tileColor = (Color) colors.getTileAt(x, y).Value;
                    pattern.SetPixel(x, y, tileColor);
                    data[new Point(x, y)] = tileColor;
                }
            }

            ImageR cur = new(pattern.Size, data);

            foreach ((ImageR reference, int i) in curBitmaps.Select((reference, i) => (reference, i))) {
                if (transforms.Select(transform => cur.Data.All(x =>
                        x.Value == reference.Data[
                            transform.Invoke(cur.Size.Width - 1, cur.Size.Height - 1, x.Key.X, x.Key.Y)]))
                    .Any(match => match)) {
                    similarityMap[i].Add(pattern);
                    return;
                }
            }

            curBitmaps.Add(cur);
            similarityMap[patternCount] = new List<Bitmap> {pattern};

            Avalonia.Media.Imaging.Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(pattern);
            mainWindowVM.Tiles.Add(new TileViewModel(avaloniaBitmap, weight));

            patternCount++;
        }

        private readonly List<Func<int, int, int, int, Point>> transforms = new() {
            (_, _, x, y) => new Point(x, y), // rotated 0
            (w, _, x, y) => new Point(w - y, x), // rotated 90
            (w, h, x, y) => new Point(w - x, h - y), // rotated 180
            (_, h, x, y) => new Point(y, h - x), // rotated 270
            (w, _, x, y) => new Point(w - x, y), // rotated 0 and mirrored
            (w, _, x, y) => new Point(w - (w - y), x), // rotated 90 and mirrored
            (w, h, x, y) => new Point(w - (w - x), h - y), // rotated 180 and mirrored
            (w, h, x, y) => new Point(w - y, h - x), // rotated 270 and mirrored
        };

        private record ImageR(Size Size, Dictionary<Point, Color> Data) {
            public Size Size { get; } = Size;
            public Dictionary<Point, Color> Data { get; } = Data;
        }

        public void updateInstantCollapse(int newValue) {
            bool ic = newValue == 100;
            mainWindowVM.StepAmountString = ic ? "Instantly generate output" : "Steps to take: " + newValue;
            mainWindowVM.InstantCollapse = !ic;
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

        /*
         * Getters
         */

        public bool isChangingModels() {
            return _isChangingModels;
        }

        private int getCurrentStep() {
            return currentStep;
        }

        private void setCurrentStep(int newCurStep) {
            currentStep = newCurStep;
        }

        private Bitmap getLatestOutput() {
            return latestOutput;
        }

        private bool isCollapsed() {
            return dbPropagator.Status == Resolution.DECIDED;
        }

        private bool isOverlappingModel() {
            return mainWindowVM.ModelSelectionText.Contains("Tile");
        }

        public InputControl getIC() {
            return inputControl;
        }

        public Bitmap setTile(int a, int b, int toSet) {
            dbPropagator.select(a, b, 0,
                new Tile(currentColors.ElementAt(toSet)));

            Bitmap outputBitmap;
            int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

            if (isOverlappingModel()) {
                outputBitmap = new Bitmap(outputWidth, outputHeight);
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < outputHeight; y++) {
                    for (int x = 0; x < outputWidth; x++) {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            } else {
                outputBitmap = new Bitmap(outputWidth * tileSize, outputHeight * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < outputHeight; y++) {
                    for (int x = 0; x < outputWidth; x++) {
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

            return outputBitmap;
        }

        // ReSharper disable PossibleMultipleEnumeration
        public (int[], int) getImagePatternDimensions(string imageName) {
            if (xDoc.Root == null) {
                return (Array.Empty<int>(), -1);
            }

            IEnumerable<XElement> xElements
                = xDoc.Root.Elements("simpletiled").Concat(xDoc.Root.Elements("overlapping"));
            IEnumerable<int> matchingElements = xElements.Where(x =>
                (x.Attribute("name")?.Value ?? "") == imageName).Select(t =>
                int.Parse(t.Attribute("patternSize")?.Value ?? "3"));

            List<int> patternDimensionsList = new();
            int j = 0;
            for (int i = 2; i < 6; i++) {
                if (i >= 4 && !matchingElements.Contains(5) && !matchingElements.Contains(4)) {
                    break;
                }

                patternDimensionsList.Add(i);
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
                    .Where(xElement => (xElement.Attribute("category")?.Value ?? "").Equals(category))
                    .Select(xElement => xElement.Attribute("name")?.Value ?? "").ToList();
            }

            images.Sort();

            return images.Distinct().ToArray();
        }

        private static Bitmap getImage(string name) {
            return new Bitmap($"samples/{name}.png");
        }

        public void updateInputImage(string newImage) {
            // ReSharper disable once InconsistentNaming
            string URI = $"samples/{newImage}.png";
            mainWindowVM.InputImage = new Avalonia.Media.Imaging.Bitmap(URI);
        }

        /*
         * Setters
         */

        public void setInputChanged(string source) {
            if (_isChangingModels) {
                return;
            }
#if (DEBUG)
            Trace.WriteLine(@$"Input changed on {source}");
#endif
            inputHasChanged = true;
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

        public void processClick(int clickX, int clickY, int imgWidth, int imgHeight) {
            int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
                b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);
#if (DEBUG)
            Trace.WriteLine($@"(x:{clickX}, y:{clickY}) -> (a:{a}, b:{b})");
#endif
            //TODO CF2

            // Bitmap result2 = inputManager.setTile(a, b, 3);
            //
            // resultPB.Image = inputManager.resizeBitmap(result2,
            //     Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result2.Width));
        }
    }
}