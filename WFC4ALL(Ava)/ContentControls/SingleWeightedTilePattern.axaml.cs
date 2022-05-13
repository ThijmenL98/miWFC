using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WFC4ALL.ContentControls;

public partial class SingleWeightedTilePattern : UserControl {
    public SingleWeightedTilePattern() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}