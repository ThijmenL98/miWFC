using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using miWFC.ContentControls;
using miWFC.Managers;
using miWFC.Utils;

// ReSharper disable UnusedParameter.Local

namespace miWFC.Views;

/// <summary>
///     Main window of the application
/// </summary>
public partial class MainWindow : Window {
    private CentralManager? centralManager;
    private bool triggered;

    public int ChangeAmount = 1;

    /*
     * Initializing Functions & Constructor
     */

    public MainWindow() {
        InitializeComponent();

        KeyDown += KeyDownHandler;
        Closing += (_, _) => {
            centralManager!.GetPaintingWindow().Close();
            centralManager!.GetItemWindow().Close();
            centralManager!.GetWeightMapWindow().Close();
            Environment.Exit(0);
        };
    }

    public void SetCentralManager(CentralManager cm) {
        centralManager = cm;
        this.Find<InputControl>("inputControl").SetCentralManager(cm);
        this.Find<OutputControl>("outputControl").SetCentralManager(cm);
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
        if (centralManager == null) {
            return;
        }

        int newTab = (sender as TabControl)!.SelectedIndex;
        centralManager!.GetMainWindowVM().ChangeModel(newTab);

        e.Handled = true;
    }

    /// <summary>
    ///     Custom handler for keyboard input
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private async void KeyDownHandler(object? sender, KeyEventArgs e) {
        if (centralManager == null) {
            return;
        }

        switch (e.Key) {
            case Key.Left:
            case Key.Delete:
            case Key.Back:
                if (centralManager.GetUIManager().PopUpOpened()) {
                    centralManager.GetUIManager().HidePopUp();
                } else {
                    centralManager.GetInputManager().RevertStep();
                }

                e.Handled = true;
                break;
            case Key.Right:
                centralManager.GetInputManager().AdvanceStep();
                e.Handled = true;
                break;
            case Key.PageDown:
            case Key.PageUp:
            case Key.Up:
            case Key.Down:
                e.Handled = true;
                break;
            case Key.Space:
                centralManager.GetInputManager().Animate();
                e.Handled = true;
                break;
            case Key.S:
            case Key.M:
                centralManager.GetInputManager().PlaceMarker();
                e.Handled = true;
                break;
            case Key.E:
                centralManager.GetInputManager().ExportSolution();
                e.Handled = true;
                break;
            case Key.I:
                centralManager.GetInputManager().ImportSolution();
                e.Handled = true;
                break;
            case Key.L:
                centralManager.GetInputManager().LoadMarker();
                e.Handled = true;
                break;
            case Key.R:
                await centralManager.GetInputManager().RestartSolution("Keydown Restart");
                e.Handled = true;
                break;
            case Key.P:
                await centralManager.GetUIManager().SwitchWindow(Windows.PAINTING);
                e.Handled = true;
                break;
            case Key.Escape:
                if (centralManager.GetUIManager().PopUpOpened()) {
                    centralManager.GetUIManager().HidePopUp();
                }

                e.Handled = true;
                break;
            case Key.Z:
                if ((e.KeyModifiers & KeyModifiers.Control) != 0) {
                    if (!centralManager.GetUIManager().PopUpOpened()) {
                        centralManager.GetInputManager().RevertStep();
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

        await centralManager!.GetInputManager().RestartSolution("Window activation", true);
        triggered = true;

        centralManager!.GetInputManager().ResetMask();
        centralManager!.GetPaintingWindow().SetTemplates(Util.GetTemplates(
            centralManager.GetMainWindowVM().InputImageSelection, centralManager.GetWFCHandler().IsOverlappingModel(),
            centralManager.GetWFCHandler().GetTileSize()));
    }

    /// <summary>
    ///     Callback when the user clicks anywhere in the application and the informative popup window is opened, causing
    ///     it to close
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerPressedEventArgs</param>
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) {
        if (centralManager != null && !this.Find<Popup>("infoPopup").IsPointerOverPopup &&
            centralManager.GetUIManager().PopUpOpened()) {
            centralManager.GetUIManager().HidePopUp();
        }
    }

    /// <summary>
    ///     Callback when the user moves their mouse to update whether the output has been collapsed.
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerEventArgs</param>
    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e) {
        centralManager!.GetWFCHandler().IsCollapsed();
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