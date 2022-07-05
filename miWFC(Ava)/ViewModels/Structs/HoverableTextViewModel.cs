using ReactiveUI;
// ReSharper disable UnusedMember.Local

namespace miWFC.ViewModels.Structs;

/// <summary>
/// View Model element for combobox string entries, allows the element to be hovered instead of being pure text
/// </summary>
public class HoverableTextViewModel : ReactiveObject {
    private readonly string _displayText = "";
    private readonly string _toolTipText = "";

    /*
     * Initializing Functions & Constructor
     */

    public HoverableTextViewModel(string displayText = "", string hoverText = "") {
        DisplayText = displayText;
        ToolTipText = hoverText;
    }

    /*
     * Getters & Setters
     */
    
    // Strings
    
    /// <summary>
    /// Text shown to the user by default
    /// </summary>
    public string DisplayText {
        get => _displayText;
        private init => this.RaiseAndSetIfChanged(ref _displayText, value);
    }

    /// <summary>
    /// Text shown to the user when hovering
    /// </summary>
    private string ToolTipText {
        get => _toolTipText;
        init => this.RaiseAndSetIfChanged(ref _toolTipText, value);
    }
    
    // Numeric (Integer, Double, Float, Long ...)
    
    // Booleans
    
    // Images
    
    // Objects
    
    // Lists
    
    // Other

    /*
     * UI Callbacks
     */
}