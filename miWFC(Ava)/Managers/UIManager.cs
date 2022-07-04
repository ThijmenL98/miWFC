using System;
using System.Collections.Concurrent;
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
using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using miWFC.DeBroglie.Models;
using miWFC.Utils;
using miWFC.ViewModels;
using miWFC.Views;

namespace miWFC.Managers;

/// <summary>
/// Manager to handle (everything) UI Related
/// </summary>
public class UIManager {
    private readonly CentralManager centralManager;
    private readonly MainWindow mainWindow;

    private readonly MainWindowViewModel mainWindowVM;

    private readonly List<Func<int, int, int, int, Point>> transforms = new() {
        (_, _, x, y) => new Point(x, y), // rotated 0
        (w, _, x, y) => new Point(w - y, x), // rotated 90
        (w, h, x, y) => new Point(w - x, h - y), // rotated 180
        (_, h, x, y) => new Point(y, h - x), // rotated 270
        (w, _, x, y) => new Point(w - x, y), // rotated 0 and mirrored
        (w, _, x, y) => new Point(w - (w - y), x), // rotated 90 and mirrored
        (w, h, x, y) => new Point(w - (w - x), h - y), // rotated 180 and mirrored
        (w, h, x, y) => new Point(w - y, h - x) // rotated 270 and mirrored
    };

    private HashSet<ImageR> curBitmaps = new();

    private DispatcherTimer dt = new();
    private PixelPoint? origPos;
    private int patternCount;
    private Dictionary<int, List<Bitmap>> similarityMap = new();

    /*
     * Initializing Functions & Constructor
     */

    public UIManager(CentralManager parent) {
        centralManager = parent;
        mainWindowVM = parent.getMainWindowVM();
        mainWindow = parent.getMainWindow();
    }
    
    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    // Images

    // Objects

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /*
     * Functions
     */
    
    /// <summary>
    /// Update the categories combo box 
    /// </summary>
    /// 
    /// <param name="values">New Combo Box values</param>
    /// <param name="idx">Index, default is the first item</param>
    public void updateCategories(string[]? values, int idx = 0) {
        if (values != null) {
            mainWindow.getInputControl()
                .setCategories(
                    values.Select(cat => new HoverableTextViewModel(cat, Util.getDescription(cat))).ToArray(), idx);
        }
    }

    /// <summary>
    /// Update the input images combo box
    /// </summary>
    /// 
    /// <param name="values">New Combo Box values</param>
    /// <param name="idx">Index, default is the first item</param>
    public void updateInputImages(string[]? values, int idx = 0) {
        mainWindow.getInputControl().setInputImages(values, idx);
    }
    
    /// <summary>
    /// Update the pattern sizes combo box
    /// </summary>
    /// 
    /// <param name="values">New Combo Box values</param>
    /// <param name="idx">Index, default is the first item</param>
    public void updatePatternSizes(int[]? values, int idx = 0) {
        mainWindow.getInputControl().setPatternSizes(values, idx);
    }

    /// <summary>
    /// Set the input image in the UI to the currently selected image
    /// </summary>
    /// 
    /// <param name="newImage">Name of the selected input image</param>
    public void updateInputImage(string newImage) {
        // ReSharper disable once InconsistentNaming
        string URI = $"{AppContext.BaseDirectory}/samples/{newImage}.png";
        mainWindowVM.InputImage = new Bitmap(URI);
    }

    /// <summary>
    /// Set whether the application should instantly collapse or take steps
    /// </summary>
    /// 
    /// <param name="newValue">Amount of steps to take (>100 = instantly collapse)</param>
    public void updateInstantCollapse(int newValue) {
        bool ic = newValue >= 100;
        mainWindowVM.StepAmountString = ic ? "Instantly generate output" : "Steps to take: " + newValue;
        mainWindowVM.InstantCollapse = !ic;
    }

    /// <summary>
    /// Update the position of the "Current location in time" timestamp marker
    /// </summary>
    /// 
    /// <param name="amountCollapsed">Percentage of the output collapsed</param>
    public void updateTimeStampPosition(double amountCollapsed) {
        double tWidth = centralManager.getMainWindow().IsVisible
            ? mainWindow.getOutputControl().getTimelineWidth()
            : centralManager.getPaintingWindow().getTimelineWidth();
        mainWindowVM.TimeStampOffset = tWidth * amountCollapsed - 9;
    }

    /// <summary>
    /// Show each window's popup to the user
    /// </summary>
    ///
    /// <param name="param">Which popup to show, based on the window the user is on</param>
    public void showPopUp(string param) {
        switch (param) {
            case "M":
                mainWindowVM.MainInfoPopupVisible = true;
                break;
            case "I":
                // TODO mainWindowVM.ItemsInfoPopupVisible = true;
                break;
            case "P":
                mainWindowVM.PaintInfoPopupVisible = true;
                break;
            case "H":
                mainWindowVM.HeatmapInfoPopupVisible = true;
                break;
        }
    }

    /// <summary>
    /// Hide all popups, triggered on closing of any popup.
    /// </summary>
    public void hidePopUp() {
        mainWindowVM.MainInfoPopupVisible = false;
        mainWindowVM.PaintInfoPopupVisible = false;
        mainWindowVM.ItemsInfoPopupVisible = false;
        mainWindowVM.HeatmapInfoPopupVisible = false;
    }

    /// <summary>
    /// Whether one (or more, although designwise impossible) popups are opened or not
    /// </summary>
    /// 
    /// <returns>Is any popup opened?</returns>
    public bool popUpOpened() {
        return mainWindowVM.MainInfoPopupVisible || mainWindowVM.ItemsInfoPopupVisible ||
               mainWindowVM.PaintInfoPopupVisible || mainWindowVM.HeatmapInfoPopupVisible;
    }

    /// <summary>
    /// Reset the patterns extracted from the input image to allow for the new input image to load its patterns.
    /// </summary>
    public void resetPatterns() {
        similarityMap = new Dictionary<int, List<Bitmap>>();
        curBitmaps = new HashSet<ImageR>();
        patternCount = 0;
    }

    /// <summary>
    /// Add a new tile/pattern to the tile/pattern section of the application
    /// </summary>
    /// 
    /// <param name="colors">Colour(s) used in the pattern/tile</param>
    /// <param name="weight">Weight of the pattern/tile</param>
    /// <param name="tileSymmetries">Symmetries of the all tiles keyed by parent tiles</param>
    /// <param name="rawIndex">Raw index of the tile</param>
    /// 
    /// <returns>The TileViewModel created by the addition of the tile/pattern</returns>
    public TileViewModel? addPattern(PatternArray colors, double weight, Dictionary<int, int[]>? tileSymmetries,
        int rawIndex) {
        int n = colors.Height;
        WriteableBitmap pattern = new(new PixelSize(n, n), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        ConcurrentDictionary<Point, Color> data = new();

        using ILockedFramebuffer? frameBuffer = pattern.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, n, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < n; x++) {
                    Color c = (Color) colors.getTileAt(x, (int) y).Value;
                    dest[x] = (uint) ((c.A << 24) + (c.R << 16) + (c.G << 8) + c.B);
                    data[new Point(x, (int) y)] = c;
                }
            });
        }

        ImageR cur = new(pattern.Size, data.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value,
            data.Comparer));

        foreach ((ImageR reference, int i) in curBitmaps.Select((reference, i) => (reference, i))) {
            if (transforms.Select(transform => cur.Data.All(x =>
                    x.Value == reference.Data[
                        transform.Invoke((int) cur.Size.Width - 1, (int) cur.Size.Height - 1, (int) x.Key.X,
                            (int) x.Key.Y)]))
                .Any(match => match)) {
                similarityMap[i].Add(pattern);
                List<int> symmetries = tileSymmetries!.ContainsKey(i) ? tileSymmetries[i].ToList() : new List<int>();
                symmetries.Add(patternCount);
                tileSymmetries[i] = symmetries.ToArray();
                return null;
            }
        }

        curBitmaps.Add(cur);
        similarityMap[patternCount] = new List<Bitmap> {pattern};
        TileViewModel tvm = new(pattern, weight, patternCount, rawIndex, centralManager, 0);

        patternCount++;

        return tvm;
    }

    /// <summary>
    /// Function to shake the window if the user has done something erroneous
    /// </summary>
    /// 
    /// <param name="window">Window to shake</param>
    public void dispatchError(Window window) {
        dt.Stop();
        if (origPos != null) {
            window.Position = (PixelPoint) origPos;
        }

        dt = new DispatcherTimer();
        int counter = 0;
        int shake = 0;
        origPos = window.Position;

        dt.Tick += delegate {
            PixelPoint curPos = window.Position;
            counter++;
            switch (shake) {
                case 0:
                    window.Position = curPos.WithX(curPos.X + 5);
                    shake = 1;
                    break;
                case 1:
                    window.Position = curPos.WithX(curPos.X - 5);
                    shake = 0;
                    break;
            }

            if (counter == 10) {
                dt.Stop();
                window.Position = (PixelPoint) origPos;
            }
        };

        dt.Interval = TimeSpan.FromMilliseconds(30);
        dt.Start();
    }

    /// <summary>
    /// Function to switch windows
    /// </summary>
    /// 
    /// <param name="window">Window to switch to</param>
    /// <param name="checkClicked">Whether the user needs to agree to discard or save unsaved work</param>
    /// 
    /// <exception cref="NotImplementedException">Error caused by a nonexisting window being asked to switch to</exception>
    public async Task switchWindow(Windows window, bool checkClicked = false) {
        mainWindowVM.OutputImage = centralManager.getWFCHandler().getLatestOutputBM();

#if DEBUG
        Trace.WriteLine(@$"We want to switch to {window}");
#endif
        Window target = mainWindow, source = mainWindow;

        switch (window) {
            case Windows.MAIN:
                // Goto main
                source = centralManager.getPaintingWindow().IsVisible
                    ? centralManager.getPaintingWindow()
                    : centralManager.getItemWindow().IsVisible
                        ? centralManager.getItemWindow()
                        : centralManager.getWeightMapWindow();

                bool stillApply = await handlePaintingClose(checkClicked);

                if (stillApply) {
                    await centralManager.getMainWindowVM().OnApplyClick();
                }

                break;
            case Windows.PAINTING:
                // Goto paint
                mainWindowVM.PencilModeEnabled = true;
                target = centralManager.getPaintingWindow();
                break;
            case Windows.ITEMS:
                // Goto items
                target = centralManager.getItemWindow();
                break;
            case Windows.HEATMAP:
                // Goto Items
                target = centralManager.getWeightMapWindow();
                break;
            default:
                throw new NotImplementedException();
        }

        if (!window.Equals(Windows.HEATMAP) && !source.Equals(centralManager.getWeightMapWindow())) {
            target.Width = source.Width;
            target.Height = source.Height;
        }

        if (!Equals(target, centralManager.getItemWindow()) && !Equals(source, centralManager.getItemWindow())) {
            ObservableCollection<MarkerViewModel> mvmListCopy = new(mainWindowVM.Markers);

            mainWindowVM.Markers.Clear();
            foreach (MarkerViewModel mvm in mvmListCopy) {
                double offset = (centralManager.getMainWindow().IsVisible
                        ? mainWindow.getOutputControl().getTimelineWidth()
                        : centralManager.getPaintingWindow().getTimelineWidth()) *
                    mvm.MarkerCollapsePercentage + 1;
                mainWindowVM.Markers.Add(new MarkerViewModel(mvm.MarkerIndex,
                    offset, mvm.MarkerCollapsePercentage, mvm.Revertible));
            }

            updateTimeStampPosition(centralManager.getWFCHandler().getPercentageCollapsed());
        }

        source.Hide();
        target.Show();

        target.Position = source.Position;
    }

    /// <summary>
    /// Function to handle the closing of the painting window which might contain unsaved work
    /// </summary>
    /// 
    /// <param name="checkClicked">Whether the user has unsaved work</param>
    /// 
    /// <returns>Task that finishes upon closing the prompt to save or discard unsaved work</returns>
    private async Task<bool> handlePaintingClose(bool checkClicked) {
        if (!checkClicked) {
            IMsBoxWindow<string> messageBoxCustomWindow = MessageBoxManager
                .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                    ContentMessage = "Mask hasn't been applied!",
                    ButtonDefinitions = new[] {
                        new ButtonDefinition {Name = "Apply"},
                        new ButtonDefinition {Name = "Discard"}
                    },
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });

            string? value = await messageBoxCustomWindow.Show();

            if (value is "Apply") {
                return true;
            }
        }

        mainWindowVM.TemplateAddModeEnabled = false;
        mainWindowVM.TemplatePlaceModeEnabled = false;
        mainWindowVM.PencilModeEnabled = true;
        mainWindowVM.PaintModeEnabled = false;

        mainWindowVM.OutputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        return false;
    }

    /// <summary>
    /// Reference image record struct
    /// </summary>
    /// 
    /// <param name="Size">Size of the image reference</param>
    /// <param name="Data">Data of the image reference</param>
    private record ImageR(Size Size, Dictionary<Point, Color> Data) {
        public Size Size { get; } = Size;
        public Dictionary<Point, Color> Data { get; } = Data;
    }
}

/// <summary>
/// Enumerator for the different types of windows in this application
/// </summary>
public enum Windows {
    MAIN,
    PAINTING,
    ITEMS,
    HEATMAP
}