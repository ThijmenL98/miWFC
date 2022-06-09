using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.Managers;

namespace miWFC.Views;

public partial class WeightMapWindow : Window {
    private readonly ComboBox _selectedTileCB, _paintingSizeCB;
    private CentralManager? centralManager;

    public WeightMapWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
        Closing += (_, e) => {
            centralManager?.getUIManager().switchWindow(Windows.MAIN, true);
            e.Cancel = true;
        };

        _selectedTileCB = this.Find<ComboBox>("selectedTileCB");
        _paintingSizeCB = this.Find<ComboBox>("BrushSizeCB");
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDevTools();
#endif
    }

    /*
     * Event Handlers
     */

    private void keyDownHandler(object? sender, KeyEventArgs e) {
        if (centralManager == null) {
            return;
        }

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (e.Key) {
            default:
                base.OnKeyDown(e);
                break;
        }
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    public int getSelectedPaintIndex() {
        return _selectedTileCB.SelectedIndex;
    }

    public int getPaintBrushSize() {
        int[] sizes = {-1, 1, 2, 3, 6, 10, 15, 25};
        return sizes[_paintingSizeCB.SelectedIndex];
    }

    public static Color getGradientColor(int min, int max, int value) {
        Color[] intermediatePoints = {
            Color.Parse("#eff821"), Color.Parse("#f89540"), Color.Parse("#ca4678"), Color.Parse("#7c02a7"),
            Color.Parse("#0c0786")
        };

        double percentage = normalizeValue(value, min, max);

        return percentage switch {
            0d => intermediatePoints[0],
            < 0.25d => interpolate(intermediatePoints[0], intermediatePoints[1], normalizeValue(percentage, 0d, 0.25d)),
            0.25d => intermediatePoints[1],
            < 0.5d => interpolate(intermediatePoints[1], intermediatePoints[2],
                normalizeValue(percentage, 0.25d, 0.5d)),
            0.5d => intermediatePoints[2],
            < 0.75d => interpolate(intermediatePoints[2], intermediatePoints[3],
                normalizeValue(percentage, 0.5d, 0.75d)),
            0.75d => intermediatePoints[3],
            < 1d => interpolate(intermediatePoints[3], intermediatePoints[4], normalizeValue(percentage, 0.75d, 1d)),
            _ => intermediatePoints[4]
        };
    }

    private static double normalizeValue(double value, double fromOld, double toOld) {
        return (value - fromOld) / (toOld - fromOld);
    }

    private static Color interpolate(Color c1, Color c2, double percentage) {
        Trace.WriteLine(RGBtoHSV(c1));
        //TODO
        return Colors.Black;
    }

    private static (double, double, double) RGBtoHSV(Color rgb) {
        double h = 0, s;

        double min = Math.Min(Math.Min(rgb.R, rgb.G), rgb.B);
        double v = Math.Max(Math.Max(rgb.R, rgb.G), rgb.B);
        double delta = v - min;

        if (v == 0.0) {
            s = 0;
        } else {
            s = delta / v;
        }

        if (s == 0) {
            h = 0.0;
        } else {
            // ReSharper disable trice CompareOfFloatsByEqualityOperator
            if (rgb.R == v) {
                h = (rgb.G - rgb.B) / delta;
            } else if (rgb.G == v) {
                h = 2 + (rgb.B - rgb.R) / delta;
            } else if (rgb.B == v) {
                h = 4 + (rgb.R - rgb.G) / delta;
            }

            h *= 60;

            if (h < 0.0) {
                h += 360;
            }
        }

        return (h, s, (v / 255));
    }

    private static Color HSVtoRGB(double h, double s, double v) {
        return Colors.Black;
        //TODO
    }

    public void updateOutput(double[,] currentHeatMap) {
        int outWidth = centralManager!.getMainWindowVM().ImageOutWidth,
            outHeight = centralManager!.getMainWindowVM().ImageOutHeight;
        WriteableBitmap outputBitmap = new(new PixelSize(outWidth, outHeight), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        //TODO
        getGradientColor(0, 100, 1);
        getGradientColor(0, 100, 50);
        getGradientColor(0, 100, 99);

        centralManager!.getMainWindowVM().CurrentHeatmap = outputBitmap;
    }

    public void OutputImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
        // ReSharper disable once IntroduceOptionalParameters.Local
        OutputImageOnPointerMoved(sender, e);
    }

    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e) {
        //TODO
        if (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) {
            Trace.WriteLine("CLick");
        }
    }
}