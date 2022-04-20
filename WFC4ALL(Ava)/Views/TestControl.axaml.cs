using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WFC4ALL.ContentControls; 

public partial class TestControl : UserControl {
    public TestControl() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}