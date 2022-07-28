using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Delegators;
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
    private CentralDelegator? centralDelegator;

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
            centralDelegator?.GetInterfaceHandler().SwitchWindow(Windows.MAIN, true);
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

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
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
    public int GetPaintBrushSize() {
        int brushSize = centralDelegator!.GetMainWindowVM().BrushSize;
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
        string[] colorsStrings = {
            "#0d0887", "#100788", "#130789", "#16078a", "#19068c", "#1b068d", "#1d068e", "#20068f", "#220690",
            "#240691", "#260591", "#280592", "#2a0593", "#2c0594", "#2e0595", "#2f0596", "#310597", "#330597",
            "#350498", "#370499", "#38049a", "#3a049a", "#3c049b", "#3e049c", "#3f049c", "#41049d", "#43039e",
            "#44039e", "#46039f", "#48039f", "#4903a0", "#4b03a1", "#4c02a1", "#4e02a2", "#5002a2", "#5102a3",
            "#5302a3", "#5502a4", "#5601a4", "#5801a4", "#5901a5", "#5b01a5", "#5c01a6", "#5e01a6", "#6001a6",
            "#6100a7", "#6300a7", "#6400a7", "#6600a7", "#6700a8", "#6900a8", "#6a00a8", "#6c00a8", "#6e00a8",
            "#6f00a8", "#7100a8", "#7201a8", "#7401a8", "#7501a8", "#7701a8", "#7801a8", "#7a02a8", "#7b02a8",
            "#7d03a8", "#7e03a8", "#8004a8", "#8104a7", "#8305a7", "#8405a7", "#8606a6", "#8707a6", "#8808a6",
            "#8a09a5", "#8b0aa5", "#8d0ba5", "#8e0ca4", "#8f0da4", "#910ea3", "#920fa3", "#9410a2", "#9511a1",
            "#9613a1", "#9814a0", "#99159f", "#9a169f", "#9c179e", "#9d189d", "#9e199d", "#a01a9c", "#a11b9b",
            "#a21d9a", "#a31e9a", "#a51f99", "#a62098", "#a72197", "#a82296", "#aa2395", "#ab2494", "#ac2694",
            "#ad2793", "#ae2892", "#b02991", "#b12a90", "#b22b8f", "#b32c8e", "#b42e8d", "#b52f8c", "#b6308b",
            "#b7318a", "#b83289", "#ba3388", "#bb3488", "#bc3587", "#bd3786", "#be3885", "#bf3984", "#c03a83",
            "#c13b82", "#c23c81", "#c33d80", "#c43e7f", "#c5407e", "#c6417d", "#c7427c", "#c8437b", "#c9447a",
            "#ca457a", "#cb4679", "#cc4778", "#cc4977", "#cd4a76", "#ce4b75", "#cf4c74", "#d04d73", "#d14e72",
            "#d24f71", "#d35171", "#d45270", "#d5536f", "#d5546e", "#d6556d", "#d7566c", "#d8576b", "#d9586a",
            "#da5a6a", "#da5b69", "#db5c68", "#dc5d67", "#dd5e66", "#de5f65", "#de6164", "#df6263", "#e06363",
            "#e16462", "#e26561", "#e26660", "#e3685f", "#e4695e", "#e56a5d", "#e56b5d", "#e66c5c", "#e76e5b",
            "#e76f5a", "#e87059", "#e97158", "#e97257", "#ea7457", "#eb7556", "#eb7655", "#ec7754", "#ed7953",
            "#ed7a52", "#ee7b51", "#ef7c51", "#ef7e50", "#f07f4f", "#f0804e", "#f1814d", "#f1834c", "#f2844b",
            "#f3854b", "#f3874a", "#f48849", "#f48948", "#f58b47", "#f58c46", "#f68d45", "#f68f44", "#f79044",
            "#f79143", "#f79342", "#f89441", "#f89540", "#f9973f", "#f9983e", "#f99a3e", "#fa9b3d", "#fa9c3c",
            "#fa9e3b", "#fb9f3a", "#fba139", "#fba238", "#fca338", "#fca537", "#fca636", "#fca835", "#fca934",
            "#fdab33", "#fdac33", "#fdae32", "#fdaf31", "#fdb130", "#fdb22f", "#fdb42f", "#fdb52e", "#feb72d",
            "#feb82c", "#feba2c", "#febb2b", "#febd2a", "#febe2a", "#fec029", "#fdc229", "#fdc328", "#fdc527",
            "#fdc627", "#fdc827", "#fdca26", "#fdcb26", "#fccd25", "#fcce25", "#fcd025", "#fcd225", "#fbd324",
            "#fbd524", "#fbd724", "#fad824", "#fada24", "#f9dc24", "#f9dd25", "#f8df25", "#f8e125", "#f7e225",
            "#f7e425", "#f6e626", "#f6e826", "#f5e926", "#f5eb27", "#f4ed27", "#f3ee27", "#f3f027", "#f2f227",
            "#f1f426", "#f1f525", "#f0f724", "#f0f921"
        };

        return Color.Parse(colorsStrings[(int) value]);
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
        if (centralDelegator == null) {
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

        MainWindowViewModel mainWindowVM = centralDelegator!.GetMainWindowVM();
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

                            if (brushSize == -1 && distanceSquared == 0) {
                                maskValues[x, y] = selectedValue;
                            } else {
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

        centralDelegator.GetMainWindowVM().MappingVM.HoverImage = CreateBitmapFromData(
            centralDelegator!.GetMainWindowVM().ImageOutWidth, centralDelegator!.GetMainWindowVM().ImageOutHeight, 1,
            (x, y) => {
                double dx = (double) x - a;
                double dy = (double) y - b;
                double distanceSquared = dx * dx + dy * dy;
                
                if (brushSize == -1 && distanceSquared == 0) {
                    return Colors.Yellow;
                } else {
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
        if (centralDelegator == null) {
            return;
        }

        object? selectedItem = (e.Source as ComboBox)?.SelectedItem;
        if (selectedItem != null) {
            TileViewModel selectedTVM = (TileViewModel) selectedItem;

            MainWindowViewModel mainWindowVM = centralDelegator!.GetMainWindowVM();
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

            MainWindowViewModel mainWindowVM = centralDelegator!.GetMainWindowVM();
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
        int outWidth = centralDelegator!.GetMainWindowVM().ImageOutWidth;
        int outHeight = centralDelegator!.GetMainWindowVM().ImageOutHeight;
        
        WriteableBitmap outputBitmap
            = CreateBitmapFromData(outWidth, outHeight, 1, (x, y) => GetGradientColor(currentHeatMap[x, y]));

        centralDelegator!.GetMainWindowVM().MappingVM.CurrentHeatmap = outputBitmap;
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
        if (centralDelegator == null) {
            return;
        }

        if (!centralDelegator.GetWeightMapWindow().IsVisible) {
            return;
        }

        int brushSizeRaw = GetPaintBrushSize();
        if (oldBrushSize.Equals(brushSizeRaw) && !force) {
            return;
        }

        if (brushSizeRaw == -1) {
            centralDelegator!.GetMainWindowVM().PaintingVM.BrushSizeImage = CreateBitmapFromData(3, 3, 1, (x, y) =>
                x == 1 && y == 1 ? GetGradientColor(centralDelegator!.GetMainWindowVM().MappingVM.HeatmapValue) :
                (x + y) % 2 == 0 ? Color.Parse("#11000000") : Colors.Transparent);
            oldBrushSize = brushSizeRaw;
            return;
        }

        UpdateBrushImage(brushSizeRaw);
    }

    /// <summary>
    /// Function to update the brush size visual with the current brush size
    /// </summary>
    /// 
    /// <param name="rawBrushSize">Raw brush size selected by the user</param>
    public void UpdateBrushImage(int rawBrushSize) {
        oldBrushSize = rawBrushSize;

        int brushSize = Math.Max(5, rawBrushSize);
        int centerPoint = (int) Math.Round(brushSize / 2d);

        int minX = int.MaxValue, maxX = int.MinValue;

        for (int x = 0; x < brushSize; x++) {
            for (int y = 0; y < brushSize; y++) {
                double dx = (double) x - centerPoint;
                double dy = (double) y - centerPoint;
                double distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= rawBrushSize) {
                    if (x < minX) {
                        minX = x;
                    }

                    if (x > maxX) {
                        maxX = x;
                    }
                }
            }
        }

        int centrePoint = maxX - minX;

        centralDelegator!.GetMainWindowVM().PaintingVM.BrushSizeImage = CreateBitmapFromData(centrePoint + 3,
            centrePoint + 3, 1,
            (x, y) => {
                double dx = x - centrePoint / 2d - 1;
                double dy = y - centrePoint / 2d - 1;
                Color selected = GetGradientColor(centralDelegator!.GetMainWindowVM().MappingVM.HeatmapValue);
                double distance = dx * dx + dy * dy;
                return distance <= rawBrushSize ? centralDelegator!.GetMainWindowVM().MappingVM.HardBrushEnabled
                        ? selected
                        : Color.FromArgb((byte) (255 * (1d - distance / rawBrushSize * 0.85d)), selected.R,
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
        if (centralDelegator == null) {
            return;
        }

        if (oldColourValue.Equals(centralDelegator!.GetMainWindowVM().MappingVM.HeatmapValue)) {
            return;
        }

        oldColourValue = centralDelegator!.GetMainWindowVM().MappingVM.HeatmapValue;

        BrushSize_ValueChanged(true);
    }


    /// <summary>
    ///     Callback when the hardness toggle value is changed
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">AvaloniaPropertyChangedEventArgs</param>
    private void AvaloniaObject_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        if (oldHardnessValue.Equals(centralDelegator!.GetMainWindowVM().MappingVM.HardBrushEnabled)) {
            return;
        }

        oldHardnessValue = centralDelegator!.GetMainWindowVM().MappingVM.HardBrushEnabled;

        BrushSize_ValueChanged(true);
    }
}