using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using miWFC.DeBroglie.Topo;
using miWFC.ViewModels;
using miWFC.ViewModels.Structs;
using miWFC.Views;
using static miWFC.Utils.Util;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace miWFC.Managers;

/// <summary>
/// Input Manager of the application
/// </summary>
public class InputManager {
    private readonly CentralManager centralManager;
    private readonly MainWindow mainWindow;
    private readonly MainWindowViewModel mainWindowVM;

    private readonly Bitmap noResultFoundBM;
    private readonly Stack<int> savePoints;

    private int lastPaintedAmountCollapsed;

    private int lastProcessedX = -1, lastProcessedY = -1;

    private Color[,] maskColours;

    private DispatcherTimer timer = new();

    /*
     * Initializing Functions & Constructor
     */

    public InputManager(CentralManager parent) {
        centralManager = parent;
        mainWindowVM = centralManager.GetMainWindowVM();
        mainWindow = centralManager.GetMainWindow();

        maskColours = new Color[0, 0];

        noResultFoundBM = new Bitmap(AppContext.BaseDirectory + "/Assets/NoResultFound.png");
        savePoints = new Stack<int>();
        savePoints.Push(0);

        mainWindow.GetInputControl().SetCategories(GetCategories("overlapping")
            .Select(cat => new HoverableTextViewModel(cat, GetDescription(cat))).ToArray());

        string[] inputImageDataSource = GetModelImages("overlapping", "Textures");
        mainWindow.GetInputControl().SetInputImages(inputImageDataSource);

        (int[] patternSizeDataSource, int i) = GetImagePatternDimensions(inputImageDataSource[0]);
        mainWindow.GetInputControl().SetPatternSizes(patternSizeDataSource, i);
#pragma warning disable CS4014
        RestartSolution("Constructor");
#pragma warning restore CS4014
        centralManager.GetUIManager().UpdateInstantCollapse(1);

        lastPaintedAmountCollapsed = 0;
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    // Images

    // Objects

    /// <summary>
    /// Get the default image shown to the user if the algorithm contradicts
    /// </summary>
    /// 
    /// <returns>Bitmap</returns>
    public Bitmap GetNoResBm() {
        return noResultFoundBM;
    }

    // Lists

    /// <summary>
    /// Get the colours of the user drawn mask
    /// </summary>
    /// 
    /// <returns>Colour matrix</returns>
    public Color[,] GetMaskColours() {
        return maskColours;
    }

    // Other

    /*
     * Functionality
     */

    /// <summary>
    /// Restart the output, resetting all values to their defaults
    /// </summary>
    /// 
    /// <param name="source">DEBUG: Source of the restart</param>
    /// <param name="force">Whether the restart should be forced, </param>
    /// <param name="keepOutput">Whether the reset output should be shown to the user</param>
    public async Task RestartSolution(string source, bool force = false, bool keepOutput = false) {
#if DEBUG
        Trace.WriteLine(@$"Restarting -> {source}");
#endif
        if (centralManager.GetWFCHandler().IsChangingModels() || centralManager.GetWFCHandler().IsChangingImages()) {
            return;
        }

        if (!mainWindow.IsWindowTriggered() && !source.Equals("Window activation")) {
            return;
        }

        mainWindowVM.Markers = new ObservableCollection<MarkerViewModel>();

        while (savePoints.Count > 0) {
            savePoints.Pop();
        }

        lastPaintedAmountCollapsed = 0;

        maskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];
        mainWindowVM.OutputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        if (!GetModelImages(centralManager.GetWFCHandler().IsOverlappingModel() ? "overlapping" : "simpletiled",
                    mainWindow.GetInputControl().GetCategory())
                .Contains(mainWindow.GetInputControl().GetInputImage())) {
            return;
        }

        try {
            int stepAmount = mainWindowVM.StepAmount;
            WriteableBitmap newOutputWb = (await centralManager.GetWFCHandler()
                .RunWFCAlgorithm(true, stepAmount == 100 && !keepOutput ? -1 : 0, force)).Item1;
            if (!keepOutput) {
                mainWindowVM.OutputImage = newOutputWb;
            }
        } catch (InvalidOperationException) {
            // Error caused by multithreading which will be ignored
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    /// <summary>
    /// Advance a single step in the generation of the algorithm
    /// </summary>
    public async void AdvanceStep() {
        if (centralManager.GetWFCHandler().GetAmountCollapsed()
            .Equals(mainWindowVM.ImageOutWidth * mainWindowVM.ImageOutHeight)) {
            centralManager.GetUIManager().DispatchError(mainWindow);
            return;
        }

        try {
            (double currentWidth, double currentHeight) = centralManager.GetWFCHandler().GetPropagatorSize();

            bool weightReset = false;
            if (!mainWindowVM.IsRunning) {
                centralManager.GetWFCHandler().UpdateWeights();
                await RestartSolution("Advance step");
                weightReset = true;
            }

            (WriteableBitmap result2, bool _) = await centralManager.GetWFCHandler()
                .RunWFCAlgorithm(
                    mainWindowVM.ImageOutWidth != (int) currentWidth ||
                    mainWindowVM.ImageOutHeight != (int) currentHeight || weightReset, mainWindowVM.StepAmount);
            mainWindowVM.OutputImage = result2;
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    /// <summary>
    /// Revert a single step in the generation of the algorithm
    /// </summary>
    public void RevertStep() {
        try {
            int prevAmountCollapsed = centralManager.GetWFCHandler().GetAmountCollapsed();
            if (prevAmountCollapsed == 0) {
                return;
            }

            if (mainWindowVM.Markers.Count > 0) {
                MarkerViewModel curMarker = mainWindowVM.Markers[savePoints.Count - 1];
                bool canReturn
                    = Math.Abs(centralManager.GetWFCHandler().GetPercentageCollapsed() -
                               curMarker.MarkerCollapsePercentage) >
                    0.00001d || curMarker.Revertible;
                if (!canReturn) {
                    centralManager.GetUIManager().DispatchError(mainWindow);
                    return;
                }
            }

            int loggedAT = centralManager.GetWFCHandler().GetActionsTaken();

            Bitmap? avaloniaBitmap = null;
            int curStep = centralManager.GetWFCHandler().GetAmountCollapsed();

            while (centralManager.GetWFCHandler().GetAmountCollapsed() >
                   prevAmountCollapsed - mainWindowVM.StepAmount) {
                for (int i = 0; i < mainWindowVM.StepAmount; i++) {
                    curStep = centralManager.GetWFCHandler().GetAmountCollapsed();

                    if (savePoints.Count != 0 && curStep <= savePoints.Peek()) {
                        foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                            if (marker.MarkerIndex.Equals(savePoints.Count - 1)) {
                                if (!marker.Revertible) {
                                    goto exitOuterLoop;
                                }

                                mainWindowVM.Markers.Remove(marker);
                                savePoints.Pop();
                            }
                        }
                    }

                    avaloniaBitmap = centralManager.GetWFCHandler().StepBackWfc();
                }
            }

            exitOuterLoop:

            mainWindowVM.OutputImage = avaloniaBitmap!;

            centralManager.GetWFCHandler().SetActionsTaken(loggedAT - 1);

            if (savePoints.Count != 0 && curStep < savePoints.Peek()) {
                savePoints.Pop();
                int toRemove = savePoints.Count;
                foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                    if (marker.MarkerIndex.Equals(toRemove)) {
                        mainWindowVM.Markers.Remove(marker);
                    }
                }
            }
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    /// <summary>
    /// Reset the user editable mask to its defaults
    /// </summary>
    public void ResetMask() {
        maskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];

        if (centralManager.GetMainWindowVM().PaintingVM.TemplateCreationModeEnabled) {
            for (int x = 0; x < centralManager.GetMainWindowVM().ImageOutWidth; x++) {
                for (int y = 0; y < centralManager.GetMainWindowVM().ImageOutHeight; y++) {
                    maskColours[x, y] = Colors.Black;
                }
            }
        }
    }

    /// <summary>
    /// Place a marker or save point onto the timeline with the current state of the algorithm
    /// </summary>
    /// 
    /// <param name="revertible">Whether the user can revert into the history prior to the placement of this marker</param>
    /// <param name="force">Whether the placement of this marker should be forced</param>
    public void PlaceMarker(bool revertible = true, bool force = false) {
        if (centralManager.GetWFCHandler().IsCollapsed() && !force) {
            return;
        }

        int curStep = centralManager.GetWFCHandler().GetAmountCollapsed();
        if (curStep == 0 && !force) {
            return;
        }

        if (mainWindowVM.Markers.Count > 0 && mainWindowVM.Markers[^1].MarkerCollapsePercentage
                .Equals(centralManager.GetWFCHandler().GetPercentageCollapsed())) {
            return;
        }

        mainWindowVM.Markers.Add(new MarkerViewModel(savePoints.Count,
            (mainWindow.IsVisible
                ? mainWindow.GetOutputControl().GetTimelineWidth()
                : centralManager.GetPaintingWindow().GetTimelineWidth()) *
            centralManager.GetWFCHandler().GetPercentageCollapsed() +
            1, centralManager.GetWFCHandler().GetPercentageCollapsed(),
            revertible));

        while (true) {
            if (savePoints.Count != 0 && curStep < savePoints.Peek()) {
                savePoints.Pop();
                int toRemove = savePoints.Count;
                foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                    if (marker.MarkerIndex.Equals(toRemove)) {
                        mainWindowVM.Markers.Remove(marker);
                    }
                }
            } else {
                if (!savePoints.Contains(curStep)) {
                    savePoints.Push(curStep);
                }

                return;
            }
        }
    }

    /// <summary>
    /// Load to the first available marker unless the current marker is non-revertible
    /// </summary>
    public void LoadMarker() {
        int prevAmountCollapsed = savePoints.Count == 0 ? 0 : savePoints.Peek();

        bool skipPop = false;
        if (savePoints.Count != 0) {
            if (!mainWindowVM.Markers[savePoints.Count - 1].Revertible) {
                if (centralManager.GetWFCHandler().GetAmountCollapsed() == prevAmountCollapsed) {
                    centralManager.GetUIManager().DispatchError(mainWindow);
                    return;
                }

                skipPop = true;
            }
        }

        if (!skipPop && centralManager.GetWFCHandler().GetAmountCollapsed() == prevAmountCollapsed &&
            prevAmountCollapsed != lastPaintedAmountCollapsed && savePoints.Count != 0) {
            savePoints.Pop();

            foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                if (marker.MarkerIndex.Equals(savePoints.Count)) {
                    mainWindowVM.Markers.Remove(marker);
                }
            }

            prevAmountCollapsed = savePoints.Count == 0 ? 0 : savePoints.Peek();
        }

        if (lastPaintedAmountCollapsed != 0 &&
            prevAmountCollapsed == centralManager.GetWFCHandler().GetAmountCollapsed()) {
            centralManager.GetUIManager().DispatchError(mainWindow);
            return;
        }

        try {
            while (centralManager.GetWFCHandler().GetAmountCollapsed() > prevAmountCollapsed) {
                mainWindowVM.OutputImage = centralManager.GetWFCHandler().StepBackWfc();
            }
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    /// <summary>
    /// Allow the user to import a pre-existing solution into the application
    /// </summary>
    public async void ImportSolution() {
        OpenFileDialog ofd = new() {
            Title = @"Select png solution to import",
            Filters = new List<FileDialogFilter> {
                new() {
                    Extensions = new List<string> {"png"},
                    Name = "PNG Image (*.png)"
                }
            }
        };

        string[]? ofdResults = await ofd.ShowAsync(new Window());
        if (ofdResults != null) {
            if (ofdResults.Length == 0) {
                return;
            }

            string worldFile = ofdResults[0];

            MemoryStream ms = new(await File.ReadAllBytesAsync(worldFile));
            WriteableBitmap inputBitmap = WriteableBitmap.Decode(ms);

            int inputImageWidth = (int) inputBitmap.Size.Width, inputImageHeight = (int) inputBitmap.Size.Height;

            int tileSize = centralManager.GetWFCHandler().IsOverlappingModel()
                ? 1
                : centralManager.GetWFCHandler().GetTileSize();
            int maxImageSize = 128 * tileSize;

            if (inputImageWidth > maxImageSize || inputImageWidth < 10 || inputImageHeight > maxImageSize ||
                inputImageHeight < 10 || inputImageWidth % tileSize != 0 || inputImageHeight % tileSize != 0) {
                centralManager.GetUIManager().DispatchError(mainWindow);
                // TODO Popup telling user input image was not allowed size
                return;
            }

            int imageWidth = inputImageWidth / tileSize;
            int imageHeight = inputImageHeight / tileSize;

            mainWindowVM.ImageOutWidth = imageWidth;
            mainWindowVM.ImageOutHeight = imageHeight;
            mainWindowVM.StepAmount = 1;

            await RestartSolution("Imported image", true);
            (Color[][] colourArray, HashSet<Color> allowedColours) = ImageToColourArray(inputBitmap);

            if (centralManager.GetWFCHandler().IsOverlappingModel()) {
                bool allowed = allowedColours
                    .Select(foundColour => !foundColour.A.Equals(255) || centralManager.GetMainWindowVM().PaintTiles
                        .Any(tvm => tvm.PatternColour.Equals(foundColour)))
                    .Aggregate(true, (current, found) => current && found);

                if (!allowed) {
                    centralManager.GetUIManager().DispatchError(mainWindow);
                    // TODO Popup telling user input image was not correct colours
                    return;
                }
            }

            ObservableCollection<TileViewModel> paintTiles = centralManager.GetMainWindowVM().PaintTiles;

            if (centralManager.GetWFCHandler().IsOverlappingModel()) {
                await ImportOverlappingImage(imageWidth, imageHeight, colourArray, paintTiles);
            } else {
                await ImportAdjacentImage(imageWidth, imageHeight, worldFile, paintTiles);
            }

            mainWindowVM.OutputImage = centralManager.GetWFCHandler().GetLatestOutputBm();
            PlaceMarker(false, true);
        }
    }

    /// <summary>
    /// Handle the importing of an overlapping mode image
    /// </summary>
    /// 
    /// <param name="imageWidth">Imported image width</param>
    /// <param name="imageHeight">Imported image height</param>
    /// <param name="colourArray">Array of colours of the imported image</param>
    /// <param name="paintTiles">Tiles allowed to be placed by the algorithm</param>
    private async Task ImportOverlappingImage(int imageWidth, int imageHeight, IReadOnlyList<Color[]> colourArray,
        IReadOnlyCollection<TileViewModel> paintTiles) {
        await RestartSolution("Import weights update", true);

        for (int retry = 0; retry < 3; retry++) {
            bool noTransparentEncounter = true;
            for (int x = 0; x < imageWidth; x++) {
                for (int y = 0; y < imageHeight; y++) {
                    Color toPaint = colourArray[y][x];
                    Color foundAtPos = centralManager.GetWFCHandler().GetOverlappingOutputAt(x, y);

                    if (!toPaint.Equals(foundAtPos) && toPaint.A.Equals(255) && foundAtPos.A.Equals(0)) {
                        int idx = -1;
                        foreach (TileViewModel tvm in
                                 paintTiles.Where(tvm => tvm.PatternColour.Equals(toPaint))) {
                            idx = tvm.PatternIndex;
                            break;
                        }

                        bool? success = ProcessClick(x, y, imageWidth, imageHeight, idx, true).Result;

                        if (success != null && !(bool) success) {
                            centralManager.GetUIManager().DispatchError(mainWindow);
                            // TODO Popup telling user input image was not following patterns?
                            await RestartSolution("Imported image failure (pattern)", true);
                            return;
                        }
                    } else if (!foundAtPos.A.Equals(0) && toPaint.A.Equals(255) &&
                               !toPaint.Equals(foundAtPos) && retry == 0) {
                        centralManager.GetUIManager().DispatchError(mainWindow);
                        // TODO Popup telling user input image is illegal?
                        await RestartSolution("Imported image failure (illegal)", true);
                        return;
                    }

                    noTransparentEncounter = noTransparentEncounter &&
                                             centralManager.GetWFCHandler().GetOverlappingOutputAt(x, y).A
                                                 .Equals(255);
                }
            }

            if (noTransparentEncounter) {
                break;
            }
        }
    }

    /// <summary>
    /// Handle the importing of an adjacent mode image
    /// </summary>
    /// 
    /// <param name="imageWidth">Imported image width</param>
    /// <param name="imageHeight">Imported image height</param>
    /// <param name="worldFile">Source URI of the world file to read weights</param>
    /// <param name="paintTiles">Tiles allowed to be placed by the algorithm</param>
    private async Task ImportAdjacentImage(int imageWidth, int imageHeight, string worldFile,
        IReadOnlyCollection<TileViewModel> paintTiles) {
        byte[] input = await File.ReadAllBytesAsync(worldFile);
        // The image was appended the collapse data and the weights.
        IEnumerable<byte> trimmedInputB
            = input.Skip(Math.Max(0, input.Length - imageWidth * imageHeight - paintTiles.Count));
        byte[] inputBUntrimmed = trimmedInputB as byte[] ?? trimmedInputB.ToArray();
        Split(inputBUntrimmed, imageWidth * imageHeight, out byte[] inputB, out byte[] inputW);

        try {
            for (int x = 0; x < imageWidth; x++) {
                for (int y = 0; y < imageHeight; y++) {
                    int toSel = inputB[x * imageHeight + y] - 2;
                    int foundAtPos = centralManager.GetWFCHandler().GetPropagatorOutputA().toArray2d()[x, y];

                    if (toSel != foundAtPos && foundAtPos == -1 && toSel != -1) {
                        bool? success = ProcessClick(x, y, imageWidth, imageHeight, toSel, true).Result;

                        if (success != null && !(bool) success) {
                            centralManager.GetUIManager().DispatchError(mainWindow);
                            // TODO Popup telling user input image was not following patterns?
                            await RestartSolution("Imported image failure (pattern)", true);
                            return;
                        }
                    } else if (foundAtPos != -1 && toSel != foundAtPos) {
                        centralManager.GetUIManager().DispatchError(mainWindow);
                        // TODO Popup telling user input image is illegal?
                        await RestartSolution("Imported image failure (illegal)", true);
                        return;
                    }
                }
            }

            centralManager.GetMainWindowVM().SetWeights(inputW.Select(x => (double) x).ToArray());
        } catch (AggregateException exception) {
            // TODO pre processing?
            centralManager.GetUIManager().DispatchError(mainWindow);
            Trace.WriteLine(exception);
            // TODO Popup telling user input image is mismatching?
            await RestartSolution("Imported image failure (mismatch)", true);
        }
    }

    /// <summary>
    /// Allow the user to save the current output to their system
    /// </summary>
    public async void ExportSolution() {
        SaveFileDialog sfd = new() {
            Title = @"Select export location & file name",
            DefaultExtension = "png",
            Filters = new List<FileDialogFilter> {
                new() {
                    Extensions = new List<string> {"png"},
                    Name = "PNG Image (*.png)"
                }
            }
        };

        string? settingsFileName = await sfd.ShowAsync(new Window());
        if (settingsFileName != null) {
            bool hasItems = mainWindow.GetInputControl().GetCategory().Equals("Worlds Top-Down")
                            && centralManager.GetWFCHandler().IsCollapsed()
                            && centralManager.GetMainWindowVM().ItemVM.ItemDataGrid.Count > 0;

            if (hasItems) {
                centralManager.GetWFCHandler().GetLatestOutputBm(false)
                    .Save(settingsFileName.Replace(".png", "_worldLayer.png"));

                if (!centralManager.GetWFCHandler().IsOverlappingModel()) {
                    await AppendPictureData(settingsFileName.Replace(".png", "_worldLayer.png"),
                        centralManager.GetWFCHandler().GetPropagatorOutputA().toArray2d(), false);
                }

                Tuple<int, int>[,]? itemsGrid = mainWindowVM.ItemVM.GetLatestItemGrid();
                List<Tuple<int, int>> list = new();
                if (itemsGrid != null) {
                    for (int i = 0; i < itemsGrid.GetLength(0); i++) {
                        for (int j = 0; j < itemsGrid.GetLength(1); j++) {
                            list.Add(itemsGrid[i, j]);
                        }
                    }
                }

                if (list.Distinct().Count() > 1 ||
                    (!list.Distinct().Select(x => x.Item1).Contains(-1) && list.Distinct().Any())) {
                    GenerateRawItemImage(itemsGrid).Save(settingsFileName.Replace(".png", "_itemsLayer.png"));
                    CombineBitmaps(centralManager.GetWFCHandler().GetLatestOutputBm(false),
                            centralManager.GetWFCHandler().IsOverlappingModel()
                                ? 1
                                : centralManager.GetWFCHandler().GetTileSize(),
                            GetLatestItemBitMap(), 17, mainWindowVM.ImageOutWidth,
                            mainWindowVM.ImageOutHeight)
                        .Save(settingsFileName.Replace(".png", ".jpg"));
                }
            } else {
                centralManager.GetWFCHandler().GetLatestOutputBm(false).Save(settingsFileName);

                if (!centralManager.GetWFCHandler().IsOverlappingModel()) {
                    await AppendPictureData(settingsFileName,
                        centralManager.GetWFCHandler().GetPropagatorOutputA().toArray2d(), true);

                    await AppendPictureData(settingsFileName,
                        centralManager.GetMainWindowVM().PaintTiles.Select(x => (int) x.PatternWeight).ToArray(),
                        false);
                }
            }
        }
    }

    /// <summary>
    /// Upon clicking the animate button, a constant looping task is called every few milliseconds, until disabled or collapsed
    /// </summary>
    public async void Animate() {
        if (centralManager.GetWFCHandler().IsCollapsed()) {
            mainWindowVM.IsPlaying = false;
            centralManager.GetUIManager().DispatchError(mainWindow);
            return;
        }

        (double currentWidth, double currentHeight) = centralManager.GetWFCHandler().GetPropagatorSize();
        if (mainWindowVM.ImageOutWidth != (int) currentWidth ||
            mainWindowVM.ImageOutHeight != (int) currentHeight) {
            await RestartSolution("Animate");
        }

        if (!mainWindowVM.IsRunning && mainWindowVM.IsPlaying) {
            centralManager.GetWFCHandler().UpdateWeights();
            await RestartSolution("Animate");
        }

        bool startAnimation = mainWindowVM.IsPlaying;

        if (startAnimation) {
            lock (timer) {
                timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                timer.Tick += AnimationTimerTick!;
                timer.Start();
            }
        } else {
            lock (timer) {
                timer.Stop();
                timer = new DispatcherTimer();
            }
        }
    }

    /// <summary>
    /// The logic executed at each tick of the animation
    /// </summary>
    /// 
    /// <param name="sender">Origin of function call</param>
    /// <param name="e">EventArgs</param>
    private void AnimationTimerTick(object sender, EventArgs e) {
        lock (timer) {
            /* only work when this is no reentry while we are already working */
            if (timer.IsEnabled) {
                timer.Stop();
                timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                try {
                    (WriteableBitmap result2, bool finished) = centralManager.GetWFCHandler()
                        .RunWFCAlgorithm(false, mainWindowVM.StepAmount).Result;
                    mainWindowVM.OutputImage = result2;

                    if (finished) {
                        mainWindowVM.IsPlaying = false;
                        return;
                    }
                } catch (Exception exception) {
                    Trace.WriteLine(exception);
                    mainWindowVM.OutputImage = noResultFoundBM;
                }

                timer.Start();
            }
        }
    }

    /// <summary>
    /// Process the user clicking onto the output whilst painting
    /// </summary>
    /// 
    /// <param name="a">Click location on the x dimension</param>
    /// <param name="b">Click location on the y dimension</param>
    /// <param name="imgWidth">Width of the image the user clicked on</param>
    /// <param name="imgHeight">Height of the image the user clicked on</param>
    /// <param name="tileIdx">Index of the tile the user has selected to paint</param>
    /// <param name="skipUI">Whether to show the output affected by the user click to the user</param>
    /// 
    /// <returns>Task which is completed upon setting the user selected tile at the clicked location</returns>
    public async Task<bool?> ProcessClick(int a, int b, int imgWidth, int imgHeight, int tileIdx, bool skipUI = false) {
        if (!skipUI) {
            a = (int) Math.Floor(a * mainWindowVM.ImageOutWidth / (double) imgWidth);
            b = (int) Math.Floor(b * mainWindowVM.ImageOutHeight / (double) imgHeight);
        }

        (WriteableBitmap? bitmap, bool? showPixel)
            = await centralManager.GetWFCHandler().SetTile(a, b, tileIdx, false, skipUI);

        if (!skipUI && !mainWindowVM.PaintingVM.IsPaintOverrideEnabled) {
            if (showPixel != null && (bool) showPixel) {
                mainWindowVM.OutputImage = bitmap!;
                ProcessHoverAvailability(a, b, imgWidth, imgHeight, tileIdx, true, true);
            } else {
                mainWindowVM.OutputImage = centralManager.GetWFCHandler().GetLatestOutputBm();
            }
        }

        return showPixel;
    }

    /// <summary>
    /// Update the output hover image based on the user selected value
    /// </summary>
    /// 
    /// <param name="hoverX">Hover location on the x axis</param>
    /// <param name="hoverY">Hover location on the y axis</param>
    /// <param name="imgWidth">Width of the image the user clicked on</param>
    /// <param name="imgHeight">Height of the image the user clicked on</param>
    /// <param name="selectedValue">Index of the tile the user has selected to paint</param>
    /// <param name="pencilSelected">Whether the user has the pencil selected or a different tool</param>
    /// <param name="force">Whether to force the hover mask creation</param>
    public async void ProcessHoverAvailability(int hoverX, int hoverY, int imgWidth, int imgHeight, int selectedValue,
        bool pencilSelected, bool force = false) {
        int a = (int) Math.Floor(hoverX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(hoverY * mainWindowVM.ImageOutHeight / (double) imgHeight);

        if (lastProcessedX == a && lastProcessedY == b && !force) {
            return;
        }

        lastProcessedX = a;
        lastProcessedY = b;
        List<Color> availableAtLocC = new();
        List<int> availableAtLocI = new();
        bool? showPixel = false;

        if (pencilSelected) {
            try {
                if (centralManager.GetWFCHandler().IsOverlappingModel()) {
                    try {
                        availableAtLocC = centralManager.GetWFCHandler().GetAvailablePatternsAtLocation<Color>(a, b);
                    } catch (Exception) {
                        return;
                    }
                } else {
                    try {
                        availableAtLocI = centralManager.GetWFCHandler().GetAvailablePatternsAtLocation<int>(a, b);
                    } catch (Exception) {
                        return;
                    }
                }

                (WriteableBitmap? _, showPixel)
                    = await centralManager.GetWFCHandler().SetTile(a, b, selectedValue, true);
            } catch (IndexOutOfRangeException) {
                mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                    PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            }
        }

        if (showPixel != null && (bool) showPixel) {
            mainWindowVM.OutputPreviewMask = centralManager.GetWFCHandler().GetLatestOutputBm(false);
            centralManager.GetWFCHandler().StepBackWfc();
        } else {
            mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);
        }

        if (mainWindowVM.PaintingVM.PaintModeEnabled) {
            UpdateHoverBrushMask(a, b);
        } else if (mainWindowVM.PaintingVM.TemplateCreationModeEnabled) {
            UpdateHoverBrushMask(a, b, -1);
        } else if (mainWindowVM.PaintingVM.TemplatePlaceModeEnabled) {
            UpdateHoverTemplateMask(a, b, mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight);
        }

        mainWindowVM.HelperTiles.Clear();
        if (!mainWindowVM.PaintingVM.TemplateCreationModeEnabled && !mainWindowVM.PaintingVM.TemplatePlaceModeEnabled) {
            int selectedIndex = centralManager.GetPaintingWindow().GetSelectedPaintIndex();

            foreach (TileViewModel tvm in mainWindowVM.PaintTiles) {
                if (centralManager.GetWFCHandler().IsOverlappingModel()
                        ? availableAtLocC.Contains(centralManager.GetWFCHandler()
                            .GetColorFromIndex(tvm.PatternIndex))
                        : availableAtLocI.Contains(centralManager.GetWFCHandler().GetDescrambledIndex(tvm.PatternIndex))
                   ) {
                    tvm.Highlighted = tvm.PatternIndex == selectedIndex;
                    mainWindowVM.HelperTiles.Add(tvm);
                }
            }
        }
    }

    /// <summary>
    /// Update the actual mask values of the hover template
    /// </summary>
    /// 
    /// <param name="x">Hover location on the x axis</param>
    /// <param name="y">Hover location on the y axis</param>
    /// <param name="imgWidth">Width of the image the user clicked on</param>
    /// <param name="imgHeight">Height of the image the user clicked on</param>
    private void UpdateHoverTemplateMask(int x, int y, int imgWidth, int imgHeight) {
        int templateIndex = centralManager.GetPaintingWindow().GetSelectedTemplateIndex();
        if (templateIndex == -1) {
            return;
        }

        TemplateViewModel selectedTVM = centralManager.GetMainWindowVM().PaintingVM.Templates[templateIndex];
        int startX = x - selectedTVM.CenterPoint.Item1;
        int startY = y - selectedTVM.CenterPoint.Item2;

        int endX = startX + selectedTVM.Dimension.Item1;
        int endY = startY + selectedTVM.Dimension.Item2;

        Color[,] hoverMaskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];

        for (int placeY = startY; placeY < endY; placeY++) {
            for (int placeX = startX; placeX < endX; placeX++) {
                if (placeX >= 0 && placeY >= 0 && placeX < imgWidth && placeY < imgHeight) {
                    int mappedX = placeX - x + selectedTVM.CenterPoint.Item1,
                        mappedY = placeY - y + selectedTVM.CenterPoint.Item2;
                    if (centralManager.GetWFCHandler().IsOverlappingModel()) {
                        if (selectedTVM.TemplateDataO[mappedY, mappedX].A == 255) {
                            hoverMaskColours[placeX, placeY] = Colors.Gold;
                        }
                    } else {
                        if (selectedTVM.TemplateDataA[mappedX, mappedY] >= 0) {
                            hoverMaskColours[placeX, placeY] = Colors.Gold;
                        }
                    }
                }
            }
        }

        UpdateMask(hoverMaskColours, false);
    }

    /// <summary>
    /// Update the actual mask values of the hover template in the painting mode
    /// </summary>
    /// 
    /// <param name="a">Hover location on the x axis</param>
    /// <param name="b">Hover location on the y axis</param>
    /// <param name="overrideSize">Size of the brush, overwritten by the function if not set</param>
    private void UpdateHoverBrushMask(int a, int b, int overrideSize = -2) {
        int brushSize = overrideSize == -2 ? centralManager.GetPaintingWindow().GetPaintBrushSize() : overrideSize;

        Color[,] hoverMaskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];

        if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
            if (brushSize == -1) {
                hoverMaskColours[a, b] = Colors.Yellow;
            } else {
                for (int x = 0; x < mainWindowVM.ImageOutWidth; x++) {
                    for (int y = 0; y < mainWindowVM.ImageOutHeight; y++) {
                        double dx = (double) x - a;
                        double dy = (double) y - b;
                        double distanceSquared = dx * dx + dy * dy;

                        if (distanceSquared <= brushSize) {
                            hoverMaskColours[x, y] = Colors.Yellow;
                        }
                    }
                }
            }
        }

        UpdateMask(hoverMaskColours, false);
    }

    /// <summary>
    /// Update the mask upon clicking in the brushing mode(s)
    /// </summary>
    /// 
    /// <param name="clickX">Click location on the x axis</param>
    /// <param name="clickY">Click location on the y axis</param>
    /// <param name="imgWidth">Width of the image the user clicked on</param>
    /// <param name="imgHeight">Height of the image the user clicked on</param>
    /// <param name="add">Whether the additive or subtractive brush is selected</param>
    public void ProcessClickMask(int clickX, int clickY, int imgWidth, int imgHeight, bool add) {
        int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);

        int brushSize = centralManager.GetPaintingWindow().GetPaintBrushSize();

        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
            if (brushSize == -1) {
                maskColours[a, b] = add ? Colors.Green : Colors.Red;
                for (int x = 0; x < outputWidth; x++) {
                    for (int y = 0; y < outputHeight; y++) {
                        if (maskColours[x, y] == default) {
                            maskColours[x, y] = add ? Colors.Red : Colors.Green;
                        }
                    }
                }
            } else {
                for (int x = 0; x < outputWidth; x++) {
                    for (int y = 0; y < outputHeight; y++) {
                        double dx = (double) x - a;
                        double dy = (double) y - b;
                        double distanceSquared = dx * dx + dy * dy;

                        if (distanceSquared <= brushSize) {
                            maskColours[x, y] = add ? Colors.Green : Colors.Red;
                        } else if (maskColours[x, y] == default) {
                            maskColours[x, y] = add ? Colors.Red : Colors.Green;
                        }
                    }
                }
            }
        }

        UpdateMask(maskColours, true);
    }

    /// <summary>
    /// Update the mask when clicking in the template creation mode
    /// </summary>
    /// 
    /// <param name="clickX">Click location on the x axis</param>
    /// <param name="clickY">Click location on the y axis</param>
    /// <param name="imgWidth">Width of the image the user clicked on</param>
    /// <param name="imgHeight">Height of the image the user clicked on</param>
    /// <param name="add">Whether the user has decided to add or remove from the template outline</param>
    public void ProcessClickTemplateCreation(int clickX, int clickY, int imgWidth, int imgHeight, bool add) {
        int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);

        const int rawBrushSize = -1;
        if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
            if (rawBrushSize == -1) {
                maskColours[a, b] = add ? Colors.White : Colors.Black;
            }
        }

        UpdateMask(maskColours, true);
    }

    /// <summary>
    /// Update the output when clicking with a template onto the output
    /// </summary>
    /// 
    /// <param name="clickX">Click location on the x axis</param>
    /// <param name="clickY">Click location on the y axis</param>
    /// <param name="imgWidth">Width of the image the user clicked on</param>
    /// <param name="imgHeight">Height of the image the user clicked on</param>
    public async void ProcessClickTemplatePlace(int clickX, int clickY, int imgWidth, int imgHeight) {
        int placeCount = 0;
        bool error = false;

        try {
            int x = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
                y = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);

            int templateIndex = centralManager.GetPaintingWindow().GetSelectedTemplateIndex();
            if (templateIndex == -1) {
                return;
            }

            TemplateViewModel selectedTVM = centralManager.GetMainWindowVM().PaintingVM.Templates[templateIndex];
            int startX = x - selectedTVM.CenterPoint.Item1;
            int startY = y - selectedTVM.CenterPoint.Item2;
            int endX = startX + selectedTVM.Dimension.Item1;
            int endY = startY + selectedTVM.Dimension.Item2;

            for (int placeX = startX; placeX < endX; placeX++) {
                for (int placeY = startY; placeY < endY; placeY++) {
                    if (!error) {
                        if (placeX >= 0 && placeY >= 0 && placeX < imgWidth && placeY < imgHeight) {
                            int mappedX = placeX - x + selectedTVM.CenterPoint.Item1,
                                mappedY = placeY - y + selectedTVM.CenterPoint.Item2;
                            if (centralManager.GetWFCHandler().IsOverlappingModel()) {
                                Color toPlace = selectedTVM.TemplateDataO[mappedY, mappedX];
                                if (toPlace.A == 255) {
                                    int idx = centralManager.GetWFCHandler().GetColours()
                                        .TakeWhile(tileVM => !tileVM.PatternColour.Equals(toPlace)).Count();
                                    Color valAt = centralManager.GetWFCHandler().GetOverlappingOutputAt(placeX, placeY);
                                    if (valAt.A != 255) {
                                        (WriteableBitmap? writeableBitmap, bool? showPixel) = await centralManager
                                            .GetWFCHandler()
                                            .SetTile(placeX, placeY, idx, false);
                                        if (showPixel != null && (bool) showPixel) {
                                            mainWindowVM.OutputImage = writeableBitmap!;
                                            placeCount++;
                                        } else {
                                            error = true;
                                        }
                                    } else {
                                        error = !valAt.Equals(toPlace);
                                    }
                                }
                            } else {
                                int indexToPlace = selectedTVM.TemplateDataA[mappedX, mappedY];
                                if (indexToPlace > 0) {
                                    int valAt = centralManager.GetWFCHandler().GetPropagatorOutputA().toArray2d()[
                                        placeX,
                                        placeY];
                                    if (valAt.Equals(indexToPlace)) { } else if (valAt < 0) {
                                        (WriteableBitmap? writeableBitmap, bool? showPixel) = await centralManager
                                            .GetWFCHandler()
                                            .SetTile(placeX, placeY, indexToPlace, false);
                                        if (showPixel != null && (bool) showPixel) {
                                            mainWindowVM.OutputImage = writeableBitmap!;
                                            placeCount++;
                                        } else {
                                            error = true;
                                        }
                                    } else {
                                        error = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        } catch (Exception) {
            error = true;
        }

        if (error) {
            centralManager.GetWFCHandler().StepBackWfc(placeCount);
            mainWindowVM.OutputImage = centralManager.GetWFCHandler().GetLatestOutputBm();
            centralManager.GetUIManager().DispatchError(centralManager.GetPaintingWindow());
        }
    }

    /// <summary>
    /// Forwarding function with default values to updateMask(Color[,], bool, bool)
    /// </summary>
    public void UpdateMask() {
        UpdateMask(maskColours, true);
    }

    /// <summary>
    /// Create and apply image representation of the user created mask
    /// </summary>
    /// 
    /// <param name="colors">Mask colours</param>
    /// <param name="updateMain">Whether to update the main image or the preview image</param>
    /// <param name="invert">Whether to invert the colours found at each position</param>
    private void UpdateMask(Color[,] colors, bool updateMain) {
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        WriteableBitmap outputBitmap = CreateBitmapFromData(outputWidth, outputHeight, 1, (x, y) => {
            Color c = colors[x, y];
            if (c.Equals(Colors.Gold)) {
                return centralManager.GetWFCHandler().IsOverlappingModel() ? c : Colors.Cyan;
            }

            return c.Equals(Colors.White) ? Colors.Transparent : c;
        });

        if (updateMain) {
            mainWindowVM.OutputImageMask = outputBitmap;
        } else {
            mainWindowVM.OutputPreviewMask = outputBitmap;
        }
    }

    /// <summary>
    /// Reset the hovering mask
    /// </summary>
    public void ResetHoverAvailability() {
        mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        mainWindowVM.HelperTiles.Clear();
    }
}