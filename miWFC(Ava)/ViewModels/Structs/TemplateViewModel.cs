using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Managers;
using miWFC.Utils;
using ReactiveUI;

namespace miWFC.ViewModels.Structs;

/// <summary>
/// View model for the templates created by the user to place as a whole into the output at a later stage
/// </summary>
public class TemplateViewModel : ReactiveObject {
    private readonly WriteableBitmap _templateImage = null!;
    private readonly int[,] _templateDataAdj = { };
    private readonly Color[,] _templateDataOve = { };
    private readonly int status; // 0 = uninitialized, 1 = overlapping template, 2 = adjacent template
    private readonly (int, int) _centerP, _dim;

    private string myHash;
    
    /*
     * Initializing Functions & Constructors
     */
    
    public TemplateViewModel(WriteableBitmap image, int[,] templateData, string hash = "") {
        TemplateImage = image;
        TemplateDataA = templateData;
        status = 2;
        myHash = hash;
        
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
    
    /*
     * Getters & Setters
     */

    // Strings
    
    // Numeric (Integer, Double, Float, Long ...)
    
    // Booleans
    
    // Images

    /// <summary>
    /// Image representation of the template
    /// </summary>
    private WriteableBitmap TemplateImage {
        get => _templateImage;
        init => this.RaiseAndSetIfChanged(ref _templateImage, value);
    }
    
    // Objects
    
    // Lists

    /// <summary>
    /// Adjacent (or Simple) mode data for this template
    /// </summary>
    public int[,] TemplateDataA {
        get => _templateDataAdj;
        private init => this.RaiseAndSetIfChanged(ref _templateDataAdj, value);
    }

    /// <summary>
    /// Overlapping (or Smart) mode data for this template
    /// </summary>
    public Color[,] TemplateDataO {
        get => _templateDataOve;
        private init => this.RaiseAndSetIfChanged(ref _templateDataOve, value);
    }
    
    // Other

    /// <summary>
    /// Center coordinates of this template, which will be under the mouse location of the user when hovering and
    /// placing
    /// </summary>
    public (int, int) CenterPoint {
        get => _centerP;
        private init => this.RaiseAndSetIfChanged(ref _centerP, value);
    }

    /// <summary>
    /// Size of the template
    /// </summary>
    public (int, int) Dimension {
        get => _dim;
        private init => this.RaiseAndSetIfChanged(ref _dim, value);
    }

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Function to save the current template to the user machine
    /// </summary>
    /// 
    /// <param name="inputImage">Name of the input image currently selected</param>
    /// <param name="centralManager">Central manager to re-load all templates</param>
    /// 
    /// <returns>Task which completes once the file has been saved and template data has been appended</returns>
    public async Task Save(string inputImage, CentralManager centralManager) {
        int templateHash = 0;
        switch (status) {
            case 0:
                return;
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
                return;
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

            centralManager.GetPaintingWindow().SetTemplates(Util.GetTemplates(
                centralManager.GetMainWindowVM().InputImageSelection, centralManager.GetWFCHandler().IsOverlappingModel(),
                centralManager.GetWFCHandler().GetTileSize()));
        }
    }

    /// <summary>
    /// Function to delete the current template from the user machine
    /// </summary>
    ///
    /// <param name="inputImage">Name of the input image currently selected</param>
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
}