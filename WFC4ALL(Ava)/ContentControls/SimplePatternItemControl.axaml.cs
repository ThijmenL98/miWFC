using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WFC4ALL.ContentControls;

public partial class SimplePatternItemControl : UserControl {
    public SimplePatternItemControl() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}