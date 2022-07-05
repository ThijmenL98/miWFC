using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media;
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

    private ObservableCollection<TemplateViewModel> _templates = new();

    private bool _pencilModeEnabled,
        _paintModeEnabled,
        templateCreationModeEnabled,
        _isPaintOverrideEnabled,
        _templatePlaceModeEnabled;

    /*
     * Initializing Functions & Constructor
     */

    public PaintingViewModel(MainWindowViewModel mwvm) {
        mainWindowViewModel = mwvm;
    }

    public void setCentralManager(CentralManager cm) {
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

    // Images

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

        centralManager!.getInputManager().resetMask();
        centralManager!.getInputManager().updateMask();
    }

    /// <summary>
    /// Function called when switching to the paint mode
    /// </summary>
    public void ActivatePaintMode() {
        PaintModeEnabled = !PaintModeEnabled;
        TemplateCreationModeEnabled = false;
        TemplatePlaceModeEnabled = false;
        PencilModeEnabled = false;

        centralManager!.getInputManager().resetMask();
        centralManager!.getInputManager().updateMask();
    }

    /// <summary>
    /// Function called when switching to the template creation mode
    /// </summary>
    public void ActivateTemplateCreationMode() {
        TemplateCreationModeEnabled = !TemplateCreationModeEnabled;
        PaintModeEnabled = false;
        TemplatePlaceModeEnabled = false;
        PencilModeEnabled = false;

        centralManager!.getInputManager().resetMask();
        centralManager!.getInputManager().updateMask();
    }

    /// <summary>
    /// Function called when switching to the template placement mode
    /// </summary>
    public void ActivateTemplatePlacementMode() {
        TemplatePlaceModeEnabled = !TemplatePlaceModeEnabled;
        TemplateCreationModeEnabled = false;
        PaintModeEnabled = false;
        PencilModeEnabled = false;

        centralManager!.getInputManager().resetMask();
        centralManager!.getInputManager().updateMask();
    }

    /// <summary>
    /// Function called applying the current paint mask
    /// </summary>
    public async Task ApplyPaintMask() {
        Color[,] mask = centralManager!.getInputManager().getMaskColours();
        if (!(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green)) {
            centralManager!.getUIManager().dispatchError(centralManager.getPaintingWindow());
            return;
        }

        mainWindowViewModel.toggleLoadingAnimation(true);
        centralManager!.getMainWindowVM().StepAmount = 1;

        centralManager.getInputManager().resetMask();
        centralManager.getInputManager().updateMask();
        await centralManager.getWFCHandler().handlePaintBrush(mask);
    }

    /// <summary>
    /// Function called applying the current template
    /// </summary>
    public async Task CreateTemplate() {
        Color[,] mask = centralManager!.getInputManager().getMaskColours();

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        bool nonEmpty = false;

        if (centralManager!.getWFCHandler().isOverlappingModel()) {
            for (int a = 0; a < mask.GetLength(0); a++) {
                for (int b = 0; b < mask.GetLength(1); b++) {
                    if (mask[a, b] == Colors.Gold) {
                        Color atPos = centralManager.getWFCHandler().getOverlappingOutputAt(a, b);

                        if (atPos.A != 255) {
                            centralManager.getUIManager().dispatchError(centralManager!.getPaintingWindow());
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
                centralManager.getUIManager().dispatchError(centralManager!.getPaintingWindow());
                return;
            }

            int patternWidth = maxX - minX + 1;
            int patternHeight = maxY - minY + 1;
            Color[,] offsetMask = new Color[patternWidth, patternHeight];

            for (int a = minX; a <= maxX; a++) {
                for (int b = minY; b <= maxY; b++) {
                    if (mask[a, b] == Colors.Gold) {
                        offsetMask[a - minX, b - minY]
                            = centralManager.getWFCHandler().getOverlappingOutputAt(a, b);
                    } else {
                        offsetMask[a - minX, b - minY] = Colors.Transparent;
                    }
                }
            }

            TemplateViewModel tvm = new(Util.ColourArrayToImage(offsetMask), offsetMask);
            bool saved = await tvm.Save(mainWindowViewModel.InputImageSelection);
            if (saved) {
                Templates.Add(tvm);
            }
        } else {
            ITopoArray<int> aOutput = centralManager!.getWFCHandler().getPropagatorOutputA();
            for (int a = 0; a < mask.GetLength(0); a++) {
                for (int b = 0; b < mask.GetLength(1); b++) {
                    if (mask[a, b] == Colors.Gold) {
                        if (aOutput.get(a, b) < 0) {
                            centralManager.getUIManager().dispatchError(centralManager!.getPaintingWindow());
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
                centralManager.getUIManager().dispatchError(centralManager!.getPaintingWindow());
                return;
            }

            int patternWidth = maxX - minX + 1;
            int patternHeight = maxY - minY + 1;
            int[,] offsetMask = new int[patternWidth, patternHeight];

            for (int a = minX; a <= maxX; a++) {
                for (int b = minY; b <= maxY; b++) {
                    if (mask[a, b] == Colors.Gold) {
                        offsetMask[a - minX, b - minY] = aOutput.get(a, b);
                    } else {
                        offsetMask[a - minX, b - minY] = -1;
                    }
                }
            }

            TemplateViewModel tvm
                = new(
                    Util.ValueArrayToImage(offsetMask, centralManager!.getWFCHandler().getTileSize(),
                        centralManager!.getWFCHandler().getTileCache()), offsetMask);
            bool saved = await tvm.Save(mainWindowViewModel.InputImageSelection);
            if (saved) {
                Templates.Add(tvm);
            }
        }

        centralManager.getPaintingWindow().setTemplates(Templates);
        centralManager.getInputManager().resetMask();
        centralManager.getInputManager().updateMask();
    }

    /// <summary>
    /// Function called to reset the mask that is being painted on
    /// </summary>
    public void ResetMask() {
        centralManager!.getInputManager().resetMask();
        centralManager!.getInputManager().updateMask();
    }

    /// <summary>
    /// Function called when deleting the current template from the user system
    /// </summary>
    public void DeleteTemplate() {
        int templateIndex = centralManager!.getPaintingWindow().getSelectedTemplateIndex();
        if (templateIndex == -1) {
            return;
        }

        TemplateViewModel tvm = Templates[templateIndex];
        tvm.DeleteFile(mainWindowViewModel.InputImageSelection);
        Templates.Remove(tvm);
        centralManager!.getInputManager().resetMask();
        centralManager!.getPaintingWindow().setTemplates(Util.GetTemplates(
            centralManager.getMainWindowVM().InputImageSelection, centralManager.getWFCHandler().isOverlappingModel(),
            centralManager.getWFCHandler().getTileSize()));
    }
}