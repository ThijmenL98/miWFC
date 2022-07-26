using miWFC.ViewModels;
using miWFC.Views;

namespace miWFC.Delegators;

/// <summary>
///     Central delegator of the application, tying all windows and handlers together for communication of data
/// </summary>
public class CentralDelegator {
    private readonly OutputHandler outputHandler;
    private readonly ItemWindow itemWindow;

    private readonly MainWindow mainWindow;

    private readonly MainWindowViewModel mainWindowViewModel;
    private readonly PaintingWindow paintingWindow;
    private readonly InterfaceHandler interfaceHandler;
    private readonly WeightMapWindow weightMapWindow;
    private readonly WFCHandler wfcHandler;

    /*
     * Initializing Functions & Constructor
     */

    public CentralDelegator(MainWindowViewModel mWVM, MainWindow mW, PaintingWindow pW, ItemWindow iW,
        WeightMapWindow wMW) {
        mainWindow = mW;
        paintingWindow = pW;
        itemWindow = iW;
        weightMapWindow = wMW;

        mainWindowViewModel = mWVM;

        wfcHandler = new WFCHandler(this);
        interfaceHandler = new InterfaceHandler(this);
        outputHandler = new OutputHandler(this);
    }

    /*
     * Getters
     */

    /// <summary>
    ///     Get the application's output handler
    /// </summary>
    /// 
    /// <returns>OutputHandler</returns>
    public OutputHandler GetOutputHandler() {
        return outputHandler;
    }

    /// <summary>
    ///     Get the application's UI Handler
    /// </summary>
    /// 
    /// <returns>InterfaceHandler</returns>
    public InterfaceHandler GetInterfaceHandler() {
        return interfaceHandler;
    }

    /// <summary>
    ///     Get the application's WFC Algorithm handler
    /// </summary>
    /// 
    /// <returns>WFCHandler</returns>
    public WFCHandler GetWFCHandler() {
        return wfcHandler;
    }

    /// <summary>
    ///     Get the application's Main Window
    /// </summary>
    /// <returns>MainWindow</returns>
    public MainWindow GetMainWindow() {
        return mainWindow;
    }

    /// <summary>
    ///     Get the application's Painting Window
    /// </summary>
    /// <returns>PaintingWindow</returns>
    public PaintingWindow GetPaintingWindow() {
        return paintingWindow;
    }

    /// <summary>
    ///     Get the application's Item Window
    /// </summary>
    /// <returns>ItemWindow</returns>
    public ItemWindow GetItemWindow() {
        return itemWindow;
    }

    /// <summary>
    ///     Get the application's Main Window View Model
    /// </summary>
    /// <returns>MainWindowViewModel</returns>
    public MainWindowViewModel GetMainWindowVM() {
        return mainWindowViewModel;
    }

    /// <summary>
    ///     Get the application's Weight Mapping Window
    /// </summary>
    /// <returns>WeightMapWindow</returns>
    public WeightMapWindow GetWeightMapWindow() {
        return weightMapWindow;
    }
}