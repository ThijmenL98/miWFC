using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.Managers;
using miWFC.ViewModels;

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

    private int getPaintBrushSize() {
        int[] sizes = {1, 3, 6, 15, 25};
        return sizes[_paintingSizeCB.SelectedIndex];
    }

    private static Color getGradientColor(double value) {
        Color[] intermediatePoints = {
            Color.Parse("#eff821"), Color.Parse("#f89540"), Color.Parse("#ca4678"), Color.Parse("#7c02a7"),
            Color.Parse("#0c0786")
        };

        if (value <= 0.00000001d) {
            value = 0;
        }

        double percentage = 1d - normalizeValue(value, 0, 250);

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
            _ => Color.Parse("#0b0781")
        };
    }

    private static double normalizeValue(double value, double fromOld, double toOld) {
        return (value - fromOld) / (toOld - fromOld);
    }

    private static Color interpolate(Color c1, Color c2, double percentage) {
        (double h1, double s1, double v1) = RGBtoHSV(c1);
        (double h2, double s2, double v2) = RGBtoHSV(c2);


        double delta = Clamp(h2 - h1 - Math.Floor((h2 - h1) / 360) * 360, 0.0f, 360);
        if (delta > 180) {
            delta -= 360;
        }

        double clampedPercentage = percentage < 0d ? 0d : percentage > 1d ? 1d : percentage;
        double newH = h1 + delta * clampedPercentage;

        return HSVtoRGB(newH,
            percentage * s1 + (1d - percentage) * s2,
            percentage * v1 + (1d - percentage) * v2);
    }

    private static double Clamp(double value, double min, double max) {
        if (value < min) {
            value = min;
        } else if (value > max) {
            value = max;
        }

        return value;
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

        return (h, s, v / 255);
    }

    private static Color HSVtoRGB(double hue, double saturation, double value) {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        return hi switch {
            0 => new Color(255, (byte) v, (byte) t, (byte) p),
            1 => Color.FromArgb(255, (byte) q, (byte) v, (byte) p),
            2 => Color.FromArgb(255, (byte) p, (byte) v, (byte) t),
            3 => Color.FromArgb(255, (byte) p, (byte) q, (byte) v),
            4 => Color.FromArgb(255, (byte) t, (byte) p, (byte) v),
            _ => Color.FromArgb(255, (byte) v, (byte) p, (byte) q)
        };
    }

    public void updateOutput(double[,] currentHeatMap) {
        int outWidth = centralManager!.getMainWindowVM().ImageOutWidth;
        int outHeight = centralManager!.getMainWindowVM().ImageOutHeight;

        WriteableBitmap outputBitmap = new(new PixelSize(outWidth, outHeight), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, outHeight, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < outWidth; x++) {
                    Color toSet = getGradientColor(currentHeatMap[x, (int) y]);
                    dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);
                }
            });
        }

        centralManager!.getMainWindowVM().CurrentHeatmap = outputBitmap;
    }

    public void resetCurrentMapping() {
        if (_selectedTileCB.SelectedItem != null) {
            TileViewModel selectedTVM = (TileViewModel) _selectedTileCB.SelectedItem;

            MainWindowViewModel mainWindowVM = centralManager!.getMainWindowVM();
            int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
            double[,] maskValues = new double[outputWidth, outputHeight];

            for (int x = 0; x < outputWidth; x++) {
                for (int y = 0; y < outputHeight; y++) {
                    maskValues[x, y] = selectedTVM.PatternWeight;
                }
            }

            selectedTVM.WeightHeatMap = maskValues;
            selectedTVM.DynamicWeight = false;

            updateOutput(maskValues);
        }
    }

    public void OutputImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
        // ReSharper disable once IntroduceOptionalParameters.Local
        OutputImageOnPointerMoved(sender, e);
    }

    private int lastPosX = -1, lastPosY = -1;

    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e) {
        if (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) {
            (double posX, double posY) = e.GetPosition(e.Source as Image);
            (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;

            MainWindowViewModel mainWindowVM = centralManager!.getMainWindowVM();
            int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

            int a = (int) Math.Floor(Math.Round(posX) * mainWindowVM.ImageOutWidth / Math.Round(imgWidth)),
                b = (int) Math.Floor(Math.Round(posY) * mainWindowVM.ImageOutHeight / Math.Round(imgHeight));

            if (lastPosX == a && lastPosY == b) {
                return;
            }

            lastPosX = a;
            lastPosY = b;

            int rawBrushSize = getPaintBrushSize();
            double brushSize = rawBrushSize switch {
                1 => rawBrushSize,
                _ => rawBrushSize * 3d
            };

            if (_selectedTileCB.SelectedItem != null) {
                TileViewModel selectedTVM = (TileViewModel) _selectedTileCB.SelectedItem;

                double[,] maskValues = new double[outputWidth, outputHeight];
                if (selectedTVM.WeightHeatMap.Length != 0) {
                    maskValues = selectedTVM.WeightHeatMap;
                }

                double selectedValue = mainWindowVM.HeatmapValue > 0 ? mainWindowVM.HeatmapValue : 0.00000000001d;

                if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
                    for (int x = 0; x < outputWidth; x++) {
                        for (int y = 0; y < outputHeight; y++) {
                            double dx = (double) x - a;
                            double dy = (double) y - b;
                            double distanceSquared = dx * dx + dy * dy;

                            if (mainWindowVM.HardBrushEnabled) {
                                if (distanceSquared <= brushSize) {
                                    maskValues[x, y] = selectedValue;
                                }
                            } else {
                                if (distanceSquared <= brushSize) {
                                    double percentage = Math.Min(1d, 1.2d - distanceSquared / brushSize);
                                    maskValues[x, y] = Math.Round(percentage * selectedValue
                                                                  + (1d - percentage) * maskValues[x, y]);
                                }
                            }
                        }
                    }
                }

                HashSet<double> unique = new();
                HashSet<double> duplicates = new();

                for (int i = 0; i < maskValues.GetLength(0); ++i) {
                    for (int j = 0; j < maskValues.GetLength(1); ++j) {
                        if (!unique.Add(maskValues[i, j])) {
                            duplicates.Add(maskValues[i, j]);
                        }
                    }
                }

                selectedTVM.DynamicWeight = duplicates.Count > 1;
                if (!selectedTVM.DynamicWeight && duplicates.Count == 1) {
                    selectedTVM.PatternWeight = mainWindowVM.HeatmapValue;
                }

                selectedTVM.WeightHeatMap = maskValues;

                updateOutput(maskValues);
            }
        }
    }

    public void setSelectedTile(int patternIndex) {
        foreach (TileViewModel tvm in _selectedTileCB.Items) {
            if (tvm.RawPatternIndex.Equals(patternIndex)) {
                _selectedTileCB.SelectedItem = tvm;
            }
        }
    }

    // ReSharper disable once UnusedParameter.Local
    // ReSharper disable once SuggestBaseTypeForParameter
    private void SelectedTileCB_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        object? selectedItem = (e.Source as ComboBox)?.SelectedItem;
        if (selectedItem != null) {
            TileViewModel selectedTVM = (TileViewModel) selectedItem;

            MainWindowViewModel mainWindowVM = centralManager!.getMainWindowVM();
            int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
            double[,] maskValues = new double[outputWidth, outputHeight];

            if (selectedTVM.WeightHeatMap.Length != 0) {
                maskValues = selectedTVM.WeightHeatMap;
            }

            if (selectedTVM.WeightHeatMap.Length == 0) {
                for (int x = 0; x < outputWidth; x++) {
                    for (int y = 0; y < outputHeight; y++) {
                        maskValues[x, y] = selectedTVM.PatternWeight;
                    }
                }
            } else {
                maskValues = selectedTVM.WeightHeatMap;
            }

            updateOutput(maskValues);
        }
    }
}