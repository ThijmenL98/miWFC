﻿#if DEBUG
using System.Diagnostics;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.DeBroglie;
using miWFC.DeBroglie.Models;
using miWFC.DeBroglie.Rot;
using miWFC.DeBroglie.Topo;
using miWFC.Utils;
using miWFC.ViewModels;
using miWFC.ViewModels.Structs;
using miWFC.Views;
using static miWFC.Utils.Util;

namespace miWFC.Managers;

/// <summary>
/// Main handler for communicating to and from the algorithm
/// </summary>
public class WFCHandler {
    private static HashSet<Color>? currentColors;
    private readonly CentralManager centralManager;

    private readonly FixedSizeQueue<Tuple<int, int, int>> fsqAdj = new(10);
    private readonly MainWindow mainWindow;

    private readonly MainWindowViewModel mainWindowVM;

    private bool _isChangingModels, _isChangingImages, inputHasChanged, _isBrushing;
    private int amountCollapsed, actionsTaken, tileSize;

    private WriteableBitmap? currentBitmap;
    private TileModel dbModel;

    private TilePropagator dbPropagator;

    private List<double> originalWeights;
    private List<PatternArray> originalPatterns;

    private readonly List<Color[]> disabledPatterns = new();

    private Dictionary<int, double> origTileWeights = new();
    private double percentageCollapsed;
    private Dictionary<int, Tuple<Color[], Tile>> tileCache;
    private ITopoArray<Tile> tiles;

    private Dictionary<int, int[]> tileSymmetries;

    private List<TileViewModel> toAddPaint;
    private XElement xRoot;

    /*
     * Object Initializing Functions & Constructor
     */

#pragma warning disable CS8618
    public WFCHandler(CentralManager parent) {
#pragma warning restore CS8618
        centralManager = parent;
        mainWindowVM = centralManager.getMainWindowVM();
        mainWindow = centralManager.getMainWindow();

        _isChangingModels = false;
        _isChangingImages = false;
        inputHasChanged = true;
        currentBitmap = null;
        currentColors = new HashSet<Color>();
        tileSize = 0;
        toAddPaint = new List<TileViewModel>();
        tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
        tileSymmetries = new Dictionary<int, int[]>();

        percentageCollapsed = 0d;
        amountCollapsed = 0;
        actionsTaken = 0;

        originalWeights = new List<double>();
        originalPatterns = new List<PatternArray>();
        tiles = new TopoArray2D<Tile>(new Tile[,] { }, false);
        xRoot = new XElement("empty");
    }

    /*
     * Algorithm Initializing Functions & Constructor
     */

    /// <summary>
    /// Function that is called for every step of the algorithm, if the algorithm is not initialized, it will take
    /// care of it as well, otherwise it will just forward the call to the logic
    /// </summary>
    /// 
    /// <param name="reset">Whether to reset the algorithm</param>
    /// <param name="steps">The amount of steps to proceed in the algorithm</param>
    /// <param name="force">Whether to force the algorithm to continue if other checks fail</param>
    /// 
    /// <returns>A task with the new output image and whether the algorithm is done</returns>
    public async Task<(WriteableBitmap, bool)> initAndRunWfcDB(bool reset, int steps, bool force = false) {
        if (isChangingModels() || (!mainWindow.IsActive && !force)) {
            return (new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
                true);
        }

        mainWindowVM.setLoading(true);

        if (dbPropagator is {Status: Resolution.DECIDED} && !reset) {
            updateWeights();
            await centralManager.getInputManager().restartSolution("Force Reset Decided");
        }

        if (reset) {
            string inputImage = mainWindow.getInputControl().getInputImage();
            string category = mainWindow.getInputControl().getCategory();
            mainWindowVM.ItemVM.resetDataGrid();
            centralManager.getMainWindowVM().ItemOverlay = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            bool inputWrappingEnabled = mainWindowVM.InputWrapping || category.Contains("Side");

            switch (inputHasChanged) {
                case true: {
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

                    centralManager.getPaintingWindow().setPaintingPatterns(toAddPaint.ToArray());
                    break;
                }
                case false when isOverlappingModel(): {
                    int overlappingDimension = mainWindow.getInputControl().getPatternSize();
                    await Task.Run(() => {
                        dbModel = new OverlappingModel(overlappingDimension);
                        setOverlappingSample(category, inputImage);
                    });
                    break;
                }
            }

            int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;
            bool seamlessOutput = mainWindowVM.SeamlessOutput || category.Contains("Side");

            int curSeed = Environment.TickCount;
            Random rand = new(curSeed);
            mainWindowVM.setR(rand);

            await Task.Run(() => {
                createPropagator(outputWidth, outputHeight, seamlessOutput, rand);

                if (isOverlappingModel() && inputWrappingEnabled) {
                    bool success = false;
                    while (!success) {
                        try {
                            handleSideViewInit(outputHeight, inputImage);
                            success = true;
                        } catch (TargetException) { }
                    }
                }
            });

            inputHasChanged = false;
        }

        mainWindowVM.setLoading(false);

        (WriteableBitmap latestOutput, bool decided) = runWfcDB(steps);
        isCollapsed();
        return (latestOutput, decided);
    }

    /// <summary>
    /// Function to initialize values related to the algorithm progression
    /// </summary>
    private void initializeLocalValues() {
        currentBitmap = getSampleFromPath(mainWindow.getInputControl().getInputImage());
        mainWindowVM.PatternTiles.Clear();
        mainWindowVM.PaintTiles.Clear();
        toAddPaint = new List<TileViewModel>();

        centralManager.getInputManager().resetMask();
        centralManager.getUIManager().resetPatterns();
    }

    /// <summary>
    /// Function dedicated to initializing the overlapping model of the algorithm, if used
    /// </summary>
    /// 
    /// <param name="category">Category of the input image</param>
    /// <param name="inputImage">Name of the input image</param>
    /// <param name="patternSize">Size of the pattern</param>
    /// <param name="inputPaddingEnabled">Whether input padding is enabled</param>
    /// 
    /// <returns>List of tiles extracted from the input image</returns>
    private List<TileViewModel> initializeOverlappingModel(string category, string inputImage, int patternSize,
        bool inputPaddingEnabled) {
        List<TileViewModel> toAdd = new();

        (Color[][] colorArray, currentColors) = imageToColourArray(currentBitmap!);
        ITopoArray<Color> dbSample = TopoArray.create(colorArray, inputPaddingEnabled);
        tiles = dbSample.toTiles();

        dbModel = new OverlappingModel(patternSize);

        (List<PatternArray> patternList, List<double> patternWeights) = setOverlappingSample(category, inputImage);

        originalWeights = patternWeights;

        toAdd.AddRange(patternList.Select((t, i) => centralManager.getUIManager()
                .addPattern(t, patternWeights[i], tileSymmetries, i))
            .Where(nextTVM => nextTVM != null)!);

        Dictionary<int, double> weights = new();
        double total = 0d;

        foreach ((Tile t, int index) in tiles.toArray2d().Cast<Tile>().Distinct()
                     .Select((tile, idx) => (tile, idx))) {
            Color c = (Color) t.Value;

            WriteableBitmap pattern = new(new PixelSize(1, 1), new Vector(96, 96),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? PixelFormat.Bgra8888
                    : PixelFormat.Rgba8888,
                AlphaFormat.Unpremul);

            using ILockedFramebuffer? frameBuffer = pattern.Lock();

            unsafe {
                ((uint*) frameBuffer.Address.ToPointer())[0]
                    = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);
            }

            toAddPaint.Add(new TileViewModel(pattern, index, c, centralManager));

            double curWeight = ((OverlappingModel) dbModel).getFrequency(t).Sum();
            total += curWeight;
            weights[index] = curWeight;
        }

        origTileWeights = weights;

        foreach ((int index, double weightAvg) in weights) {
            double percentage = weightAvg / total;
            toAddPaint[index].PatternWeight = percentage;
        }

        originalPatterns = ((OverlappingModel) dbModel).patternArrays;

        return toAdd;
    }

    /// <summary>
    /// Function dedicated to initializing the adjacent model of the algorithm, if used
    /// </summary>
    /// 
    /// <param name="inputImage">Name of the input image</param>
    /// 
    /// <returns>List of tiles extracted from the input image</returns>
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
                    new TileViewModel(writeableBitmap, tileWeight, tileCache.Count - 1, rotation, shouldFlip,
                        centralManager));
                tileCache.Add(myIdx, new Tuple<Color[], Tile>(curCard, new Tile(myIdx)));

                symmetries.Add(myIdx);
            }

            tileSymmetries.Add(val, symmetries.ToArray());

            TileViewModel tvm = new(writeableBitmap, tileWeight, tileCache.Count - 1, val, centralManager,
                card: cardinality);
            toAdd.Add(tvm);
            toAddPaint.Add(tvm);
        }

        int sampleDimension = xRoot.Element("rows")?.Elements("row").Count() ?? 0;
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

    /// <summary>
    /// Function used to create a new propagator for the algorithm to use
    /// </summary>
    /// 
    /// <param name="outputWidth">Width of the output image</param>
    /// <param name="outputHeight">Height of the output image</param>
    /// <param name="seamlessOutput">Whether the output is required to be seamless</param>
    /// <param name="rand">The randomness parameter, forwarded for reproducibility using seeds (if used)</param>
    private void createPropagator(int outputWidth, int outputHeight, bool seamlessOutput, Random rand) {
        GridTopology dbTopology = new(outputWidth, outputHeight, seamlessOutput);
        dbPropagator = new TilePropagator(dbModel, dbTopology, new TilePropagatorOptions {
            RandomDouble = rand.NextDouble,
            IndexPickerType = IndexPickerType.HEAP_MIN_ENTROPY
        }, centralManager);
    }

    /// <summary>
    /// Function dedicated to initialize images under the category "Worlds Side-View", as they are a special case
    /// due to these images having an orientation, whereas other images are orientation invariant.
    /// </summary>
    /// 
    /// <param name="outputHeight">Height of the output</param>
    /// <param name="inputImage">Input image used</param>
    private void handleSideViewInit(int outputHeight, string inputImage) {
        switch (inputImage.ToLower()) {
            case "flowers": {
                // Set the bottom last 2 rows to be the ground tile
                dbPropagator.select(0, outputHeight - 1, 0,
                    new Tile(currentColors!.ElementAt(currentColors!.Count - 1)));

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

    /*
     * Getters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    /// Get the amount of cells that are collapsed
    /// </summary>
    /// 
    /// <returns>Integer</returns>
    public int getAmountCollapsed() {
        return amountCollapsed;
    }

    /// <summary>
    /// Get the amount of actions taken by the user and the algorithm
    /// </summary>
    /// 
    /// <returns>Integer</returns>
    public int getActionsTaken() {
        return actionsTaken;
    }

    /// <summary>
    /// Get the tile size used with the current input image
    /// </summary>
    /// 
    /// <returns>Integer >= 1</returns>
    public int getTileSize() {
        return tileSize;
    }

    /// <summary>
    /// Due to the algorithm not requiring to keep track on which patterns have which index, but displaying it to the
    /// user does require this, we need to descramble the user index to the algorithm index by mapping the index we
    /// assigned to the index the algorithm designed
    /// </summary>
    /// 
    /// <param name="raw">Raw tile index created by us</param>
    /// 
    /// <returns>Algorithm tile index</returns>
    public int getDescrambledIndex(int raw) {
        return dbPropagator.getTMM().GetPatterns(tileCache[raw].Item2, 0).First();
    }

    /// <summary>
    /// Get the percentage of cells to the whole of which are collapsed and which aren't (yet)
    /// </summary>
    /// 
    /// <returns>Double</returns>
    public double getPercentageCollapsed() {
        return percentageCollapsed;
    }

    /// <summary>
    /// Get the size of the propagator, this might differ to the output image due to tile sizes
    /// </summary>
    /// 
    /// <returns>Size(int, int)</returns>
    public Size getPropagatorSize() {
        return new Size(dbPropagator.Topology.Width, dbPropagator.Topology.Height);
    }

    // Booleans

    /// <summary>
    /// Whether the algorithm is currently in an overlapping mode (or adjacent)
    /// </summary>
    /// 
    /// <returns>Boolean</returns>
    public bool isOverlappingModel() {
        return !mainWindowVM.SimpleModelSelected;
    }

    /// <summary>
    /// Whether the output is collapsed entirely
    /// </summary>
    /// 
    /// <returns>Boolean</returns>
    public bool isCollapsed() {
        bool isC = dbPropagator is {Status: Resolution.DECIDED} ||
                   Math.Abs(getPercentageCollapsed() - 1d) < 0.0001d;

        centralManager.getMainWindowVM().ItemVM.ItemEditorEnabled
            = centralManager.getMainWindow().getInputControl().getCategory().Equals("Worlds Top-Down") && isC;

        return isC;
    }

    /// <summary>
    /// Whether we are in the midst of changing models, hence the algorithm shouldn't progress accidentally
    /// </summary>
    /// 
    /// <returns>Boolean</returns>
    public bool isChangingModels() {
        return _isChangingModels;
    }

    /// <summary>
    /// Whether we are in the midst of changing images, hence the algorithm shouldn't progress accidentally
    /// </summary>
    /// 
    /// <returns>Boolean</returns>
    public bool isChangingImages() {
        return _isChangingImages;
    }

    /// <summary>
    /// Whether the user is currently brushing
    /// </summary>
    /// 
    /// <returns>Boolean</returns>
    public bool isBrushing() {
        return _isBrushing;
    }

    // Images

    /// <summary>
    /// Generate the latest output image
    /// </summary>
    /// 
    /// <param name="grid">Whether to create a grid or a transparent background</param>
    /// 
    /// <returns>Latest output image</returns>
    public WriteableBitmap getLatestOutputBM(bool grid = true) {
        WriteableBitmap outputBitmap;
        if (isOverlappingModel()) {
            generateOverlappingBitmap(out outputBitmap, grid);
        } else {
            generateAdjacentBitmap(out outputBitmap, grid);
        }

        centralManager.getUIManager().updateTimeStampPosition(percentageCollapsed);

        return outputBitmap;
    }

    // Objects

    /// <summary>
    /// When painting in the overlapping mode, we need to place colours rather than tile indices, hence we need to get
    /// the colour associated with the user selected tile
    /// </summary>
    /// 
    /// <param name="index">Index of the colour</param>
    /// 
    /// <returns>Colour associated</returns>
    public Color getColorFromIndex(int index) {
        return toAddPaint[index].PatternColour;
    }

    /// <summary>
    /// Get the colour located at a coordinate
    /// </summary>
    /// 
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="grid">Whether to return transparency or a grid</param>
    /// 
    /// <returns>Colour associated</returns>
    public Color getOverlappingOutputAt(int x, int y, bool grid = false) {
        Color c = dbPropagator.toValueArray<Color>().get(x, y);
        int selectedIndex = getDbPropagator().Topology.GetIndex(x, y, 0);
        ISet<Color> possibleTiles = getDbPropagator().GetPossibleValues<Color>(selectedIndex);

        Color atPos = possibleTiles.Count == 1 ? possibleTiles.First() :
            getCurrentColors()!.Contains(c) ? c :
            grid ? (x + y) % 2 == 0 ? Color.Parse("#11000000") : Color.Parse("#00000000") : Color.Parse("#00000000");

        return atPos;
    }

    /// <summary>
    /// Get the propagator
    /// </summary>
    /// 
    /// <returns>TilePropagator</returns>
    private TilePropagator getDbPropagator() {
        return dbPropagator;
    }

    // Lists

    /// <summary>
    /// Get the tile cache of the adjacent mode of the algorithm
    /// Structure: (tile index, Tuple(Pattern colours, Tile Object))
    /// </summary>
    /// 
    /// <returns>The tile cache</returns>
    public Dictionary<int, Tuple<Color[], Tile>> getTileCache() {
        return tileCache;
    }

    /// <summary>
    /// Get all tiles used in the overlapping mode of the algorithm
    /// </summary>
    /// 
    /// <returns>List of tiles</returns>
    public List<TileViewModel> getColours() {
        return toAddPaint;
    }

    /// <summary>
    /// Get all current colours used in the output
    /// </summary>
    /// 
    /// <returns>Colours</returns>
    private static HashSet<Color>? getCurrentColors() {
        return currentColors;
    }

    /// <summary>
    /// Get the output of the overlapping mode of the algorithm
    /// </summary>
    /// 
    /// <returns>Raw algorithm output</returns>
    public ITopoArray<Color> getPropagatorOutputO() {
        return dbPropagator.toValueArray<Color>();
    }

    /// <summary>
    /// Get the output of the adjacent mode of the algorithm
    /// </summary>
    /// 
    /// <returns>Raw algorithm output</returns>
    public ITopoArray<int> getPropagatorOutputA() {
        return dbPropagator.toValueArray(-1, -2);
    }

    /// <summary>
    /// Get all weights at a given output cell
    /// </summary>
    /// 
    /// <param name="index">Cell index</param>
    /// 
    /// <returns>Weights by tile index</returns>
    public double[] getWeightsAt(int index) {
        double[] newWeights = new double[centralManager.getMainWindowVM().PaintTiles.Count];

        foreach (TileViewModel tvm in centralManager.getMainWindowVM().PatternTiles.Reverse()) {
            int xDim = mainWindowVM.ImageOutWidth, yDim = mainWindowVM.ImageOutHeight;
            if (xDim * yDim != tvm.WeightHeatMap.Length) {
                resetWeights();
            }

            double staticWeight = tvm.PatternWeight == 0 ? 0.000000000000000000001d : tvm.PatternWeight;

            double[] tmp = new double[tvm.WeightHeatMap.GetLength(0) * tvm.WeightHeatMap.GetLength(1)];
            for (int x = 0; x < tvm.WeightHeatMap.GetLength(0); x++) {
                for (int y = 0; y < tvm.WeightHeatMap.GetLength(1); y++) {
                    tmp[x + y * tvm.WeightHeatMap.GetLength(0)] = tvm.WeightHeatMap[x, y];
                }
            }

            for (int i = tvm.RawPatternIndex; i <= tvm.PatternIndex; i++) {
                foreach (TileViewModel tileViewModel in mainWindowVM.PaintTiles) {
                    if (tileViewModel.PatternIndex == i) {
                        if (tvm.DynamicWeight) {
                            newWeights[getDescrambledIndex(tileViewModel.PatternIndex)] = tmp[index];
                        } else {
                            newWeights[getDescrambledIndex(tileViewModel.PatternIndex)] = staticWeight;
                        }
                    }
                }
            }
        }

        return newWeights;
    }

    /// <summary>
    /// Get the available tiles or patterns at a given coordinate
    /// </summary>
    /// 
    /// <param name="a">X Coordinate</param>
    /// <param name="b">Y Coordinate</param>
    /// <typeparam name="T">Type used by the algorithm</typeparam>
    /// 
    /// <returns>List of available patterns associated</returns>
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

    // Other

    /*
     * Setters
     */

    // Strings

    /// <summary>
    /// Set whether an outer source has changed the input in any way
    /// </summary>
    /// 
    /// <param name="source">Source of the change</param>
    public void setInputChanged(string source) {
#if DEBUG
        Trace.WriteLine($"Input changed on {source}");
#endif
        if (isChangingModels()) {
            return;
        }

        inputHasChanged = true;
    }

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    /// Set the new amount of actions taken, not a simple increment due to multiple steps being allowed at once
    /// </summary>
    /// 
    /// <param name="newValue">New amount of steps</param>
    public void setActionsTaken(int newValue) {
        actionsTaken = newValue;
    }

    // Booleans

    /// <summary>
    /// Set whether the image is being changed
    /// </summary>
    /// 
    /// <param name="isChanging">Boolean</param>
    public void setImageChanging(bool isChanging) {
        _isChangingImages = isChanging;
    }

    /// <summary>
    /// Set whether the Model is being changed
    /// </summary>
    /// 
    /// <param name="isChanging">Boolean</param>
    public void setModelChanging(bool isChanging) {
        _isChangingModels = isChanging;
    }

    /// <summary>
    /// Disable (or enable) a pattern in the output
    /// </summary>
    /// 
    /// <param name="disabled">Whether to disable</param>
    /// <param name="patternIndex">Index of the pattern</param>
    public async void setPatternDisabled(bool disabled, int patternIndex) {
        List<Color> colorsL = new();
        PatternArray patternArray = originalPatterns[patternIndex];

        for (int xx = 0; xx < patternArray.Values.GetLength(0); xx++) {
            for (int yy = 0; yy < patternArray.Values.GetLength(1); yy++) {
                colorsL.Add((Color) patternArray.Values[xx, yy, 0].Value);
            }
        }

        if (disabled) {
            disabledPatterns.Add(colorsL.ToArray());
        } else {
            for (int i = 0; i < disabledPatterns.Count; i++) {
                bool areEqual = disabledPatterns[i].SequenceEqual(colorsL.ToArray());
                if (areEqual) {
                    disabledPatterns.RemoveAt(i);
                }
            }
        }

        if (centralManager.getMainWindowVM().IsRunning) {
            await centralManager.getInputManager().restartSolution("patterns");
        }
    }

    // Images

    // Objects

    // Lists

    /// <summary>
    /// Insert the sample associated with the current input into the model and return the patterns generated
    /// </summary>
    /// 
    /// <param name="category">Input category</param>
    /// <param name="inputImage">Input image</param>
    /// 
    /// <returns>
    /// (x, _) -> list of patterns extracted from the input
    /// (_, x) -> list of weights extracted from the input
    /// </returns>
    private (List<PatternArray> patternList, List<double> patternWeights) setOverlappingSample(string category,
        string inputImage) {
        bool hasRotations = (category.Equals("Worlds Top-Down")
                             || category.Equals("Knots") || category.Equals("Knots") ||
                             inputImage.Equals("Mazelike")) && !inputImage.Equals("Village");

        (List<PatternArray>? patternList, List<double>? patternWeights)
            = ((OverlappingModel) dbModel).addSample(tiles, disabledPatterns,
                new TileRotation(hasRotations ? 4 : 1, true));

        return (patternList, patternWeights);
    }

    /// <summary>
    /// Insert the weights set by the user into the model
    /// </summary>
    public void updateWeights() {
        if (!isOverlappingModel()) {
            List<double> userWeights
                = centralManager.getMainWindowVM().PatternTiles.Select(tvm => tvm.PatternWeight).ToList();
            foreach (((int parent, int[] symmetries), int idx) in tileSymmetries.Select((pair, idx) => (pair, idx))) {
                double weight = userWeights[idx];
                weight = weight == 0 ? 0.00000000001d : weight;
                ((AdjacentModel) dbModel).setFrequency(tileCache[parent].Item2, weight);

                foreach (int symmetryIdx in symmetries) {
                    ((AdjacentModel) dbModel).setFrequency(tileCache[symmetryIdx].Item2, weight);
                }
            }

            foreach (TileViewModel tvm in centralManager.getMainWindowVM().PatternTiles) {
                if (tvm.WeightHeatMap.Length == 0) {
                    int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
                    tvm.WeightHeatMap = new double[outputWidth, outputHeight];
                    for (int i = 0; i < outputWidth; i++) {
                        for (int j = 0; j < outputHeight; j++) {
                            tvm.WeightHeatMap[i, j] = tvm.PatternWeight;
                        }
                    }

                    tvm.DynamicWeight = false;
                }
            }
        }
    }

    /// <summary>
    /// Update the transformations allowed or disallowed by the user in the model
    /// </summary>
    public void updateTransformations() {
        foreach (TileViewModel tvm in mainWindowVM.PatternTiles) {
            if (tvm.RotateDisabled || tvm.FlipDisabled) {
                int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
                int patternCardinality = tvm.PatternIndex - tvm.RawPatternIndex + 1;
                int rotRaw = (int) (tvm.UserRotation / 90d);
                List<int> toBan = new();

                switch (patternCardinality) {
                    case 4: {
                        List<int> idsToSkip = new();

                        rotRaw = rotRaw switch {
                            1 => 3,
                            3 => 1,
                            _ => rotRaw
                        };
                        idsToSkip.Add(tvm.RawPatternIndex + rotRaw);

                        for (int i = tvm.RawPatternIndex; i <= tvm.PatternIndex; i++) {
                            if (!idsToSkip.Contains(i)) {
                                toBan.Add(i);
                            }
                        }

                        break;
                    }
                    case 8:
                        switch (tvm.RotateDisabled) {
                            case true when tvm.FlipDisabled: {
                                // Only 1 pattern should be skipped
                                for (int i = tvm.RawPatternIndex; i <= tvm.PatternIndex; i++) {
                                    toBan.AddRange(from tvmPatt in mainWindowVM.PaintTiles.Reverse()
                                        where tvmPatt.PatternIndex.Equals(i)
                                        let myRot = (int) ((tvmPatt.PatternRotation + 360d) % 360d / 90d)
                                        where !myRot.Equals(rotRaw) || tvmPatt.PatternFlipping.Equals(tvm.UserFlipping)
                                        select tvmPatt.PatternIndex);
                                }

                                break;
                            }
                            case true: {
                                for (int i = tvm.RawPatternIndex; i <= tvm.PatternIndex; i++) {
                                    toBan.AddRange(from tvmPatt in mainWindowVM.PaintTiles.Reverse()
                                        where tvmPatt.PatternIndex.Equals(i)
                                        let myRot = (int) ((tvmPatt.PatternRotation + 360d) % 360d / 90d)
                                        where !myRot.Equals(rotRaw)
                                        select tvmPatt.PatternIndex);
                                }

                                // 2 patterns should be skipped
                                break;
                            }
                            default: {
                                if (tvm.FlipDisabled) {
                                    // 4 patterns should be skipped
                                    for (int i = tvm.RawPatternIndex; i <= tvm.PatternIndex; i++) {
                                        toBan.AddRange(from tvmPatt in mainWindowVM.PaintTiles.Reverse()
                                            where tvmPatt.PatternIndex.Equals(i) &&
                                                  tvmPatt.PatternFlipping.Equals(tvm.UserFlipping)
                                            select tvmPatt.PatternIndex);
                                    }
                                }

                                break;
                            }
                        }

                        break;
                }

                foreach (TileViewModel tvmPatt in mainWindowVM.PaintTiles.Reverse()) {
                    if (toBan.Contains(tvmPatt.PatternIndex)) {
                        int curPattIdx = getDescrambledIndex(tvmPatt.PatternIndex);

                        bool stopLoop = false;
                        for (int x = 0; x < outputWidth; x++) {
                            for (int y = 0; y < outputHeight; y++) {
                                if (!stopLoop) {
                                    try {
                                        dbPropagator.getWP().InternalBan(x * outputWidth + y, curPattIdx);
                                    } catch (TargetException) {
                                        // Accidental double call to this function, pattern is already banned
                                        // Caused by a force updating of the transformation on button click AND solution restart
                                        stopLoop = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Other

    /*
     * Functions
     */

    /// <summary>
    /// Actually progress in the algorithm given a number of steps.
    /// </summary>
    /// 
    /// <param name="steps">Amount of steps to progress</param>
    /// 
    /// <returns>New output bitmap with the steps progressed</returns>
    /// 
    /// <exception cref="TimeoutException">Exception caused by the algorithm taking too long and stalling</exception>
    private (WriteableBitmap, bool) runWfcDB(int steps = 1) {
        Resolution dbStatus = Resolution.UNDECIDED;

        if (!isOverlappingModel() && !mainWindowVM.IsRunning) {
            updateTransformations();
        }

        if (steps == -1) {
            for (int retry = 0; retry < 10; retry++) {
                dbStatus = dbPropagator.run();
                if (dbStatus.Equals(Resolution.TIMEOUT)) {
                    throw new TimeoutException();
                }
            }
        } else {
            for (int i = 0; i < steps; i++) {
                for (int retry = 0; retry < 10; retry++) {
                    dbStatus = dbPropagator.step();

                    if (dbStatus.Equals(Resolution.DECIDED) || dbStatus.Equals(Resolution.UNDECIDED)) {
                        retry = 10;
                    }
                }

                actionsTaken++;
            }
        }


        return (getLatestOutputBM(), dbStatus == Resolution.DECIDED);
    }

    /// <summary>
    /// Revert a set number of steps in the algorithm, also called backtracking
    /// </summary>
    /// 
    /// <param name="steps">Amount of steps to backtrack</param>
    /// 
    /// <returns>Updated output image</returns>
    public WriteableBitmap stepBackWfc(int steps = 1) {
        isCollapsed();
        for (int i = 0; i < steps; i++) {
            dbPropagator.doBacktrack();
            actionsTaken--;
        }

        centralManager.getMainWindowVM().ItemOverlay = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        return getLatestOutputBM();
    }

    /// <summary>
    /// Apply the painted mask to the output to exclude certain areas of interest
    /// </summary>
    /// 
    /// <param name="colors">User created mask</param>
    public async Task handlePaintBrush(Color[,] colors) {
        _isBrushing = true;
        int imageWidth = mainWindowVM.ImageOutWidth, imageHeight = mainWindowVM.ImageOutHeight;
        if (isOverlappingModel()) {
            Dictionary<Tuple<int, int>, Color> newInputDict = new();

            await Task.Run(() => {
                for (int y = 0; y < imageHeight; y++) {
                    for (int x = 0; x < imageWidth; x++) {
                        if (colors[x, y].Equals(Colors.Green)) {
                            Color toSet = getOverlappingOutputAt(x, y);

                            if (toSet.A == 255) {
                                newInputDict.Add(new Tuple<int, int>(x, y), toSet);
                            }
                        }
                    }
                }
            });

            await centralManager.getInputManager().restartSolution("Paint Brush", true);
            int oldInputDictSize = newInputDict.Count;

            await Task.Delay(1000);

            await Task.Run(() => {
                while (newInputDict.Count > 0) {
                    foreach ((Tuple<int, int> key, Color toSet) in
                             new Dictionary<Tuple<int, int>, Color>(newInputDict)) {
                        int idx = -1;
                        foreach (TileViewModel tvm in
                                 toAddPaint.Where(tvm => tvm.PatternColour.Equals(toSet))) {
                            idx = tvm.PatternIndex;
                            break;
                        }

                        bool? success = centralManager?.getInputManager()
                            .processClick(key.Item1, key.Item2, imageWidth, imageHeight, idx, true).Result;

                        if (success != null && (bool) success) {
                            newInputDict.Remove(key);
                        }
                    }

                    if (oldInputDictSize.Equals(newInputDict.Count)) {
#if DEBUG
                        Trace.WriteLine("\nAYOAYOAYO\n");
#endif
                        break;
                    }

                    oldInputDictSize = newInputDict.Count;
                }

                _isBrushing = false;
                mainWindowVM.setLoading(false);
            });
        } else {
            Dictionary<Tuple<int, int>, int> newInputDict = new();
            await Task.Run(() => {
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < imageHeight; y++) {
                    for (int x = 0; x < imageWidth; x++) {
                        int value = dbOutput.get(x, y);
                        bool isCollapsed = value >= 0;

                        if (isCollapsed && colors[x, y].Equals(Colors.Green)) {
                            newInputDict.Add(new Tuple<int, int>(x, y), value);
                        }
                    }
                }
            });

            await centralManager.getInputManager().restartSolution("Paint Brush", true);
            int oldInputDictSize = newInputDict.Count;
            await Task.Delay(1);

            await Task.Run(() => {
                while (newInputDict.Count > 0) {
                    foreach ((Tuple<int, int> key, int value) in new Dictionary<Tuple<int, int>, int>(newInputDict)) {
                        bool? success = centralManager?.getInputManager()
                            .processClick(key.Item1, key.Item2, imageWidth, imageHeight, value, true).Result;

                        if (success != null && (bool) success) {
                            newInputDict.Remove(key);
                        }
                    }

                    if (oldInputDictSize.Equals(newInputDict.Count)) {
#if DEBUG
                        Trace.WriteLine("\nAYOAYOAYO\n");
#endif
                        break;
                    }

                    oldInputDictSize = newInputDict.Count;
                }

                _isBrushing = false;
                mainWindowVM.setLoading(false);
            });
        }

        mainWindowVM.OutputImage = getLatestOutputBM();
        centralManager.getInputManager().placeMarker(false);
    }

    /// <summary>
    /// Set a given value on a given cell, usually used for painting cells
    /// </summary>
    /// 
    /// <param name="a">X Coordinate</param>
    /// <param name="b">Y Coordinate</param>
    /// <param name="toSet">Value to set at the coordinate</param>
    /// <param name="hover">Whether this is a hover operation or actual click operation</param>
    /// <param name="returnTrueAlreadyCorrect">Whether to return success if the clicked location already contains the
    /// desired value</param>
    /// 
    /// <returns>Updated output image and whether the output has converged</returns>
    public async Task<(WriteableBitmap?, bool?)> setTile(int a, int b, int toSet, bool hover,
        bool returnTrueAlreadyCorrect = false) {
        Resolution status;
        bool paintOverwrite = !hover && mainWindowVM.PaintingVM.IsPaintOverrideEnabled;
#if DEBUG
        const bool internalDebug = false;
        if (internalDebug) {
            Trace.WriteLine("");
        }
#endif

        if (isOverlappingModel()) {
            #region Overlapping Tile Selection

            try {
                dbPropagator.TileCoordToPatternCoord(a, b, 0, out int _, out int _, out int _, out int _);
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

            if ((possibleTiles.Count == 1 || !possibleTiles.Contains(c)) && !paintOverwrite) {
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

            if (paintOverwrite && (possibleTiles.Count == 1 || !possibleTiles.Contains(c))) {
                mainWindowVM.setLoading(true);

                Color[,] prevOutput = getPropagatorOutputO().toArray2d();
                int width = prevOutput.GetLength(0);
                int height = prevOutput.GetLength(1);

                Dictionary<Tuple<int, int>, Color> distinctList = new(), tempList = new();
                Dictionary<double, List<Tuple<int, int>>> distances = new();

                for (int i = 0; i < width; i++) {
                    for (int j = 0; j < height; j++) {
                        Tuple<int, int> key = new(i, j);

                        Color value = getOverlappingOutputAt(i, j);
                        Color? toReSet = currentColors!.Contains(value) ? value : null;

                        if (toReSet != null) {
                            tempList[key] = value;
                            double dist = (a - i) * (a - i) + (b - j) * (b - j);

                            if (!distances.ContainsKey(dist)) {
                                distances[dist] = new List<Tuple<int, int>>();
                            }

                            distances[dist].Add(key);
                        }
                    }
                }

                foreach ((double _, List<Tuple<int, int>> keys) in distances.OrderBy(key => key.Key)) {
                    foreach (Tuple<int, int> key in keys) {
                        distinctList[key] = tempList[key];
                    }
                }

                await centralManager.getInputManager().restartSolution("Override click", true, true);
                await Task.Delay(10);

                IEnumerable<Tile> tilesToSelect = tiles.toArray2d().Cast<Tile>()
                    .Where(tile => ((Color) tile.Value).Equals(c));
                dbPropagator.select(a, b, 0, tilesToSelect);

                for (int retries = 0; retries < 3; retries++) {
                    foreach ((Tuple<int, int> key, Color value) in
                             new Dictionary<Tuple<int, int>, Color>(distinctList)) {
                        int idx = -1;
                        foreach (TileViewModel tvm in
                                 toAddPaint.Where(tvm => tvm.PatternColour.Equals(value))) {
                            idx = tvm.PatternIndex;
                            break;
                        }

                        if (idx != -1) {
                            bool? successfulSet;
                            try {
                                (_, successfulSet)
                                    = setTile(key.Item1, key.Item2, idx, true, true).Result;
                            } catch (TargetException) {
                                continue;
                            }

                            if (successfulSet != null && (bool) successfulSet) {
                                distinctList.Remove(key);
                            }
                        }
                    }
                }

                mainWindowVM.setLoading(false);

                for (int i = 0; i < distinctList.Count; i++) {
                    centralManager.getInputManager().advanceStep();
                }

                mainWindowVM.OutputImage = getLatestOutputBM();
                centralManager.getInputManager().placeMarker(false);
                return (returnTrueAlreadyCorrect ? null : getLatestOutputBM(), true);
            } else {
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
                    Trace.WriteLine(
                        $@"Overlapping CONTRADICTION: We want to paint at ({a}, {b}) (({px}, {py})) with Tile {toSet}, Available patterns: ");
#endif
                    stepBackWfc();
                    return (null, false);
                }
            }

            return (returnTrueAlreadyCorrect ? null : getLatestOutputBM(), true);

            #endregion
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

            if ((availableAtLoc.Count == 1 || !availableAtLoc.Contains(descrambledIndex)) && !paintOverwrite) {
#if DEBUG
                if (internalDebug) {
                    Trace.WriteLine(availableAtLoc.Count == 1
                        ? "Returning because already collapsed"
                        : "Returning because not allowed");
                }
#endif

                bool hasItself = availableAtLoc.Count == 1 && availableAtLoc.Contains(descrambledIndex);
                return (null, returnTrueAlreadyCorrect ? hasItself : null);
            }

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine(paintOverwrite ? "Painting override enabled" : "Painting is allowed, continuing");
            }
#endif

            if (paintOverwrite && (availableAtLoc.Count == 1 || !availableAtLoc.Contains(descrambledIndex))) {
                mainWindowVM.setLoading(true);

                int[,] prevOutput = getPropagatorOutputA().toArray2d();
                int width = prevOutput.GetLength(0);
                int height = prevOutput.GetLength(1);
                Dictionary<Tuple<int, int>, int> distinctList = new();
                for (int i = 0; i < width; i++) {
                    for (int j = 0; j < height; j++) {
                        Tuple<int, int> key = new(i, j);
                        int value = prevOutput[i, j];
                        bool isCollapsed = value >= 0;

                        if (isCollapsed) {
                            distinctList[key] = value;
                        }
                    }
                }

                await centralManager.getInputManager().restartSolution("Override click", true, true);
                await Task.Delay(1);

                fsqAdj.Enqueue(new Tuple<int, int, int>(a, b, toSet));

                foreach ((int xLoc, int yLoc, int value) in fsqAdj.toList()) {
                    await setTile(xLoc, yLoc, value, true);
                }

                try {
                    await Task.Run(() => {
                        foreach ((Tuple<int, int> key, int value) in
                                 new Dictionary<Tuple<int, int>, int>(distinctList)) {
#pragma warning disable CS4014
                            setTile(key.Item1, key.Item2, value, true, true);
#pragma warning restore CS4014
                        }

                        mainWindowVM.setLoading(false);
                    });
                } catch (TargetException) {
                    mainWindowVM.OutputImage = centralManager.getInputManager().getNoResBM();
                    return (null, false);
                }

                mainWindowVM.OutputImage = getLatestOutputBM();
                centralManager.getInputManager().placeMarker(false);

                status = dbPropagator.Status;
            } else {
                try {
                    status = dbPropagator.selWith(a, b, descrambledIndex);
                } catch (TargetException) {
                    mainWindowVM.OutputImage = centralManager.getInputManager().getNoResBM();
                    return (null, false);
                }
            }

#if DEBUG
            if (internalDebug) {
                Trace.WriteLine($@"Proceeded with status: {status}");
            }
#endif

            if (status.Equals(Resolution.CONTRADICTION)) {
#if DEBUG
                Trace.WriteLine(
                    $@"Adjacency CONTRADICTION: We want to paint at ({a}, {b}) with Tile Idx:{toSet} Descrambled:{descrambledIndex}, Available patterns: {string.Join(", ", availableAtLoc)}");
#endif
                stepBackWfc();

                return (null, false);
            }

            return (returnTrueAlreadyCorrect ? null : getLatestOutputBM(), true);

            #endregion
        }
    }

    /// <summary>
    /// Generate a new image for the overlapping model
    /// </summary>
    /// 
    /// <param name="outputBitmap">Output: Image</param>
    /// <param name="grid">Whether to add a grid instead of transparency</param>
    private void generateOverlappingBitmap(out WriteableBitmap outputBitmap, bool grid) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth, outputHeight), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);
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

                    Color toSet = possibleTiles.Count == 1 ? possibleTiles.First() :
                        currentColors!.Contains(c) ? c :
                        grid ? (x + y) % 2 == 0 ? Color.Parse("#11000000") : Color.Parse("#00000000") :
                        Color.Parse("#00000000");
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

    /// <summary>
    /// Generate a new image for the adjacent model
    /// </summary>
    /// 
    /// <param name="outputBitmap">Output: Image</param>
    /// <param name="grid">Whether to add a grid instead of transparency</param>
    private void generateAdjacentBitmap(out WriteableBitmap outputBitmap, bool grid) {
        int collapsedTiles = 0;
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        outputBitmap = new WriteableBitmap(new PixelSize(outputWidth * tileSize, outputHeight * tileSize),
            new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);
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

    /// <summary>
    /// Reset the weights of all tiles.
    /// </summary>
    /// 
    /// <param name="updateStaticWeight">Whether to skip the update of the dynamic weights, reverting to static</param>
    /// <param name="force">Whether to force the resetting </param>
    public void resetWeights(bool updateStaticWeight = true, bool force = false) {
        int xDim = mainWindowVM.ImageOutWidth, yDim = mainWindowVM.ImageOutHeight;
        foreach (TileViewModel tileViewModel in mainWindowVM.PatternTiles) {
            double oldWeight = originalWeights[tileViewModel.RawPatternIndex];
            if (updateStaticWeight) {
                tileViewModel.PatternWeight = oldWeight;
                tileViewModel.RotateDisabled = false;
                tileViewModel.FlipDisabled = false;
            }

            if (force || tileViewModel.WeightHeatMap.Length == 0 ||
                tileViewModel.WeightHeatMap.Length != xDim * yDim) {
                tileViewModel.WeightHeatMap = new double[xDim, yDim];

                for (int i = 0; i < xDim; i++) {
                    for (int j = 0; j < yDim; j++) {
                        tileViewModel.WeightHeatMap[i, j] = oldWeight;
                    }
                }

                tileViewModel.DynamicWeight = false;
            }
        }
    }
}