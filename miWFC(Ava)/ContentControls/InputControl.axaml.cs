using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable UnusedParameter.Local

namespace miWFC.ContentControls;

/// <summary>
/// Separated control for the input side of the application
/// </summary>
public partial class InputControl : UserControl {
    private readonly ComboBox _categoryCB, _inputCB, _patternSizeCB;
    private CentralManager? centralManager;

    /*
     * Initializing Functions & Constructor
     */

    public InputControl() {
        InitializeComponent();

        _categoryCB = this.Find<ComboBox>("categoryCB");
        _inputCB = this.Find<ComboBox>("inputCB");
        _patternSizeCB = this.Find<ComboBox>("patternSizeCB");
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
        inImgCBChangeHandler(null, null);
    }

    /*
     * Getters
     */

    /// <summary>
    /// Get the currently selected input category as string
    /// </summary>
    /// 
    /// <returns>Input Category String, default "Textures"</returns>
    public string getCategory() {
        return (_categoryCB.SelectedItem as HoverableTextViewModel)?.DisplayText ?? "Textures";
    }

    /// <summary>
    /// Get the currently selected input image as string
    /// </summary>
    /// 
    /// <returns>Input Image String, default "3Bricks"</returns>
    public string getInputImage() {
        return _inputCB.SelectedItem as string ?? "3Bricks";
    }

    /// <summary>
    /// Get the currently selected pattern size
    /// </summary>
    /// 
    /// <returns>Pattern Size, default 3</returns>
    public int getPatternSize() {
        return (int) (_patternSizeCB.SelectedItem ?? 3);
    }

    /*
     * Setters
     */

    /// <summary>
    /// Set the input categories
    /// </summary>
    /// 
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void setCategories(HoverableTextViewModel[]? values, int idx = 0) {
        if (values != null) {
            _categoryCB.Items = values;
        }

        _categoryCB.SelectedIndex = idx;
    }

    /// <summary>
    /// Set the input images, decided by the selected input category
    /// </summary>
    /// 
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void setInputImages(string[]? values, int idx = 0) {
        if (values != null) {
            _inputCB.Items = values;
        }

        _inputCB.SelectedIndex = idx;
    }

    /// <summary>
    /// Set the pattern sizes, decided by the selected input image
    /// </summary>
    /// 
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void setPatternSizes(int[]? values, int idx = 0) {
        if (values != null) {
            _patternSizeCB.Items = values;
        }

        _patternSizeCB.SelectedIndex = idx;
    }

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Callback for when the user changes the input category
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    private void catCBChangeHandler(object sender, SelectionChangedEventArgs e) {
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

    /// <summary>
    /// Callback for when the user changes the input image
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    public async void inImgCBChangeHandler(object? sender, SelectionChangedEventArgs? e) {
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
        await centralManager.getInputManager().restartSolution("Image CB Handler", true);

        centralManager!.getInputManager().resetMask();
        centralManager!.getPaintingWindow().setTemplates(Util.GetTemplates(centralManager.getMainWindowVM().InputImageSelection, centralManager.getWFCHandler().isOverlappingModel(), centralManager.getWFCHandler().getTileSize()));

        if (e != null) {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Callback for when the user changes the pattern size
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    private async void pattSizeCBChangeHandler(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        centralManager.getWFCHandler().setInputChanged("Pattern Size CB");
        e.Handled = true;
        if (centralManager.getMainWindowVM().SelectedTabIndex > 1) {
            await centralManager.getInputManager().restartSolution("Pattern CB Handler");
        }
    }
}