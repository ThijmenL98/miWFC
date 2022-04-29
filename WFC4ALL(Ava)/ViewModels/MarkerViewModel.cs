using System.Diagnostics;
using ReactiveUI;

namespace WFC4ALL.ViewModels; 

public class MarkerViewModel : ReactiveObject {
    private readonly int _index;
    private readonly double _widthOffset, _collapsePercentage; 
    // ReSharper disable trice UnusedMember.Local
    
    public int MarkerIndex {
        get => _index;
        init => this.RaiseAndSetIfChanged(ref _index, value);
    }

    public double MarkerOffset {
        get => _widthOffset;
        init => this.RaiseAndSetIfChanged(ref _widthOffset, value);
    }

    public double MarkerCollapsePercentage {
        get => _collapsePercentage;
        init => this.RaiseAndSetIfChanged(ref _collapsePercentage, value);
    }

    public MarkerViewModel(int index, double offset, double collapsePercentage) {
        MarkerIndex = index;
        MarkerOffset = offset;
        MarkerCollapsePercentage = collapsePercentage;
    }
}