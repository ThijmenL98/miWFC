using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace miWFC.ContentControls;

public partial class AdvancedPatternItemControl : UserControl {
    public AdvancedPatternItemControl() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}