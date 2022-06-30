using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using miWFC.Managers;
using miWFC.ViewModels;

namespace miWFC.Views;

public partial class PaintingWindow : Window {
    private readonly ComboBox _paintingPatternsCB, _templatesCB, _paintingSizeCB;

    private bool canUsePencil = true;
    private CentralManager? centralManager;

    public PaintingWindow() {
        InitializeComponent();

        KeyDown += keyDownHandler;
        Closing += (_, e) => {
            Color[,] mask = centralManager!.getInputManager().getMaskColours();
            centralManager?.getUIManager()
                .switchWindow(Windows.MAIN, !(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green));
            e.Cancel = true;
        };

        _paintingPatternsCB = this.Find<ComboBox>("tilePaintSelectCB");
        _templatesCB = this.Find<ComboBox>("templateCB");
        _paintingSizeCB = this.Find<ComboBox>("BrushSizeCB");
    }

    /*
     * Event Handlers
     */

    private async void keyDownHandler(object? sender, KeyEventArgs e) {
        if (centralManager == null) {
            return;
        }

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (e.Key) {
            case Key.P:
                centralManager.getMainWindowVM().OnPencilModeClick();
                e.Handled = true;
                break;
            case Key.B:
                centralManager.getMainWindowVM().OnPaintModeClick();
                e.Handled = true;
                break;
            case Key.R:
                centralManager.getMainWindowVM().OnMaskReset();
                e.Handled = true;
                break;
            case Key.A:
                await centralManager.getMainWindowVM().OnApplyClick();
                e.Handled = true;
                break;
            case Key.Back:
            case Key.Escape:
                if (centralManager.getUIManager().popUpOpened()) {
                    centralManager.getUIManager().hidePopUp();
                } else {
                    Color[,] mask = centralManager!.getInputManager().getMaskColours();
                    centralManager?.getUIManager()
                        .switchWindow(Windows.MAIN, !(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green));
                }

                e.Handled = true;
                break;
            case Key.S:
            case Key.M:
                centralManager.getInputManager().placeMarker();
                e.Handled = true;
                break;
            case Key.L:
                centralManager.getInputManager().loadMarker();
                e.Handled = true;
                break;
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
        OutputImageOnPointerMoved(sender, e, true);
    }

    public void OutputImageOnPointerReleased(object sender, PointerReleasedEventArgs e) {
        canUsePencil = true;
    }

    public int getSelectedPaintIndex() {
        return _paintingPatternsCB.SelectedIndex;
    }

    public int getSelectedTemplateIndex() {
        return _templatesCB.SelectedIndex;
    }

    private void OutputImageOnPointerMoved(object sender, PointerEventArgs e) {
        // ReSharper disable once IntroduceOptionalParameters.Local
        OutputImageOnPointerMoved(sender, e, false);
    }

    private async void OutputImageOnPointerMoved(object sender, PointerEventArgs e, bool forceClick) {
        (double posX, double posY) = e.GetPosition(e.Source as Image);
        (double imgWidth, double imgHeight) = (sender as Image)!.DesiredSize;
        bool allowClick = !centralManager!.getMainWindowVM().IsPaintOverrideEnabled || forceClick;
        if ((centralManager!.getMainWindowVM().PaintModeEnabled)
            && (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed ||
                e.GetCurrentPoint(e.Source as Image).Properties.IsRightButtonPressed) && allowClick) {
            try {
                centralManager?.getInputManager().processClickMask((int) Math.Round(posX),
                    (int) Math.Round(posY),
                    (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                    (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
                    e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed);
            } catch (IndexOutOfRangeException) { }
        } else if (centralManager!.getMainWindowVM().PencilModeEnabled && canUsePencil) {
            if (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed && allowClick) {
                int idx = getSelectedPaintIndex();

                bool? success = await centralManager?.getInputManager().processClick((int) Math.Round(posX),
                    (int) Math.Round(posY),
                    (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                    (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
                    idx)!;
                if (success != null && !(bool) success) {
                    centralManager?.getUIManager().dispatchError(this);
                    canUsePencil = false;
                }
            }
        } else if (centralManager!.getMainWindowVM().TemplateAddModeEnabled) {
            if (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed ||
                e.GetCurrentPoint(e.Source as Image).Properties.IsRightButtonPressed) {
                try {
                    centralManager?.getInputManager().processClickTemplate((int) Math.Round(posX),
                        (int) Math.Round(posY),
                        (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
                        (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
                        e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed);
                } catch (IndexOutOfRangeException) { }
            }
        } else if (centralManager!.getMainWindowVM().TemplatePlaceModeEnabled) {
            if (e.GetCurrentPoint(e.Source as Image).Properties.IsLeftButtonPressed) { }
        }

        centralManager?.getInputManager().processHoverAvailability((int) Math.Round(posX),
            (int) Math.Round(posY),
            (int) Math.Round(imgWidth - (sender as Image)!.Margin.Right - (sender as Image)!.Margin.Left),
            (int) Math.Round(imgHeight - (sender as Image)!.Margin.Top - (sender as Image)!.Margin.Bottom),
            _paintingPatternsCB.SelectedIndex, centralManager!.getMainWindowVM().PencilModeEnabled);

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

    public void setTemplates(ObservableCollection<TemplateViewModel> values, int idx = 0) {
        if (centralManager == null || centralManager.getWFCHandler().isChangingModels()) {
            return;
        }
        _templatesCB.Items = values;
        centralManager!.getMainWindowVM().Templates = new ObservableCollection<TemplateViewModel>(values);
        _templatesCB.SelectedIndex = idx;
    }

    public double getTimelineWidth() {
        return this.Find<Grid>("timeline").Bounds.Width;
    }

    public int getPaintBrushSize() {
        int[] sizes = {-1, 1, 2, 3, 6, 10, 15, 25};
        return sizes[_paintingSizeCB.SelectedIndex];
    }
}