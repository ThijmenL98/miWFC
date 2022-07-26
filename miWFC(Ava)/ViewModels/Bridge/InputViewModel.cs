using miWFC.Delegators;
using ReactiveUI;

// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class InputViewModel : ReactiveObject {
    private readonly MainWindowViewModel mainWindowViewModel;

    private bool _inputIsSideView;
    private CentralDelegator? centralDelegator;

    /*
     * Initializing Functions & Constructor
     */

    public InputViewModel(MainWindowViewModel mwvm) {
        mainWindowViewModel = mwvm;
    }

    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    /// <summary>
    ///     Whether the current custom input is a side-view image
    /// </summary>
    public bool InputIsSideView {
        get => _inputIsSideView;
        set => this.RaiseAndSetIfChanged(ref _inputIsSideView, value);
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
    }

    // Images

    // Objects

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Function called from the UI once the input wrapping is changed
    /// </summary>
    public async void ToggleInputWrapping() {
        mainWindowViewModel.InputWrapping = !mainWindowViewModel.InputWrapping;
        centralDelegator!.GetWFCHandler().SetInputChanged("Input Wrapping Change");
        await centralDelegator!.GetOutputHandler().RestartSolution("Input Wrapping Change");
    }
}