using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using WFC4ALL.Managers;
using WFC4ALL.ViewModels;

namespace WFC4ALL.Views; 

public partial class PaintingWindow : Window {
    private readonly ComboBox _paintingPatternsCB, _paintingSizeCB;
    private CentralManager? centralManager;

    public PaintingWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
        Closing += (_, e) => {
            centralManager?.getUIManager().switchWindow(Windows.MAIN, false);
            e.Cancel = true;
        };

        _paintingPatternsCB = this.Find<ComboBox>("tilePaintSelectCB");
        _paintingSizeCB = this.Find<ComboBox>("BrushSizeCB");
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
        } else if (centralManager!.getMainWindowVM().PaintEraseModeEnabled
                   || centralManager!.getMainWindowVM().PaintKeepModeEnabled) {
            OutputImageOnPointerMoved(sender, e);
        }
    }

    public int getSelectedPaintIndex() {
        return _paintingPatternsCB.SelectedIndex;
    }

    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e) {
        if ((centralManager!.getMainWindowVM().PaintEraseModeEnabled
                || centralManager!.getMainWindowVM().PaintKeepModeEnabled)
            && e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) {
            (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;
            (double clickX, double clickY) = e.GetPosition(e.Source as Image);

            try {
                centralManager?.getInputManager().processClickMask((int) Math.Round(clickX),
                    (int) Math.Round(clickY),
                    (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                    (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
                    centralManager!.getMainWindowVM().PaintKeepModeEnabled);
            } catch (IndexOutOfRangeException exception) {
#if DEBUG
                Trace.WriteLine(exception);
#endif
            }
        } else if (centralManager!.getMainWindowVM().PencilModeEnabled) {
            (double clickX2, double clickY2) = e.GetPosition(e.Source as Image);
            (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;

            centralManager?.getInputManager().processHoverAvailability((int) Math.Round(clickX2),
                (int) Math.Round(clickY2),
                (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom));
        }
    }

    public void setPaintingPatterns(TileViewModel[]? values, int idx = 0) {
        if (values != null) {
            _paintingPatternsCB.Items = values;
            centralManager!.getMainWindowVM().PaintTiles = new ObservableCollection<TileViewModel>(values);
        }

        _paintingPatternsCB.SelectedIndex = idx;
    }

    public int getPaintBrushSize() {
        int[] sizes = {1, 2, 3, 6, 10, 15, 25};
        return sizes[_paintingSizeCB.SelectedIndex];
    }

    private void Window_OnClosing(object? sender, CancelEventArgs e) {
        centralManager?.getUIManager().handlePaintingClose(false);
    }
}