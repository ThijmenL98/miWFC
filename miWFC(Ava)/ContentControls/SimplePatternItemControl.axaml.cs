using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace miWFC.ContentControls;

/// <summary>
///     Separated control for the tiles shown in the center of the application
/// </summary>
public partial class SimplePatternItemControl : UserControl {
    /*
     * Initializing Functions & Constructor
     */

    public SimplePatternItemControl() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    /*
     * Getters
     */

    /*
     * Setters
     */

    /*
     * UI Callbacks
     */
}