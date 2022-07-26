using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.Delegators;
using miWFC.Utils;
using ReactiveUI;

// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class MappingViewModel : ReactiveObject {
    private readonly MainWindowViewModel mainWindowViewModel;

    private Bitmap _currentHeatMap
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _hoverImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    private bool _hardBrushEnabled = true;

    private int _heatmapValue = 50;
    private CentralDelegator? centralDelegator;

    /*
     * Initializing Functions & Constructor
     */

    public MappingViewModel(MainWindowViewModel mwvm) {
        mainWindowViewModel = mwvm;
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    ///     Default value for the entire heatmap
    /// </summary>
    public int HeatmapValue {
        get => _heatmapValue;
        set => this.RaiseAndSetIfChanged(ref _heatmapValue, value);
    }

    // Booleans

    /// <summary>
    ///     Whether the hard brush or soft brush is enabled
    /// </summary>
    public bool HardBrushEnabled {
        get => _hardBrushEnabled;
        set => this.RaiseAndSetIfChanged(ref _hardBrushEnabled, value);
    }

    // Images

    /// <summary>
    ///     Image associated with the current bitmap
    /// </summary>
    public Bitmap CurrentHeatmap {
        get => _currentHeatMap;
        set => this.RaiseAndSetIfChanged(ref _currentHeatMap, value);
    }

    /// <summary>
    ///     Image associated with the hovering of the mouse
    /// </summary>
    public Bitmap HoverImage {
        get => _hoverImage;
        set => this.RaiseAndSetIfChanged(ref _hoverImage, value);
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
    }

    // Objects

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Function called when resetting the weight mapping
    /// </summary>
    public void ResetWeightMapping() {
        centralDelegator!.GetWeightMapWindow().ResetCurrentMapping();
    }

    /// <summary>
    ///     Function called upon importing a weight map
    /// </summary>
    public async void ImportWeightMap() {
        OpenFileDialog ofd = new() {
            Title = @"Select export location & file name",
            Filters = new List<FileDialogFilter> {
                new() {
                    Extensions = new List<string> {"png"},
                    Name = "Weight Mapping (*.png)"
                }
            }
        };

        string[]? ofdResults = await ofd.ShowAsync(new Window());
        if (ofdResults != null) {
            if (ofdResults.Length == 0) {
                return;
            }

            string mapFile = ofdResults[0];

            MemoryStream ms = new(await File.ReadAllBytesAsync(mapFile));
            WriteableBitmap inputBitmap = WriteableBitmap.Decode(ms);

            int inputImageWidth = (int) inputBitmap.Size.Width, inputImageHeight = (int) inputBitmap.Size.Height;
            double[,] mapValues = new double[inputImageWidth, inputImageHeight];

            using ILockedFramebuffer? frameBuffer = inputBitmap.Lock();

            unsafe {
                uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
                int stride = frameBuffer.RowBytes;

                Parallel.For(0L, mainWindowViewModel.ImageOutHeight, y => {
                    uint* dest = backBuffer + (int) y * stride / 4;
                    for (int x = 0; x < mainWindowViewModel.ImageOutWidth; x++) {
                        int greyValue = (byte) ((dest[x] >> 16) & 0xFF);
                        mapValues[x, y] = greyValue;
                    }
                });
            }

            centralDelegator!.GetWeightMapWindow().UpdateOutput(mapValues);
            centralDelegator!.GetWeightMapWindow().SetCurrentMapping(mapValues);
        }
    }

    /// <summary>
    ///     Function called upon exporting a weight map
    /// </summary>
    public async void ExportWeightMap() {
        SaveFileDialog sfd = new() {
            Title = @"Select export location & file name",
            DefaultExtension = "png",
            Filters = new List<FileDialogFilter> {
                new() {
                    Extensions = new List<string> {"png"},
                    Name = "Weight Mapping (*.png)"
                }
            }
        };

        string? settingsFileName = await sfd.ShowAsync(new Window());
        if (settingsFileName != null) {
            WriteableBitmap exportBitmap = Util.CreateBitmapFromData(mainWindowViewModel.ImageOutWidth,
                mainWindowViewModel.ImageOutHeight, 1, (x, y) => {
                    int greyValue = (int) centralDelegator!.GetWeightMapWindow().GetGradientValue(x, y);
                    return Color.FromRgb((byte) greyValue, (byte) greyValue, (byte) greyValue);
                });

            exportBitmap.Save(settingsFileName);
        }
    }
}