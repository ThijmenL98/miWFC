using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace miWFC.ContentControls;

public partial class SimplePatternItemControl : UserControl {
    public SimplePatternItemControl() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}