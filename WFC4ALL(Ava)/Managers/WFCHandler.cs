using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using WFC4ALL.DeBroglie;
using WFC4ALL.DeBroglie.Models;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.Models;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using static WFC4ALL.Utils.Util;
using Color = Avalonia.Media.Color;

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

    private List<TileViewModel> toAddPaint;

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
        toAddPaint = new List<TileViewModel>();
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
        
        if (reset) {
            if (inputHasChanged) {
                initializeLocalValues();

                List<TileViewModel>? toAdd = null;
                string category = mainWindow.getInputControl().getCategory();
                string inputImage = mainWindow.getInputControl().getInputImage();
                int overlappingDimension = mainWindow.getInputControl().getPatternSize();

                await Task.Run(() => {
                    toAdd = isOverlappingModel() ? initializeOverlappingModel(category, inputImage, overlappingDimension) : initializeAdjacentModel(inputImage);
                });
                mainWindowVM.PatternTiles = new ObservableCollection<TileViewModel>(toAdd!);

                parentCM.getPaintingWindow().setPaintingPatterns(toAddPaint.ToArray());
            }

            createPropagator();
            
            if (isOverlappingModel()) {
                handleSideViewInit();
            }

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

    private void createPropagator() {
        string category = mainWindow.getInputControl().getCategory();
        string inputImage = mainWindow.getInputControl().getInputImage();
        
        bool outputPaddingEnabled;
        if (category.Equals("Textures")) {
            outputPaddingEnabled = isOverlappingModel() && mainWindowVM.PaddingEnabled;
        } else {
            outputPaddingEnabled = category.Equals("Worlds Side-View") || inputImage.Equals("Font")
                || category.Equals("Knots") && !inputImage.Equals("Nested");
        }
        
        int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;

        GridTopology dbTopology = new(outputWidth, outputHeight, outputPaddingEnabled);
        int curSeed = Environment.TickCount;
        dbPropagator = new TilePropagator(dbModel, dbTopology, new TilePropagatorOptions {
            BackTrackDepth = -1,
            RandomDouble = new Random(curSeed).NextDouble,
        });
    }

    private void handleSideViewInit() {
        int outputHeight = mainWindowVM.ImageOutWidth;
        string inputImage = mainWindow.getInputControl().getInputImage();
        
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
    
    private List<TileViewModel> initializeOverlappingModel(string category, string inputImage, int patternSize) {
        List<TileViewModel> toAdd = new ();

        bool inputPaddingEnabled = category.Equals("Worlds Top-Down") || category.Equals("Worlds Side-View")
            || category.Equals("Font") || category.Equals("Knots") && !inputImage.Equals("Nested")
            && !inputImage.Equals("NotKnot");

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
        
        toAdd.AddRange(patternList.Select((t, i) => parentCM.getUIManager()
                .addPattern(t, patternWeights[i], tileSymmetries))
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
        List<TileViewModel> toAdd = new ();
        dbModel = new AdjacentModel();
        
        xRoot = XDocument.Load($"{AppContext.BaseDirectory}/samples/{inputImage}/data.xml").Root ??
            new XElement("");

        tileSize = int.Parse(xRoot.Attribute("size")?.Value ?? "16");

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

                toAddPaint.Add(
                    new TileViewModel(writeableBitmap, tileCache.Count - 1, rotation, shouldFlip));
                tileCache.Add(myIdx, new Tuple<Color[], Tile>(curCard, new Tile(myIdx)));
                symmetries.Add(myIdx);
            }

            tileSymmetries.Add(val, symmetries.ToArray());

            double tileWeight = double.Parse(xTile.Attribute("weight")?.Value ?? "1.0");
            TileViewModel tvm = new(writeableBitmap, tileWeight, tileCache.Count - 1, cardinality > 4);

            toAdd.Add(tvm);
            toAddPaint.Add(tvm);
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

        return toAdd;
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

        sw.Restart();

        WriteableBitmap outputBitmap = getLatestOutputBM();
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
        
        sw.Restart();

        WriteableBitmap outputBitmap = getLatestOutputBM();
        return outputBitmap;
    }

    public (WriteableBitmap?, bool) setTile(int a, int b, int toSet) {
        Resolution status;

        if (isOverlappingModel()) {
            if (dbPropagator.toValueArray<Color>().get(a, b).A.Equals(255)) {
                return (null, false);
            }
        }

        int patternCount = 0;
        if (isOverlappingModel()) {
            Color c = toAddPaint[toSet].PatternColour;
            IEnumerable<Tile> tilesToSelect = tiles.toArray2d().Cast<Tile>()
                .Where(tile => ((Color) tile.Value).Equals(c));
            status = dbPropagator.select(a, b, 0, tilesToSelect, out patternCount);
        } else {
            status = dbPropagator.select(a, b, 0, tileCache[toSet].Item2);
        }

        bool showPixel = status != Resolution.CONTRADICTION;

        currentStep++;
        timeStamp++;

        if (!showPixel) {
            dbPropagator.doBacktrack();
            if (isOverlappingModel()) {
                for (int i = 1; i < patternCount; i++) {
                    dbPropagator.doBacktrack();
                }
            } else {
                dbPropagator.doBacktrack();
            }

            return (null, false);
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
        bool grid = !parentCM.getMainWindow().IsVisible;
        if (isOverlappingModel()) {
            generateOverlappingBitmap(out outputBitmap, grid);
        } else {
            generateAdjacentBitmap(out outputBitmap, grid);
        }

        parentCM.getUIManager().updateTimeStampPosition(amountCollapsed);

        return outputBitmap;
    }

    private void generateOverlappingBitmap(out WriteableBitmap outputBitmap, bool grid) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        Graphics? g = Graphics.FromHwnd(IntPtr.Zero);
        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth, outputHeight), new Vector(g.DpiX, g.DpiY),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Premul);
        ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();

        Dictionary<Tuple<int, int>, Color> overwriteColorCache = getOverlapDict();

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
                        currentColors!.Contains(c) ? c :
                        grid ? (x + y) % 2 == 0 ? Color.Parse("#11000000") :
                        Color.Parse("#00000000") :
                        Color.Parse("#00000000");
                    dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);

                    if (!toSet.Equals(Color.Parse("#00000000"))) {
                        Interlocked.Increment(ref collapsedTiles);
                    }
                }
            });
        }

        amountCollapsed = (double) collapsedTiles / (outputHeight * outputWidth);
    }

    private void generateAdjacentBitmap(out WriteableBitmap outputBitmap, bool grid) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        Graphics? g = Graphics.FromHwnd(IntPtr.Zero);
        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth * tileSize, outputHeight * tileSize),
            new Vector(g.DpiX, g.DpiY),
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
                            (int) Math.Floor((double) y / tileSize)) % 2 == 0 ? Color.Parse("#11000000") :
                        Color.Parse("#00000000")
                        : Color.Parse("#00000000"));
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

    public bool isOverlappingModel() {
        return mainWindowVM.ModelSelectionText.Contains("Tile");
    }

    public bool isCollapsed() {
        return dbPropagator != null && dbPropagator.Status == Resolution.DECIDED;
    }

    public WriteableBitmap getLatestOutput() {
        return latestOutput;
    }

    public double getAmountCollapsed() {
        return amountCollapsed;
    }

    public IEnumerable<TileViewModel> getPaintableTiles() {
        return toAddPaint;
    }

    public int getTileSize() {
        return tileSize;
    }
}