using ReactiveUI;

namespace WFC4ALL.ViewModels; 

public class MarkerViewModel : ReactiveObject {
    private readonly int _index;
    private readonly string _toolTipText = "";
    private readonly double _widthOffset; // ReSharper disable trice UnusedMember.Local
    public int MarkerIndex {
        get => _index;
        init => this.RaiseAndSetIfChanged(ref _index, value);
    }

    private double MarkerOffset {
        get => _widthOffset;
        init => this.RaiseAndSetIfChanged(ref _widthOffset, value);
    }

    private bool ToolTipVisible => !_toolTipText.Equals("");

    private string ToolTipText {
        get => _toolTipText;
        init => this.RaiseAndSetIfChanged(ref _toolTipText, value);
    }

    public MarkerViewModel(int index, double offset, bool paintingMarker) {
        MarkerIndex = index;
        MarkerOffset = offset;
        if (paintingMarker) {
            ToolTipText = "Marker set by painting";
        }
    }
}