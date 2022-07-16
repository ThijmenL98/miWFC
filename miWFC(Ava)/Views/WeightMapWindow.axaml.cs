using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Managers;
using miWFC.ViewModels;
using miWFC.ViewModels.Structs;
using static miWFC.Utils.Util;

// ReSharper disable UnusedParameter.Local

namespace miWFC.Views;

/// <summary>
///     Window that handles the dynamic weight mapping of the application
/// </summary>
public partial class WeightMapWindow : Window {
    private readonly ComboBox _selectedTileCB;
    private CentralManager? centralManager;

    private double[,] currentHeatMap = new double[0, 0];

    private int lastPosX = -1, lastPosY = -1;

    private int oldBrushSize = -2, oldColourValue = -2;
    private bool oldHardnessValue;

    /*
     * Initializing Functions & Constructor
     */

    public WeightMapWindow() {
        InitializeComponent();

        KeyDown += KeyDownHandler;
        Closing += (_, e) => {
            centralManager?.GetUIManager().SwitchWindow(Windows.MAIN, true);
            e.Cancel = true;
        };

        _selectedTileCB = this.Find<ComboBox>("selectedTileCB");
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public void SetCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    ///     Get the selected size of the brush, these values are diameters, not radii
    /// </summary>
    /// <returns>Selected brush size</returns>
    private int GetPaintBrushSize() {
        int brushSize = centralManager!.GetMainWindowVM().BrushSize;
        double mappingValue = 13.2d * Math.Exp(0.125 * brushSize) - 15.95d;
        return (int) Math.Round(mappingValue, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    ///     Get the value of the weight mapping at the desired position
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <returns>Value of the gradient at the desired position</returns>
    public double GetGradientValue(int x, int y) {
        if (currentHeatMap.GetLength(0) <= x || currentHeatMap.GetLength(1) <= y) {
            return 0d;
        }

        return currentHeatMap[x, y];
    }

    // Booleans

    // Images

    // Objects

    /// <summary>
    ///     Get a colour representation of a 0-1 value on the inferno colour spectrum
    /// </summary>
    /// <param name="value">Value to get a colour representation of</param>
    /// <returns>Hex-colour representation of the input value</returns>
    private static Color GetGradientColor(double value) {
        Color[] intermediatePoints = {
            Color.Parse("#eff821"), Color.Parse("#f89540"), Color.Parse("#ca4678"), Color.Parse("#7c02a7"),
            Color.Parse("#0c0786")
        };

        if (value <= 0.00000001d) {
            value = 0;
        }

        double percentage = 1d - NormalizeValue(value, 0, 250);

        return percentage switch {
            0d => intermediatePoints[0],
            < 0.25d => Interpolate(intermediatePoints[0], intermediatePoints[1], NormalizeValue(percentage, 0d, 0.25d)),
            0.25d => intermediatePoints[1],
            < 0.5d => Interpolate(intermediatePoints[1], intermediatePoints[2],
                NormalizeValue(percentage, 0.25d, 0.5d)),
            0.5d => intermediatePoints[2],
            < 0.75d => Interpolate(intermediatePoints[2], intermediatePoints[3],
                NormalizeValue(percentage, 0.5d, 0.75d)),
            0.75d => intermediatePoints[3],
            < 1d => Interpolate(intermediatePoints[3], intermediatePoints[4], NormalizeValue(percentage, 0.75d, 1d)),
            _ => Color.Parse("#0b0781")
        };
    }

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Custom handler for keyboard input
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private void KeyDownHandler(object? sender, KeyEventArgs e) {
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
    ///     Forwarding function to OutputImageOnPointerMoved(object, PointerEventArgs, bool)
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerPressedEventArgs</param>
    public void OutputImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
        OutputImageOnPointerMoved(sender, e);
    }

    /// <summary>
    ///     Callback when the user moves or clicks on the output image to adjust weights depending on the brush size at
    ///     the clicked location
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerEventArgs</param>
    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e) {
        (double posX, double posY) = e.GetPosition(e.Source as Image);
        Image imageSource = (sender as Image)!;

        MainWindowViewModel mainWindowVM = centralManager!.GetMainWindowVM();
        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        double imgWidth = imageSource.DesiredSize.Width - imageSource.Margin.Right - imageSource.Margin.Left;
        double imgHeight = imageSource.DesiredSize.Height - imageSource.Margin.Top - imageSource.Margin.Bottom;

        int a = (int) Math.Floor(Math.Round(posX) * outputWidth / Math.Round(imgWidth)),
            b = (int) Math.Floor(Math.Round(posY) * outputHeight / Math.Round(imgHeight));

        if (lastPosX == a && lastPosY == b && !e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) {
            return;
        }

        lastPosX = a;
        lastPosY = b;

        int brushSize = GetPaintBrushSize();

        if (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) {
            if (_selectedTileCB.SelectedItem != null) {
                TileViewModel selectedTVM = (TileViewModel) _selectedTileCB.SelectedItem;

                double[,] maskValues = new double[outputWidth, outputHeight];
                if (selectedTVM.WeightHeatMap.Length != 0) {
                    maskValues = selectedTVM.WeightHeatMap;
                }

                double selectedValue = mainWindowVM.MappingVM.HeatmapValue > 0
                    ? mainWindowVM.MappingVM.HeatmapValue
                    : 0.00000000001d;

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

                UpdateOutput(maskValues);
            }
        }

        centralManager.GetMainWindowVM().MappingVM.HoverImage = CreateBitmapFromData(
            centralManager!.GetMainWindowVM().ImageOutWidth, centralManager!.GetMainWindowVM().ImageOutHeight, 1,
            (x, y) => {
                double dx = (double) x - a;
                double dy = (double) y - b;
                double distanceSquared = dx * dx + dy * dy;

                if (mainWindowVM.MappingVM.HardBrushEnabled) {
                    if (distanceSquared <= brushSize) {
                        return Colors.Yellow;
                    }
                } else {
                    if (distanceSquared <= brushSize) {
                        double percentage = distanceSquared / brushSize;
                        double percentageMapped = 1d - (percentage * 0.5d + 0.5d);
                        return Color.FromArgb((byte) (percentageMapped * 255), 255, 255, 0);
                    }
                }

                return Colors.Transparent;
            }
        );
    }

    /// <summary>
    ///     Callback when the user changes the currently selected tile to set the mapping for
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    private void SelectedTileCB_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        object? selectedItem = (e.Source as ComboBox)?.SelectedItem;
        if (selectedItem != null) {
            TileViewModel selectedTVM = (TileViewModel) selectedItem;

            MainWindowViewModel mainWindowVM = centralManager!.GetMainWindowVM();
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

            UpdateOutput(maskValues);
        }
    }

    /*
     * Functions
     */

    /// <summary>
    ///     Function to reset the current mapping and update the output image shown to the user
    /// </summary>
    public void ResetCurrentMapping() {
        if (_selectedTileCB.SelectedItem != null) {
            TileViewModel selectedTVM = (TileViewModel) _selectedTileCB.SelectedItem;

            MainWindowViewModel mainWindowVM = centralManager!.GetMainWindowVM();
            int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;
            double[,] maskValues = new double[outputWidth, outputHeight];

            for (int x = 0; x < outputWidth; x++) {
                for (int y = 0; y < outputHeight; y++) {
                    maskValues[x, y] = selectedTVM.PatternWeight;
                }
            }

            selectedTVM.WeightHeatMap = maskValues;
            selectedTVM.DynamicWeight = false;

            UpdateOutput(maskValues);
        }
    }

    /// <summary>
    ///     Function to set the currently selected tile forcibly based on index
    /// </summary>
    /// <param name="patternIndex">index to select</param>
    public void SetSelectedTile(int patternIndex) {
        foreach (TileViewModel tvm in _selectedTileCB.Items) {
            if (tvm.RawPatternIndex.Equals(patternIndex)) {
                _selectedTileCB.SelectedItem = tvm;
            }
        }
    }

    /// <summary>
    ///     Function to update the output image based on the current mapping
    /// </summary>
    /// <param name="newCurrentHeatMap">Current weight mapping</param>
    public void UpdateOutput(double[,] newCurrentHeatMap) {
        currentHeatMap = newCurrentHeatMap;
        int outWidth = centralManager!.GetMainWindowVM().ImageOutWidth;
        int outHeight = centralManager!.GetMainWindowVM().ImageOutHeight;

        WriteableBitmap outputBitmap
            = CreateBitmapFromData(outWidth, outHeight, 1, (x, y) => GetGradientColor(currentHeatMap[x, y]));

        centralManager!.GetMainWindowVM().MappingVM.CurrentHeatmap = outputBitmap;
    }

    /// <summary>
    ///     Function to forcibly set the weight mapping of the currently selected tile to a new mapping
    /// </summary>
    /// <param name="maskValues">The new weight mapping</param>
    public void SetCurrentMapping(double[,] maskValues) {
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

    /// <summary>
    ///     Callback to update the brush size image shown to the user
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">AvaloniaPropertyChangedEventArgs</param>
    private void BrushSize_ValueChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        BrushSize_ValueChanged(false);
    }

    /// <summary>
    ///     Callback to update the brush size image shown to the user
    /// </summary>
    /// <param name="force">Whether to force the creation of the bitmap</param>
    private void BrushSize_ValueChanged(bool force) {
        if (centralManager == null) {
            return;
        }

        if (!centralManager.GetWeightMapWindow().IsVisible) {
            return;
        }

        int brushSizeRaw = GetPaintBrushSize();

        if (oldBrushSize.Equals(brushSizeRaw) && !force) {
            return;
        }

        oldBrushSize = brushSizeRaw;

        if (brushSizeRaw == -1) {
            centralManager!.GetMainWindowVM().PaintingVM.BrushSizeImage = CreateBitmapFromData(3, 3, 1, (x, y) =>
                x == 1 && y == 1 ? GetGradientColor(centralManager!.GetMainWindowVM().MappingVM.HeatmapValue) :
                (x + y) % 2 == 0 ? Color.Parse("#11000000") : Colors.Transparent);
            return;
        }

        int brushSize = Math.Max(5, brushSizeRaw);
        bool[,] brushImageRaw = new bool[brushSize, brushSize];
        int centerPoint = (int) Math.Round(brushSize / 2d);
        int minX = int.MaxValue, maxX = int.MinValue;

        for (int x = 0; x < brushSize; x++) {
            for (int y = 0; y < brushSize; y++) {
                double dx = (double) x - centerPoint;
                double dy = (double) y - centerPoint;
                double distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= brushSizeRaw) {
                    brushImageRaw[x, y] = true;

                    if (x < minX) {
                        minX = x;
                    }

                    if (x > maxX) {
                        maxX = x;
                    }
                }
            }
        }

        int cp = maxX - minX;
        centralManager!.GetMainWindowVM().PaintingVM.BrushSizeImage = CreateBitmapFromData(cp + 3,
            cp + 3, 1,
            (x, y) => {
                double dx = x - cp / 2d - 1;
                double dy = y - cp / 2d - 1;
                Color selected = GetGradientColor(centralManager!.GetMainWindowVM().MappingVM.HeatmapValue);
                double distance = dx * dx + dy * dy;
                return distance <= brushSizeRaw ? centralManager!.GetMainWindowVM().MappingVM.HardBrushEnabled
                        ? selected
                        : Color.FromArgb((byte) (255 * (1d - distance / brushSizeRaw * 0.85d)), selected.R,
                            selected.G, selected.B) :
                    (x + y) % 2 == 0 ? Colors.Transparent : Color.Parse("#11000000");
            });
    }

    /// <summary>
    ///     Callback when the colour slider value is changed
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">AvaloniaPropertyChangedEventArgs</param>
    private void ColourSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        if (oldColourValue.Equals(centralManager!.GetMainWindowVM().MappingVM.HeatmapValue)) {
            return;
        }

        oldColourValue = centralManager!.GetMainWindowVM().MappingVM.HeatmapValue;

        BrushSize_ValueChanged(true);
    }


    /// <summary>
    ///     Callback when the hardness toggle value is changed
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">AvaloniaPropertyChangedEventArgs</param>
    private void AvaloniaObject_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        if (oldHardnessValue.Equals(centralManager!.GetMainWindowVM().MappingVM.HardBrushEnabled)) {
            return;
        }

        oldHardnessValue = centralManager!.GetMainWindowVM().MappingVM.HardBrushEnabled;

        BrushSize_ValueChanged(true);
    }
}