using miWFC.Managers;
using ReactiveUI;

// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class OutputViewModel : ReactiveObject {
    private readonly MainWindowViewModel mainWindowViewModel;
    private CentralManager? centralManager;

    /*
     * Initializing Functions & Constructor
     */

    public OutputViewModel(MainWindowViewModel mwvm) {
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

    // Images

    // Objects

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Function called when starting or stopping the animation
    /// </summary>
    public void ToggleAnimation() {
        mainWindowViewModel.IsPlaying = !mainWindowViewModel.IsPlaying;
        centralManager!.GetInputManager().Animate();
    }

    /// <summary>
    ///     Function called when advancing a single step
    /// </summary>
    public void AdvanceStep() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.GetInputManager().AdvanceStep();
    }

    /// <summary>
    ///     Function called when placing a marker
    /// </summary>
    public void PlaceMarker() {
        centralManager!.GetInputManager().PlaceMarker();
    }

    /// <summary>
    ///     Function called when loading to the most recent previous marker
    /// </summary>
    public void RevertToMarker() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.GetInputManager().LoadMarker();
    }

    /// <summary>
    ///     Function called when stepping a single step back
    /// </summary>
    public void BacktrackStep() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.GetInputManager().RevertStep();
    }

    /// <summary>
    ///     Function called when toggling the seamless output of the image
    /// </summary>
    public async void ToggleSeamlessness() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        mainWindowViewModel.SeamlessOutput = !mainWindowViewModel.SeamlessOutput;
        await centralManager!.GetInputManager().RestartSolution("Padding Toggle Change");
    }

    /// <summary>
    ///     Function called when importing an image
    /// </summary>
    public void ImportFromDevice() {
        centralManager!.GetInputManager().ImportSolution();
    }

    /// <summary>
    ///     Function called when exporting an image
    /// </summary>
    public void ExportToDevice() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.GetInputManager().ExportSolution();
    }

    /// <summary>
    ///     Function called when restarting the solution
    /// </summary>
    public async void Restart() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralManager!.GetWFCHandler().ResetWeights(false);
        centralManager!.GetWFCHandler().UpdateWeights();

        await centralManager!.GetInputManager().RestartSolution("Restart UI call");
    }
}