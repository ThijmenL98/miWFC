using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.DeBroglie.Topo;
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels.Structs;
using ReactiveUI;

// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class PaintingViewModel : ReactiveObject {
    private CentralManager? centralManager;
    private readonly MainWindowViewModel mainWindowViewModel;

    private Bitmap _brushSizeImage
        = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    private ObservableCollection<TemplateViewModel> _templates = new();

    private bool _pencilModeEnabled,
        _paintModeEnabled,
        templateCreationModeEnabled,
        _isPaintOverrideEnabled,
        _templatePlaceModeEnabled,
        clickedInCurrentMode;

    /*
     * Initializing Functions & Constructor
     */

    public PaintingViewModel(MainWindowViewModel mwvm) {
        mainWindowViewModel = mwvm;
    }

    public void SetCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    /// <summary>
    /// Whether the pencil mode is selected
    /// </summary>
    public bool PencilModeEnabled {
        get => _pencilModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _pencilModeEnabled, value);
    }

    /// <summary>
    /// Whether the painting mode is selected
    /// </summary>
    public bool PaintModeEnabled {
        get => _paintModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _paintModeEnabled, value);
    }

    /// <summary>
    /// Whether the template creation mode is selected
    /// </summary>
    public bool TemplateCreationModeEnabled {
        get => templateCreationModeEnabled;
        set => this.RaiseAndSetIfChanged(ref templateCreationModeEnabled, value);
    }

    /// <summary>
    /// Whether the template placing mode is selected
    /// </summary>
    public bool TemplatePlaceModeEnabled {
        get => _templatePlaceModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _templatePlaceModeEnabled, value);
    }

    /// <summary>
    /// Whether the painting override mode is enabled, meaning you can paint on collapsed cells
    /// </summary>
    public bool IsPaintOverrideEnabled {
        get => _isPaintOverrideEnabled;
        set => this.RaiseAndSetIfChanged(ref _isPaintOverrideEnabled, value);
    }

    /// <summary>
    /// Whether the user has clicked since the current mode has been initialized
    /// </summary>
    public bool ClickedInCurrentMode {
        get => clickedInCurrentMode;
        set => this.RaiseAndSetIfChanged(ref clickedInCurrentMode, value);
    }
    
    // Images

    /// <summary>
    /// Image shown to the user depending on the size of the brush they're using
    /// </summary>
    public Bitmap BrushSizeImage {
        get => _brushSizeImage;
        set => this.RaiseAndSetIfChanged(ref _brushSizeImage, value);
    }

    // Objects

    // Lists

    /// <summary>
    /// All templates associated with the current input image
    /// </summary>
    public ObservableCollection<TemplateViewModel> Templates {
        get => _templates;
        set => this.RaiseAndSetIfChanged(ref _templates, value);
    }

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Function called when switching to the pencil mode
    /// </summary>
    public void ActivatePencilMode() {
        PencilModeEnabled = !PencilModeEnabled;
        TemplateCreationModeEnabled = false;
        TemplatePlaceModeEnabled = false;
        PaintModeEnabled = false;

        clickedInCurrentMode = false;

        centralManager!.GetInputManager().ResetMask();
        centralManager!.GetInputManager().UpdateMask();
    }

    /// <summary>
    /// Function called when switching to the paint mode
    /// </summary>
    public void ActivatePaintMode() {
        PaintModeEnabled = !PaintModeEnabled;
        TemplateCreationModeEnabled = false;
        TemplatePlaceModeEnabled = false;
        PencilModeEnabled = false;

        centralManager!.GetInputManager().ResetMask();
        centralManager!.GetInputManager().UpdateMask();
    }

    /// <summary>
    /// Function called when switching to the template creation mode
    /// </summary>
    public void ActivateTemplateCreationMode() {
        TemplateCreationModeEnabled = !TemplateCreationModeEnabled;
        PaintModeEnabled = false;
        TemplatePlaceModeEnabled = false;
        PencilModeEnabled = false;

        centralManager!.GetInputManager().ResetMask();
        centralManager!.GetInputManager().UpdateMask();
    }

    /// <summary>
    /// Function called when switching to the template placement mode
    /// </summary>
    public void ActivateTemplatePlacementMode() {
        TemplatePlaceModeEnabled = !TemplatePlaceModeEnabled;
        TemplateCreationModeEnabled = false;
        PaintModeEnabled = false;
        PencilModeEnabled = false;
        
        centralManager!.GetInputManager().ResetMask();
        centralManager!.GetInputManager().UpdateMask();
    }

    /// <summary>
    /// Function called applying the current paint mask
    /// </summary>
    public async Task ApplyPaintMask() {
        Color[,] mask = centralManager!.GetInputManager().GetMaskColours();
        if (!(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green)) {
            centralManager!.GetUIManager().DispatchError(centralManager.GetPaintingWindow(), "No mask has been painted");
            return;
        }

        mainWindowViewModel.ToggleLoadingAnimation(true);
        centralManager!.GetMainWindowVM().StepAmount = 1;

        centralManager.GetInputManager().ResetMask();
        centralManager.GetInputManager().UpdateMask();
        await centralManager.GetWFCHandler().HandlePaintBrush(mask);
    }

    /// <summary>
    /// Function called applying the current template
    /// </summary>
    public async void CreateTemplate() {
        Color[,] mask = centralManager!.GetInputManager().GetMaskColours();

        if (centralManager!.GetWFCHandler().IsOverlappingModel()) {
            await CreateOverlappingTemplate(mask);
        } else {
            await CreateAdjacentTemplate(mask);
        }

        centralManager.GetPaintingWindow().SetTemplates(Templates);
        centralManager.GetInputManager().ResetMask();
        centralManager.GetInputManager().UpdateMask();
    }

    /// <summary>
    /// Create a template with the current mask in the overlapping mode
    /// </summary>
    /// 
    /// <param name="mask">User created mask</param>
    private async Task CreateOverlappingTemplate(Color[,] mask) {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        bool nonEmpty = false;

        for (int a = 0; a < mask.GetLength(0); a++) {
            for (int b = 0; b < mask.GetLength(1); b++) {
                if (mask[a, b] == Colors.White) {
                    Color atPos = centralManager!.GetWFCHandler().GetOverlappingOutputAt(a, b);

                    if (atPos.A != 255) {
                        centralManager.GetUIManager().DispatchError(centralManager!.GetPaintingWindow(), "Cannot include transparent cells in template");
                        return;
                    }

                    nonEmpty = true;

                    if (a < minX) {
                        minX = a;
                    }

                    if (a > maxX) {
                        maxX = a;
                    }

                    if (b < minY) {
                        minY = b;
                    }

                    if (b > maxY) {
                        maxY = b;
                    }
                }
            }
        }

        if (!nonEmpty) {
            centralManager!.GetUIManager().DispatchError(centralManager!.GetPaintingWindow(), "There was no template drawn");
            return;
        }

        int patternWidth = maxX - minX + 1;
        int patternHeight = maxY - minY + 1;
        Color[,] offsetMask = new Color[patternWidth, patternHeight];

        for (int a = minX; a <= maxX; a++) {
            for (int b = minY; b <= maxY; b++) {
                if (mask[a, b] == Colors.White) {
                    offsetMask[a - minX, b - minY]
                        = centralManager!.GetWFCHandler().GetOverlappingOutputAt(a, b);
                } else {
                    offsetMask[a - minX, b - minY] = Colors.Transparent;
                }
            }
        }

        TemplateViewModel tvm = new(Util.ColourArrayToImage(offsetMask), offsetMask);
        await tvm.Save(mainWindowViewModel.InputImageSelection, centralManager!);
    }

    /// <summary>
    /// Create a template with the current mask in the adjacent mode
    /// </summary>
    /// 
    /// <param name="mask">User created mask</param>
    private async Task CreateAdjacentTemplate(Color[,] mask) {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        bool nonEmpty = false;

        ITopoArray<int> aOutput = centralManager!.GetWFCHandler().GetPropagatorOutputA();
        for (int a = 0; a < mask.GetLength(0); a++) {
            for (int b = 0; b < mask.GetLength(1); b++) {
                if (mask[a, b] == Colors.White) {
                    if (aOutput.get(a, b) < 0) {
                        centralManager.GetUIManager().DispatchError(centralManager!.GetPaintingWindow(), "Cannot include transparent cells in template");
                        return;
                    }

                    nonEmpty = true;

                    if (a < minX) {
                        minX = a;
                    }

                    if (a > maxX) {
                        maxX = a;
                    }

                    if (b < minY) {
                        minY = b;
                    }

                    if (b > maxY) {
                        maxY = b;
                    }
                }
            }
        }

        if (!nonEmpty) {
            centralManager.GetUIManager().DispatchError(centralManager!.GetPaintingWindow(), "There was no template drawn");
            return;
        }

        int patternWidth = maxX - minX + 1;
        int patternHeight = maxY - minY + 1;
        int[,] offsetMask = new int[patternWidth, patternHeight];

        for (int a = minX; a <= maxX; a++) {
            for (int b = minY; b <= maxY; b++) {
                if (mask[a, b] == Colors.White) {
                    offsetMask[a - minX, b - minY] = aOutput.get(a, b);
                } else {
                    offsetMask[a - minX, b - minY] = -1;
                }
            }
        }

        TemplateViewModel tvm
            = new(
                Util.ValueArrayToImage(offsetMask, centralManager!.GetWFCHandler().GetTileSize(),
                    centralManager!.GetWFCHandler().GetTileCache()), offsetMask);
        await tvm.Save(mainWindowViewModel.InputImageSelection, centralManager!);
    }

    /// <summary>
    /// Function called to reset the mask that is being painted on
    /// </summary>
    public void ResetMask() {
        centralManager!.GetInputManager().ResetMask();
        centralManager!.GetInputManager().UpdateMask();
    }

    /// <summary>
    /// Function called when deleting the current template from the user system
    /// </summary>
    public void DeleteTemplate() {
        int templateIndex = centralManager!.GetPaintingWindow().GetSelectedTemplateIndex();
        if (templateIndex == -1) {
            return;
        }

        TemplateViewModel tvm = Templates[templateIndex];
        tvm.DeleteFile(mainWindowViewModel.InputImageSelection);
        Templates.Remove(tvm);
        centralManager!.GetInputManager().ResetMask();
        centralManager!.GetPaintingWindow().SetTemplates(Util.GetTemplates(
            centralManager.GetMainWindowVM().InputImageSelection, centralManager.GetWFCHandler().IsOverlappingModel(),
            centralManager.GetWFCHandler().GetTileSize()));
    }
}