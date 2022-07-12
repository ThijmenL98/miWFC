﻿#if DEBUG
using Avalonia;
#endif
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

        KeyDown += KeyDownHandler;
        Closing += (_, e) => {
            centralManager?.GetUIManager().SwitchWindow(Windows.MAIN, true);
            GetDataGrid().SelectedIndex = -1;
            e.Cancel = true;
        };
        Opened += (_, _) => {
            if (centralManager != null) {
                centralManager!.GetItemWindow().GetItemAddMenu().UpdateAllowedTiles();
            }
        };
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    public void SetCentralManager(CentralManager cm) {
        centralManager = cm;
        this.Find<ItemAddMenu>("itemAddMenu").SetCentralManager(cm);
        this.Find<RegionDefineMenu>("regionDefineMenu").SetCentralManager(cm);
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
    public ItemAddMenu GetItemAddMenu() {
        return this.Find<ItemAddMenu>("itemAddMenu");
    }

    /// <summary>
    /// Get the Region Defining Menu
    /// </summary>
    /// 
    /// <returns></returns>
    public RegionDefineMenu GetRegionDefineMenu() {
        return this.Find<RegionDefineMenu>("regionDefineMenu");
    }

    /// <summary>
    /// Get the data grid which holds the to-be-added items to the output
    /// </summary>
    ///
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
    /// Custom handler for keyboard input
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">KeyEventArgs</param>
    private void KeyDownHandler(object? sender, KeyEventArgs e) {
        if (centralManager == null) {
            return;
        }

        switch (e.Key) {
            case Key.R:
                if (!centralManager!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralManager!.GetMainWindowVM().ItemVM.GenerateItemGrid();
                    e.Handled = true;
                }

                break;
            case Key.N:
            case Key.C:
                if (!centralManager!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralManager!.GetMainWindowVM().ItemVM.CreateNewItem();
                    e.Handled = true;
                }

                break;
            case Key.E:
                if (!centralManager!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralManager!.GetMainWindowVM().ItemVM.EditSelectedItem();
                    e.Handled = true;
                }

                break;
            case Key.Delete:
            case Key.Back:
                if (!centralManager!.GetMainWindowVM().ItemVM.InAnyMenu) {
                    centralManager!.GetMainWindowVM().ItemVM.RemoveSelectedItem();
                    e.Handled = true;
                }

                break;
            case Key.Escape:
                if (centralManager.GetUIManager().PopUpOpened()) {
                    centralManager.GetUIManager().HidePopUp();
                } else if (centralManager!.GetMainWindowVM().ItemVM.InRegionDefineMenu) {
                    centralManager!.GetMainWindowVM().ItemVM.InRegionDefineMenu = false;
                } else if (centralManager!.GetMainWindowVM().ItemVM.InItemMenu) {
                    centralManager!.GetMainWindowVM().ItemVM.InItemMenu = false;
                } else {
                    centralManager?.GetUIManager().SwitchWindow(Windows.MAIN);
                }

                e.Handled = true;
                break;
            case Key.W:
                bool hasFocusOnTB = GetItemAddMenu().GetItemColourTB().IsFocused ||
                                    GetItemAddMenu().GetItemNameTB().IsFocused ||
                                    GetItemAddMenu().GetDepItemNameTB().IsFocused ||
                                    GetItemAddMenu().GetDepItemColourTB().IsFocused;
                if (centralManager!.GetMainWindowVM().ItemVM.InItemMenu && !hasFocusOnTB) {
                    centralManager!.GetMainWindowVM().ItemVM.InRegionDefineMenu = true;
                    e.Handled = true;
                }

                break;
            default:
                base.OnKeyDown(e);
                break;
        }
    }
}