using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace WFC4ALL.ContentControls;

public partial class PatternItemControl : UserControl {
    public PatternItemControl() {
        InitializeComponent();

        Button incButton = this.Find<Button>("patternIncButton");
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}