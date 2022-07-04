using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using miWFC.ContentControls;
using miWFC.Managers;
using miWFC.Utils;

// ReSharper disable UnusedParameter.Local

namespace miWFC.Views;

/// <summary>
/// Main window of the application
/// </summary>
public partial class MainWindow : Window {
    private CentralManager? centralManager;
    private bool triggered;

    /*
     * Initializing Functions & Constructor
     */

    public MainWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
        Closing += (_, _) => {
            centralManager!.getPaintingWindow().Close();
            centralManager!.getItemWindow().Close();
            centralManager!.getWeightMapWindow().Close();
            Environment.Exit(0);
        };
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
        this.Find<InputControl>("inputControl").setCentralManager(cm);
        this.Find<OutputControl>("outputControl").setCentralManager(cm);
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    /// <summary>
    /// Whether the main window has already been initialized and activated
    /// </summary>
    /// 
    /// <returns>Boolean</returns>
    public bool isWindowTriggered() {
        return triggered;
    }

    // Images

    // Objects

    /// <summary>
    /// Get the input control of the application
    /// </summary>
    /// 
    /// <returns>InputControl</returns>
    public InputControl getInputControl() {
        return this.Find<InputControl>("inputControl");
    }

    /// <summary>
    /// Get the output control of the application
    /// </summary>
    /// 
    /// <returns>OutputControl</returns>
    public OutputControl getOutputControl() {
        return this.Find<OutputControl>("outputControl");
    }

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Callback for changing tabs, causing the mode of the algorithm to change
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    private void OnTabChange(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        int newTab = (sender as TabControl)!.SelectedIndex;
        centralManager!.getMainWindowVM().OnModelClick(newTab);

        e.Handled = true;
    }

    /// <summary>
    /// Custom handler for keyboard input
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private async void keyDownHandler(object? sender, KeyEventArgs e) {
        if (centralManager == null) {
            return;
        }

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (e.Key) {
            case Key.Left:
            case Key.Delete:
            case Key.Back:
                if (centralManager.getUIManager().popUpOpened()) {
                    centralManager.getUIManager().hidePopUp();
                } else {
                    centralManager.getInputManager().revertStep();
                }

                e.Handled = true;
                break;
            case Key.Right:
                centralManager.getInputManager().advanceStep();
                e.Handled = true;
                break;
            case Key.PageDown:
            case Key.PageUp:
            case Key.Up:
            case Key.Down:
                e.Handled = true;
                break;
            case Key.Space:
                centralManager.getInputManager().animate();
                e.Handled = true;
                break;
            case Key.S:
            case Key.M:
                centralManager.getInputManager().placeMarker();
                e.Handled = true;
                break;
            case Key.E:
                centralManager.getInputManager().exportSolution();
                e.Handled = true;
                break;
            case Key.I:
                centralManager.getInputManager().importSolution();
                e.Handled = true;
                break;
            case Key.L:
                centralManager.getInputManager().loadMarker();
                e.Handled = true;
                break;
            case Key.R:
                await centralManager.getInputManager().restartSolution("Keydown Restart");
                e.Handled = true;
                break;
            case Key.P:
                await centralManager.getUIManager().switchWindow(Windows.PAINTING);
                e.Handled = true;
                break;
            case Key.Escape:
                if (centralManager.getUIManager().popUpOpened()) {
                    centralManager.getUIManager().hidePopUp();
                }

                e.Handled = true;
                break;
            case Key.Z:
                if ((e.KeyModifiers & KeyModifiers.Control) != 0) {
                    if (!centralManager.getUIManager().popUpOpened()) {
                        centralManager.getInputManager().revertStep();
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
    /// Callback when the window has been initialized and activated
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">EventArgs</param>
    private async void WindowBase_OnActivated(object? sender, EventArgs e) {
        if (triggered) {
            return;
        }

        await centralManager!.getInputManager().restartSolution("Window activation", true);
        triggered = true;

        centralManager!.getInputManager().resetMask();
        centralManager!.getPaintingWindow().setTemplates(Util.GetTemplates(
            centralManager.getMainWindowVM().InputImageSelection, centralManager.getWFCHandler().isOverlappingModel(),
            centralManager.getWFCHandler().getTileSize()));
    }

    /// <summary>
    /// Callback when the user clicks anywhere in the application and the informative popup window is opened, causing
    /// it to close
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerPressedEventArgs</param>
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) {
        if (centralManager != null && !this.Find<Popup>("infoPopup").IsPointerOverPopup &&
            centralManager.getUIManager().popUpOpened()) {
            centralManager.getUIManager().hidePopUp();
        }
    }

    /// <summary>
    /// Callback when the user moves their mouse to update whether the output has been collapsed.
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerEventArgs</param>
    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e) {
        centralManager!.getWFCHandler().isCollapsed();
    }
}