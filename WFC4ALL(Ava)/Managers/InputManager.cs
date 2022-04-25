using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

        restartSolution();
        parentCM.getUIManager().updateInstantCollapse(1);

        lastPaintedAmountCollapsed = 0;
    }

    /*
     * Functionality
     */

    public async void restartSolution(bool force = false) {
        if (parentCM.getWFCHandler().isChangingModels() || parentCM.getWFCHandler().isChangingImages()) {
            return;
        }

        mainWindowVM.Markers = new ObservableCollection<MarkerViewModel>();

        while (savePoints.Count > 0) {
            savePoints.Pop();
        }

        lastPaintedAmountCollapsed = 0;

        maskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];
        mainWindowVM.OutputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul);

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
#if (DEBUG)
            Trace.WriteLine(exception);
#endif
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    public async void advanceStep() {
        try {
            (double currentWidth, double currentHeight) = parentCM.getWFCHandler().getPropagatorSize();

            bool weightReset = false;
            if (!mainWindowVM.IsRunning) {
                parentCM.getWFCHandler().updateWeights();
                restartSolution();
                weightReset = true;
            }

            (WriteableBitmap result2, bool finished) = await parentCM.getWFCHandler()
                .initAndRunWfcDB(
                    mainWindowVM.ImageOutWidth != (int) currentWidth ||
                    mainWindowVM.ImageOutHeight != (int) currentHeight || weightReset, mainWindowVM.StepAmount);

            if (finished) {
                return;
            }

            mainWindowVM.OutputImage = result2;
        } catch (Exception exception) {
#if (DEBUG)
            Trace.WriteLine(exception);
#endif
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    public void revertStep() {
        try {
            int prevAmountCollapsed = parentCM.getWFCHandler().getAmountCollapsed();
            if (prevAmountCollapsed == 0) {
                return;
            }

            int loggedAT = parentCM.getWFCHandler().getActionsTaken();

            Bitmap? avaloniaBitmap = null;
            while (parentCM.getWFCHandler().getAmountCollapsed() > prevAmountCollapsed - mainWindowVM.StepAmount) {
                avaloniaBitmap = parentCM.getWFCHandler().stepBackWfc(mainWindowVM.StepAmount);
            }

            mainWindowVM.OutputImage = avaloniaBitmap!;

            int curStep = parentCM.getWFCHandler().getAmountCollapsed();
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
#if (DEBUG)
            Trace.WriteLine(exception);
#endif
            mainWindowVM.OutputImage = noResultFoundBM;
        }
    }

    public void resetOverwriteCache() {
        maskColours = new Color[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];
    }

    public void placeMarker() {
        if (parentCM.getWFCHandler().isCollapsed()) {
            return;
        }
        
        int curStep = parentCM.getWFCHandler().getAmountCollapsed();
        if (curStep == 0) {
            return;
        }

        mainWindowVM.Markers.Add(new MarkerViewModel(savePoints.Count,
            (parentCM.getMainWindow().IsVisible
                ? mainWindow.getOutputControl().getTimelineWidth()
                : parentCM.getPaintingWindow().getTimelineWidth()) *
            parentCM.getWFCHandler().getPercentageCollapsed() + 1));

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
#if (DEBUG)
            Trace.WriteLine(exception);
#endif
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
            restartSolution();
        }

        (double currentWidth, double currentHeight) = parentCM.getWFCHandler().getPropagatorSize();
        if (mainWindowVM.ImageOutWidth != (int) currentWidth ||
            mainWindowVM.ImageOutHeight != (int) currentHeight) {
            restartSolution();
        }

        if (!mainWindowVM.IsRunning && mainWindowVM.IsPlaying) {
            parentCM.getWFCHandler().updateWeights();
            restartSolution();
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
#if (DEBUG)
                    Trace.WriteLine(exception);
#endif
                    mainWindowVM.OutputImage = noResultFoundBM;
                }

                timer.Start();
            }
        }
    }

    public bool processClick(int clickX, int clickY, int imgWidth, int imgHeight, int tileIdx) {
        int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);

        WriteableBitmap? bitmap;
        bool showPixel;

        if (parentCM.getWFCHandler().isOverlappingModel()) {
            (bitmap, showPixel) = parentCM.getWFCHandler().setTile(a, b, tileIdx);
        } else {
            (bitmap, showPixel) = parentCM.getWFCHandler().setTile(a, b, tileIdx);
        }

        if (showPixel) {
            mainWindowVM.OutputImage = bitmap!;
            processHoverAvailability(clickX, clickY, imgWidth, imgHeight, true);
        } else {
            mainWindowVM.OutputImage = parentCM.getWFCHandler().getLatestOutputBM();
        }

        return showPixel;
    }

    private int lastProcessedX = -1, lastProcessedY = -1;

    public void processHoverAvailability(int hoverX, int hoverY, int imgWidth, int imgHeight, bool force = false) {
        int a = (int) Math.Floor(hoverX * mainWindowVM.ImageOutWidth / (double) imgWidth),
            b = (int) Math.Floor(hoverY * mainWindowVM.ImageOutHeight / (double) imgHeight);
        if (parentCM.getWFCHandler().isOverlappingModel()) {
            List<Color> availableAtLoc;
            try {
                availableAtLoc = parentCM.getWFCHandler().getAvailablePatternsAtLocation<Color>(a, b);
            } catch (Exception) {
                return;
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
            if (lastProcessedX == a && lastProcessedY == b && !force) {
                return;
            }

            lastProcessedX = a;
            lastProcessedY = b;

            List<int> availableAtLoc;
            try {
                availableAtLoc = parentCM.getWFCHandler().getAvailablePatternsAtLocation<int>(a, b);
            } catch (Exception) {
                return;
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

        for (int x = 0; x < outputWidth; x++) {
            for (int y = 0; y < outputHeight; y++) {
                double dx = (double) x - a;
                double dy = (double) y - b;
                double distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= brushSize) {
                    maskColours[x, y] = add ? Colors.Green : Colors.Red;
                }
            }
        }

        WriteableBitmap bitmap = new(new PixelSize(outputWidth, outputHeight),
            new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Premul);

        using ILockedFramebuffer? frameBuffer = bitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, outputHeight, yy => {
                uint* dest = backBuffer + (int) yy * stride / 4;
                for (int xx = 0; xx < outputWidth; xx++) {
                    Color c = maskColours[xx, (int) yy];
                    dest[xx] = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);
                }
            });
        }

        mainWindowVM.OutputImageMask = bitmap;
    }

    public void resetHoverAvailability() {
        mainWindowVM.HelperTiles.Clear();
    }
}