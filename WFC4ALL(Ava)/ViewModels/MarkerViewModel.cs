using ReactiveUI;

namespace WFC4ALL.ViewModels; 

public class MarkerViewModel : ReactiveObject {
    private readonly int _index;
    private readonly double _widthOffset; // ReSharper disable trice UnusedMember.Local
    public int MarkerIndex {
        get => _index;
        init => this.RaiseAndSetIfChanged(ref _index, value);
    }

    private double MarkerOffset {
        get => _widthOffset;
        init => this.RaiseAndSetIfChanged(ref _widthOffset, value);
    }

    public MarkerViewModel(int index, double offset) {
        MarkerIndex = index;
        MarkerOffset = offset;
    }
}