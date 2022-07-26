#if DEBUG
using Avalonia;
#endif
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using miWFC.ContentControls;
using miWFC.Delegators;

namespace miWFC.Views;

/// <summary>
///     Window that handles the item addition of the application
/// </summary>
public partial class ItemWindow : Window {
    private CentralDelegator? centralDelegator;

    /*
     * Initializing Functions & Constructor
     */

    public ItemWindow() {
        InitializeComponent();

        KeyDown += KeyDownHandler;
        Closing += (_, e) => {
            centralDelegator?.GetInterfaceHandler().SwitchWindow(Windows.MAIN, true);
            GetDataGrid().SelectedIndex = -1;
            e.Cancel = true;
        };
        Opened += (_, _) => {
            if (centralDelegator != null) {
                centralDelegator!.GetItemWindow().GetItemAddMenu().UpdateAllowedTiles();
            }
        };
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
        this.Find<ItemAddMenu>("itemAddMenu").SetCentralDelegator(cd);
        this.Find<RegionDefineMenu>("regionDefineMenu").SetCentralDelegator(cd);
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
    ///     Get the Item Add Menu
    /// </summary>
    /// <returns></returns>
    public ItemAddMenu GetItemAddMenu() {
        return this.Find<ItemAddMenu>("itemAddMenu");
    }

    /// <summary>
    ///     Get the Region Defining Menu
    /// </summary>
    /// <returns></returns>
    public RegionDefineMenu GetRegionDefineMenu() {
        return this.Find<RegionDefineMenu>("regionDefineMenu");
    }

    /// <summary>
    ///     Get the data grid which holds the to-be-added items to the output
    /// </summary>
    /// <returns></returns>
    public DataGrid GetDataGrid() {
        return this.Find<DataGrid>("itemsDataGrid");
    }

    // Lists

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Custom handler for keyboard input
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private void KeyDownHandler(object? sender, KeyEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        switch (e.Key) {
            case Key.R:
                if (!centralDelegator!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralDelegator!.GetMainWindowVM().ItemVM.GenerateItemGrid();
                    e.Handled = true;
                }

                break;
            case Key.N:
            case Key.C:
                if (!centralDelegator!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralDelegator!.GetMainWindowVM().ItemVM.CreateNewItem();
                    e.Handled = true;
                }

                break;
            case Key.E:
                if (!centralDelegator!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralDelegator!.GetMainWindowVM().ItemVM.EditSelectedItem();
                    e.Handled = true;
                }

                break;
            case Key.Delete:
            case Key.Back:
                if (!centralDelegator!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralDelegator!.GetMainWindowVM().ItemVM.RemoveSelectedItem();
                    e.Handled = true;
                }

                break;
            case Key.Escape:
                if (centralDelegator.GetInterfaceHandler().PopUpOpened()) {
                    centralDelegator.GetInterfaceHandler().HidePopUp();
                } else if (centralDelegator!.GetMainWindowVM().ItemVM.InRegionDefineMenu) {
                    centralDelegator!.GetMainWindowVM().ItemVM.InRegionDefineMenu = false;
                } else if (centralDelegator!.GetMainWindowVM().ItemVM.InItemMenu) {
                    centralDelegator!.GetMainWindowVM().ItemVM.InItemMenu = false;
                } else {
                    centralDelegator?.GetInterfaceHandler().SwitchWindow(Windows.MAIN);
                }

                e.Handled = true;
                break;
            case Key.W:
                bool hasFocusOnTB = GetItemAddMenu().GetItemColourTB().IsFocused ||
                                    GetItemAddMenu().GetItemNameTB().IsFocused ||
                                    GetItemAddMenu().GetDepItemNameTB().IsFocused ||
                                    GetItemAddMenu().GetDepItemColourTB().IsFocused;
                if (centralDelegator!.GetMainWindowVM().ItemVM.InItemMenu && !hasFocusOnTB) {
                    centralDelegator!.GetMainWindowVM().ItemVM.InRegionDefineMenu = true;
                    e.Handled = true;
                }

                break;
            default:
                base.OnKeyDown(e);
                break;
        }
    }
}