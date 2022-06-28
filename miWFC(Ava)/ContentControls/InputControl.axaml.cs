using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable UnusedParameter.Local

namespace miWFC.ContentControls;

public partial class InputControl : UserControl {
    private readonly ComboBox _categoryCB, _inputCB, _patternSizeCB;
    private CentralManager? centralManager;

    public InputControl() {
        InitializeComponent();

        _categoryCB = this.Find<ComboBox>("categoryCB");
        _inputCB = this.Find<ComboBox>("inputCB");
        _patternSizeCB = this.Find<ComboBox>("patternSizeCB");
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
        inImgCBChangeHandler(null, null);
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    /*
     * Handlers
     */

    private void catCBChangeHandler(object _, SelectionChangedEventArgs e) {
        if (centralManager == null || centralManager.getWFCHandler().isChangingModels()) {
            return;
        }

        string newValue = getCategory();
        bool isOverlapping = !centralManager!.getMainWindowVM().SimpleModelSelected;

        string[] inputImageDataSource
            = Util.getModelImages(isOverlapping ? "overlapping" : "simpletiled", newValue);
        setInputImages(inputImageDataSource);
        e.Handled = true;
    }

    public void inImgCBChangeHandler(object? o, SelectionChangedEventArgs? e) {
        inImgCBChangeHandler(o, e, -1);
    }

    private async void inImgCBChangeHandler(object? o, SelectionChangedEventArgs? e, int newTab) {
        if (centralManager == null || centralManager.getWFCHandler().isChangingModels()) {
            return;
        }

        centralManager.getWFCHandler().setImageChanging(true);

        string newValue = getInputImage();
        centralManager.getUIManager().updateInputImage(newValue);

        centralManager.getWFCHandler().setInputChanged("Image CB");

        if (!centralManager.getMainWindowVM().SimpleModelSelected) {
            (int[] patternSizeDataSource, int i) = Util.getImagePatternDimensions(newValue);
            setPatternSizes(patternSizeDataSource, i);
        }

        centralManager.getWFCHandler().setImageChanging(false);
        if (newTab > 1 || newTab == -1) {
            await centralManager.getInputManager().restartSolution("Image CB Handler", true);
        }

        if (e != null) {
            e.Handled = true;
        }
    }

    // ReSharper disable once UnusedMember.Local
    private async void pattSizeCBChangeHandler(object? _, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        centralManager.getWFCHandler().setInputChanged("Pattern Size CB");
        e.Handled = true;
        if (centralManager.getMainWindowVM().SelectedTabIndex > 1) {
            await centralManager.getInputManager().restartSolution("Pattern CB Handler");
        }
    }

    /*
     * Setters
     */

    public void setInputImages(string[]? values, int idx = 0) {
        if (values != null) {
            _inputCB.Items = values;
        }

        _inputCB.SelectedIndex = idx;
    }

    public void setPatternSizes(int[]? values, int idx = 0) {
        if (values != null) {
            _patternSizeCB.Items = values;
        }

        _patternSizeCB.SelectedIndex = idx;
    }

    public void setCategories(HoverableTextViewModel[]? values, int idx = 0) {
        if (values != null) {
            _categoryCB.Items = values;
        }

        _categoryCB.SelectedIndex = idx;
    }

    /*
     * Getters
     */

    public string getInputImage() {
        return _inputCB.SelectedItem as string ?? "3Bricks";
    }

    public string getCategory() {
        return (_categoryCB.SelectedItem as HoverableTextViewModel)?.DisplayText ?? "Textures";
    }

    public int getPatternSize() {
        return (int) (_patternSizeCB.SelectedItem ?? 3);
    }

    public double getInputImageHeight() {
        return this.Find<Image>("inputImage").Bounds.Height;
    }

    public double getInputImageWidth() {
        return this.Find<Image>("inputImage").Bounds.Width;
    }
}