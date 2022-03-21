using Avalonia.Media.Imaging;
using ReactiveUI;

namespace WFC4ALL.ContentControls; 

public class TileViewModel : ReactiveObject {
    
    private Bitmap _patternImage = null!;
    private int _patternWeight = 0;

    public Bitmap PatternImage {
        get => _patternImage;
        set => this.RaiseAndSetIfChanged(ref _patternImage, value);
    }

    public int PatternWeight {
        get => _patternWeight;
        set => this.RaiseAndSetIfChanged(ref _patternWeight, value);
    }

    public TileViewModel(Bitmap image, double weight) {
        PatternImage = image;
        PatternWeight = (int) weight;
    }
    
}