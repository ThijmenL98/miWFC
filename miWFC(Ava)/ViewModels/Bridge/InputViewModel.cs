using miWFC.Managers;
using ReactiveUI;

// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class InputViewModel : ReactiveObject {
    private readonly MainWindowViewModel mainWindowViewModel;

    private bool _inputIsSideView;
    private CentralManager? centralManager;

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

    public void SetCentralManager(CentralManager cm) {
        centralManager = cm;
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
        centralManager!.GetWFCHandler().SetInputChanged("Input Wrapping Change");
        await centralManager!.GetInputManager().RestartSolution("Input Wrapping Change");
    }
}