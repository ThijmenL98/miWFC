using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels.Bridge;
using miWFC.ViewModels.Structs;
using ReactiveUI;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

namespace miWFC.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private string _categoryDescription = "",
        _selectedInputImage = "",
        _stepAmountString = "Steps to take: 1";

    private Bitmap _inputImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _outputImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _itemOverlay
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _outputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _outputPreviewMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    private bool _isPlaying,
        _seamlessOutput,
        _inputWrapping,
        _instantCollapse,
        _mainInfoPopupVisible,
        _itemsInfoPopupVisible,
        _paintInfoPopupVisible,
        _heatmapInfoPopupVisible,
        _isLoading,
        _advancedEnabled,
        _simpleModel,
        _advancedOverlapping,
        simpleAdvanced,
        _advancedOverlappingIW,
        _isRunning;

    private ObservableCollection<MarkerViewModel> _markers = new();

    private ObservableCollection<TileViewModel> _patternTiles = new(), _paintTiles = new(), _helperTiles = new();

    private HoverableTextViewModel _selectedCategory = new();

    private int _stepAmount = 1,
        _animSpeed = 100,
        _imgOutWidth,
        _imgOutHeight,
        _patternSize = 3,
        _selectedTabIndex;

    private double _timeStampOffset, _timelineWidth = 600d;

    private CentralManager? centralManager;
    private Tuple<string, string>? lastOverlapSelection, lastSimpleSelection;

    public Random R = new();

    private InputViewModel InputVM { get; }
    private OutputViewModel OutputVM { get; }
    public PaintingViewModel PaintingVM { get; }
    public MappingViewModel MappingVM { get; }
    public ItemViewModel ItemVM { get; }

    /*
     * Initializing Functions & Constructor
     */

    public MainWindowViewModel() {
        InputVM = new InputViewModel(this);
        OutputVM = new OutputViewModel(this);
        PaintingVM = new PaintingViewModel(this);
        MappingVM = new MappingViewModel(this);
        ItemVM = new ItemViewModel(this);
    }

    /*
     * Getters & Setters
     */

    // Strings

    /// <summary>
    /// String representation of the current category
    /// </summary>
    private string CategoryDescription {
        get => _categoryDescription;
        set => this.RaiseAndSetIfChanged(ref _categoryDescription, value);
    }

    /// <summary>
    /// String representation of the current input image
    /// </summary>
    public string InputImageSelection {
        get => _selectedInputImage;
        set => this.RaiseAndSetIfChanged(ref _selectedInputImage, value);
    }

    /// <summary>
    /// String representation of the current amount of steps to take
    /// </summary>
    public string StepAmountString {
        get => _stepAmountString;
        set => this.RaiseAndSetIfChanged(ref _stepAmountString, value);
    }

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    /// The amount of steps currently selected
    /// </summary>
    public int StepAmount {
        get => _stepAmount;
        set => this.RaiseAndSetIfChanged(ref _stepAmount, value);
    }

    /// <summary>
    /// The speed of the animation in MS
    /// </summary>
    public int AnimSpeed {
        get => _animSpeed;
        set => this.RaiseAndSetIfChanged(ref _animSpeed, value);
    }

    /// <summary>
    /// The width of the image in cells
    /// </summary>
    public int ImageOutWidth {
        get => _imgOutWidth;
        set => this.RaiseAndSetIfChanged(ref _imgOutWidth, Math.Min(Math.Max(10, value), 128));
    }

    /// <summary>
    /// The height of the image in cells
    /// </summary>
    public int ImageOutHeight {
        get => _imgOutHeight;
        set => this.RaiseAndSetIfChanged(ref _imgOutHeight, Math.Min(Math.Max(10, value), 128));
    }

    /// <summary>
    /// The size of the pattern in cells
    /// </summary>
    public int PatternSize {
        get => _patternSize;
        set => this.RaiseAndSetIfChanged(ref _patternSize, value);
    }

    /// <summary>
    /// The selected index of the tabs, currently 0 or 1 for advanced or simple
    /// </summary>
    public int SelectedTabIndex {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    /// <summary>
    /// The offset on the timeline in a percentage for the "now" marker.
    /// </summary>
    public double TimeStampOffset {
        get => _timeStampOffset;
        set => this.RaiseAndSetIfChanged(ref _timeStampOffset, value);
    }

    /// <summary>
    /// The width of the timeline
    /// </summary>
    public double TimelineWidth {
        get => _timelineWidth;
        set => this.RaiseAndSetIfChanged(ref _timelineWidth, value);
    }

    // Booleans

    /// <summary>
    /// Whether the info popup of the main window is visible
    /// </summary>
    public bool MainInfoPopupVisible {
        get => _mainInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _mainInfoPopupVisible, value);
    }

    /// <summary>
    /// Whether the info popup of the painting window is visible
    /// </summary>
    public bool PaintInfoPopupVisible {
        get => _paintInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _paintInfoPopupVisible, value);
    }

    /// <summary>
    /// Whether the info popup of the items window is visible
    /// </summary>
    public bool ItemsInfoPopupVisible {
        get => _itemsInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _itemsInfoPopupVisible, value);
    }

    /// <summary>
    /// Whether the info popup of the heat mapping window is visible
    /// </summary>
    public bool HeatmapInfoPopupVisible {
        get => _heatmapInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _heatmapInfoPopupVisible, value);
    }

    /// <summary>
    /// Whether the application is loading
    /// </summary>
    public bool IsLoading {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    /// <summary>
    /// Whether the advanced mode of the application is enabled
    /// </summary>
    private bool AdvancedEnabled {
        get => _advancedEnabled;
        set {
            this.RaiseAndSetIfChanged(ref _advancedEnabled, value);
            OverlappingAdvancedEnabled = AdvancedEnabled && !SimpleModelSelected;
            SimpleAdvancedEnabled = AdvancedEnabled && SimpleModelSelected;
            OverlappingAdvancedEnabledIW = OverlappingAdvancedEnabled &&
                                           !centralManager!.getMainWindow().getInputControl().getCategory()
                                               .Contains("Side");
        }
    }

    /// <summary>
    /// Whether the advanced mode of the application is enabled AND the overlapping mode is selected
    /// </summary>
    private bool OverlappingAdvancedEnabled {
        get => _advancedOverlapping;
        set => this.RaiseAndSetIfChanged(ref _advancedOverlapping, value);
    }

    /// <summary>
    /// Whether the advanced mode of the application is enabled AND the adjacent mode is selected
    /// </summary>
    private bool SimpleAdvancedEnabled {
        get => simpleAdvanced;
        set => this.RaiseAndSetIfChanged(ref simpleAdvanced, value);
    }

    /// <summary>
    /// Whether the advanced mode of the application is enabled AND the overlapping mode is selected AND the input
    /// category is not Side-View
    /// </summary>
    private bool OverlappingAdvancedEnabledIW {
        get => _advancedOverlappingIW;
        set => this.RaiseAndSetIfChanged(ref _advancedOverlappingIW, value);
    }

    /// <summary>
    /// Whether the application is currently in progress
    /// </summary>
    public bool IsRunning {
        get => _isRunning;
        set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }

    /// <summary>
    /// Whether the simple/adjacent model is selected
    /// </summary>
    public bool SimpleModelSelected {
        get => _simpleModel;
        private set {
            this.RaiseAndSetIfChanged(ref _simpleModel, value);
            OverlappingAdvancedEnabled = AdvancedEnabled && !SimpleModelSelected;
            SimpleAdvancedEnabled = AdvancedEnabled && SimpleModelSelected;
            OverlappingAdvancedEnabledIW = OverlappingAdvancedEnabled &&
                                           !centralManager!.getMainWindow().getInputControl().getCategory()
                                               .Contains("Side");
        }
    }

    /// <summary>
    /// Whether the animation is playing
    /// </summary>
    public bool IsPlaying {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    /// <summary>
    /// Whether seamless output is enabled
    /// </summary>
    public bool SeamlessOutput {
        get => _seamlessOutput;
        set => this.RaiseAndSetIfChanged(ref _seamlessOutput, value);
    }

    /// <summary>
    /// Whether input wrapping is enabled
    /// </summary>
    public bool InputWrapping {
        get => _inputWrapping;
        set => this.RaiseAndSetIfChanged(ref _inputWrapping, value);
    }

    /// <summary>
    /// Whether the user has set the output to instantly collapse (or inversely with steps)
    /// </summary>
    public bool InstantCollapse {
        get => _instantCollapse;
        set => this.RaiseAndSetIfChanged(ref _instantCollapse, value);
    }

    // Images

    /// <summary>
    /// The input image
    /// </summary>
    public Bitmap InputImage {
        get => _inputImage;
        set => this.RaiseAndSetIfChanged(ref _inputImage, value);
    }

    /// <summary>
    /// The currently (being) generated output image
    /// </summary>
    public Bitmap OutputImage {
        get => _outputImage;
        set => this.RaiseAndSetIfChanged(ref _outputImage, value);
    }

    /// <summary>
    /// The items as an overlay for the output image
    /// </summary>
    public Bitmap ItemOverlay {
        get => _itemOverlay;
        set => this.RaiseAndSetIfChanged(ref _itemOverlay, value);
    }

    /// <summary>
    /// The mask to put over the output image for the painting mode when brushing or creating templates
    /// </summary>
    public Bitmap OutputImageMask {
        get => _outputImageMask;
        set => this.RaiseAndSetIfChanged(ref _outputImageMask, value);
    }

    /// <summary>
    /// The mask to put over the output image when hovering
    /// </summary>
    public Bitmap OutputPreviewMask {
        get => _outputPreviewMask;
        set => this.RaiseAndSetIfChanged(ref _outputPreviewMask, value);
    }

    // Objects

    /// <summary>
    /// The currently selected category as a hoverable text object
    /// </summary>
    private HoverableTextViewModel CategorySelection {
        get => _selectedCategory;
        set {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            if (centralManager != null) {
                OverlappingAdvancedEnabledIW = OverlappingAdvancedEnabled &&
                                               !centralManager!.getMainWindow().getInputControl().getCategory()
                                                   .Contains("Side");
            }

            CategoryDescription = Util.getDescription(CategorySelection.DisplayText);
        }
    }

    /// <summary>
    /// Me :)
    /// </summary>
    public MainWindowViewModel VM => this;

    // Lists

    /// <summary>
    /// List of the unique patterns/tiles extracted from the input image, basically PaintTiles without transformations
    /// </summary>
    public ObservableCollection<TileViewModel> PatternTiles {
        get => _patternTiles;
        set => this.RaiseAndSetIfChanged(ref _patternTiles, value);
    }

    /// <summary>
    /// List with all patterns/tiles available at the hovered cell in the painting window
    /// </summary>
    public ObservableCollection<TileViewModel> HelperTiles {
        get => _helperTiles;
        set => this.RaiseAndSetIfChanged(ref _helperTiles, value);
    }

    /// <summary>
    /// List with all the tiles allowed for the user to paint with
    /// </summary>
    public ObservableCollection<TileViewModel> PaintTiles {
        get => _paintTiles;
        set => this.RaiseAndSetIfChanged(ref _paintTiles, value);
    }

    /// <summary>
    /// List of all markers set by the user
    /// </summary>
    public ObservableCollection<MarkerViewModel> Markers {
        get => _markers;
        set => this.RaiseAndSetIfChanged(ref _markers, value);
    }

    // Other

    /*
     * UI Callbacks
     */

    /*
     * Logic
     */

    /// <summary>
    /// Set my randomness function
    /// </summary>
    /// 
    /// <param name="newRand">The Randomness function</param>
    public void setR(Random newRand) {
        R = newRand;
    }

    public void setCentralManager(CentralManager cm) {
        lastOverlapSelection = new Tuple<string, string>("Textures", "3Bricks");
        lastSimpleSelection = new Tuple<string, string>("Worlds Top-Down", "Castle");
        centralManager = cm;

        InputVM.setCentralManager(cm);
        OutputVM.setCentralManager(cm);
        PaintingVM.setCentralManager(cm);
        MappingVM.setCentralManager(cm);
        ItemVM.setCentralManager(cm);

        ImageOutWidth = 24;
        ImageOutHeight = 24;
    }

    /// <summary>
    /// Function called when switching models
    /// </summary>
    /// 
    /// <param name="newTab">Tab index, currently eiter 0 or 1</param>
    public void OnModelClick(int newTab) {
        centralManager!.getWFCHandler().setModelChanging(true);
        SimpleModelSelected = !SimpleModelSelected;
        OverlappingAdvancedEnabledIW = OverlappingAdvancedEnabled &&
                                       !centralManager!.getMainWindow().getInputControl().getCategory()
                                           .Contains("Side");

        if (IsPlaying) {
            OutputVM.OnAnimate();
        }

        bool changingToSmart = newTab is 0 or 2;

        string lastCat = CategorySelection.DisplayText;

        string[] catDataSource = Util.getCategories(changingToSmart ? "overlapping" : "simpletiled");

        string lastImg = InputImageSelection;
        int catIndex = changingToSmart
            ? Array.IndexOf(catDataSource, lastOverlapSelection!.Item1)
            : Array.IndexOf(catDataSource, lastSimpleSelection!.Item1);
        centralManager.getUIManager().updateCategories(catDataSource, catIndex);

        string[] images = Util.getModelImages(
            changingToSmart ? "overlapping" : "simpletiled",
            changingToSmart ? lastOverlapSelection!.Item1 : lastSimpleSelection!.Item1);

        int index = changingToSmart
            ? Array.IndexOf(images, lastOverlapSelection!.Item2)
            : Array.IndexOf(images, lastSimpleSelection!.Item2);
        centralManager.getUIManager().updateInputImages(images, index);
        (int[] patternSizeDataSource, int i) = Util.getImagePatternDimensions(images[index]);
        centralManager.getUIManager().updatePatternSizes(patternSizeDataSource, i);

        if (changingToSmart) {
            lastSimpleSelection = new Tuple<string, string>(lastCat, lastImg);
        } else {
            lastOverlapSelection = new Tuple<string, string>(lastCat, lastImg);
        }

        centralManager.getWFCHandler().setModelChanging(false);
        centralManager.getWFCHandler().setInputChanged("Model change");
        centralManager.getMainWindow().getInputControl().inImgCBChangeHandler(null, null);
    }

    /// <summary>
    /// Function called to reset all weights to their default values
    /// </summary>
    public void OnWeightReset() {
        centralManager!.getWFCHandler().resetWeights(force: true);
    }

    /// <summary>
    /// Function called when clicking on the info button in any window
    /// </summary>
    /// 
    /// <param name="param">The window the info button was clicked on</param>
    public void OnInfoClick(string param) {
        if (IsPlaying) {
            OutputVM.OnAnimate();
        }

        centralManager!.getUIManager().showPopUp(param);
    }

    /// <summary>
    /// Function called to switch between the different windows of the application
    /// </summary>
    /// 
    /// <param name="param">Window to switch to</param>
    /// 
    /// <exception cref="NotImplementedException">If an non-existing window is asked to be opened</exception>
    public async void OnCustomizeWindowSwitch(string param) {
        switch (param) {
            case "P":
                if (IsPlaying) {
                    OutputVM.OnAnimate();
                }

                await centralManager!.getUIManager().switchWindow(Windows.PAINTING);
                break;
            case "M":
                Color[,] mask = centralManager!.getInputManager().getMaskColours();
                await centralManager!.getUIManager().switchWindow(Windows.MAIN,
                    !(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green));
                centralManager!.getInputManager().resetMask();
                break;
            case "I":
                if (!centralManager!.getWFCHandler().isCollapsed()) {
                    centralManager.getUIManager().dispatchError(centralManager.getMainWindow());
                }

                await centralManager!.getUIManager().switchWindow(Windows.ITEMS);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Function called to toggle the loading animation of the application
    /// </summary>
    /// 
    /// <param name="value">Toggle value</param>
    public void setLoading(bool value) {
        IsLoading = value || centralManager!.getWFCHandler().isBrushing();
        centralManager?.getMainWindow().InvalidateVisual();
    }

    /// <summary>
    /// Function called when closing the popup on any window
    /// </summary>
    public void OnCloseClick() {
        centralManager!.getUIManager().hidePopUp();
    }

    /// <summary>
    /// Update the weights based on the user input
    /// </summary>
    /// 
    /// <param name="weights">New weights to use</param>
    public void setWeights(double[] weights) {
        for (int i = 0; i < PaintTiles.Count; i++) {
            PaintTiles[i].PatternWeight = weights[i];
        }
    }
}