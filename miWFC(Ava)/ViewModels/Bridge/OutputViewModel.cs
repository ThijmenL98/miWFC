using miWFC.Managers;
using ReactiveUI;

// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class OutputViewModel : ReactiveObject {
    private CentralManager? centralManager;
    private readonly MainWindowViewModel mainWindowViewModel;

    /*
     * Initializing Functions & Constructor
     */

    public OutputViewModel(MainWindowViewModel mwvm) {
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

    // Images

    // Objects

    // Lists

    // Other

    /*
     * UI Callbacks
     */
    
    /// <summary>
    /// Function called when starting or stopping the animation
    /// </summary>
    public void ToggleAnimation() {
        mainWindowViewModel.IsPlaying = !mainWindowViewModel.IsPlaying;
        centralManager!.getInputManager().animate();
    }

    /// <summary>
    /// Function called when advancing a single step
    /// </summary>
    public void AdvanceStep() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.getInputManager().advanceStep();
    }

    /// <summary>
    /// Function called when placing a marker
    /// </summary>
    public void PlaceMarker() {
        centralManager!.getInputManager().placeMarker();
    }

    /// <summary>
    /// Function called when loading to the most recent previous marker
    /// </summary>
    public void RevertToMarker() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.getInputManager().loadMarker();
    }

    /// <summary>
    /// Function called when stepping a single step back
    /// </summary>
    public void BacktrackStep() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.getInputManager().revertStep();
    }

    /// <summary>
    /// Function called when toggling the seamless output of the image
    /// </summary>
    public async void ToggleSeamlessness() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        mainWindowViewModel.SeamlessOutput = !mainWindowViewModel.SeamlessOutput;
        await centralManager!.getInputManager().restartSolution("Padding Toggle Change");
    }

    /// <summary>
    /// Function called when importing an image
    /// </summary>
    public void ImportFromDevice() {
        centralManager!.getInputManager().importSolution();
    }

    /// <summary>
    /// Function called when exporting an image
    /// </summary>
    public void ExportToDevice() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.getInputManager().exportSolution();
    }

    /// <summary>
    /// Function called when restarting the solution
    /// </summary>
    public async void Restart() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.getWFCHandler().resetWeights(false);
        centralManager!.getWFCHandler().updateWeights();

        await centralManager!.getInputManager().restartSolution("Restart UI call");
    }
}