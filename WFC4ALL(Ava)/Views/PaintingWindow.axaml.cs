using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using WFC4ALL.Managers;
using WFC4ALL.Models;

namespace WFC4ALL.Views;

public partial class PaintingWindow : Window {
    private CentralManager? centralManager;
    private readonly ComboBox _paintingPatternsCB;

    public PaintingWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
        Closing += (_, e) => {
            centralManager?.getUIManager().switchWindow(Windows.MAIN);
            e.Cancel = true;
        };
        
        _paintingPatternsCB = this.Find<ComboBox>("tilePaintSelectCB");
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

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    public void OutputImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
        if (centralManager!.getMainWindowVM().PencilModeEnabled) {
            (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;
            (double clickX, double clickY) = e.GetPosition(e.Source as Image);

            int idx = _paintingPatternsCB.SelectedIndex;

            bool? success = centralManager?.getInputManager().processClick((int) Math.Round(clickX),
                (int) Math.Round(clickY),
                (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom), idx);
            if (success != null && !(bool) success) {
                centralManager?.getUIManager().dispatchError(this);
            }
        } else {
            centralManager?.getUIManager().dispatchError(this);
        }
    }

    public void setPaintingPatterns(TileViewModel[]? values, int idx = 0) {
        if (values != null) {
            _paintingPatternsCB.Items = values;
        }

        _paintingPatternsCB.SelectedIndex = idx;
    }
}