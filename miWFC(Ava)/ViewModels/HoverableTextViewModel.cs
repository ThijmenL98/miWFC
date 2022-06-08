using ReactiveUI;

namespace miWFC.ViewModels;

public class HoverableTextViewModel : ReactiveObject {
    private readonly string _toolTipText = "";
    private readonly string _displayText = "";

    // ReSharper disable twice UnusedMember.Local
    public string DisplayText {
        get => _displayText;
        init => this.RaiseAndSetIfChanged(ref _displayText, value);
    }

    private string ToolTipText {
        get => _toolTipText;
        init => this.RaiseAndSetIfChanged(ref _toolTipText, value);
    }

    public HoverableTextViewModel(string displayText = "", string hoverText = "") {
        DisplayText = displayText;
        ToolTipText = hoverText;
    }
}