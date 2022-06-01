using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using static WFC4ALL.Utils.Util;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace WFC4ALL.Managers;

public class InputManager {
    private readonly MainWindow mainWindow;
    private readonly MainWindowViewModel mainWindowVM;

    private readonly Bitmap noResultFoundBM;

    private readonly CentralManager parentCM;
    private readonly Stack<int> savePoints;

    private int lastPaintedAmountCollapsed;

    private Color[,] maskColours;

    private DispatcherTimer timer = new();

    public InputManager(CentralManager parent) {
        parentCM = parent;
        mainWindowVM = parentCM.getMainWindowVM();
        mainWindow = parentCM.getMainWindow();

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
        parentCM.getUIManager().updateInstantCollapse(1);

        lastPaintedAmountCollapsed = 0;
    }

    /*
     * Functionality
     */

    public async void restartSolution(string source, bool force = false) {
        if (parentCM.getWFCHandler().isChangingModels() || parentCM.getWFCHandler().isChangingImages()) {
            return;
        }

        if (!parentCM.getMainWindow().isWindowTriggered() && !source.Equals("Window activation")) {
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

        if (!getModelImages(parentCM.getWFCHandler().isOverlappingModel() ? "overlapping" : "simpletiled",
                    mainWindow.getInputControl().getCategory())
                .Contains(mainWindow.getInputControl().getInputImage())) {
            return;
        }

        try {
            int stepAmount = mainWindowVM.StepAmount;
            mainWindowVM.OutputImage = (await parentCM.getWFCHandler()
                .initAndRunWfcDB(true, stepAmount == 100 ? -1 : 0, force)).Item1;
        } catch (InvalidOperationException) {
            // Error caused by multithreading which will be ignored
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    public async void advanceStep() {
        if (parentCM.getWFCHandler().getAmountCollapsed().Equals(mainWindowVM.ImageOutWidth * mainWindowVM.ImageOutHeight)) {
            parentCM.getUIManager().dispatchError(parentCM.getMainWindow());
            return;
        }
        
        try {
            (double currentWidth, double currentHeight) = parentCM.getWFCHandler().getPropagatorSize();

            bool weightReset = false;
            if (!mainWindowVM.IsRunning) {
                parentCM.getWFCHandler().updateWeights();
                restartSolution("Advance step");
                weightReset = true;
            }

            (WriteableBitmap result2, bool _) = await parentCM.getWFCHandler()
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
            int prevAmountCollapsed = parentCM.getWFCHandler().getAmountCollapsed();
            if (prevAmountCollapsed == 0) {
                return;
            }

            if (mainWindowVM.Markers.Count > 0) {
                MarkerViewModel curMarker = mainWindowVM.Markers[savePoints.Count - 1];
                bool canReturn
                    = Math.Abs(parentCM.getWFCHandler().getPercentageCollapsed() - curMarker.MarkerCollapsePercentage) >
                    0.00001d || curMarker.Revertible;
                if (!canReturn) {
                    parentCM.getUIManager().dispatchError(parentCM.getMainWindow());
                    return;
                }
            }

            int loggedAT = parentCM.getWFCHandler().getActionsTaken();

            Bitmap? avaloniaBitmap = null;
            int curStep = parentCM.getWFCHandler().getAmountCollapsed();

            while (parentCM.getWFCHandler().getAmountCollapsed() > prevAmountCollapsed - mainWindowVM.StepAmount) {
                for (int i = 0; i < mainWindowVM.StepAmount; i++) {
                    curStep = parentCM.getWFCHandler().getAmountCollapsed();

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

                    avaloniaBitmap = parentCM.getWFCHandler().stepBackWfc();
                }
            }

            exitOuterLoop:

            mainWindowVM.OutputImage = avaloniaBitmap!;

            parentCM.getWFCHandler().setActionsTaken(loggedAT - 1);

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
        if (parentCM.getWFCHandler().isCollapsed()) {
            return;
        }

        int curStep = parentCM.getWFCHandler().getAmountCollapsed();
        if (curStep == 0) {
            return;
        }

        if (mainWindowVM.Markers.Count > 0 && mainWindowVM.Markers[^1].MarkerCollapsePercentage
                .Equals(parentCM.getWFCHandler().getPercentageCollapsed())) {
            return;
        }

        mainWindowVM.Markers.Add(new MarkerViewModel(savePoints.Count,
            (parentCM.getMainWindow().IsVisible
                ? mainWindow.getOutputControl().getTimelineWidth()
                : parentCM.getPaintingWindow().getTimelineWidth()) *
            parentCM.getWFCHandler().getPercentageCollapsed() + 1, parentCM.getWFCHandler().getPercentageCollapsed(),
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
                parentCM.getUIManager().dispatchError(parentCM.getMainWindow());
                return;
            }
        }

        if (parentCM.getWFCHandler().getAmountCollapsed() == prevAmountCollapsed &&
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
            prevAmountCollapsed == parentCM.getWFCHandler().getAmountCollapsed()) {
            parentCM.getUIManager().dispatchError(parentCM.getMainWindow());
            return;
        }

        try {
            if (doBacktrack) {
                while (parentCM.getWFCHandler().getAmountCollapsed() > prevAmountCollapsed) {
                    mainWindowVM.OutputImage = parentCM.getWFCHandler().stepBackWfc();
                }
            }
        } catch (Exception exception) {
            Trace.WriteLine(exception);
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    public async void exportSolution() {
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
            parentCM.getWFCHandler().getLatestOutput().Save(settingsFileName);
        }
    }

    public void animate() {
        if (parentCM.getWFCHandler().isCollapsed()) {
            mainWindowVM.IsPlaying = false;
            parentCM.getUIManager().dispatchError(parentCM.getMainWindow());
            return;
        }

        (double currentWidth, double currentHeight) = parentCM.getWFCHandler().getPropagatorSize();
        if (mainWindowVM.ImageOutWidth != (int) currentWidth ||
            mainWindowVM.ImageOutHeight != (int) currentHeight) {
            restartSolution("Animate");
        }

        if (!mainWindowVM.IsRunning && mainWindowVM.IsPlaying) {
            parentCM.getWFCHandler().updateWeights();
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
                    (WriteableBitmap result2, bool finished) = parentCM.getWFCHandler()
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

    public bool? processClick(int a, int b, int imgWidth, int imgHeight, int tileIdx, bool skipUI = false) {
        if (!skipUI) {
            a = (int) Math.Floor(a * mainWindowVM.ImageOutWidth / (double) imgWidth);
            b = (int) Math.Floor(b * mainWindowVM.ImageOutHeight / (double) imgHeight);
        }

        (WriteableBitmap? bitmap, bool? showPixel) = parentCM.getWFCHandler().setTile(a, b, tileIdx, skipUI);

        if (!skipUI) {
            if (showPixel != null && (bool) showPixel) {
                mainWindowVM.OutputImage = bitmap!;
                processHoverAvailability(a, b, imgWidth, imgHeight, tileIdx, true, true);
            } else {
                mainWindowVM.OutputImage = parentCM.getWFCHandler().getLatestOutputBM();
            }
        }

        return showPixel;
    }

    private int lastProcessedX = -1, lastProcessedY = -1;

    public void processHoverAvailability(int hoverX, int hoverY, int imgWidth, int imgHeight, int selectedValue,
        bool pencilSelected, bool force = false) {
        int a = (int) Math.Floor(hoverX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(hoverY * mainWindowVM.ImageOutHeight / (double) imgHeight);

        if (lastProcessedX == a && lastProcessedY == b && !force) {
            return;
        }

        lastProcessedX = a;
        lastProcessedY = b;

        if (parentCM.getWFCHandler().isOverlappingModel()) {
            List<Color> availableAtLoc;
            try {
                availableAtLoc = parentCM.getWFCHandler().getAvailablePatternsAtLocation<Color>(a, b);
            } catch (Exception) {
                return;
            }

            if (pencilSelected) {
                try {
                    (WriteableBitmap? _, bool? showPixel) = parentCM.getWFCHandler().setTile(a, b, selectedValue);
                    if (showPixel != null && (bool) showPixel) {
                        mainWindowVM.OutputPreviewMask = parentCM.getWFCHandler().getLatestOutputBM(false);
                        parentCM.getWFCHandler().stepBackWfc();
                    } else {
                        mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
                    }
                } catch (IndexOutOfRangeException) {
                    mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                        PixelFormat.Bgra8888, AlphaFormat.Unpremul);
                }
            } else if (parentCM.getMainWindowVM().PaintEraseModeEnabled ||
                       parentCM.getMainWindowVM().PaintKeepModeEnabled) {
                updateHoverBrushMask(a, b);
            }

            mainWindowVM.HelperTiles.Clear();
            int selectedIndex = parentCM.getPaintingWindow().getSelectedPaintIndex();
            foreach (TileViewModel tvm in mainWindowVM.PaintTiles) {
                if (availableAtLoc.Contains(parentCM.getWFCHandler().getColorFromIndex(tvm.PatternIndex))) {
                    tvm.Highlighted = tvm.PatternIndex == selectedIndex;
                    mainWindowVM.HelperTiles.Add(tvm);
                }
            }
        } else {
            List<int> availableAtLoc;
            try {
                availableAtLoc = parentCM.getWFCHandler().getAvailablePatternsAtLocation<int>(a, b);
            } catch (Exception) {
                return;
            }

            if (pencilSelected) {
                (WriteableBitmap? _, bool? showPixel) = parentCM.getWFCHandler().setTile(a, b, selectedValue);
                if (showPixel != null && (bool) showPixel) {
                    mainWindowVM.OutputPreviewMask = parentCM.getWFCHandler().getLatestOutputBM(false);
                    parentCM.getWFCHandler().stepBackWfc();
                } else {
                    mainWindowVM.OutputPreviewMask = new WriteableBitmap(new PixelSize(1, 1), Vector.One,
                        PixelFormat.Bgra8888, AlphaFormat.Unpremul);
                }
            } else if (parentCM.getMainWindowVM().PaintEraseModeEnabled ||
                       parentCM.getMainWindowVM().PaintKeepModeEnabled) {
                updateHoverBrushMask(a, b);
            }

            mainWindowVM.HelperTiles.Clear();
            int selectedIndex = parentCM.getPaintingWindow().getSelectedPaintIndex();

            foreach (TileViewModel tvm in mainWindowVM.PaintTiles) {
                if (availableAtLoc.Contains(parentCM.getWFCHandler().getDescrambledIndex(tvm.PatternIndex))) {
                    tvm.Highlighted = tvm.PatternIndex == selectedIndex;
                    mainWindowVM.HelperTiles.Add(tvm);
                }
            }
        }
    }

    private void updateHoverBrushMask(int a, int b) {
        int rawBrushSize = parentCM.getPaintingWindow().getPaintBrushSize();
        bool add = parentCM.getMainWindowVM().PaintKeepModeEnabled;

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

        int rawBrushSize = parentCM.getPaintingWindow().getPaintBrushSize();
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
}