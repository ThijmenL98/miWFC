using miWFC.Managers;
using ReactiveUI;
// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class InputViewModel : ReactiveObject {
    private CentralManager? centralManager;
    private readonly MainWindowViewModel mainWindowViewModel;
    
    /*
     * Initializing Functions & Constructor
     */

    public InputViewModel(MainWindowViewModel mwvm) {
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
    /// Function called from the UI once the input wrapping is changed
    /// </summary>
    public async void ToggleInputWrapping() {
        mainWindowViewModel.InputWrapping = !mainWindowViewModel.InputWrapping;
        centralManager!.getWFCHandler().setInputChanged("Input Wrapping Change");
        await centralManager!.getInputManager().restartSolution("Input Wrapping Change");
    }
}