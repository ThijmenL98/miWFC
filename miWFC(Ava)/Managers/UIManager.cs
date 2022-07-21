using System;
using System.Collections.Concurrent;
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
using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using miWFC.DeBroglie.Models;
using miWFC.Utils;
using miWFC.ViewModels;
using miWFC.ViewModels.Structs;
using miWFC.Views;

namespace miWFC.Managers;

/// <summary>
///     Manager to handle (everything) UI Related
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

    private bool windowClosed = true;

    /*
     * Initializing Functions & Constructor
     */

    public UIManager(CentralManager parent) {
        centralManager = parent;
        mainWindowVM = parent.GetMainWindowVM();
        mainWindow = parent.GetMainWindow();
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
    ///     Update the categories combo box
    /// </summary>
    /// <param name="values">New Combo Box values</param>
    /// <param name="idx">Index, default is the first item</param>
    public void UpdateCategories(string[]? values, int idx = 0) {
        if (values != null) {
            mainWindow.GetInputControl()
                .SetCategories(
                    values.Select(cat => new HoverableTextViewModel(cat, Util.GetDescription(cat))).ToArray(), idx);
        }
    }

    /// <summary>
    ///     Update the input images combo box
    /// </summary>
    /// <param name="values">New Combo Box values</param>
    /// <param name="idx">Index, default is the first item</param>
    public void UpdateInputImages(string[]? values, int idx = 0) {
        mainWindow.GetInputControl().SetInputImages(values, idx);
    }

    /// <summary>
    ///     Update the pattern sizes combo box
    /// </summary>
    /// <param name="values">New Combo Box values</param>
    /// <param name="idx">Index, default is the first item</param>
    public void UpdatePatternSizes(int[]? values, int idx = 0) {
        mainWindow.GetInputControl().SetPatternSizes(values, idx);
    }

    /// <summary>
    ///     Set the input image in the UI to the currently selected image
    /// </summary>
    /// <param name="newImage">Name of the selected input image</param>
    /// <returns>Whether the image was correctly loaded</returns>
    public bool UpdateInputImage(string newImage) {
        try {
            mainWindowVM.InputImage
                = new Bitmap(
                    $"{AppContext.BaseDirectory}/samples/{(centralManager.GetMainWindow().GetInputControl().GetCategory().Equals("Custom") ? "Custom" : "Default")}/{newImage}.png");
            return true;
        } catch (FileNotFoundException) {
            return false;
        } catch (DirectoryNotFoundException) {
            return false;
        }
    }

    /// <summary>
    ///     Set whether the application should instantly collapse or take steps
    /// </summary>
    /// <param name="newValue">Amount of steps to take (>100 = instantly collapse)</param>
    public void UpdateInstantCollapse(int newValue) {
        bool ic = newValue >= 100;
        mainWindowVM.StepAmountString = ic ? "Instantly generate output" : "Steps to take: " + newValue;
        mainWindowVM.InstantCollapse = !ic;
    }

    /// <summary>
    ///     Update the position of the "Current location in time" timestamp marker
    /// </summary>
    /// <param name="amountCollapsed">Percentage of the output collapsed</param>
    public void UpdateTimeStampPosition(double amountCollapsed) {
        double tWidth = centralManager.GetMainWindow().IsVisible
            ? mainWindow.GetOutputControl().GetTimelineWidth()
            : centralManager.GetPaintingWindow().GetTimelineWidth();
        mainWindowVM.TimeStampOffset = tWidth * amountCollapsed - 9;
    }

    /// <summary>
    ///     Show each window's popup to the user
    /// </summary>
    /// <param name="param">Which popup to show, based on the window the user is on</param>
    public void ShowPopUp(string param) {
        switch (param) {
            case "M":
                mainWindowVM.MainInfoPopupVisible = true;
                break;
            case "I":
                mainWindowVM.ItemsInfoPopupVisible = true;
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
    ///     Hide all popups, triggered on closing of any popup.
    /// </summary>
    public void HidePopUp() {
        mainWindowVM.MainInfoPopupVisible = false;
        mainWindowVM.PaintInfoPopupVisible = false;
        mainWindowVM.ItemsInfoPopupVisible = false;
        mainWindowVM.HeatmapInfoPopupVisible = false;
    }

    /// <summary>
    ///     Whether one (or more, although design-wise impossible) popups are opened or not
    /// </summary>
    /// <returns>Is any popup opened?</returns>
    public bool PopUpOpened() {
        return mainWindowVM.MainInfoPopupVisible || mainWindowVM.ItemsInfoPopupVisible ||
               mainWindowVM.PaintInfoPopupVisible || mainWindowVM.HeatmapInfoPopupVisible;
    }

    /// <summary>
    ///     Reset the patterns extracted from the input image to allow for the new input image to load its patterns.
    /// </summary>
    public void ResetPatterns() {
        similarityMap = new Dictionary<int, List<Bitmap>>();
        curBitmaps = new HashSet<ImageR>();
        patternCount = 0;
    }

    /// <summary>
    ///     Add a new tile/pattern to the tile/pattern section of the application
    /// </summary>
    /// <param name="colors">Colour(s) used in the pattern/tile</param>
    /// <param name="weight">Weight of the pattern/tile</param>
    /// <param name="tileSymmetries">Symmetries of the all tiles keyed by parent tiles</param>
    /// <param name="rawIndex">Raw index of the tile</param>
    /// <returns>The TileViewModel created by the addition of the tile/pattern</returns>
    public TileViewModel? AddPattern(PatternArray colors, double weight, Dictionary<int, int[]>? tileSymmetries,
        int rawIndex) {
        int n = colors.Height;

        WriteableBitmap pattern = Util.CreateBitmapFromData(n, n, 1, (x, y) => (Color) colors.getTileAt(x, y).Value);

        ConcurrentDictionary<Point, Color> data = new();

        for (int y = 0; y < n; y++) {
            for (int x = 0; x < n; x++) {
                data[new Point(x, y)] = (Color) colors.getTileAt(x, y).Value;
            }
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
    ///     Function to shake the window if the user has done something erroneous
    /// </summary>
    /// <param name="window">Window to shake</param>
    /// <param name="errorMessage">The error message to show in a popup</param>
    public async void DispatchError(Window window, string? errorMessage) {
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

        if (errorMessage != null) {
            if (windowClosed) {
                windowClosed = false;
                IMsBoxWindow<ButtonResult>? currentWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                            ContentTitle = "Error", ContentMessage = errorMessage + Environment.NewLine,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        }
                    );
                Task<ButtonResult> windowResult = currentWindow.Show(window);
                await windowResult.ContinueWith(_ => { windowClosed = true; });
            }
        }
    }

    /// <summary>
    ///     Function to switch windows
    /// </summary>
    /// <param name="window">Window to switch to</param>
    /// <param name="checkClicked">Whether the user needs to agree to discard or save unsaved work</param>
    /// <exception cref="NotImplementedException">Error caused by a nonexistent window being asked to switch to</exception>
    public async Task SwitchWindow(Windows window, bool checkClicked = false) {
        if (mainWindowVM.ImageOutWidth != (int) centralManager.GetWFCHandler().GetPropagatorSize().Width ||
            mainWindowVM.ImageOutHeight != (int) centralManager.GetWFCHandler().GetPropagatorSize().Height) {
            if (!mainWindowVM.IsRunning && !centralManager.GetWFCHandler().IsCollapsed()) {
                await centralManager.GetInputManager().RestartSolution("Window switch nonequal input");
            } else {
                mainWindowVM.ImageOutWidth = (int) centralManager.GetWFCHandler().GetPropagatorSize().Width;
                mainWindowVM.ImageOutHeight = (int) centralManager.GetWFCHandler().GetPropagatorSize().Height;
            }
        }

        mainWindowVM.OutputImage = centralManager.GetWFCHandler().GetLatestOutputBm();

#if DEBUG
        Trace.WriteLine(@$"We want to switch to {window}");
#endif
        Window target = mainWindow, source = mainWindow;

        switch (window) {
            case Windows.MAIN:
                // Goto main
                source = centralManager.GetPaintingWindow().IsVisible
                    ? centralManager.GetPaintingWindow()
                    : centralManager.GetItemWindow().IsVisible
                        ? centralManager.GetItemWindow()
                        : centralManager.GetWeightMapWindow();

                bool stillApply = await HandlePaintingClose(checkClicked);

                if (stillApply) {
                    await centralManager.GetMainWindowVM().PaintingVM.ApplyPaintMask();
                }

                break;
            case Windows.PAINTING:
                // Goto paint
                mainWindowVM.PaintingVM.PencilModeEnabled = true;
                target = centralManager.GetPaintingWindow();
                break;
            case Windows.ITEMS:
                // Goto items
                target = centralManager.GetItemWindow();
                break;
            case Windows.HEATMAP:
                // Goto weight mapping
                target = centralManager.GetWeightMapWindow();
                centralManager.GetWeightMapWindow()
                    .UpdateBrushImage(centralManager.GetWeightMapWindow().GetPaintBrushSize());
                break;
            default:
                throw new NotImplementedException();
        }

        if (!window.Equals(Windows.HEATMAP) && !source.Equals(centralManager.GetWeightMapWindow())) {
            target.Width = source.Width;
            target.Height = source.Height;
        }

        if (!Equals(target, centralManager.GetItemWindow()) && !Equals(source, centralManager.GetItemWindow())) {
            ObservableCollection<MarkerViewModel> mvmListCopy = new(mainWindowVM.Markers);

            mainWindowVM.Markers.Clear();
            foreach (MarkerViewModel mvm in mvmListCopy) {
                double offset = (centralManager.GetMainWindow().IsVisible
                        ? mainWindow.GetOutputControl().GetTimelineWidth()
                        : centralManager.GetPaintingWindow().GetTimelineWidth()) *
                    mvm.MarkerCollapsePercentage + 1;
                mainWindowVM.Markers.Add(new MarkerViewModel(mvm.MarkerIndex,
                    offset, mvm.MarkerCollapsePercentage, mvm.Revertible));
            }

            UpdateTimeStampPosition(centralManager.GetWFCHandler().GetPercentageCollapsed());
        }

        source.Hide();
        target.Show();

        target.Position = source.Position;

        centralManager.GetUIManager().HidePopUp();
    }

    /// <summary>
    ///     Function to handle the closing of the painting window which might contain unsaved work
    /// </summary>
    /// <param name="checkClicked">Whether the user has unsaved work</param>
    /// <returns>Task that finishes upon closing the prompt to save or discard unsaved work</returns>
    private async Task<bool> HandlePaintingClose(bool checkClicked) {
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

        mainWindowVM.PaintingVM.TemplateCreationModeEnabled = false;
        mainWindowVM.PaintingVM.TemplatePlaceModeEnabled = false;
        mainWindowVM.PaintingVM.PencilModeEnabled = true;
        mainWindowVM.PaintingVM.PaintModeEnabled = false;

        mainWindowVM.OutputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        return false;
    }

    /// <summary>
    ///     Reference image record struct
    /// </summary>
    /// <param name="Size">Size of the image reference</param>
    /// <param name="Data">Data of the image reference</param>
    private record ImageR(Size Size, Dictionary<Point, Color> Data) {
        public Size Size { get; } = Size;
        public Dictionary<Point, Color> Data { get; } = Data;
    }
}

/// <summary>
///     Enumerator for the different types of windows in this application
/// </summary>
public enum Windows {
    MAIN,
    PAINTING,
    ITEMS,
    HEATMAP
}