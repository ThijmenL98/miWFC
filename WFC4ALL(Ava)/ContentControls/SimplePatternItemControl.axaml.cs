using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace WFC4ALL.ContentControls;

public partial class SimplePatternItemControl : UserControl {
    public SimplePatternItemControl() {
        InitializeComponent();

        Button incButton = this.Find<Button>("patternIncButton");
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}