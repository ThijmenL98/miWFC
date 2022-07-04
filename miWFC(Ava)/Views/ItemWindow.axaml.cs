using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using miWFC.ContentControls;
using miWFC.Managers;

namespace miWFC.Views;

/// <summary>
/// Window that handles the item addition of the application
/// </summary>
public partial class ItemWindow : Window {
    private CentralManager? centralManager;
    
    /*
     * Initializing Functions & Constructor
     */

    public ItemWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
        Closing += (_, e) => {
            centralManager?.getUIManager().switchWindow(Windows.MAIN, true);
            getDataGrid().SelectedIndex = -1;
            e.Cancel = true;
        };
        Opened += (_, _) => {
            if (centralManager != null) {
                centralManager!.getItemWindow().getItemAddMenu().updateAllowedTiles();
            }
        };
    }
    
    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
        this.Find<ItemAddMenu>("itemAddMenu").setCentralManager(cm);
    }
    
    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    // Images

    // Objects

    /// <summary>
    /// Get the Item Add Menu
    /// </summary>
    /// 
    /// <returns></returns>
    public ItemAddMenu getItemAddMenu() {
        return this.Find<ItemAddMenu>("itemAddMenu");
    }

    /// <summary>
    /// Get the data grid which holds the to-be-added items to the output
    /// </summary>
    /// 
    /// <returns></returns>
    public DataGrid getDataGrid() {
        return this.Find<DataGrid>("itemsDataGrid");
    }

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Custom handler for keyboard input
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private void keyDownHandler(object? sender, KeyEventArgs e) {
        if (centralManager == null) {
            return;
        }

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (e.Key) {
            default:
                base.OnKeyDown(e);
                break;
        }
    }
}