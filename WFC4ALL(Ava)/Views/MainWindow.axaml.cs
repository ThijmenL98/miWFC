using Avalonia.Controls;
using Avalonia.Input;
using WFC4ALL.ContentControls;
using InputManager = WFC4All.InputManager;

namespace WFC4ALL.Views {
    public partial class MainWindow : Window {
        private InputManager? inputManager;

        public MainWindow() {
            InitializeComponent();

            KeyDown += keyDownHandler;
        }

        public void setInputManager(InputManager im) {
            inputManager = im;
            this.Find<InputControl>("inputControl").setInputManager(im);
            this.Find<OutputControl>("outputControl").setInputManager(im);
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
            if (inputManager == null) {
                return;
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (e.Key) {
                case Key.Left:
                case Key.Delete:
                case Key.Back:
                    if (inputManager.popUpOpened()) {
                        inputManager.hidePopUp();
                    } else {
                        inputManager.revertStep();
                    }
                    e.Handled = true;
                    break;
                case Key.Right:
                    inputManager.advanceStep();
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
                    inputManager.animate();
                    e.Handled = true;
                    break;
                case Key.S:
                case Key.M:
                    inputManager.placeMarker();
                    e.Handled = true;
                    break;
                case Key.E:
                    inputManager.export();
                    e.Handled = true;
                    break;
                case Key.L:
                    inputManager.loadMarker();
                    e.Handled = true;
                    break;
                case Key.R:
                    inputManager.restartSolution();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    if (inputManager.popUpOpened()) {
                        inputManager.hidePopUp();
                    }
                    e.Handled = true;
                    break;
                default:
                    base.OnKeyDown(e);
                    break;
            }

            e.Handled = true;
        }
    }
}