using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using WFC4ALL.ContentControls;
using WFC4ALL.Managers;

namespace WFC4ALL.Views; 

public partial class ItemWindow : Window {
    private CentralManager? centralManager;
    
    public ItemWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
        Closing += (_, e) => {
            centralManager?.getUIManager().switchWindow(Windows.MAIN, true);
            getDataGrid().SelectedIndex = -1;
            e.Cancel = true;
        };
        Opened += (_, e) => {
            if (centralManager != null) {
                centralManager!.getItemWindow().getItemAddMenu().updateCheckBoxesLength();
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
    
    public ItemAddMenu getItemAddMenu() {
        return this.Find<ItemAddMenu>("itemAddMenu");
    }

    public DataGrid getDataGrid() {
        return this.Find<DataGrid>("itemsDataGrid");
    }

    /*
     * Event Handlers
     */

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