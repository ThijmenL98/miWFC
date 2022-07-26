using miWFC.Delegators;
using ReactiveUI;

// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class OutputViewModel : ReactiveObject {
    private readonly MainWindowViewModel mainWindowViewModel;
    private CentralDelegator? centralDelegator;

    /*
     * Initializing Functions & Constructor
     */

    public OutputViewModel(MainWindowViewModel mwvm) {
        mainWindowViewModel = mwvm;
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
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
        centralDelegator!.GetOutputHandler().Animate();
    }

    /// <summary>
    ///     Function called when advancing a single step
    /// </summary>
    public void AdvanceStep() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralDelegator!.GetOutputHandler().AdvanceStep();
    }

    /// <summary>
    ///     Function called when placing a marker
    /// </summary>
    public void PlaceMarker() {
        centralDelegator!.GetOutputHandler().PlaceMarker();
    }

    /// <summary>
    ///     Function called when loading to the most recent previous marker
    /// </summary>
    public void RevertToMarker() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralDelegator!.GetOutputHandler().LoadMarker();
    }

    /// <summary>
    ///     Function called when stepping a single step back
    /// </summary>
    public void BacktrackStep() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralDelegator!.GetOutputHandler().RevertStep();
    }

    /// <summary>
    ///     Function called when toggling the seamless output of the image
    /// </summary>
    public async void ToggleSeamlessness() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        mainWindowViewModel.SeamlessOutput = !mainWindowViewModel.SeamlessOutput;
        await centralDelegator!.GetOutputHandler().RestartSolution("Padding Toggle Change");
    }

    /// <summary>
    ///     Function called when importing an image
    /// </summary>
    public void ImportFromDevice() {
        centralDelegator!.GetOutputHandler().ImportSolution();
    }

    /// <summary>
    ///     Function called when exporting an image
    /// </summary>
    public async void ExportToDevice() {
        if (centralDelegator == null) {
            return;
        }
        
        if (centralDelegator.GetMainWindowVM().ImageOutWidth != (int) centralDelegator.GetWFCHandler().GetPropagatorSize().Width ||
            centralDelegator.GetMainWindowVM().ImageOutHeight != (int) centralDelegator.GetWFCHandler().GetPropagatorSize().Height) {
            if (!centralDelegator.GetMainWindowVM().IsRunning && !centralDelegator.GetWFCHandler().IsCollapsed()) {
                await centralDelegator.GetOutputHandler().RestartSolution("Export non-equal input");
            } else {
                centralDelegator.GetMainWindowVM().ImageOutWidth = (int) centralDelegator.GetWFCHandler().GetPropagatorSize().Width;
                centralDelegator.GetMainWindowVM().ImageOutHeight = (int) centralDelegator.GetWFCHandler().GetPropagatorSize().Height;
            }
        }
        
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralDelegator!.GetOutputHandler().ExportSolution();
    }

    /// <summary>
    ///     Function called when restarting the solution
    /// </summary>
    public async void Restart() {
        if (mainWindowViewModel.IsPlaying) {
            ToggleAnimation();
        }

        centralDelegator!.GetWFCHandler().ResetWeights(false);
        centralDelegator!.GetWFCHandler().UpdateWeights();

        await centralDelegator!.GetOutputHandler().RestartSolution("Restart UI call");
    }
}