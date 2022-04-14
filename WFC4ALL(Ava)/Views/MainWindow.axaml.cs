using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using WFC4ALL.ContentControls;
using WFC4ALL.Managers;

namespace WFC4ALL.Views; 

public partial class MainWindow : Window {
    private CentralManager? centralManager;
    private bool triggered;

    public MainWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
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

    private void keyDownHandler(object? sender, KeyEventArgs e) {
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
            case Key.P:
                centralManager.getInputManager().animate();
                e.Handled = true;
                break;
            case Key.S:
            case Key.M:
                centralManager.getInputManager().placeMarker(false);
                e.Handled = true;
                break;
            case Key.E:
                centralManager.getInputManager().exportSolution();
                e.Handled = true;
                break;
            case Key.L:
                centralManager.getInputManager().loadMarker();
                e.Handled = true;
                break;
            case Key.R:
                centralManager.getInputManager().restartSolution();
                e.Handled = true;
                break;
            case Key.C:
                centralManager.getUIManager().switchWindow(Windows.PAINTING, false);
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
    private void WindowBase_OnActivated(object? sender, EventArgs e) {
        if (triggered) {
            return;
        }

        centralManager?.getInputManager().restartSolution(true);
        triggered = true;
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) {
        if (centralManager != null && !this.Find<Popup>("infoPopup").IsPointerOverPopup &&
            centralManager.getUIManager().popUpOpened()) {
            centralManager.getUIManager().hidePopUp();
        }
    }
}