using ReactiveUI;

namespace WFC4ALL.ContentControls; 

public class GridlineViewModel : ReactiveObject {

    private int x1, x2, y1, y2;
    
    public int X1 {
        get => x1;
        set => this.RaiseAndSetIfChanged(ref x1, value);
    }
    
    public int X2 {
        get => x2;
        set => this.RaiseAndSetIfChanged(ref x2, value);
    }

    public int Y1 {
        get => y1;
        set => this.RaiseAndSetIfChanged(ref y1, value);
    }

    public int Y2 {
        get => y2;
        set => this.RaiseAndSetIfChanged(ref y2, value);
    }

    public GridlineViewModel(int x1, int y1, int x2, int y2) {
        X1 = x1;
        X2 = x2;
        Y1 = y1;
        Y2 = y2;
    }
    
}