﻿using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Delegators;
using miWFC.Utils;
using miWFC.ViewModels.Structs;

// ReSharper disable UnusedParameter.Local

namespace miWFC.Views;

/// <summary>
///     Window that handles the painting features of the application
/// </summary>
public partial class PaintingWindow : Window {
    private readonly ComboBox _paintingPatternsCB, _templatesCB;

    private bool canUsePencil = true;
    private CentralDelegator? centralDelegator;

    private int oldBrushSize = -2;

    /*
     * Initializing Functions & Constructor
     */

    public PaintingWindow() {
        InitializeComponent();

        KeyDown += KeyDownHandler;
        Closing += (_, e) => {
            Color[,] mask = centralDelegator!.GetOutputHandler().GetMaskColours();
            centralDelegator?.GetInterfaceHandler()
                .SwitchWindow(Windows.MAIN, !(mask[0, 0] == Util.negativeColour || mask[0, 0] == Util.positiveColour));
            e.Cancel = true;
        };

        _paintingPatternsCB = this.Find<ComboBox>("tilePaintSelectCB");
        _templatesCB = this.Find<ComboBox>("templateCB");
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
     * Getters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    ///     Get the index of the currently selected tile or pattern to paint with
    /// </summary>
    /// <returns>Selected pattern index</returns>
    public int GetSelectedPaintIndex() {
        return _paintingPatternsCB.SelectedIndex;
    }

    /// <summary>
    ///     Get the index of the currently selected pattern to place in the output
    /// </summary>
    /// <returns>Selected template index</returns>
    public int GetSelectedTemplateIndex() {
        return _templatesCB.SelectedIndex;
    }

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
    ///     Get the width of the timeline UI element
    /// </summary>
    /// <returns>Width</returns>
    public double GetTimelineWidth() {
        return this.Find<Grid>("timeline").Bounds.Width;
    }

    // Booleans

    // Images

    // Objects

    // Lists

    // Other

    /*
     * Setters
     */

    /// <summary>
    ///     Set the painting patterns, decided by the selected input image
    /// </summary>
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void SetPaintingPatterns(TileViewModel[]? values, int idx = 0) {
        if (values != null) {
            _paintingPatternsCB.Items = values;
            centralDelegator!.GetMainWindowVM().PaintTiles = new ObservableCollection<TileViewModel>(values);
        }

        _paintingPatternsCB.SelectedIndex = idx;
    }


    /// <summary>
    ///     Set the placeable templates, decided by the selected input image
    /// </summary>
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void SetTemplates(ObservableCollection<TemplateViewModel> values, int idx = 0) {
        if (centralDelegator == null || centralDelegator.GetWFCHandler().IsChangingModels()) {
            return;
        }

        _templatesCB.Items = values;
        centralDelegator!.GetMainWindowVM().PaintingVM.Templates = new ObservableCollection<TemplateViewModel>(values);
        _templatesCB.SelectedIndex = idx;
    }

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Custom handler for keyboard input
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private async void KeyDownHandler(object? sender, KeyEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        switch (e.Key) {
            case Key.P:
                centralDelegator.GetMainWindowVM().PaintingVM.ActivatePencilMode();
                e.Handled = true;
                break;
            case Key.B:
                centralDelegator.GetMainWindowVM().PaintingVM.ActivatePaintMode();
                e.Handled = true;
                break;
            case Key.R:
                centralDelegator.GetMainWindowVM().PaintingVM.ResetMask();
                e.Handled = true;
                break;
            case Key.A:
                if (centralDelegator.GetMainWindowVM().PaintingVM.PaintModeEnabled) {
                    await centralDelegator.GetMainWindowVM().PaintingVM.ApplyPaintMask();
                } else if (centralDelegator.GetMainWindowVM().PaintingVM.TemplateCreationModeEnabled) {
                    centralDelegator.GetMainWindowVM().PaintingVM.CreateTemplate();
                }

                e.Handled = true;
                break;
            case Key.Back:
            case Key.Escape:
                if (centralDelegator.GetInterfaceHandler().PopUpOpened()) {
                    centralDelegator.GetInterfaceHandler().HidePopUp();
                } else {
                    Color[,] mask = centralDelegator!.GetOutputHandler().GetMaskColours();
                    centralDelegator?.GetInterfaceHandler()
                        .SwitchWindow(Windows.MAIN, !(mask[0, 0] == Util.negativeColour || mask[0, 0] == Util.positiveColour));
                }

                e.Handled = true;
                break;
            case Key.T:
                if ((e.KeyModifiers & KeyModifiers.Control) != 0) {
                    centralDelegator.GetMainWindowVM().PaintingVM.ActivateTemplatePlacementMode();
                } else {
                    centralDelegator.GetMainWindowVM().PaintingVM.ActivateTemplateCreationMode();
                }

                break;
            case Key.S:
            case Key.M:
                centralDelegator.GetOutputHandler().PlaceMarker();
                e.Handled = true;
                break;
            case Key.L:
                centralDelegator.GetOutputHandler().LoadMarker();
                e.Handled = true;
                break;
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
        OutputImageOnPointerMoved(sender, e, true);
    }

    /// <summary>
    ///     Callback when the user releases their left mouse button, once again allowing painting to make sure that the user
    ///     cannot paint outside of the image borders
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerReleasedEventArgs</param>
    public void OutputImageOnPointerReleased(object sender, PointerReleasedEventArgs e) {
        canUsePencil = true;
    }

    /// <summary>
    ///     Forwarding function to OutputImageOnPointerMoved(object, PointerEventArgs, bool)
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e"></param>
    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e) {
        // ReSharper disable once IntroduceOptionalParameters.Local
        OutputImageOnPointerMoved(sender, e, false);
    }

    /// <summary>
    ///     Handle all clicking onto the output image, both painting, brushing and template placing
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">PointerEventArgs</param>
    /// <param name="mouseClicked">Whether this function was called from a mouse click or mouse move</param>
    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e, bool mouseClicked) {
        (double posX, double posY) = e.GetPosition(e.Source as Image);
        (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;
        bool allowClick = !centralDelegator!.GetMainWindowVM().PaintingVM.IsPaintOverrideEnabled || mouseClicked;

        bool lbmPressed = e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed;
        bool rbmPressed = e.GetCurrentPoint(e.Source as Image).Properties.IsRightButtonPressed;

        if (centralDelegator!.GetMainWindowVM().PaintingVM.PaintModeEnabled && allowClick) {
            if (lbmPressed || rbmPressed) {
                HandlePaintModeMouseMove(posX, posY, imgWidth, imgHeight, (sender as Image)!, lbmPressed);
            }
        } else if (centralDelegator!.GetMainWindowVM().PaintingVM.PencilModeEnabled && canUsePencil) {
            HandlePencilModeMouseMove(posX, posY, imgWidth, imgHeight, (sender as Image)!, lbmPressed, allowClick);
        } else if (centralDelegator!.GetMainWindowVM().PaintingVM.TemplateCreationModeEnabled) {
            HandleTemplateCreationModeMouseMove(posX, posY, imgWidth, imgHeight, (sender as Image)!, lbmPressed,
                rbmPressed);
        } else if (centralDelegator!.GetMainWindowVM().PaintingVM.TemplatePlaceModeEnabled) {
            HandleTemplatePlaceModeMouseMove(posX, posY, imgWidth, imgHeight, (sender as Image)!, lbmPressed);
        }

        centralDelegator?.GetOutputHandler().ProcessHoverAvailability((int) Math.Round(posX),
            (int) Math.Round(posY),
            (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
            (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
            _paintingPatternsCB.SelectedIndex, centralDelegator!.GetMainWindowVM().PaintingVM.PencilModeEnabled);

        e.Handled = true;
    }

    /// <summary>
    ///     Handle the movement of the mouse in the Paint Mode
    /// </summary>
    /// <param name="posX">X position of the mouse</param>
    /// <param name="posY">Y position of the mouse</param>
    /// <param name="imageWidth">Image width</param>
    /// <param name="imageHeight">Image height</param>
    /// <param name="imageSource">Source of the mouse movement call</param>
    /// <param name="lbmPressed">Whether the left mouse button is pressed</param>
    private void HandlePaintModeMouseMove(double posX, double posY, double imageWidth, double imageHeight,
        ILayoutable imageSource, bool lbmPressed) {
        try {
            centralDelegator?.GetOutputHandler().ProcessClickMask((int) Math.Round(posX),
                (int) Math.Round(posY),
                (int) Math.Round(imageWidth - imageSource.Margin.Right - imageSource.Margin.Left),
                (int) Math.Round(imageHeight - imageSource.Margin.Top - imageSource.Margin.Bottom),
                lbmPressed);
        } catch (IndexOutOfRangeException) { }
    }

    /// <summary>
    ///     Handle the movement of the mouse in the Pencil Mode
    /// </summary>
    /// <param name="posX">X position of the mouse</param>
    /// <param name="posY">Y position of the mouse</param>
    /// <param name="imageWidth">Image width</param>
    /// <param name="imageHeight">Image height</param>
    /// <param name="imageSource">Source of the mouse movement call</param>
    /// <param name="lbmPressed">Whether the left mouse button is pressed</param>
    /// <param name="allowClick">Whether the user is allowed to click at this position</param>
    private async void HandlePencilModeMouseMove(double posX, double posY, double imageWidth, double imageHeight,
        ILayoutable imageSource, bool lbmPressed, bool allowClick) {
        if (lbmPressed && allowClick) {
            int idx = GetSelectedPaintIndex();
            if (!centralDelegator!.GetMainWindowVM().PaintingVM.ClickedInCurrentMode) {
                centralDelegator!.GetOutputHandler().PlaceMarker();
            }

            bool? success = await centralDelegator?.GetOutputHandler().ProcessClick((int) Math.Round(posX),
                (int) Math.Round(posY),
                (int) Math.Round(imageWidth - imageSource.Margin.Right - imageSource.Margin.Left),
                (int) Math.Round(imageHeight - imageSource.Margin.Top - imageSource.Margin.Bottom),
                idx)!;
            if (success != null && !(bool) success) {
                centralDelegator?.GetInterfaceHandler().DispatchError(this, null);
                canUsePencil = false;

                if (!centralDelegator!.GetMainWindowVM().PaintingVM.ClickedInCurrentMode) {
                    centralDelegator!.GetOutputHandler().RemoveLastMarker();
                }
            } else if (success != null && (bool) success) {
                if (!centralDelegator!.GetMainWindowVM().PaintingVM.ClickedInCurrentMode) {
                    centralDelegator!.GetMainWindowVM().PaintingVM.ClickedInCurrentMode = true;
                }
            }
        }
    }

    /// <summary>
    ///     Handle the movement of the mouse in the Template Creation Mode
    /// </summary>
    /// <param name="posX">X position of the mouse</param>
    /// <param name="posY">Y position of the mouse</param>
    /// <param name="imageWidth">Image width</param>
    /// <param name="imageHeight">Image height</param>
    /// <param name="imageSource">Source of the mouse movement call</param>
    /// <param name="lbmPressed">Whether the left mouse button is pressed</param>
    /// <param name="rbmPressed">Whether the right mouse button is pressed</param>
    private void HandleTemplateCreationModeMouseMove(double posX, double posY, double imageWidth, double imageHeight,
        ILayoutable imageSource, bool lbmPressed, bool rbmPressed) {
        if (lbmPressed || rbmPressed) {
            try {
                centralDelegator?.GetOutputHandler().ProcessClickTemplateCreation((int) Math.Round(posX),
                    (int) Math.Round(posY),
                    (int) Math.Round(imageWidth - imageSource.Margin.Right - imageSource.Margin.Left),
                    (int) Math.Round(imageHeight - imageSource.Margin.Top - imageSource.Margin.Bottom),
                    lbmPressed);
            } catch (IndexOutOfRangeException) { }
        }
    }

    /// <summary>
    ///     Handle the movement of the mouse in the Template Placement Mode
    /// </summary>
    /// <param name="posX">X position of the mouse</param>
    /// <param name="posY">Y position of the mouse</param>
    /// <param name="imageWidth">Image width</param>
    /// <param name="imageHeight">Image height</param>
    /// <param name="imageSource">Source of the mouse movement call</param>
    /// <param name="lbmPressed">Whether the left mouse button is pressed</param>
    private async void HandleTemplatePlaceModeMouseMove(double posX, double posY, double imageWidth, double imageHeight,
        ILayoutable imageSource, bool lbmPressed) {
        if (lbmPressed) {
            centralDelegator!.GetOutputHandler().PlaceMarker();
            bool success = await centralDelegator!.GetOutputHandler().ProcessClickTemplatePlace((int) Math.Round(posX),
                (int) Math.Round(posY),
                (int) Math.Round(imageWidth - imageSource.Margin.Right - imageSource.Margin.Left),
                (int) Math.Round(imageHeight - imageSource.Margin.Top - imageSource.Margin.Bottom));
            if (!success) {
                centralDelegator!.GetOutputHandler().RemoveLastMarker();
            }
        }
    }

    /// <summary>
    ///     Callback to reset the mask to allow an updated version of the mask to be recalculated
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerEventArgs</param>
    private void OnPointerMoved(object sender, PointerEventArgs e) {
        centralDelegator?.GetOutputHandler().ResetHoverAvailability();
    }

    /// <summary>
    ///     Callback to update the brush size image shown to the user
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">AvaloniaPropertyChangedEventArgs</param>
    private void BrushSize_ValueChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        if (!centralDelegator.GetPaintingWindow().IsVisible) {
            return;
        }

        int brushSizeRaw = GetPaintBrushSize();

        if (oldBrushSize.Equals(brushSizeRaw)) {
            return;
        }

        if (brushSizeRaw == -1) {
            centralDelegator!.GetMainWindowVM().PaintingVM.BrushSizeImage = Util.CreateBitmapFromData(3, 3, 1, (x, y) =>
                x == 1 && y == 1 ? Color.Parse("#424242") :
                (x + y) % 2 == 0 ? Color.Parse("#11000000") : Colors.Transparent);
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
                return dx * dx + dy * dy <= brushSizeRaw ? Color.Parse("#424242") :
                    (x + y) % 2 == 0 ? Color.Parse("#11000000") : Colors.Transparent;
            });
        centralDelegator!.GetMainWindowVM().PaintingVM.BrushSizeImage = bm;
    }
}