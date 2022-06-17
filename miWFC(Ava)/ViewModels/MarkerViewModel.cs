using ReactiveUI;

namespace miWFC.ViewModels;

public class MarkerViewModel : ReactiveObject {
    private readonly int _index;
    private readonly bool _revertible;
    private readonly double _widthOffset, _collapsePercentage; // ReSharper disable trice UnusedMember.Local

    public int MarkerIndex {
        get => _index;
        // ReSharper disable once MemberCanBePrivate.Global
        init => this.RaiseAndSetIfChanged(ref _index, value);
    }

    public double MarkerOffset {
        get => _widthOffset;
        init => this.RaiseAndSetIfChanged(ref _widthOffset, value);
    }

    public double MarkerCollapsePercentage {
        get => _collapsePercentage;
        // ReSharper disable once MemberCanBePrivate.Global
        init => this.RaiseAndSetIfChanged(ref _collapsePercentage, value);
    }

    public bool Revertible {
        get => _revertible;
        init => this.RaiseAndSetIfChanged(ref _revertible, value);
    }

    public MarkerViewModel(int index, double offset, double collapsePercentage, bool revertible) {
        MarkerIndex = index;
        MarkerOffset = offset;
        MarkerCollapsePercentage = collapsePercentage;
        Revertible = revertible;
    }
}