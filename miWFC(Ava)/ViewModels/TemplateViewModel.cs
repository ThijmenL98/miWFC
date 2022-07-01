using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Utils;
using ReactiveUI;

namespace miWFC.ViewModels;

public class TemplateViewModel : ReactiveObject {
    private readonly WriteableBitmap _templateImage = null!;
    private readonly int[,] _templateDataAdj = { };
    private readonly Color[,] _templateDataOve = { };
    private readonly int status; // 0 = uninitialized, 1 = overlapping template, 2 = adjacent template
    private readonly (int, int) _centerP, _dim;

    private string myHash;

    public TemplateViewModel(WriteableBitmap image, int[,] templateData, string hash = "") {
        TemplateImage = image;
        TemplateDataA = templateData;
        status = 2;
        myHash = hash;

        templateData = Util.RotateArrayClockwise(templateData);

        CenterPoint = ((int) Math.Floor((templateData.GetLength(0) - 1) / 2d),
            (int) Math.Floor((templateData.GetLength(1) - 1) / 2d));
        Dimension = (templateData.GetLength(0), templateData.GetLength(1));
    }

    public TemplateViewModel(WriteableBitmap image, Color[,] templateData, string hash = "") {
        TemplateImage = image;
        TemplateDataO = templateData;
        status = 1;
        myHash = hash;

        templateData = Util.RotateArrayClockwise(templateData);

        CenterPoint = ((int) Math.Floor((templateData.GetLength(0) - 1) / 2d),
            (int) Math.Floor((templateData.GetLength(1) - 1) / 2d));
        Dimension = (templateData.GetLength(0), templateData.GetLength(1));
    }

    public async Task<bool> Save(string inputImage) {
        int templateHash = 0;
        switch (status) {
            case 0:
                return false;
            case 1:
                for (int x = 0; x < TemplateDataO.GetLength(0); x++) {
                    for (int y = 0; y < TemplateDataO.GetLength(1); y++) {
                        templateHash += TemplateDataO[x, y].GetHashCode() * (x + 1) * (y + 1);
                    }
                }

                break;
            case 2:
                for (int x = 0; x < TemplateDataA.GetLength(0); x++) {
                    for (int y = 0; y < TemplateDataA.GetLength(1); y++) {
                        templateHash += TemplateDataA[x, y].GetHashCode() * (x + 1) * (y + 1);
                    }
                }

                break;
            default:
                return false;
        }

        string baseDir = $"{AppContext.BaseDirectory}/Assets/Templates/";
        if (!Directory.Exists(baseDir)) {
            Directory.CreateDirectory(baseDir);
        }

        myHash = Convert.ToString(templateHash, 16);
        string fileName = baseDir + $"{inputImage}_{myHash}.wfcPatt";
        if (!File.Exists(fileName)) {
            TemplateImage.Save(fileName);

            if (status == 2) {
                await Util.AppendPictureData(fileName, TemplateDataA, true);
            }

            return true;
        }

        return false;
    }

    public void DeleteFile(string inputImage) {
        if (myHash.Equals("")) {
            return;
        }

        string baseDir = $"{AppContext.BaseDirectory}/Assets/Templates/";
        if (!Directory.Exists(baseDir)) {
            Directory.CreateDirectory(baseDir);
        }

        string fileName = baseDir + $"{inputImage}_{myHash}.wfcPatt";

#if DEBUG
        Trace.WriteLine($"Deleting {fileName}");
#endif
        if (!File.Exists(fileName)) {
            return;
        }

        File.Delete(fileName);
    }

    private WriteableBitmap TemplateImage {
        get => _templateImage;
        init => this.RaiseAndSetIfChanged(ref _templateImage, value);
    }

    public int[,] TemplateDataA {
        get => _templateDataAdj;
        init => this.RaiseAndSetIfChanged(ref _templateDataAdj, value);
    }

    public Color[,] TemplateDataO {
        get => _templateDataOve;
        init => this.RaiseAndSetIfChanged(ref _templateDataOve, value);
    }

    public (int, int) CenterPoint {
        get => _centerP;
        init => this.RaiseAndSetIfChanged(ref _centerP, value);
    }

    public (int, int) Dimension {
        get => _dim;
        init => this.RaiseAndSetIfChanged(ref _dim, value);
    }
}