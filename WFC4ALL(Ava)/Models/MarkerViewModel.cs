using ReactiveUI;

namespace WFC4ALL.Models; 

public class MarkerViewModel : ReactiveObject {
    private int _index;
    private double _widthOffset;

    public int MarkerIndex {
        get => _index;
        set => this.RaiseAndSetIfChanged(ref _index, value);
    }

    public double MarkerOffset {
        get => _widthOffset;
        private init => this.RaiseAndSetIfChanged(ref _widthOffset, value);
    }

    public MarkerViewModel(int index, double offset) {
        MarkerIndex = index;
        MarkerOffset = offset;
    }
}