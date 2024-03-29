using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using miWFC.Delegators;

namespace miWFC.ContentControls;

/// <summary>
///     Separated control for the output side of the application
/// </summary>
public partial class OutputControl : UserControl {
    private CentralDelegator? centralDelegator;

    /*
     * Initializing Functions & Constructor
     */

    public OutputControl() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
    }

    /*
     * Getters
     */

    /// <summary>
    ///     Get the width of the timeline UI element
    /// </summary>
    /// <returns>Width</returns>
    public double GetTimelineWidth() {
        return this.Find<Grid>("timeline").Bounds.Width;
    }

    /*
     * Setters
     */

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Callback when the speed slider is changed as sliding this all the way to the right sets the output
    ///     to instantly collapse rather than take jumps of x
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">AvaloniaPropertyChangedEventArgs</param>
    public void SpeedSliderChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        if (e.Property.ToString().Equals("Value") && e.NewValue != null) {
            centralDelegator?.GetInterfaceHandler().UpdateInstantCollapse((int) (double) e.NewValue);
        }
    }
}