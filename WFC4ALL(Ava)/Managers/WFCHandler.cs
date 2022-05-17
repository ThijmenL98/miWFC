#if DEBUG
using System.Diagnostics;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WFC4ALL.DeBroglie;
using WFC4ALL.DeBroglie.Models;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using static WFC4ALL.Utils.Util;

namespace WFC4ALL.Managers;

public class WFCHandler {
    private static HashSet<Color>? currentColors;
    private readonly MainWindow mainWindow;

    private readonly MainWindowViewModel mainWindowVM;
    private readonly CentralManager parentCM;

    private bool _isChangingModels, _isChangingImages, inputHasChanged;
    private int amountCollapsed, actionsTaken, tileSize;
    private double percentageCollapsed;

    private WriteableBitmap? currentBitmap;
    private TileModel dbModel;

    private TilePropagator dbPropagator;

    private WriteableBitmap latestOutput;
    private Dictionary<int, Tuple<Color[], Tile>> tileCache;
    private ITopoArray<Tile> tiles;

    private Dictionary<int, int[]> tileSymmetries;

    private List<TileViewModel> toAddPaint;
    private XElement xRoot;

    private List<double> originalWeights;

#pragma warning disable CS8618
    public WFCHandler(CentralManager parent) {
#pragma warning restore CS8618
        parentCM = parent;
        mainWindowVM = parentCM.getMainWindowVM();
        mainWindow = parentCM.getMainWindow();

        initialize();
    }

    private void initialize() {
        _isChangingModels = false;
        _isChangingImages = false;
        inputHasChanged = true;
        latestOutput
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul);
        currentBitmap = null;
        currentColors = new HashSet<Color>();
        tileSize = 0;
        toAddPaint = new List<TileViewModel>();
        tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
        tileSymmetries = new Dictionary<int, int[]>();

        percentageCollapsed = 0d;
        amountCollapsed = 0;
        actionsTaken = 0;
    }

    /*
     * Algorithm Blocker Getters & Setters
     */

    public void setImageChanging(bool isChanging) {
        _isChangingImages = isChanging;
    }

    public void setModelChanging(bool isChanging) {
        _isChangingModels = isChanging;
    }

    public bool isChangingModels() {
        return _isChangingModels;
    }

    public bool isChangingImages() {
        return _isChangingImages;
    }

    public void setInputChanged(string source) {
        if (isChangingModels()) {
            return;
        }

        inputHasChanged = true;
    }

    /*
     * Direct WFC Manipulation
     */

    public async Task<(WriteableBitmap, bool)> initAndRunWfcDB(bool reset, int steps, bool force = false) {
        if (isChangingModels() || !mainWindow.IsActive && !force) {
            return (new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul),
                true);
        }

        mainWindowVM.setLoading(true);

        if (reset) {
            string inputImage = mainWindow.getInputControl().getInputImage();
            string category = mainWindow.getInputControl().getCategory();
            bool inputWrappingEnabled = mainWindowVM.InputWrapping || category.Contains("Side");

            if (inputHasChanged) {
                initializeLocalValues();

                List<TileViewModel>? toAdd = null;
                int overlappingDimension = mainWindow.getInputControl().getPatternSize();

                await Task.Run(() => {
                    toAdd = isOverlappingModel()
                        ? initializeOverlappingModel(category, inputImage, overlappingDimension,
                            inputWrappingEnabled)
                        : initializeAdjacentModel(inputImage);
                });

                mainWindowVM.PatternTiles = new ObservableCollection<TileViewModel>(toAdd!);

                parentCM.getPaintingWindow().setPaintingPatterns(toAddPaint.ToArray());
            }

            int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;
            bool seamlessOutput = mainWindowVM.SeamlessOutput || category.Contains("Side");

            await Task.Run(() => {
                createPropagator(outputWidth, outputHeight, seamlessOutput);

                if (isOverlappingModel() && inputWrappingEnabled) {
                    handleSideViewInit(outputHeight, inputImage);
                }
            });

            inputHasChanged = false;
        }

        mainWindowVM.setLoading(false);

        (latestOutput, bool decided) = runWfcDB(steps);
        return (latestOutput, decided);
    }

    private void initializeLocalValues() {
        currentBitmap = getImageFromURI(mainWindow.getInputControl().getInputImage());
        mainWindowVM.PatternTiles.Clear();
        mainWindowVM.PaintTiles.Clear();
        toAddPaint = new List<TileViewModel>();

        parentCM.getInputManager().resetOverwriteCache();
        parentCM.getUIManager().resetPatterns();
    }

    private void createPropagator(int outputWidth, int outputHeight, bool seamlessOutput) {
        bool outputPaddingEnabled = isOverlappingModel() && seamlessOutput;

        GridTopology dbTopology = new(outputWidth, outputHeight, outputPaddingEnabled);
        int curSeed = Environment.TickCount;
        dbPropagator = new TilePropagator(dbModel, dbTopology, new TilePropagatorOptions {
            RandomDouble = new Random(curSeed).NextDouble,
            IndexPickerType = IndexPickerType.HEAP_MIN_ENTROPY
        });
    }

    private void handleSideViewInit(int outputHeight, string inputImage) {
        switch (inputImage.ToLower()) {
            case "flowers": {
                // Set the bottom last 2 rows to be the ground tile
                dbPropagator.@select(0, outputHeight - 1, 0,
                    new Tile(currentColors!.ElementAt(currentColors!.Count - 1)));
                dbPropagator.select(0, outputHeight - 2, 0,
                    new Tile(currentColors.ElementAt(currentColors.Count - 1)));

                // And ban it elsewhere
                for (int y = 0; y < outputHeight - 2; y++) {
                    dbPropagator?.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                }

                break;
            }
            case "skyline": {
                // Set the bottom last row to be the ground tile
                dbPropagator.select(0, outputHeight - 1, 0,
                    new Tile(currentColors!.ElementAt(currentColors!.Count - 1)));

                // And ban it elsewhere
                for (int y = 0; y < outputHeight - 1; y++) {
                    dbPropagator.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                }

                break;
            }
        }
    }

    public void updateWeights() {
        if (isOverlappingModel()) {
            // double[] userWeights = new double[tileCache.Count];
            // foreach (TileViewModel tvm in parentCM.getMainWindowVM().PatternTiles) {
            //     userWeights[tvm.PatternIndex] = tvm.PatternWeight;
            // }
            // TODO properly get the toSet from the pattern TVM
            // foreach ((double weight, int idx) in userWeights.ToList().Select((w, i) => (w, i))) {
            //     Tile toSet = tiles.toArray2d().Cast<Tile>().ToArray()[idx];
            //     Trace.WriteLine($@"Pre: {((OverlappingModel) dbModel).getFrequency(toSet)} -> {weight}");
            //     ((OverlappingModel) dbModel).setFrequency(toSet, weight);
            //     Trace.WriteLine($@"Post: {((OverlappingModel) dbModel).getFrequency(toSet)}");
            // }
        } else {
            List<double> userWeights
                = parentCM.getMainWindowVM().PatternTiles.Select(tvm => tvm.PatternWeight).ToList();
            foreach (((int parent, int[] symmetries), int idx) in tileSymmetries.Select((pair, idx) => (pair, idx))) {
                double weight = userWeights[idx];
                weight = weight == 0 ? 0.00000000001d : weight;
                ((AdjacentModel) dbModel).setFrequency(tileCache[parent].Item2, weight);
                if (weight == 0) {
#if DEBUG
                    Trace.WriteLine("Zero weight found");
#endif
                    dbPropagator.updateZeroWeight(parent);
                }

                foreach (int symmetryIdx in symmetries) {
                    ((AdjacentModel) dbModel).setFrequency(tileCache[symmetryIdx].Item2, weight);
                    if (weight == 0) {
                        dbPropagator.updateZeroWeight(symmetryIdx);
                    }
                }
            }
        }
    }

    private List<TileViewModel> initializeOverlappingModel(string category, string inputImage, int patternSize,
        bool inputPaddingEnabled) {
        List<TileViewModel> toAdd = new();

        (Color[][] colorArray, currentColors) = imageToColourArray(currentBitmap!);
        ITopoArray<Color> dbSample = TopoArray.create(colorArray, inputPaddingEnabled);
        tiles = dbSample.toTiles();
        dbModel = new OverlappingModel(patternSize);
        bool hasRotations = (category.Equals("Worlds Top-Down")
                             || category.Equals("Knots") || category.Equals("Knots") ||
                             inputImage.Equals("Mazelike")) && !inputImage.Equals("Village");

        (List<PatternArray>? patternList, List<double>? patternWeights)
            = ((OverlappingModel) dbModel).addSample(tiles,
                new TileRotation(hasRotations ? 4 : 1, false));

        originalWeights = patternWeights;

        toAdd.AddRange(patternList.Select((t, i) => parentCM.getUIManager()
                .addPattern(t, patternWeights[i], tileSymmetries, i))
            .Where(nextTVM => nextTVM != null)!);

        foreach ((Tile t, int index) in tiles.toArray2d().Cast<Tile>().Distinct()
                     .Select((tile, idx) => (tile, idx))) {
            Color c = (Color) t.Value;

            WriteableBitmap pattern = new(new PixelSize(1, 1), new Vector(96, 96),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? PixelFormat.Bgra8888
                    : PixelFormat.Rgba8888,
                AlphaFormat.Premul);

            using ILockedFramebuffer? frameBuffer = pattern.Lock();

            unsafe {
                ((uint*) frameBuffer.Address.ToPointer())[0]
                    = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);
            }

            toAddPaint.Add(new TileViewModel(pattern, index, c));
        }

        return toAdd;
    }

    private List<TileViewModel> initializeAdjacentModel(string inputImage) {
        List<TileViewModel> toAdd = new();
        dbModel = new AdjacentModel();
        List<double> weights = new();

        xRoot = XDocument.Load($"{AppContext.BaseDirectory}/samples/{inputImage}/data.xml").Root ??
                new XElement("");

        tileSize = int.Parse(xRoot.Attribute("size")?.Value ?? "16", CultureInfo.InvariantCulture);

        tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
        tileSymmetries = new Dictionary<int, int[]>();

        foreach (XElement xTile in xRoot.Element("tiles")?.Elements("tile")!) {
            MemoryStream ms = new(File.ReadAllBytes(
                $"{AppContext.BaseDirectory}/samples/{inputImage}/{xTile.Attribute("name")?.Value}.png"));
            WriteableBitmap writeableBitmap = WriteableBitmap.Decode(ms);

            int cardinality = char.Parse(xTile.Attribute("symmetry")?.Value ?? "X") switch {
                'L' => 4,
                'T' => 4,
                'I' => 2,
                '\\' => 2,
                'F' => 8,
                _ => 1
            };

            Color[] cur = extractColours(writeableBitmap);
            int val = tileCache.Count;
            Tile curTile = new(val);
            tileCache.Add(val, new Tuple<Color[], Tile>(cur, curTile));

            List<int> symmetries = new();

            double tileWeight
                = double.Parse(xTile.Attribute("weight")?.Value ?? "1.0", CultureInfo.InvariantCulture);
            weights.Add(tileWeight);

            for (int t = 1; t < cardinality; t++) {
                int myIdx = tileCache.Count;
                Color[] curCard = t <= 3
                    ? rotate(tileCache[val + t - 1].Item1.ToArray(), tileSize)
                    : reflect(tileCache[val + t - 4].Item1.ToArray(), tileSize);

                int rotation, shouldFlip = 1;
                if (t <= 3) {
                    rotation = -90 * t;
                } else {
                    rotation = -90 * (t - 4);
                    shouldFlip = -1;
                    if (t == 4) {
                        shouldFlip = 1;
                    }
                }

                weights.Add(tileWeight);

                toAddPaint.Add(
                    new TileViewModel(writeableBitmap, tileWeight, tileCache.Count - 1, rotation, shouldFlip));
                tileCache.Add(myIdx, new Tuple<Color[], Tile>(curCard, new Tile(myIdx)));

                symmetries.Add(myIdx);
            }

            tileSymmetries.Add(val, symmetries.ToArray());

            TileViewModel tvm = new(writeableBitmap, tileWeight, tileCache.Count - 1, val, cardinality > 4);
            toAdd.Add(tvm);
            toAddPaint.Add(tvm);
        }

        const int sampleDimension = 50;
        int[][] values = new int[sampleDimension][];

        int j = 0;
        foreach (XElement xTile in xRoot.Element("rows")?.Elements("row")!) {
            string[] row = xTile.Value.Split(',');
            values[j] = new int[sampleDimension];
            for (int k = 0; k < sampleDimension; k++) {
                values[j][k] = int.Parse(row[k], CultureInfo.InvariantCulture);
            }

            j++;
        }

        ITopoArray<int> sample = TopoArray.create(values, false);
        dbModel = new AdjacentModel(sample.toTiles());

        for (int i = 0; i < tileCache.Count; i++) {
            Tile refTile = tileCache.ElementAt(i).Value.Item2;
            ((AdjacentModel) dbModel).setFrequency(refTile, weights[i]);
        }

        originalWeights = weights;

        return toAdd;
    }

    private (WriteableBitmap, bool) runWfcDB(int steps = 1) {
        Resolution dbStatus = Resolution.UNDECIDED;

        if (steps == -1) {
            dbStatus = dbPropagator.run();
        } else {
            for (int i = 0; i < steps; i++) {
                dbStatus = dbPropagator.step();
                actionsTaken++;
            }
        }

        return (getLatestOutputBM(), dbStatus == Resolution.DECIDED);
    }

    public WriteableBitmap stepBackWfc(int steps = 1) {
        for (int i = 0; i < steps; i++) {
            dbPropagator.doBacktrack();
            actionsTaken--;
        }

        return getLatestOutputBM();
    }

    public Color getColorFromIndex(int index) {
        return toAddPaint[index].PatternColour;
    }

    public async void handlePaintBrush(Color[,] colors) {
        int imageWidth = mainWindowVM.ImageOutWidth, imageHeight = mainWindowVM.ImageOutHeight;
        if (isOverlappingModel()) {
            ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
            Dictionary<Tuple<int, int>, Color> newInputDict = new();

            for (int y = 0; y < imageHeight; y++) {
                for (int x = 0; x < imageWidth; x++) {
                    if (colors[x, y].Equals(Colors.Green)) {
                        Color c = dbOutput.get(x, y);
                        int selectedIndex = dbPropagator.Topology.GetIndex(x, y, 0);
                        ISet<Color> possibleTiles = dbPropagator.GetPossibleValues<Color>(selectedIndex);

                        Color? toSet = possibleTiles.Count == 1 ? possibleTiles.First() :
                            currentColors!.Contains(c) ? c : null;

                        if (toSet != null) {
                            newInputDict.Add(new Tuple<int, int>(x, y), (Color) toSet);
                        }
                    }
                }
            }

            parentCM.getInputManager().restartSolution("Paint Brush", true);
            int oldInputDictSize = newInputDict.Count;
            Trace.WriteLine(oldInputDictSize);

            await Task.Delay(1);

            while (newInputDict.Count > 0) {
                foreach ((Tuple<int, int> key, Color toSet) in new Dictionary<Tuple<int, int>, Color>(newInputDict)) {
                    int idx = -1;
                    foreach (TileViewModel tvm in
                             toAddPaint.Where(tvm => tvm.PatternColour.Equals(toSet))) {
                        idx = tvm.PatternIndex;
                        break;
                    }

                    bool? success = parentCM?.getInputManager()
                        .processClick(key.Item1, key.Item2, imageWidth, imageHeight, idx, true);

                    if (success != null && (bool) success) {
                        newInputDict.Remove(key);
                    }
                }

                if (oldInputDictSize.Equals(newInputDict.Count)) {
#if DEBUG
                    Trace.WriteLine("");
                    Trace.WriteLine("AYOAYOAYO");
                    Trace.WriteLine("");
#endif
                    break;
                }

                oldInputDictSize = newInputDict.Count;
            }
        } else {
            ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
            Dictionary<Tuple<int, int>, int> newInputDict = new();

            for (int y = 0; y < imageHeight; y++) {
                for (int x = 0; x < imageWidth; x++) {
                    int value = dbOutput.get(x, y);
                    bool isCollapsed = value >= 0;

                    if (isCollapsed && colors[x, y].Equals(Colors.Green)) {
                        newInputDict.Add(new Tuple<int, int>(x, y), value);
                    }
                }
            }

            parentCM.getInputManager().restartSolution("Paint Brush", true);
            int oldInputDictSize = newInputDict.Count;

            await Task.Delay(1);

            while (newInputDict.Count > 0) {
                foreach ((Tuple<int, int> key, int value) in new Dictionary<Tuple<int, int>, int>(newInputDict)) {
                    bool? success = parentCM?.getInputManager()
                        .processClick(key.Item1, key.Item2, imageWidth, imageHeight, value, true);

                    if (success != null && (bool) success) {
                        newInputDict.Remove(key);
                    }
                }

                if (oldInputDictSize.Equals(newInputDict.Count)) {
#if DEBUG
                    Trace.WriteLine("");
                    Trace.WriteLine("AYOAYOAYO");
                    Trace.WriteLine("");
#endif
                    break;
                }

                oldInputDictSize = newInputDict.Count;
            }
        }

        mainWindowVM.OutputImage = getLatestOutputBM();
        parentCM!.getInputManager().placeMarker(false);
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
#pragma warning disable CS0162
    public (WriteableBitmap?, bool?) setTile(int a, int b, int toSet, bool returnTrueAlreadyCorrect = false) {
        Resolution status;
#if DEBUG
        const bool internalDebug = false;
        if (internalDebug) {
            Trace.WriteLine("");
        }
#endif

        if (isOverlappingModel()) {
            #region Overlapping Tile Selection

            // ReSharper disable twice NotAccessedVariable
            int px, py;
            try {
                dbPropagator.TileCoordToPatternCoord(a, b, 0, out px, out py, out int _, out int _);
            } catch (IndexOutOfRangeException) {
                // Click was continued beyond the size of the image
                return (null, false);
            }

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine($@"Overlapping: We want to paint at ({a}, {b}) (({px}, {py})) with Tile {toSet}");
            }
#endif

            List<Color> possibleTiles = getAvailablePatternsAtLocation<Color>(a, b);

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine($@"Available colours: {string.Join(", ", possibleTiles)}");
            }
#endif

            Color c = toAddPaint[toSet].PatternColour;

            if (possibleTiles.Count == 1 || !possibleTiles.Contains(c)) {
#if DEBUG
                if (internalDebug) {
                    Trace.WriteLine(possibleTiles.Count == 1
                        ? "Returning because already collapsed"
                        : "Returning because not allowed");
                }
#endif

                if (possibleTiles.Count == 1 && possibleTiles.Contains(c)) {
                    return (null, returnTrueAlreadyCorrect ? true : null);
                }

                return (null, false);
            }

            IEnumerable<Tile> tilesToSelect = tiles.toArray2d().Cast<Tile>()
                .Where(tile => ((Color) tile.Value).Equals(c));

            dbPropagator.AddBackTrackPoint();
            status = dbPropagator.select(a, b, 0, tilesToSelect);

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine($@"Proceeded with status: {status}");
            }
#endif

            if (status.Equals(Resolution.CONTRADICTION)) {
#if DEBUG
                Trace.WriteLine($@"Overlapping: We want to paint at ({a}, {b}) (({px}, {py})) with Tile {toSet}");
                Trace.WriteLine($@"Available patterns: {string.Join(", ", tilesToSelect)}");
                Trace.WriteLine("");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("");
#endif
                stepBackWfc();
                return (null, false);
            }

            return (getLatestOutputBM(), true);

            #endregion

            // ReSharper disable once RedundantIfElseBlock
        } else {
            #region Adjacent Tile Selection

            int descrambledIndex = getDescrambledIndex(toSet);

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine(
                    $@"Adjacent: We want to paint at ({a}, {b}) with Tile Idx:{toSet} Descrambled:{descrambledIndex}");
            }
#endif

            List<int> availableAtLoc = getAvailablePatternsAtLocation<int>(a, b);

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine($@"Available patterns: {string.Join(", ", availableAtLoc)}");
            }
#endif

            if (availableAtLoc.Count == 1 || !availableAtLoc.Contains(descrambledIndex)) {
#if DEBUG
                if (internalDebug) {
                    Trace.WriteLine(availableAtLoc.Count == 1
                        ? "Returning because already collapsed"
                        : "Returning because not allowed");
                }
#endif

                bool hasItself = availableAtLoc.Count == 1 && availableAtLoc.Contains(descrambledIndex);
                return (null, returnTrueAlreadyCorrect && hasItself);
            }

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine("Painting is allowed, continuing");
            }
#endif

            status = dbPropagator.selWith(a, b, descrambledIndex);

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine($@"Proceeded with status: {status}");
            }
#endif

            if (status.Equals(Resolution.CONTRADICTION)) {
#if DEBUG
                Trace.WriteLine(
                    $@"Adjacent: We want to paint at ({a}, {b}) with Tile Idx:{toSet} Descrambled:{descrambledIndex}");
                Trace.WriteLine($@"Available patterns: {string.Join(", ", availableAtLoc)}");
                Trace.WriteLine("");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("CONTRADICTION");
                Trace.WriteLine("");
#endif
                stepBackWfc();

                return (null, false);
            }

            return (getLatestOutputBM(), true);

            #endregion
        }
    }
#pragma warning restore CS0162

    public int getDescrambledIndex(int raw) {
        return dbPropagator.getTMM().GetPatterns(tileCache[raw].Item2, 0).First();
    }

    public List<T> getAvailablePatternsAtLocation<T>(int a, int b) {
        List<T> availableAtLoc;
        if (isOverlappingModel()) {
            int selectedIndex = dbPropagator.Topology.GetIndex(a, b, 0);
            availableAtLoc = dbPropagator.GetPossibleValues<T>(selectedIndex).ToList();
        } else {
            List<int> tempList = new();
            dbPropagator.TileCoordToPatternCoord(a, b, 0, out int px, out int py, out int pz, out int _);
            for (int pattern = 0; pattern < dbPropagator.getWP().PatternCount; pattern++) {
                if (dbPropagator.getWP().Wave.Get(dbPropagator.Topology.GetIndex(px, py, pz), pattern)) {
                    tempList.Add(pattern);
                }
            }

            availableAtLoc = new List<T>((IEnumerable<T>) tempList);
        }

        return availableAtLoc;
    }

    public WriteableBitmap getLatestOutputBM(bool gridOverride = true) {
        WriteableBitmap outputBitmap;
        bool grid = !parentCM.getMainWindow().IsVisible && gridOverride;
        if (isOverlappingModel()) {
            generateOverlappingBitmap(out outputBitmap, grid);
        } else {
            generateAdjacentBitmap(out outputBitmap, grid);
        }

        parentCM.getUIManager().updateTimeStampPosition(percentageCollapsed);

        latestOutput = outputBitmap;
        return outputBitmap;
    }

    private void generateOverlappingBitmap(out WriteableBitmap outputBitmap, bool grid) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth, outputHeight), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Premul);
        ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, outputHeight, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < outputWidth; x++) {
                    Color c = dbOutput.get(x, (int) y);

                    int selectedIndex = dbPropagator.Topology.GetIndex(x, (int) y, 0);
                    ISet<Color> possibleTiles = dbPropagator.GetPossibleValues<Color>(selectedIndex);

                    Color toSet = possibleTiles.Count == 1
                        ? possibleTiles.First()
                        : currentColors!.Contains(c)
                            ? c
                            : grid
                                ? (x + y) % 2 == 0 ? Color.Parse("#11000000") : Color.Parse("#00000000")
                                : Color.Parse("#00000000");
                    dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);

                    if (toSet.A > 200) {
                        Interlocked.Increment(ref collapsedTiles);
                    }
                }
            });
        }

        amountCollapsed = collapsedTiles;
        percentageCollapsed = (double) collapsedTiles / (outputHeight * outputWidth);
        mainWindowVM.IsRunning = amountCollapsed != 0 && (int) percentageCollapsed != 1;
    }

    private void generateAdjacentBitmap(out WriteableBitmap outputBitmap, bool grid) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth * tileSize, outputHeight * tileSize),
            new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Premul);
        ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, outputHeight * tileSize, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < outputWidth * tileSize; x++) {
                    int value = dbOutput.get((int) Math.Floor((double) x / tileSize),
                        (int) Math.Floor((double) y / tileSize));
                    bool isCollapsed = value >= 0;
                    Color[]? outputPattern = isCollapsed ? tileCache.ElementAt(value).Value.Item1 : null;
                    Color c = outputPattern?[y % tileSize * tileSize + x % tileSize] ?? (grid
                        ? ((int) Math.Floor((double) x / tileSize) +
                           (int) Math.Floor((double) y / tileSize)) % 2 == 0
                            ? Color.Parse("#11000000")
                            : Color.Parse("#00000000")
                        : Color.Parse("#00000000"));
                    dest[x] = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);

                    if (isCollapsed && y % tileSize == 0 && x % tileSize == 0) {
                        Interlocked.Increment(ref collapsedTiles);
                    }
                }
            });
        }

        amountCollapsed = collapsedTiles;
        percentageCollapsed = (double) collapsedTiles / (outputHeight * outputWidth);
        mainWindowVM.IsRunning = amountCollapsed != 0 && (int) percentageCollapsed != 1;
    }

    /*
     * WFC Helper Functions
     */

    public bool isOverlappingModel() {
        return !mainWindowVM.SimpleModelSelected;
    }

    public bool isCollapsed() {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract, MergeIntoPattern
        return dbPropagator != null && dbPropagator.Status == Resolution.DECIDED;
    }

    public WriteableBitmap getLatestOutput() {
        return latestOutput;
    }

    public double getPercentageCollapsed() {
        return percentageCollapsed;
    }

    public int getAmountCollapsed() {
        return amountCollapsed;
    }

    public int getActionsTaken() {
        return actionsTaken;
    }

    public void setActionsTaken(int newValue) {
        actionsTaken = newValue;
    }

    public Size getPropagatorSize() {
        return new Size(dbPropagator.Topology.Width, dbPropagator.Topology.Height);
    }

    public void resetWeights() {
        foreach (TileViewModel tileViewModel in mainWindowVM.PatternTiles) {
            tileViewModel.PatternWeight = originalWeights[tileViewModel.RawPatternIndex];
        }
    }
}