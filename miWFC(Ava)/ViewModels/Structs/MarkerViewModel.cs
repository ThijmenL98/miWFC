using ReactiveUI;

namespace miWFC.ViewModels.Structs;

/// <summary>
///     View model for the markers shown on the application timeline
/// </summary>
public class MarkerViewModel : ReactiveObject {
    private readonly int _index;
    private readonly bool _revertible;
    private readonly double _widthOffset, _collapsePercentage;

    /*
     * Initializing Functions & Constructor
     */

    public MarkerViewModel(int index, double offset, double collapsePercentage, bool revertible) {
        MarkerIndex = index;
        MarkerOffset = offset;
        MarkerCollapsePercentage = collapsePercentage;
        Revertible = revertible;
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    ///     Index of this marker object
    /// </summary>
    public int MarkerIndex {
        get => _index;
        private init => this.RaiseAndSetIfChanged(ref _index, value);
    }

    /// <summary>
    ///     Horizontal offset of this marker object on the timeline
    /// </summary>
    public double MarkerOffset {
        get => _widthOffset;
        init => this.RaiseAndSetIfChanged(ref _widthOffset, value);
    }

    /// <summary>
    ///     Percentage of output collapse this marker object represents
    /// </summary>
    public double MarkerCollapsePercentage {
        get => _collapsePercentage;
        private init => this.RaiseAndSetIfChanged(ref _collapsePercentage, value);
    }

    // Booleans

    /// <summary>
    ///     Whether this marker allows the user to revert further into the history of the generation prior to this marker
    /// </summary>
    public bool Revertible {
        get => _revertible;
        init => this.RaiseAndSetIfChanged(ref _revertible, value);
    }

    // Images

    // Objects

    // Lists

    // Other

    /*
     * UI Callbacks
     */
}