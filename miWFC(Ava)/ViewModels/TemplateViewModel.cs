using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace miWFC.ViewModels;

public class TemplateViewModel : ReactiveObject {
    private readonly WriteableBitmap _templateImage = null!;
    private readonly int[,] _templateDataAdj = { };
    private readonly Color[,] _templateDataOve = { };

    public TemplateViewModel(WriteableBitmap image, int[,] templateData) {
        TemplateImage = image;
        TemplateDataA = templateData;
    }

    public TemplateViewModel(WriteableBitmap image, Color[,] templateData) {
        TemplateImage = image;
        TemplateDataO = templateData;
    }

    private WriteableBitmap TemplateImage {
        get => _templateImage;
        init => this.RaiseAndSetIfChanged(ref _templateImage, value);
    }

    private int[,] TemplateDataA {
        get => _templateDataAdj;
        init => this.RaiseAndSetIfChanged(ref _templateDataAdj, value);
    }

    private Color[,] TemplateDataO {
        get => _templateDataOve;
        init => this.RaiseAndSetIfChanged(ref _templateDataOve, value);
    }
}