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

    private bool canUsePencil = true;

    public void OutputImageOnPointerPressed(object sender, PointerPressedEventArgs e) {
        if (centralManager!.getMainWindowVM().PencilModeEnabled ||
            centralManager!.getMainWindowVM().PaintEraseModeEnabled || 
            centralManager!.getMainWindowVM().PaintKeepModeEnabled) {
            OutputImageOnPointerMoved(sender, e);
        }

    }

    public void OutputImageOnPointerReleased(object sender, PointerReleasedEventArgs e) {
        canUsePencil = true;
    }

    public int getSelectedPaintIndex() {
        return _paintingPatternsCB.SelectedIndex;
    }
    
    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e) {
        (double clickX, double clickY) = e.GetPosition(e.Source as Image);
        if ((centralManager!.getMainWindowVM().PaintEraseModeEnabled
                || centralManager!.getMainWindowVM().PaintKeepModeEnabled)
            && e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) {
            (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;

            try {
                centralManager?.getInputManager().processClickMask((int) Math.Round(clickX),
                    (int) Math.Round(clickY),
                    (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                    (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
                    centralManager!.getMainWindowVM().PaintKeepModeEnabled);
            } catch (IndexOutOfRangeException exception) {
                Trace.WriteLine(exception);
            }
        } else if (centralManager!.getMainWindowVM().PencilModeEnabled && canUsePencil) {
            (double hoverX, double hoverY) = e.GetPosition(e.Source as Image);
            (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;

            if (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) {
                int idx = _paintingPatternsCB.SelectedIndex;

                bool? success = centralManager?.getInputManager().processClick((int) Math.Round(clickX),
                    (int) Math.Round(clickY),
                    (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                    (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom), idx);
                if (success != null && !(bool) success) {
                    centralManager?.getUIManager().dispatchError(this);
                    canUsePencil = false;
                }
            }

            centralManager?.getInputManager().processHoverAvailability((int) Math.Round(hoverX),
                (int) Math.Round(hoverY),
                (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
                _paintingPatternsCB.SelectedIndex);
        }

        e.Handled = true;
    }

    // ReSharper disable twice UnusedParameter.Local
    private void OnPointerMoved(object sender, PointerEventArgs e) {
        centralManager?.getInputManager().resetHoverAvailability();
    }

    public void setPaintingPatterns(TileViewModel[]? values, int idx = 0) {
        if (values != null) {
            _paintingPatternsCB.Items = values;
            centralManager!.getMainWindowVM().PaintTiles = new ObservableCollection<TileViewModel>(values);
        }

        _paintingPatternsCB.SelectedIndex = idx;
    }

    public double getTimelineWidth() {
        return this.Find<Grid>("timeline").Bounds.Width;
    }

    public int getPaintBrushSize() {
        int[] sizes = {1, 2, 3, 6, 10, 15, 25};
        return sizes[_paintingSizeCB.SelectedIndex];
    }

    // ReSharper disable twice UnusedParameter.Local
    private void Window_OnClosing(object? sender, CancelEventArgs e) {
        centralManager?.getUIManager().handlePaintingClose(false);
    }
}