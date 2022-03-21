using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using WFC4All;
using WFC4ALL.ContentControls;

namespace WFC4ALL.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += keyDownHandler;
        }

        public void setInputManager(InputManager im)
        {
            this.Find<InputControl>("inputControl").setInputManager(im);
        }

        public InputControl getInputControl()
        {
            return this.Find<InputControl>("inputControl");
        }

        public OutputControl getOutputControl()
        {
            return this.Find<OutputControl>("outputControl");
        }

        /*
         * Event Handlers
         */

        private void keyDownHandler(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            //TODO
            Trace.WriteLine(e.Key);
            e.Handled = true;
        }
    }
}
