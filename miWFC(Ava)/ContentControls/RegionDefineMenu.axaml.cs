using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels;

// ReSharper disable IntroduceOptionalParameters.Local

// ReSharper disable SuggestBaseTypeForParameter

// ReSharper disable UnusedParameter.Local

namespace miWFC.ContentControls;

/// <summary>
/// Separated control for the item addition menu
/// </summary>
public partial class RegionDefineMenu : UserControl {
    private CentralManager? centralManager;

    private int oldBrushSize = -2;

    private bool[,] maskAllowances;

    /*
     * Initializing Functions & Constructor
     */
    public RegionDefineMenu() {
        InitializeComponent();

        maskAllowances = new bool[0, 0];
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    /*
     * Getters
     */

    /// <summary>
    /// Get the selected size of the brush, these values are diameters, not radii
    /// </summary>
    /// 
    /// <returns>Selected brush size</returns>
    private int GetPaintBrushSize() {
        int brushSize = centralManager!.GetMainWindowVM().BrushSize;
        double mappingValue = 13.2d * Math.Exp(0.125 * brushSize) - 15.95d;
        return (int) Math.Round(mappingValue, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Get the allowance of the image appearing on each coordinate
    /// </summary>
    /// 
    /// <returns>Colour matrix</returns>
    public bool[,] GetAllowanceMask() {
        return maskAllowances;
    }

    /*
     * Setters
     */

    /// <summary>
    /// Reset the user editable mask to its defaults
    /// </summary>
    public void ResetAllowanceMask() {
        MainWindowViewModel mainWindowVM = centralManager!.GetMainWindowVM();
        maskAllowances = new bool[mainWindowVM.ImageOutWidth, mainWindowVM.ImageOutHeight];

        for (int x = 0; x < mainWindowVM.ImageOutWidth; x++) {
            for (int y = 0; y < mainWindowVM.ImageOutHeight; y++) {
                maskAllowances[x, y] = true;
            }
        }
    }

    /// <summary>
    /// Set the user editable mask to an item preset
    /// </summary>
    public void SetAllowanceMask(bool[,] itemSelectedAppearanceRegion) {
        maskAllowances = itemSelectedAppearanceRegion;
    }

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Callback to update the brush size image shown to the user
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">AvaloniaPropertyChangedEventArgs</param>
    private void BrushSize_ValueChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        if (!centralManager.GetItemWindow().IsVisible) {
            return;
        }

        int brushSizeRaw = GetPaintBrushSize();

        if (oldBrushSize.Equals(brushSizeRaw)) {
            return;
        }

        if (brushSizeRaw == -1) {
            centralManager!.GetMainWindowVM().PaintingVM.BrushSizeImage = Util.CreateBitmapFromData(3, 3, 1, (x, y) =>
                x == 1 && y == 1 ? Colors.Black : (x + y) % 2 == 0 ? Color.Parse("#11000000") : Colors.Transparent);
            return;
        }

        oldBrushSize = brushSizeRaw;

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
        WriteableBitmap bm = Util.CreateBitmapFromData(cp + 3, cp + 3, 1,
            (x, y) => {
                double dx = x - cp / 2d - 1;
                double dy = y - cp / 2d - 1;
                return dx * dx + dy * dy <= brushSizeRaw ? Colors.Black :
                    (x + y) % 2 == 0 ? Color.Parse("#11000000") : Colors.Transparent;
            });
        centralManager!.GetMainWindowVM().PaintingVM.BrushSizeImage = bm;
    }

    /// <summary>
    /// Callback when the user clicks on the region image, this function is called
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerPressedEventArgs</param>
    private void RegionImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
        (double posX, double posY) = e.GetPosition(e.Source as Image);
        (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;

        bool lbmPressed = e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed;
        bool rbmPressed = e.GetCurrentPoint(e.Source as Image).Properties.IsRightButtonPressed;

        if (lbmPressed || rbmPressed) {
            try {
                HandleMouseClick(posX, posY, imgWidth, imgHeight, (sender as Image)!, lbmPressed);
            } catch (IndexOutOfRangeException) { }
        }
    }

    /// <summary>
    /// Callback when the user moves while clicking clicks on the region image, this function is called
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerPressedEventArgs</param>
    private void RegionImageOnPointerMoved(object? sender, PointerEventArgs e) {
        (double posX, double posY) = e.GetPosition(e.Source as Image);
        (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;

        bool lbmPressed = e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed;
        bool rbmPressed = e.GetCurrentPoint(e.Source as Image).Properties.IsRightButtonPressed;

        if (lbmPressed || rbmPressed) {
            try {
                HandleMouseClick(posX, posY, imgWidth, imgHeight, (sender as Image)!, lbmPressed);
            } catch (IndexOutOfRangeException) { }
        }
    }

    /// <summary>
    /// Handling of the clicking the mouse on the output
    /// </summary>
    /// 
    /// <param name="clickX">X position of the mouse</param>
    /// <param name="clickY">Y position of the mouse</param>
    /// <param name="imgWidth">Image width</param>
    /// <param name="imgHeight">Image height</param>
    /// <param name="imageSource">Source of the mouse movement call</param>
    /// <param name="add">Whether the left mouse button is pressed</param>
    private void HandleMouseClick(double clickX, double clickY, double imgWidth, double imgHeight,
        ILayoutable imageSource, bool add) {
        MainWindowViewModel mainWindowVM = centralManager!.GetMainWindowVM();
        int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / imgWidth),
            b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / imgHeight);

        int brushSize = GetPaintBrushSize();

        int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

        if (a < mainWindowVM.ImageOutWidth && b < mainWindowVM.ImageOutHeight) {
            if (brushSize == -1) {
                maskAllowances[a, b] = add;
            } else {
                for (int x = 0; x < outputWidth; x++) {
                    for (int y = 0; y < outputHeight; y++) {
                        double dx = (double) x - a;
                        double dy = (double) y - b;
                        double distanceSquared = dx * dx + dy * dy;

                        if (distanceSquared <= brushSize) {
                            maskAllowances[x, y] = add;
                        }
                    }
                }
            }
        }

        UpdateMask(maskAllowances);
    }

    /// <summary>
    /// Create and apply image representation of the user created mask
    /// </summary>
    /// 
    /// <param name="colors">Mask colours</param>
    private void UpdateMask(bool[,] colors) {
        MainWindowViewModel mainWindowVM = centralManager!.GetMainWindowVM();
        mainWindowVM.ItemVM.RegionImage = Util.CreateBitmapFromData(mainWindowVM.ImageOutWidth,
            mainWindowVM.ImageOutHeight, 1, (x, y) => colors[x, y] ? Colors.Green : Colors.Red);
    }
}