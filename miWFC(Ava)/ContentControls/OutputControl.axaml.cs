using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using miWFC.Managers;

namespace miWFC.ContentControls;

public partial class OutputControl : UserControl {
    private CentralManager? centralManager;

    public OutputControl() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    public void speedSliderChanged(object? _, AvaloniaPropertyChangedEventArgs e) {
        if (e.Property.ToString().Equals("Value") && e.NewValue != null) {
            centralManager?.getUIManager().updateInstantCollapse((int) (double) e.NewValue);
        }
    }

    public double getTimelineWidth() {
        return this.Find<Grid>("timeline").Bounds.Width;
    }

    public void setBorderPaddingVisible(bool visible) {
        this.Find<Button>("borderPaddingToggle").IsVisible = visible;
    }
}