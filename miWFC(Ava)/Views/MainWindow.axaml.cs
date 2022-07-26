using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using miWFC.ContentControls;
using miWFC.Delegators;
using miWFC.Utils;

// ReSharper disable UnusedParameter.Local

namespace miWFC.Views;

/// <summary>
///     Main window of the application
/// </summary>
public partial class MainWindow : Window {
    private CentralDelegator? centralDelegator;
    private bool triggered;

    public int ChangeAmount = 1;

    /*
     * Initializing Functions & Constructor
     */

    public MainWindow() {
        InitializeComponent();

        KeyDown += KeyDownHandler;
        Closing += (_, _) => {
            centralDelegator!.GetPaintingWindow().Close();
            centralDelegator!.GetItemWindow().Close();
            centralDelegator!.GetWeightMapWindow().Close();
            Environment.Exit(0);
        };
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
        this.Find<InputControl>("inputControl").SetCentralDelegator(cd);
        this.Find<OutputControl>("outputControl").SetCentralDelegator(cd);
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    /// <summary>
    ///     Whether the main window has already been initialized and activated
    /// </summary>
    /// <returns>Boolean</returns>
    public bool IsWindowTriggered() {
        return triggered;
    }

    // Images

    // Objects

    /// <summary>
    ///     Get the input control of the application
    /// </summary>
    /// <returns>InputControl</returns>
    public InputControl GetInputControl() {
        return this.Find<InputControl>("inputControl");
    }

    /// <summary>
    ///     Get the output control of the application
    /// </summary>
    /// <returns>OutputControl</returns>
    public OutputControl GetOutputControl() {
        return this.Find<OutputControl>("outputControl");
    }

    public SimplePatternItemControl GetSimplePatternItemControl() {
        return this.Find<SimplePatternItemControl>("simplePatternItemControl");
    }

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Callback for changing tabs, causing the mode of the algorithm to change
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    private void OnTabChange(object? sender, SelectionChangedEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        int newTab = (sender as TabControl)!.SelectedIndex;
        centralDelegator!.GetMainWindowVM().ChangeModel(newTab);

        e.Handled = true;
    }

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
            case Key.Left:
            case Key.Delete:
            case Key.Back:
                if (centralDelegator.GetInterfaceHandler().PopUpOpened()) {
                    centralDelegator.GetInterfaceHandler().HidePopUp();
                } else {
                    centralDelegator.GetOutputHandler().RevertStep();
                }

                e.Handled = true;
                break;
            case Key.Right:
                centralDelegator.GetOutputHandler().AdvanceStep();
                e.Handled = true;
                break;
            case Key.PageDown:
            case Key.PageUp:
            case Key.Up:
            case Key.Down:
                e.Handled = true;
                break;
            case Key.Space:
                centralDelegator.GetOutputHandler().Animate();
                e.Handled = true;
                break;
            case Key.S:
            case Key.M:
                centralDelegator.GetOutputHandler().PlaceMarker();
                e.Handled = true;
                break;
            case Key.E:
                centralDelegator.GetOutputHandler().ExportSolution();
                e.Handled = true;
                break;
            case Key.I:
                centralDelegator.GetOutputHandler().ImportSolution();
                e.Handled = true;
                break;
            case Key.L:
                centralDelegator.GetOutputHandler().LoadMarker();
                e.Handled = true;
                break;
            case Key.R:
                await centralDelegator.GetOutputHandler().RestartSolution("Keydown Restart");
                e.Handled = true;
                break;
            case Key.P:
                await centralDelegator.GetInterfaceHandler().SwitchWindow(Windows.PAINTING);
                e.Handled = true;
                break;
            case Key.Escape:
                if (centralDelegator.GetInterfaceHandler().PopUpOpened()) {
                    centralDelegator.GetInterfaceHandler().HidePopUp();
                }

                e.Handled = true;
                break;
            case Key.Z:
                if ((e.KeyModifiers & KeyModifiers.Control) != 0) {
                    if (!centralDelegator.GetInterfaceHandler().PopUpOpened()) {
                        centralDelegator.GetOutputHandler().RevertStep();
                    }

                    e.Handled = true;
                }

                break;
            default:
                base.OnKeyDown(e);
                break;
        }
    }

    /// <summary>
    ///     Callback when the window has been initialized and activated
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">EventArgs</param>
    private async void WindowBase_OnActivated(object? sender, EventArgs e) {
        if (triggered) {
            return;
        }

        await centralDelegator!.GetOutputHandler().RestartSolution("Window activation", true);
        triggered = true;

        centralDelegator!.GetOutputHandler().ResetMask();
        centralDelegator!.GetPaintingWindow().SetTemplates(Util.GetTemplates(
            centralDelegator.GetMainWindowVM().InputImageSelection, centralDelegator.GetWFCHandler().IsOverlappingModel(),
            centralDelegator.GetWFCHandler().GetTileSize()));
    }

    /// <summary>
    ///     Callback when the user clicks anywhere in the application and the informative popup window is opened, causing
    ///     it to close
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerPressedEventArgs</param>
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) {
        if (centralDelegator != null && !this.Find<Popup>("infoPopup").IsPointerOverPopup &&
            centralDelegator.GetInterfaceHandler().PopUpOpened()) {
            centralDelegator.GetInterfaceHandler().HidePopUp();
        }
    }

    /// <summary>
    ///     Callback when the user moves their mouse to update whether the output has been collapsed.
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerEventArgs</param>
    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e) {
        centralDelegator!.GetWFCHandler().IsCollapsed();
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e) {
        ChangeAmount = e.Key switch {
            Key.LeftShift or Key.RightShift => 50,
            Key.LeftCtrl or Key.RightCtrl => 10,
            _ => 1
        };
    }

    private void InputElement_OnKeyUp(object? sender, KeyEventArgs e) {
        ChangeAmount = 1;
    }
}