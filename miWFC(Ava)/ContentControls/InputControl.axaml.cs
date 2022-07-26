using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using miWFC.Delegators;
using miWFC.Utils;
using miWFC.ViewModels.Structs;

// ReSharper disable UnusedParameter.Local
// ReSharper disable SuggestBaseTypeForParameter

namespace miWFC.ContentControls;

/// <summary>
///     Separated control for the input side of the application
/// </summary>
public partial class InputControl : UserControl {
    private readonly ComboBox _categoryCB, _inputCB, _patternSizeCB;
    private CentralDelegator? centralDelegator;

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

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
        InImgCBChangeHandler(null, null);
    }

    /*
     * Getters
     */

    /// <summary>
    ///     Get the currently selected input category as string
    /// </summary>
    /// <returns>Input Category String, default "Textures"</returns>
    public string GetCategory() {
        return (_categoryCB.SelectedItem as HoverableTextViewModel)?.DisplayText ?? "Textures";
    }

    /// <summary>
    ///     Get the currently selected input image as string
    /// </summary>
    /// <returns>Input Image String, default "3Bricks"</returns>
    public string GetInputImage() {
        return _inputCB.SelectedItem as string ?? "3Bricks";
    }

    /// <summary>
    ///     Get the currently selected pattern size
    /// </summary>
    /// <returns>Pattern Size, default 3</returns>
    public int GetPatternSize() {
        return (int) (_patternSizeCB.SelectedItem ?? 3);
    }

    /*
     * Setters
     */

    /// <summary>
    ///     Set the input categories
    /// </summary>
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void SetCategories(HoverableTextViewModel[]? values, int idx = 0) {
        if (values != null) {
            _categoryCB.Items = values;
        }

        _categoryCB.SelectedIndex = idx;
    }

    /// <summary>
    ///     Set the input images, decided by the selected input category
    /// </summary>
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void SetInputImages(string[]? values, int idx = 0) {
        if (values != null) {
            _inputCB.Items = values;
        }

        _inputCB.SelectedIndex = idx;
    }

    /// <summary>
    ///     Set the pattern sizes, decided by the selected input image
    /// </summary>
    /// <param name="idx">Index</param>
    /// <param name="values">New Combo Box Values</param>
    public void SetPatternSizes(int[]? values, int idx = 0) {
        if (values != null) {
            _patternSizeCB.Items = values;
        }

        _patternSizeCB.SelectedIndex = idx;
    }

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Callback for when the user changes the input category
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    private void CatCBChangeHandler(object sender, SelectionChangedEventArgs e) {
        if (centralDelegator == null || centralDelegator.GetWFCHandler().IsChangingModels()) {
            return;
        }

        string newValue = GetCategory();
        bool isOverlapping = !centralDelegator!.GetMainWindowVM().SimpleModelSelected;

        string[] inputImageDataSource
            = Util.GetModelImages(isOverlapping ? "overlapping" : "simpletiled", newValue);
        SetInputImages(inputImageDataSource);

        centralDelegator.GetMainWindowVM().CustomInputSelected = newValue.Equals("Custom");
        centralDelegator.GetMainWindowVM().InputImageMinWidth = newValue.Equals("Custom") ? 130 : 200;

        e.Handled = true;
    }

    /// <summary>
    ///     Callback for when the user changes the input image
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    public async void InImgCBChangeHandler(object? sender, SelectionChangedEventArgs? e) {
        if (centralDelegator == null || centralDelegator.GetWFCHandler().IsChangingModels()) {
            return;
        }

        centralDelegator.GetWFCHandler().SetImageChanging(true);

        string newValue = GetInputImage();
        if (!centralDelegator.GetInterfaceHandler().UpdateInputImage(newValue)) {
            return;
        }

        centralDelegator.GetWFCHandler().SetInputChanged("Image CB");

        if (!centralDelegator.GetMainWindowVM().SimpleModelSelected) {
            (int[] patternSizeDataSource, int i) = Util.GetImagePatternDimensions(newValue);
            SetPatternSizes(patternSizeDataSource, i);
        }

        centralDelegator.GetWFCHandler().SetImageChanging(false);
        await centralDelegator.GetOutputHandler().RestartSolution("Image CB Handler", true);

        centralDelegator!.GetOutputHandler().ResetMask();
        centralDelegator!.GetPaintingWindow().SetTemplates(Util.GetTemplates(
            centralDelegator.GetMainWindowVM().InputImageSelection, centralDelegator.GetWFCHandler().IsOverlappingModel(),
            centralDelegator.GetWFCHandler().GetTileSize()));

        if (e != null) {
            e.Handled = true;
        }
    }

    /// <summary>
    ///     Callback for when the user changes the pattern size
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
    private async void PattSizeCBChangeHandler(object? sender, SelectionChangedEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        centralDelegator.GetWFCHandler().SetInputChanged("Pattern Size CB");
        e.Handled = true;
        await centralDelegator.GetOutputHandler().RestartSolution("Pattern CB Handler");
    }
}