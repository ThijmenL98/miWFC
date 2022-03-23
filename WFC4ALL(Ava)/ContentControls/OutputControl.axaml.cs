using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using InputManager = WFC4All.InputManager;

namespace WFC4ALL.ContentControls {
    public partial class OutputControl : UserControl {
        private InputManager? inputManager;

        public OutputControl() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public void setInputManager(InputManager im) {
            inputManager = im;
        }

        public void speedSliderChanged(object? _, AvaloniaPropertyChangedEventArgs e) {
            if (e.Property.ToString().Equals("Value") && e.NewValue != null) {
                inputManager?.updateInstantCollapse((int) (double) e.NewValue);
            }
        }

        public double getTimelineWidth() {
            return this.Find<Grid>("timeline").Bounds.Width;
        }

        public void OutputImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
            (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;
            (double clickX, double clickY) = e.GetPosition(e.Source as Image);
            inputManager?.processClick((int) Math.Round(clickX), (int) Math.Round(clickY),
                (int) Math.Round(imgHeight - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom));
        }
    }
}