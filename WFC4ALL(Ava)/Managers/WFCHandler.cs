using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WFC4ALL.ContentControls;
using WFC4All.DeBroglie;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using static WFC4ALL.Utils.Util;

namespace WFC4ALL.Managers;

public class WFCHandler {
    private readonly CentralManager parentCM;

    private readonly MainWindowViewModel mainWindowVM;
    private readonly MainWindow mainWindow;

    private double amountCollapsed;
    private WriteableBitmap latestOutput;

    private bool _isChangingModels, _isChangingImages;
    private bool inputHasChanged;

    private int currentStep, timeStamp;
    private WriteableBitmap? currentBitmap;

    private static HashSet<Color>? currentColors;

    private TilePropagator dbPropagator;
    private TileModel dbModel;
    private ITopoArray<Tile> tiles;
    private int tileSize;
    private Dictionary<int, Tuple<Color[], Tile>> tileCache;

    private Dictionary<int, int[]> tileSymmetries;
    private XElement xRoot;

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
        latestOutput = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul);
        currentStep = 0;
        timeStamp = 0;
        currentBitmap = null;
        currentColors = new HashSet<Color>();
        tileSize = 0;
        tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
        tileSymmetries = new Dictionary<int, int[]>();

        amountCollapsed = 0d;
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

#if DEBUG
        Trace.WriteLine(@$"Input changed on {source}");
#endif
        inputHasChanged = true;
    }

    private bool hasInputCHanged() {
        return inputHasChanged;
    }

    /*
     * Direct WFC Manipulation
     */

    public (WriteableBitmap, bool) initAndRunWfcDB(bool reset, int steps, bool force = false) {
        if (isChangingModels() || !mainWindow.IsActive && !force) {
            return (new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul), true);
        }

        Stopwatch sw = new();
        sw.Restart();

        if (reset) {
            bool inputPaddingEnabled, outputPaddingEnabled;
            string category = mainWindow.getInputControl().getCategory();
            string inputImage = mainWindow.getInputControl().getInputImage();

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

            if (hasInputCHanged()) {
                // form.displayLoading(true);
                currentBitmap = getImageFromURI(mainWindow.getInputControl().getInputImage());
                mainWindowVM.Tiles.Clear();
                parentCM.getInputManager().resetOverwriteCache();
                parentCM.getUIManager().resetPatterns();

                List<TileViewModel> toAdd = new();

                if (isOverlappingModel()) {
                    (Color[][] colorArray, currentColors) = imageToColourArray(currentBitmap);
                    ITopoArray<Color> dbSample = TopoArray.create(colorArray, inputPaddingEnabled);
                    tiles = dbSample.toTiles();
                    dbModel = new OverlappingModel(mainWindow.getInputControl().getPatternSize());
                    bool hasRotations = mainWindow.getInputControl().getCategory().Equals("Worlds Top-Down")
                                        || category.Equals("Knots") || category.Equals("Knots") ||
                                        inputImage.Equals("Mazelike");

                    (List<PatternArray>? patternList, List<double>? patternWeights)
                        = ((OverlappingModel) dbModel).addSample(tiles,
                            new TileRotation(hasRotations ? 4 : 1, false));

                    for (int i = 0; i < patternList.Count; i++) {
                        TileViewModel? nextTVM = parentCM.getUIManager()
                            .addPattern(patternList[i], patternWeights[i], tileSymmetries);
                        if (nextTVM != null) {
                            toAdd.Add(nextTVM);
                        }
                    }
                } else {
                    dbModel = new AdjacentModel();
                    xRoot = XDocument.Load($"samples/{inputImage}/data.xml").Root ?? new XElement("");

                    tileSize = int.Parse(xRoot.Attribute("size")?.Value ?? "16");

                    tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
                    tileSymmetries = new Dictionary<int, int[]>();

                    foreach (XElement xTile in xRoot.Element("tiles")?.Elements("tile")!) {
                        MemoryStream ms = new(File.ReadAllBytes(
                            $"samples{Path.DirectorySeparatorChar}{inputImage}{Path.DirectorySeparatorChar}{xTile.Attribute("name")?.Value}.png"));
                        WriteableBitmap writeableBitmap = WriteableBitmap.Decode(ms);

                        int cardinality = char.Parse(xTile.Attribute("symmetry")?.Value ?? "X") switch {
                            'L' => 4,
                            'T' => 4,
                            'I' => 2,
                            '\\' => 2,
                            'F' => 8,
                            _ => 1
                        };

                        // Trace.WriteLine(xTile.Attribute("name")?.Value + " " + tileCache.Count + "(" + cardinality + ")");

                        Color[] cur = extractColours(writeableBitmap);
                        int val = tileCache.Count;
                        Tile curTile = new(val);
                        tileCache.Add(val, new Tuple<Color[], Tile>(cur, curTile));

                        List<int> symmetries = new();

                        for (int t = 1; t < cardinality; t++) {
                            int myIdx = tileCache.Count;
                            Color[] curCard = t <= 3
                                ? rotate(tileCache[val + t - 1].Item1.ToArray(), tileSize)
                                : reflect(tileCache[val + t - 4].Item1.ToArray(), tileSize);
                            tileCache.Add(myIdx, new Tuple<Color[], Tile>(curCard, new Tile(myIdx)));
                            symmetries.Add(myIdx);
                        }

                        tileSymmetries.Add(val, symmetries.ToArray());

                        double tileWeight = double.Parse(xTile.Attribute("weight")?.Value ?? "1.0");
                        toAdd.Add(new TileViewModel(writeableBitmap, tileWeight, val));
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

                    for (int i = 0; i < tileCache.Count; i++) {
                        Tile refTile = tileCache.ElementAt(i).Value.Item2;
                        ((AdjacentModel) dbModel).setFrequency(refTile, i);
                    }
                }

                mainWindowVM.Tiles = new ObservableCollection<TileViewModel>(toAdd);
#if (DEBUG)
                if (sw.ElapsedMilliseconds >= 10) {
                    Trace.Write(@$"Init took {sw.ElapsedMilliseconds}ms. - ");
                }
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
            if (sw.ElapsedMilliseconds >= 10) {
                Trace.Write(@$"Assigning took {sw.ElapsedMilliseconds}ms. - ");
            }
#endif
            sw.Restart();

            if (isOverlappingModel()) {
                switch (inputImage.ToLower()) {
                    case "flowers": {
                        // Set the bottom last 2 rows to be the ground tile
                        dbPropagator.select(0, outputHeight - 1, 0,
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

            inputHasChanged = false;
        }

        //TODO
        //form.displayLoading(false);

        (latestOutput, bool decided) = runWfcDB(steps);
        return (latestOutput, decided);
    }

    private (WriteableBitmap, bool) runWfcDB(int steps) {
        Stopwatch sw = new();
        sw.Restart();
        Resolution dbStatus = Resolution.UNDECIDED;
        if (steps == -1) {
            dbStatus = dbPropagator.run();
        } else {
            for (int i = 0; i < steps; i++) {
                dbStatus = dbPropagator.step();
                currentStep++;
                timeStamp++;
            }
        }

#if (DEBUG)
        if (sw.ElapsedMilliseconds >= 10) {
            Trace.WriteLine(@$"Stepping forward took {sw.ElapsedMilliseconds}ms.");
        }
#endif
        sw.Restart();

        WriteableBitmap outputBitmap = getLatestOutputBM();
        parentCM.getUIManager().updateTimeStampPosition(amountCollapsed);
        return (outputBitmap, dbStatus == Resolution.DECIDED);
    }

    public WriteableBitmap stepBackWfc(int steps) {
        Stopwatch sw = new();
        sw.Restart();
        for (int i = 0; i < steps; i++) {
            dbPropagator.doBacktrack();
            timeStamp--;
            currentStep--;
        }

#if (DEBUG)
        if (sw.ElapsedMilliseconds >= 10) {
            Trace.WriteLine(@$"Stepping back took {sw.ElapsedMilliseconds}ms.");
        }
#endif
        sw.Restart();

        WriteableBitmap outputBitmap = getLatestOutputBM();
        parentCM.getUIManager().updateTimeStampPosition(amountCollapsed);
        return outputBitmap;
    }

    public (WriteableBitmap, bool) setTile(int a, int b, int toSet) {
        Tile t = isOverlappingModel() ? tiles.get(toSet) : tileCache[toSet].Item2;
        Resolution status = dbPropagator.select(a, b, 0, t);

        bool showPixel = true;
        int count = 0;

        while (status == Resolution.CONTRADICTION) {
            foreach (int symmetryIndex in getSymmetries(toSet)) {
                Trace.WriteLine("CONT");
                dbPropagator.doBacktrack();
                status = dbPropagator.select(a, b, 0,
                    isOverlappingModel() ? tiles.get(symmetryIndex) : tileCache[symmetryIndex].Item2);
                if (status != Resolution.CONTRADICTION) {
                    break;
                }
            }

            if (count == 100) {
                showPixel = false;
                break;
            }

            count++;
        }

        return (getLatestOutputBM(), showPixel);
    }

    private Dictionary<Tuple<int, int>, Color> getOverlapDict() {
        Dictionary<Tuple<int, int>, Tuple<Color, int>> overwriteColorCache
            = parentCM.getInputManager().getOverwriteColorCache();
        Dictionary<Tuple<int, int>, Color> returnCache = new();

        foreach ((Tuple<int, int> key, (Color c, int item2)) in new Dictionary<Tuple<int, int>, Tuple<Color, int>>(
                     overwriteColorCache)) {
            if (parentCM.getWFCHandler().getCurrentTimeStamp() >= item2) {
                returnCache.Add(key, c);
            }
        }

        return returnCache;
    }

    public WriteableBitmap getLatestOutputBM() {
        WriteableBitmap outputBitmap;
        if (isOverlappingModel()) {
            generateOverlappingBitmap(out outputBitmap);
        } else {
            generateAdjacentBitmap(out outputBitmap);
        }

        return outputBitmap;
    }

    private void generateOverlappingBitmap(out WriteableBitmap outputBitmap) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth, outputHeight), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
        ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();

        Dictionary<Tuple<int, int>, Color> overwriteColorCache = getOverlapDict();
        foreach (Tuple<int, int> key in overwriteColorCache.Keys) {
            Trace.WriteLine($@"Key <{key.Item1},{key.Item2}>");
        }

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, outputHeight, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < outputWidth; x++) {
                    Color c = dbOutput.get(x, (int) y);
                    Tuple<int, int> overlapKey = new(x, (int) y);
                    Color toSet = overwriteColorCache.ContainsKey(overlapKey) ? overwriteColorCache[overlapKey] :
                        currentColors!.Contains(c) ? c : Colors.Transparent;
                    dest[x] = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);

                    if (!toSet.Equals(Colors.Transparent)) {
                        Interlocked.Increment(ref collapsedTiles);
                    }
                }
            });
        }

        Trace.WriteLine(collapsedTiles);
        amountCollapsed = (double) collapsedTiles / (outputHeight * outputWidth);
    }

    private void generateAdjacentBitmap(out WriteableBitmap outputBitmap) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth * tileSize, outputHeight * tileSize),
            new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
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
                    
                    Color c = outputPattern?[y % tileSize * tileSize + x % tileSize] ?? Colors.Transparent;
                    dest[x] = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);

                    if (isCollapsed && y % tileSize == 0 && x % tileSize == 0) {
                        Interlocked.Increment(ref collapsedTiles);
                    }
                }
            });
        }

        amountCollapsed = (double) collapsedTiles / (outputHeight * outputWidth);
    }

    public int getCurrentStep() {
        return currentStep;
    }

    public int getCurrentTimeStamp() {
        return timeStamp;
    }

    public void setCurrentStep(int newCurStep) {
        currentStep = newCurStep;
    }

    /*
     * WFC Helper Functions
     */

    private IEnumerable<int> getSymmetries(int value) {
        List<int> valueSymmetries = new();
        foreach ((int key, int[] v) in tileSymmetries) {
            if (value.Equals(key) || ((IList) v).Contains(value)) {
                valueSymmetries.Add(key);
                valueSymmetries.AddRange(v);
            }

            valueSymmetries.Remove(value);
        }

        return valueSymmetries.ToArray();
    }

    public bool isOverlappingModel() {
        return mainWindowVM.ModelSelectionText.Contains("Tile");
    }

    public bool isCollapsed() {
        return dbPropagator.Status == Resolution.DECIDED;
    }

    public WriteableBitmap getLatestOutput() {
        return latestOutput;
    }

    public double getAmountCollapsed() {
        return amountCollapsed;
    }

    public ITopoArray<Tile> getTiles() {
        return tiles;
    }

    public Dictionary<int, Tuple<Color[], Tile>> getTileCache() {
        return tileCache;
    }
}