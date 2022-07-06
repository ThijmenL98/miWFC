using miWFC.ViewModels;
using miWFC.Views;

namespace miWFC.Managers;

/// <summary>
/// Central manager of the application, tying all windows and managers together for communication of data
/// </summary>
public class CentralManager {
    private readonly InputManager inputManager;
    private readonly ItemWindow itemWindow;

    private readonly MainWindow mainWindow;

    private readonly MainWindowViewModel mainWindowViewModel;
    private readonly PaintingWindow paintingWindow;
    private readonly UIManager uiManager;
    private readonly WeightMapWindow weightMapWindow;
    private readonly WFCHandler wfcHandler;

    /*
     * Initializing Functions & Constructor
     */

    public CentralManager(MainWindowViewModel mWVM, MainWindow mW, PaintingWindow pW, ItemWindow iW,
        WeightMapWindow wMW) {
        mainWindow = mW;
        paintingWindow = pW;
        itemWindow = iW;
        weightMapWindow = wMW;

        mainWindowViewModel = mWVM;

        wfcHandler = new WFCHandler(this);
        uiManager = new UIManager(this);
        inputManager = new InputManager(this);
    }
    
    /*
     * Getters
     */

    /// <summary>
    /// Get the application's input manager
    /// </summary>
    /// 
    /// <returns>InputManager</returns>
    public InputManager GetInputManager() {
        return inputManager;
    }
    
    /// <summary>
    /// Get the application's UI manager
    /// </summary>
    /// 
    /// <returns>UIManager</returns>
    public UIManager GetUIManager() {
        return uiManager;
    }

    /// <summary>
    /// Get the application's WFC Algorithm manager
    /// </summary>
    /// 
    /// <returns>WFCHandler</returns>
    public WFCHandler GetWFCHandler() {
        return wfcHandler;
    }

    /// <summary>
    /// Get the application's Main Window
    /// </summary>
    /// 
    /// <returns>MainWindow</returns>
    public MainWindow GetMainWindow() {
        return mainWindow;
    }

    /// <summary>
    /// Get the application's Painting Window
    /// </summary>
    /// 
    /// <returns>PaintingWindow</returns>
    public PaintingWindow GetPaintingWindow() {
        return paintingWindow;
    }

    /// <summary>
    /// Get the application's Item Window
    /// </summary>
    /// 
    /// <returns>ItemWindow</returns>
    public ItemWindow GetItemWindow() {
        return itemWindow;
    }

    /// <summary>
    /// Get the application's Main Window View Model
    /// </summary>
    /// 
    /// <returns>MainWindowViewModel</returns>
    public MainWindowViewModel GetMainWindowVM() {
        return mainWindowViewModel;
    }

    /// <summary>
    /// Get the application's Weight Mapping Window
    /// </summary>
    /// 
    /// <returns>WeightMapWindow</returns>
    public WeightMapWindow GetWeightMapWindow() {
        return weightMapWindow;
    }
}