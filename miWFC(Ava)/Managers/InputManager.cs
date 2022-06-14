using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using miWFC.DeBroglie.Topo;
using miWFC.ViewModels;
using miWFC.Views;
using static miWFC.Utils.Util;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace miWFC.Managers;

public class InputManager {
    private readonly MainWindow mainWindow;
    private readonly MainWindowViewModel mainWindowVM;

    private readonly Bitmap noResultFoundBM;

    private readonly CentralManager centralManager;
    private readonly Stack<int> savePoints;

    private int lastPaintedAmountCollapsed;

    private Color[,] maskColours;

    private DispatcherTimer timer = new();

    public InputManager(CentralManager parent) {
        centralManager = parent;
        mainWindowVM = centralManager.getMainWindowVM();
        mainWindow = centralManager.getMainWindow();

        maskColours = new Color[0, 0];

        noResultFoundBM = new Bitmap(AppContext.BaseDirectory + "/Assets/NoResultFound.png");
        savePoints = new Stack<int>();
        savePoints.Push(0);

        mainWindow.getInputControl().setCategories(getCategories("overlapping")
            .Select(cat => new HoverableTextViewModel(cat, getDescription(cat))).ToArray());

        string[] inputImageDataSource = getModelImages("overlapping", "Textures");
        mainWindow.getInputControl().setInputImages(inputImageDataSource);

        (int[] patternSizeDataSource, int i) = getImagePatternDimensions(inputImageDataSource[0]);
        mainWindow.getInputControl().setPatternSizes(patternSizeDataSource, i);

        restartSolution("Constructor");
        centralManager.getUIManager().updateInstantCollapse(1);

        lastPaintedAmountCollapsed = 0;
    }

    /*
     * Functionality
     */

    public async void restartSolution(string source, bool force = false, bool keepOutput = false) {
        if (centralManager.getWFCHandler().isChangingModels() || centralManager.getWFCHandler().isChangingImages()) {
            return;
        }

        if (!mainWindow.isWindowTriggered() && !source.Equals("Window activation")) {
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

        if (!getModelImages(centralManager.getWFCHandler().isOverlappingModel() ? "overlapping" : "simpletiled",
                    mainWindow.getInputControl().getCategory())
                .Contains(mainWindow.getInputControl().getInputImage())) {
            return;
        }

        try {
            int stepAmount = mainWindowVM.StepAmount;
            WriteableBitmap newOutputWb = (await centralManager.getWFCHandler()
                .initAndRunWfcDB(true, stepAmount == 100 && !keepOutput ? -1 : 0, force)).Item1;
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

    public async void advanceStep() {
        if (centralManager.getWFCHandler().getAmountCollapsed()
            .Equals(mainWindowVM.ImageOutWidth * mainWindowVM.ImageOutHeight)) {
            centralManager.getUIManager().dispatchError(mainWindow);
            return;
        }

        try {
            (double currentWidth, double currentHeight) = centralManager.getWFCHandler().getPropagatorSize();

            bool weightReset = false;
            if (!mainWindowVM.IsRunning) {
                centralManager.getWFCHandler().updateWeights();
                restartSolution("Advance step");
                weightReset = true;
            }

            (WriteableBitmap result2, bool _) = await centralManager.getWFCHandler()
                .initAndRunWfcDB(
                    mainWindowVM.ImageOutWidth != (int) currentWidth ||
                    mainWindowVM.ImageOutHeight != (int) currentHeight || weightReset, mainWindowVM.StepAmount);
            mainWindowVM.OutputImage = result2;
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    public void revertStep() {
        try {
            int prevAmountCollapsed = centralManager.getWFCHandler().getAmountCollapsed();
            if (prevAmountCollapsed == 0) {
                return;
            }

            if (mainWindowVM.Markers.Count > 0) {
                MarkerViewModel curMarker = mainWindowVM.Markers[savePoints.Count - 1];
                bool canReturn
                    = Math.Abs(centralManager.getWFCHandler().getPercentageCollapsed() -
                               curMarker.MarkerCollapsePercentage) >
                    0.00001d || curMarker.Revertible;
                if (!canReturn) {
                    centralManager.getUIManager().dispatchError(mainWindow);
                    return;
                }
            }

            int loggedAT = centralManager.getWFCHandler().getActionsTaken();

            Bitmap? avaloniaBitmap = null;
            int curStep = centralManager.getWFCHandler().getAmountCollapsed();

            while (centralManager.getWFCHandler().getAmountCollapsed() >
                   prevAmountCollapsed - mainWindowVM.StepAmount) {
                for (int i = 0; i < mainWindowVM.StepAmount; i++) {
                    curStep = centralManager.getWFCHandler().getAmountCollapsed();

                    if (savePoints.Count != 0 && curStep <= savePoints.Peek()) {
                        foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                            if (marker.MarkerIndex.Equals(savePoints.Count - 1)) {
                                if (!marker.Revertible) {
                                    goto exitOuterLoop;
                                } else {
                                    mainWindowVM.Markers.Remove(marker);
                                    savePoints.Pop();
                                }
                            }
                        }
                    }

                    avaloniaBitmap = centralManager.getWFCHandler().stepBackWfc();
                }
            }

            exitOuterLoop:

            mainWindowVM.OutputImage = avaloniaBitmap!;

            centralManager.getWFCHandler().setActionsTaken(loggedAT - 1);

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

    public void resetOverwriteCache() {
        maskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];
    }

    public Color[,] getMaskColours() {
        return maskColours;
    }

    public Stack<int> getSavePoints() {
        return savePoints;
    }

    public void placeMarker(bool revertible = true) {
        if (centralManager.getWFCHandler().isCollapsed()) {
            return;
        }

        int curStep = centralManager.getWFCHandler().getAmountCollapsed();
        if (curStep == 0) {
            return;
        }

        if (mainWindowVM.Markers.Count > 0 && mainWindowVM.Markers[^1].MarkerCollapsePercentage
                .Equals(centralManager.getWFCHandler().getPercentageCollapsed())) {
            return;
        }

        mainWindowVM.Markers.Add(new MarkerViewModel(savePoints.Count,
            (mainWindow.IsVisible
                ? mainWindow.getOutputControl().getTimelineWidth()
                : centralManager.getPaintingWindow().getTimelineWidth()) *
            centralManager.getWFCHandler().getPercentageCollapsed() +
            1, centralManager.getWFCHandler().getPercentageCollapsed(),
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

    public void loadMarker(bool doBacktrack = true) {
        int prevAmountCollapsed = savePoints.Count == 0 ? 0 : savePoints.Peek();

        if (savePoints.Count != 0) {
            if (!mainWindowVM.Markers[savePoints.Count - 1].Revertible) {
                centralManager.getUIManager().dispatchError(mainWindow);
                return;
            }
        }

        if (centralManager.getWFCHandler().getAmountCollapsed() == prevAmountCollapsed &&
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
            prevAmountCollapsed == centralManager.getWFCHandler().getAmountCollapsed()) {
            centralManager.getUIManager().dispatchError(mainWindow);
            return;
        }

        try {
            if (doBacktrack) {
                while (centralManager.getWFCHandler().getAmountCollapsed() > prevAmountCollapsed) {
                    mainWindowVM.OutputImage = centralManager.getWFCHandler().stepBackWfc();
                }
            }
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    public async void importSolution() {
        OpenFileDialog ofd = new() {
            Title = @"Select png solution (and its items layer (*_itemsLayer.png)) to import",
            AllowMultiple = true,
            Filters = new List<FileDialogFilter> {
                new() {
                    Extensions = new List<string> {"png"},
                    Name = "PNG Image (*.png)"
                }
            }
        };

        string[]? ofdResults = await ofd.ShowAsync(new Window());
        if (ofdResults != null) {
            string? worldFile = null, itemsFile = null;

            if (ofdResults.Length == 1) {
                worldFile = ofdResults[0];
            } else {
                foreach (string foundFile in ofdResults) {
                    if (!foundFile.EndsWith("_itemsLayer.png") && worldFile == null) {
                        worldFile = foundFile;
                    }

                    if (foundFile.EndsWith("_itemsLayer.png") && itemsFile == null) {
                        itemsFile = foundFile;
                    }
                }
            }

            if (worldFile == null) {
                centralManager.getUIManager().dispatchError(mainWindow);
                return;
            }

            MemoryStream ms = new(await File.ReadAllBytesAsync(worldFile));
            WriteableBitmap inputBitmap = WriteableBitmap.Decode(ms);

            int inputImageWidth = (int) inputBitmap.Size.Width, inputImageHeight = (int) inputBitmap.Size.Height;

            int tileSize = centralManager.getWFCHandler().isOverlappingModel()
                ? 1
                : centralManager.getWFCHandler().getTileSize();
            int maxImageSize = 128 * tileSize;

            if (inputImageWidth > maxImageSize || inputImageWidth < 10 || inputImageHeight > maxImageSize ||
                inputImageHeight < 10) {
                centralManager.getUIManager().dispatchError(mainWindow);
                //TODO Popup telling user input image was not allowed size
                return;
            }

            int imageWidth = inputImageWidth / tileSize;
            int imageHeight = inputImageHeight / tileSize;

            mainWindowVM.ImageOutWidth = imageWidth;
            mainWindowVM.ImageOutHeight = imageHeight;
            mainWindowVM.StepAmount = 1;

            centralManager.getInputManager().restartSolution("Imported image", true);
            (Color[][] colourArray, HashSet<Color> allowedColours) = imageToColourArray(inputBitmap);

            bool allowed = allowedColours
                .Select(foundColour => !foundColour.A.Equals(255) || centralManager.getMainWindowVM().PaintTiles
                    .Any(tvm => tvm.PatternColour.Equals(foundColour)))
                .Aggregate(true, (current, found) => current && found);

            if (!allowed) {
                centralManager.getUIManager().dispatchError(mainWindow);
                //TODO Popup telling user input image was not correct colours
                return;
            }

            ObservableCollection<TileViewModel> paintTiles = centralManager.getMainWindowVM().PaintTiles;

            if (centralManager.getWFCHandler().isOverlappingModel()) {
                for (int x = 0; x < imageWidth; x++) {
                    for (int y = 0; y < imageHeight; y++) {
                        Color toPaint = colourArray[x][y];
                        Color foundAtPos = centralManager.getWFCHandler().getPropagatorOutputO().toArray2d()[y, x];

                        if (!toPaint.Equals(foundAtPos) && toPaint.A.Equals(255) && foundAtPos.A.Equals(0)) {
                            int idx = -1;
                            foreach (TileViewModel tvm in paintTiles.Where(tvm => tvm.PatternColour.Equals(toPaint))) {
                                idx = tvm.PatternIndex;
                                break;
                            }

                            bool? success = centralManager.getInputManager()
                                .processClick(y, x, imageWidth, imageHeight, idx).Result;

                            if (success != null && !(bool) success) {
                                centralManager.getUIManager().dispatchError(mainWindow);
                                //TODO Popup telling user input image was not following patterns?
                                centralManager.getInputManager().restartSolution("Imported image failure", true);
                                return;
                            }
                        } else if (!foundAtPos.A.Equals(0) && toPaint.A.Equals(255) &&
                                   !toPaint.Equals(foundAtPos)) {
                            Trace.WriteLine($@"OUT Error mate -> {toPaint} @ {foundAtPos}");
                            centralManager.getUIManager().dispatchError(mainWindow);
                            //TODO Popup telling user input image had unknown error?
                            centralManager.getInputManager().restartSolution("Imported image failure", true);
                            return;
                        }
                    }
                }

                if (itemsFile != null) {
                    //TODO Items Layer
                }
            } else {
                //TODO World Layer

                if (itemsFile != null) {
                    //TODO Items Layer
                }
            }
        }
    }

    public async void exportSolution() {
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
            bool hasItems = mainWindow.getInputControl().getCategory().Equals("Worlds Top-Down") &&
                            centralManager.getWFCHandler().isCollapsed();
            if (hasItems) {
                centralManager.getWFCHandler().getLatestOutputBM(false)
                    .Save(settingsFileName.Replace(".png", "_worldLayer.png"));

                int[,] itemsGrid = mainWindowVM.getLatestItemGrid();
                int[] tmp = new int[itemsGrid.GetLength(0) * itemsGrid.GetLength(1)];
                Buffer.BlockCopy(itemsGrid, 0, tmp, 0, tmp.Length * sizeof(int));
                List<int> list = new(tmp);

                if (list.Distinct().Count() > 1 || (!list.Distinct().Contains(-1) && list.Distinct().Any())) {
                    generateRawItemImage(itemsGrid).Save(settingsFileName.Replace(".png", "_itemsLayer.png"));
                    combineBitmaps(centralManager.getWFCHandler().getLatestOutput(),
                            centralManager.getWFCHandler().isOverlappingModel()
                                ? 1
                                : centralManager.getWFCHandler().getTileSize(),
                            getLatestItemBitMap(), getDimension(), mainWindowVM.ImageOutWidth,
                            mainWindowVM.ImageOutHeight)
                        .Save(settingsFileName.Replace(".png", ".jpg"));
                }
            } else {
                centralManager.getWFCHandler().getLatestOutput().Save(settingsFileName);
            }
        }
    }

    public void animate() {
        if (centralManager.getWFCHandler().isCollapsed()) {
            mainWindowVM.IsPlaying = false;
            centralManager.getUIManager().dispatchError(mainWindow);
            return;
        }

        (double currentWidth, double currentHeight) = centralManager.getWFCHandler().getPropagatorSize();
        if (mainWindowVM.ImageOutWidth != (int) currentWidth ||
            mainWindowVM.ImageOutHeight != (int) currentHeight) {
            restartSolution("Animate");
        }

        if (!mainWindowVM.IsRunning && mainWindowVM.IsPlaying) {
            centralManager.getWFCHandler().updateWeights();
            restartSolution("Animate");
        }

        bool startAnimation = mainWindowVM.IsPlaying;

        if (startAnimation) {
            lock (timer) {
                timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                timer.Tick += animationTimerTick!;
                timer.Start();
            }
        } else {
            lock (timer) {
                timer.Stop();
                timer = new DispatcherTimer();
            }
        }
    }

    private void animationTimerTick(object sender, EventArgs e) {
        lock (timer) {
            /* only work when this is no reentry while we are already working */
            if (timer.IsEnabled) {
                timer.Stop();
                timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                try {
                    (WriteableBitmap result2, bool finished) = centralManager.getWFCHandler()
                        .initAndRunWfcDB(false, mainWindowVM.StepAmount).Result;
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

    public async Task<bool?> processClick(int a, int b, int imgWidth, int imgHeight, int tileIdx, bool skipUI = false) {
        if (!skipUI) {
            a = (int) Math.Floor(a * mainWindowVM.ImageOutWidth / (double) imgWidth);
            b = (int) Math.Floor(b * mainWindowVM.ImageOutHeight / (double) imgHeight);
        }

        (WriteableBitmap? bitmap, bool? showPixel)
            = await centralManager.getWFCHandler().setTile(a, b, tileIdx, false, skipUI);

        if (!skipUI && !mainWindowVM.IsPaintOverrideEnabled) {
            if (showPixel != null && (bool) showPixel) {
                mainWindowVM.OutputImage = bitmap!;
                processHoverAvailability(a, b, imgWidth, imgHeight, tileIdx, true, true);
            } else {
                mainWindowVM.OutputImage = centralManager.getWFCHandler().getLatestOutputBM();
            }
        }

        return showPixel;
    }

    private int lastProcessedX = -1, lastProcessedY = -1;

    public async void processHoverAvailability(int hoverX, int hoverY, int imgWidth, int imgHeight, int selectedValue,
        bool pencilSelected, bool force = false) {
        int a = (int) Math.Floor(hoverX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(hoverY * mainWindowVM.ImageOutHeight / (double) imgHeight);

        if (lastProcessedX == a && lastProcessedY == b && !force) {
            return;
        }

        lastProcessedX = a;
        lastProcessedY = b;

        if (centralManager.getWFCHandler().isOverlappingModel()) {
            List<Color> availableAtLoc;
            try {
                availableAtLoc = centralManager.getWFCHandler().getAvailablePatternsAtLocation<Color>(a, b);
            } catch (Exception) {
                return;
            }

            if (pencilSelected) {
                try {
                    (WriteableBitmap? _, bool? showPixel)
                        = await centralManager.getWFCHandler().setTile(a, b, selectedValue, true);
                    if (showPixel != null && (bool) showPixel) {
                        mainWindowVM.OutputPreviewMask = centralManager.getWFCHandler().getLatestOutputBM(false);
                        centralManager.getWFCHandler().stepBackWfc();
                    } else {
                        mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
                    }
                } catch (IndexOutOfRangeException) {
                    mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                        PixelFormat.Bgra8888, AlphaFormat.Unpremul);
                }
            } else if (mainWindowVM.PaintEraseModeEnabled || mainWindowVM.PaintKeepModeEnabled) {
                updateHoverBrushMask(a, b);
            }

            mainWindowVM.HelperTiles.Clear();
            int selectedIndex = centralManager.getPaintingWindow().getSelectedPaintIndex();
            foreach (TileViewModel tvm in mainWindowVM.PaintTiles) {
                if (availableAtLoc.Contains(centralManager.getWFCHandler().getColorFromIndex(tvm.PatternIndex))) {
                    tvm.Highlighted = tvm.PatternIndex == selectedIndex;
                    mainWindowVM.HelperTiles.Add(tvm);
                }
            }
        } else {
            List<int> availableAtLoc;
            try {
                availableAtLoc = centralManager.getWFCHandler().getAvailablePatternsAtLocation<int>(a, b);
            } catch (Exception) {
                return;
            }

            if (pencilSelected) {
                (WriteableBitmap? _, bool? showPixel)
                    = await centralManager.getWFCHandler().setTile(a, b, selectedValue, true);
                if (showPixel != null && (bool) showPixel) {
                    mainWindowVM.OutputPreviewMask = centralManager.getWFCHandler().getLatestOutputBM(false);
                    centralManager.getWFCHandler().stepBackWfc();
                } else {
                    mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                        PixelFormat.Bgra8888, AlphaFormat.Unpremul);
                }
            } else if (mainWindowVM.PaintEraseModeEnabled || mainWindowVM.PaintKeepModeEnabled) {
                updateHoverBrushMask(a, b);
            }

            mainWindowVM.HelperTiles.Clear();
            int selectedIndex = centralManager.getPaintingWindow().getSelectedPaintIndex();

            foreach (TileViewModel tvm in mainWindowVM.PaintTiles) {
                if (availableAtLoc.Contains(centralManager.getWFCHandler().getDescrambledIndex(tvm.PatternIndex))) {
                    tvm.Highlighted = tvm.PatternIndex == selectedIndex;
                    mainWindowVM.HelperTiles.Add(tvm);
                }
            }
        }
    }

    private void updateHoverBrushMask(int a, int b) {
        int rawBrushSize = centralManager.getPaintingWindow().getPaintBrushSize();
        bool add = mainWindowVM.PaintKeepModeEnabled;

        double brushSize = rawBrushSize switch {
            1 => rawBrushSize,
            2 => rawBrushSize * 2d,
            _ => rawBrushSize * 3d
        };

        Color[,] hoverMaskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];

        if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
            if (rawBrushSize == -1) {
                hoverMaskColours[a, b] = add ? Colors.Green : Colors.Red;
            } else {
                for (int x = 0; x < mainWindowVM.ImageOutWidth; x++) {
                    for (int y = 0; y < mainWindowVM.ImageOutHeight; y++) {
                        double dx = (double) x - a;
                        double dy = (double) y - b;
                        double distanceSquared = dx * dx + dy * dy;

                        if (distanceSquared <= brushSize) {
                            hoverMaskColours[x, y] = add ? Colors.Green : Colors.Red;
                        }
                    }
                }
            }
        }

        updateMask(hoverMaskColours, false);
    }

    public void processClickMask(int clickX, int clickY, int imgWidth, int imgHeight, bool add) {
        int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);

        int rawBrushSize = centralManager.getPaintingWindow().getPaintBrushSize();
        double brushSize = rawBrushSize switch {
            1 => rawBrushSize,
            2 => rawBrushSize * 2d,
            _ => rawBrushSize * 3d
        };

        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
            if (rawBrushSize == -1) {
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

        updateMask(maskColours, true);
    }

    public void updateMask() {
        updateMask(maskColours, true);
    }

    private void updateMask(Color[,] colors, bool updateMain) {
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
        WriteableBitmap bitmap = new(new PixelSize(outputWidth, outputHeight),
            new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = bitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, outputHeight, yy => {
                uint* dest = backBuffer + (int) yy * stride / 4;
                for (int xx = 0; xx < outputWidth; xx++) {
                    Color c = colors[xx, (int) yy];
                    dest[xx] = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);
                }
            });
        }

        if (updateMain) {
            mainWindowVM.OutputImageMask = bitmap;
        } else {
            mainWindowVM.OutputPreviewMask = bitmap;
        }
    }

    public void resetHoverAvailability() {
        mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        mainWindowVM.HelperTiles.Clear();
    }

    public Bitmap getNoResBM() {
        return noResultFoundBM;
    }
}