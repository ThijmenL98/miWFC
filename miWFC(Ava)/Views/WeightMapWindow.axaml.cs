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
using miWFC.ViewModels.Structs;
using static miWFC.Utils.Util;
// ReSharper disable UnusedParameter.Local

namespace miWFC.Views;

/// <summary>
/// Window that handles the dynamic weight mapping of the application
/// </summary>
public partial class WeightMapWindow : Window {
    private readonly ComboBox _selectedTileCB, _paintingSizeCB;
    private CentralManager? centralManager;

    private int lastPosX = -1, lastPosY = -1;

    private double[,] currentHeatMap = new double[0, 0];

    /*
     * Initializing Functions & Constructor
     */

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

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    /// Get the selected size of the brush, these values are diameters, not radii
    /// </summary>
    /// 
    /// <returns>Selected brush size</returns>
    private int getPaintBrushSize() {
        int[] sizes = {1, 3, 6, 15, 25};
        return sizes[_paintingSizeCB.SelectedIndex];
    }

    /// <summary>
    /// Get the value of the weight mapping at the desired position
    /// </summary>
    /// 
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// 
    /// <returns>Value of the gradient at the desired position</returns>
    public double getGradientValue(int x, int y) {
        if (currentHeatMap.GetLength(0) <= x || currentHeatMap.GetLength(1) <= y) {
            return 0d;
        }

        return currentHeatMap[x, y];
    }

    // Booleans

    // Images

    // Objects

    /// <summary>
    /// Get a colour representation of a 0-1 value on the inferno colour spectrum
    /// </summary>
    /// 
    /// <param name="value">Value to get a colour representation of</param>
    /// 
    /// <returns>Hex-colour representation of the input value</returns>
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

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Custom handler for keyboard input
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private void keyDownHandler(object? sender, KeyEventArgs e) {
        if (centralManager == null) {
            return;
        }

        switch (e.Key) {
            default:
                base.OnKeyDown(e);
                break;
        }
    }

    /// <summary>
    /// Forwarding function to OutputImageOnPointerMoved(object, PointerEventArgs, bool)
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerPressedEventArgs</param>
    public void OutputImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
        OutputImageOnPointerMoved(sender, e);
    }

    /// <summary>
    /// Callback when the user moves or clicks on the output image to adjust weights depending on the brush size at
    /// the clicked location
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerEventArgs</param>
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

                double selectedValue = mainWindowVM.MappingVM.HeatmapValue > 0 ? mainWindowVM.MappingVM.HeatmapValue : 0.00000000001d;

                if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
                    for (int x = 0; x < outputWidth; x++) {
                        for (int y = 0; y < outputHeight; y++) {
                            double dx = (double) x - a;
                            double dy = (double) y - b;
                            double distanceSquared = dx * dx + dy * dy;

                            if (mainWindowVM.MappingVM.HardBrushEnabled) {
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
                    selectedTVM.PatternWeight = mainWindowVM.MappingVM.HeatmapValue;
                }

                selectedTVM.WeightHeatMap = maskValues;

                updateOutput(maskValues);
            }
        }
    }

    /// <summary>
    /// Callback when the user changes the currently selected tile to set the mapping for
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
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

    /*
     * Functions
     */

    /// <summary>
    /// Function to reset the current mapping and update the output image shown to the user
    /// </summary>
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

    /// <summary>
    /// Function to set the currently selected tile forcibly based on index
    /// </summary>
    ///
    /// <param name="patternIndex">index to select</param>
    public void setSelectedTile(int patternIndex) {
        foreach (TileViewModel tvm in _selectedTileCB.Items) {
            if (tvm.RawPatternIndex.Equals(patternIndex)) {
                _selectedTileCB.SelectedItem = tvm;
            }
        }
    }

    /// <summary>
    /// Function to update the output image based on the current mapping
    /// </summary>
    /// 
    /// <param name="newCurrentHeatMap">Current weight mapping</param>
    public void updateOutput(double[,] newCurrentHeatMap) {
        currentHeatMap = newCurrentHeatMap;
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

        centralManager!.getMainWindowVM().MappingVM.CurrentHeatmap = outputBitmap;
    }

    /// <summary>
    /// Function to forcibly set the weight mapping of the currently selected tile to a new mapping
    /// </summary>
    /// 
    /// <param name="maskValues">The new weight mapping</param>
    public void setCurrentMapping(double[,] maskValues) {
        if (_selectedTileCB.SelectedItem != null) {
            TileViewModel selectedTVM = (TileViewModel) _selectedTileCB.SelectedItem;
            selectedTVM.WeightHeatMap = maskValues;

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
        }
    }
}