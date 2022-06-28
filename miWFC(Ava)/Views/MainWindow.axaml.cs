using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using miWFC.ContentControls;
using miWFC.Managers;

namespace miWFC.Views;

public partial class MainWindow : Window {
    private CentralManager? centralManager;
    private bool triggered;

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

    public InputControl getInputControl() {
        return this.Find<InputControl>("inputControl");
    }

    public OutputControl getOutputControl() {
        return this.Find<OutputControl>("outputControl");
    }

    /*
     * Event Handlers
     */
    private void OnTabChange(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        int newTab = (sender as TabControl)!.SelectedIndex;
        centralManager!.getMainWindowVM().OnModelClick(newTab);

        e.Handled = true;
    }

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
    } // ReSharper disable twice UnusedParameter.Local

    private async void WindowBase_OnActivated(object? sender, EventArgs e) {
        if (triggered) {
            return;
        }

        await centralManager!.getInputManager().restartSolution("Window activation", true);
        triggered = true;
    }

    public bool isWindowTriggered() {
        return triggered;
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) {
        if (centralManager != null && !this.Find<Popup>("infoPopup").IsPointerOverPopup &&
            centralManager.getUIManager().popUpOpened()) {
            centralManager.getUIManager().hidePopUp();
        }
    }

    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e) {
        centralManager!.getWFCHandler().isCollapsed();
    }
}