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

public class UIManager {
    private readonly MainWindow mainWindow;

    private readonly MainWindowViewModel mainWindowVM;
    private readonly CentralManager parentCM;

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

    public UIManager(CentralManager parent) {
        parentCM = parent;
        mainWindowVM = parent.getMainWindowVM();
        mainWindow = parent.getMainWindow();
    }

    /*
     * UI Elements Content Updating
     */

    public void updateCategories(string[]? values, int idx = 0) {
        if (values != null) {
            mainWindow.getInputControl()
                .setCategories(
                    values.Select(cat => new HoverableTextViewModel(cat, Util.getDescription(cat))).ToArray(), idx);
        }
    }

    public void updateInputImages(string[]? values, int idx = 0) {
        mainWindow.getInputControl().setInputImages(values, idx);
    }

    public void updatePatternSizes(int[]? values, int idx = 0) {
        mainWindow.getInputControl().setPatternSizes(values, idx);
    }

    public void updateInputImage(string newImage) {
        // ReSharper disable once InconsistentNaming
        string URI = $"{AppContext.BaseDirectory}/samples/{newImage}.png";
        mainWindowVM.InputImage = new Bitmap(URI);
    }

    public void updateInstantCollapse(int newValue) {
        bool ic = newValue == 100;
        mainWindowVM.StepAmountString = ic ? "Instantly generate output" : "Steps to take: " + newValue;
        mainWindowVM.InstantCollapse = !ic;
    }

    public void updateTimeStampPosition(double amountCollapsed) {
        double tWidth = parentCM.getMainWindow().IsVisible
            ? mainWindow.getOutputControl().getTimelineWidth()
            : parentCM.getPaintingWindow().getTimelineWidth();
        mainWindowVM.TimeStampOffset = tWidth * amountCollapsed - 9;
    }

    /*
     * UI Window Manipulation
     */

    public void showPopUp(string param) {
        if (param.Equals("M")) {
            mainWindowVM.MainInfoPopupVisible = true;
        }
    }

    public void hidePopUp() {
        mainWindowVM.MainInfoPopupVisible = false;
    }

    public bool popUpOpened() {
        return mainWindowVM.MainInfoPopupVisible;
    }

    public void resetPatterns() {
        similarityMap = new Dictionary<int, List<Bitmap>>();
        curBitmaps = new HashSet<ImageR>();
        patternCount = 0;
    }

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
        TileViewModel tvm = new(pattern, weight, patternCount, rawIndex, cm: parentCM);

        patternCount++;

        return tvm;
    }

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

    public async Task switchWindow(Windows window, bool checkClicked = false) {
        Window target, source;

        mainWindowVM.OutputImage = parentCM.getWFCHandler().getLatestOutputBM();

        switch (window) {
            case Windows.MAIN:
                // Goto main
                target = mainWindow;
                source = parentCM.getPaintingWindow().IsVisible
                    ? parentCM.getPaintingWindow()
                    : parentCM.getItemWindow().IsVisible
                        ? parentCM.getItemWindow()
                        : parentCM.getWeightMapWindow();

                bool stillApply = await handlePaintingClose(checkClicked);

                if (stillApply) {
                    await parentCM.getMainWindowVM().OnApplyClick();
                }

                break;
            case Windows.PAINTING:
                // Goto paint
                mainWindowVM.PencilModeEnabled = true;
                target = parentCM.getPaintingWindow();
                source = mainWindow;
                break;
            case Windows.ITEMS:
                // Goto items
                target = parentCM.getItemWindow();
                source = mainWindow;
                break;
            case Windows.HEATMAP:
                // Goto Items
                target = parentCM.getWeightMapWindow();
                source = mainWindow;
                break;
            default:
                throw new NotImplementedException();
        }

        if (!window.Equals(Windows.HEATMAP) && !source.Equals(parentCM.getWeightMapWindow())) {
            target.Width = source.Width;
            target.Height = source.Height;
        }

        source.Hide();
        target.Show();

        if (!Equals(target, parentCM.getItemWindow()) && !Equals(source, parentCM.getItemWindow())) {
            ObservableCollection<MarkerViewModel> mvmListCopy = new(mainWindowVM.Markers);

            mainWindowVM.Markers.Clear();
            foreach (MarkerViewModel mvm in mvmListCopy) {
                double offset = (parentCM.getMainWindow().IsVisible
                        ? mainWindow.getOutputControl().getTimelineWidth()
                        : parentCM.getPaintingWindow().getTimelineWidth()) *
                    mvm.MarkerCollapsePercentage + 1;
                mainWindowVM.Markers.Add(new MarkerViewModel(mvm.MarkerIndex,
                    offset, mvm.MarkerCollapsePercentage, mvm.Revertible));
            }

            updateTimeStampPosition(parentCM.getWFCHandler().getPercentageCollapsed());
        }

        target.Position = source.Position;
    }

    private async Task<bool> handlePaintingClose(bool checkClicked) {
        if (!checkClicked) {
            IMsBoxWindow<string> messageBoxCustomWindow = MessageBoxManager
                .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                    ContentMessage = "Brush mask has not been applied!",
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

        mainWindowVM.PaintEraseModeEnabled = false;
        mainWindowVM.EraseModeEnabled = false;
        mainWindowVM.PencilModeEnabled = true;
        mainWindowVM.PaintKeepModeEnabled = false;

        mainWindowVM.OutputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        return false;
    }

    /*
     * Helper functions
     */

    private record ImageR(Size Size, Dictionary<Point, Color> Data) {
        public Size Size { get; } = Size;
        public Dictionary<Point, Color> Data { get; } = Data;
    }
}

public enum Windows {
    MAIN,
    PAINTING,
    ITEMS,
    HEATMAP
}