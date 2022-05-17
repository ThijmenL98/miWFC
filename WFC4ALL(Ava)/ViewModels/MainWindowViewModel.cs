using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using WFC4ALL.Managers;
using WFC4ALL.Utils;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace WFC4ALL.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private Bitmap _inputImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul),
        _outputImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul),
        _outputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul),
        _outputPreviewMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul);

    private bool _isPlaying,
        _seamlessOutput,
        _inputWrapping,
        _instantCollapse,
        _popupVisible,
        _isLoading,
        _advancedEnabled,
        _simpleModel,
        _advancedOverlapping,
        simpleAdvanced,
        _advancedOverlappingIW;

    private ObservableCollection<MarkerViewModel> _markers = new();

    private ObservableCollection<TileViewModel> _patternTiles = new(), _paintTiles = new(), _helperTiles = new();

    private bool _pencilModeEnabled, _eraseModeEnabled, _paintKeepModeEnabled, _paintEraseModeEnabled, _isRunning;

    private HoverableTextViewModel _selectedCategory = new();

    private string _categoryDescription = "",
        _selectedInputImage = "",
        _stepAmountString = "Steps to take: 1";

    private int _stepAmount = 1, _animSpeed = 100, _imgOutWidth, _imgOutHeight, _patternSize = 3, _selectedTabIndex;
    private double _timeStampOffset, _timelineWidth = 600d;

    private CentralManager? centralManager;
    private Tuple<string, string>? lastOverlapSelection, lastSimpleSelection;

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

    private HoverableTextViewModel CategorySelection {
        get => _selectedCategory;
        // ReSharper disable once UnusedMember.Local
        set {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            OverlappingAdvancedEnabledIW = OverlappingAdvancedEnabled &&
                                           !centralManager!.getMainWindow().getInputControl().getCategory()
                                               .Contains("Side");
            CategoryDescription = Util.getDescription(CategorySelection.DisplayText);
        }
    }

    private string CategoryDescription {
        get => _categoryDescription;
        set => this.RaiseAndSetIfChanged(ref _categoryDescription, value);
    }

    private string InputImageSelection {
        get => _selectedInputImage;
        // ReSharper disable once UnusedMember.Local
        set => this.RaiseAndSetIfChanged(ref _selectedInputImage, value);
    }

    public string StepAmountString {
        get => _stepAmountString;
        set => this.RaiseAndSetIfChanged(ref _stepAmountString, value);
    }

    public bool IsPlaying {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    public bool SeamlessOutput {
        get => _seamlessOutput;
        private set => this.RaiseAndSetIfChanged(ref _seamlessOutput, value);
    }

    public bool InputWrapping {
        get => _inputWrapping;
        private set => this.RaiseAndSetIfChanged(ref _inputWrapping, value);
    }

    public bool InstantCollapse {
        get => _instantCollapse;
        set => this.RaiseAndSetIfChanged(ref _instantCollapse, value);
    }

    public int StepAmount {
        get => _stepAmount;
        set => this.RaiseAndSetIfChanged(ref _stepAmount, value);
    }

    public int AnimSpeed {
        get => _animSpeed;
        set => this.RaiseAndSetIfChanged(ref _animSpeed, value);
    }

    public int ImageOutWidth {
        get => _imgOutWidth;
        set => this.RaiseAndSetIfChanged(ref _imgOutWidth, Math.Min(Math.Max(10, value), 128));
    }

    public int ImageOutHeight {
        get => _imgOutHeight;
        set => this.RaiseAndSetIfChanged(ref _imgOutHeight, Math.Min(Math.Max(10, value), 128));
    }

    public int PatternSize {
        get => _patternSize;
        set => this.RaiseAndSetIfChanged(ref _patternSize, value);
    }

    public int SelectedTabIndex {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    public double TimeStampOffset {
        get => _timeStampOffset;
        set => this.RaiseAndSetIfChanged(ref _timeStampOffset, value);
    }

    public double TimelineWidth {
        get => _timelineWidth;
        set => this.RaiseAndSetIfChanged(ref _timelineWidth, value);
    }

    public Bitmap InputImage {
        get => _inputImage;
        set => this.RaiseAndSetIfChanged(ref _inputImage, value);
    }

    public Bitmap OutputImage {
        get => _outputImage;
        set => this.RaiseAndSetIfChanged(ref _outputImage, value);
    }

    public Bitmap OutputImageMask {
        get => _outputImageMask;
        set => this.RaiseAndSetIfChanged(ref _outputImageMask, value);
    }

    public Bitmap OutputPreviewMask {
        get => _outputPreviewMask;
        set => this.RaiseAndSetIfChanged(ref _outputPreviewMask, value);
    }

    public ObservableCollection<TileViewModel> PatternTiles {
        get => _patternTiles;
        set => this.RaiseAndSetIfChanged(ref _patternTiles, value);
    }

    public ObservableCollection<TileViewModel> HelperTiles {
        get => _helperTiles;
        set => this.RaiseAndSetIfChanged(ref _helperTiles, value);
    }

    public ObservableCollection<TileViewModel> PaintTiles {
        get => _paintTiles;
        set => this.RaiseAndSetIfChanged(ref _paintTiles, value);
    }

    public ObservableCollection<MarkerViewModel> Markers {
        get => _markers;
        set => this.RaiseAndSetIfChanged(ref _markers, value);
    }

    public MainWindowViewModel VM => this;

    public bool PopupVisible {
        get => _popupVisible;
        set => this.RaiseAndSetIfChanged(ref _popupVisible, value);
    }

    public bool PencilModeEnabled {
        get => _pencilModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _pencilModeEnabled, value);
    }

    public bool EraseModeEnabled {
        get => _eraseModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _eraseModeEnabled, value);
    }

    public bool PaintKeepModeEnabled {
        get => _paintKeepModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _paintKeepModeEnabled, value);
    }

    public bool PaintEraseModeEnabled {
        get => _paintEraseModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _paintEraseModeEnabled, value);
    }

    public bool IsLoading {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

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

    private bool OverlappingAdvancedEnabled {
        get => _advancedOverlapping;
        set => this.RaiseAndSetIfChanged(ref _advancedOverlapping, value);
    }

    private bool SimpleAdvancedEnabled {
        get => simpleAdvanced;
        set => this.RaiseAndSetIfChanged(ref simpleAdvanced, value);
    }

    private bool OverlappingAdvancedEnabledIW {
        get => _advancedOverlappingIW;
        set => this.RaiseAndSetIfChanged(ref _advancedOverlappingIW, value);
    }

    public bool IsRunning {
        get => _isRunning;
        set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }

    /*
     * Logic
     */

    public void setCentralManager(CentralManager cm) {
        lastOverlapSelection = new Tuple<string, string>("Textures", "3Bricks");
        lastSimpleSelection = new Tuple<string, string>("Worlds Top-Down", "Castle");
        centralManager = cm;

        ImageOutWidth = 24;
        ImageOutHeight = 24;
    }

    public void OnModelClick(int newTab) {
        centralManager!.getWFCHandler().setModelChanging(true);
        SimpleModelSelected = !SimpleModelSelected;
        OverlappingAdvancedEnabledIW = OverlappingAdvancedEnabled &&
                                       !centralManager!.getMainWindow().getInputControl().getCategory()
                                           .Contains("Side");

        if (IsPlaying) {
            OnAnimate();
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

    public void OnPaddingToggle() {
        if (IsPlaying) {
            OnAnimate();
        }

        SeamlessOutput = !SeamlessOutput;
        centralManager!.getInputManager().restartSolution("Padding Toggle Change");
    }

    public void OnInputWrappingChanged() {
        InputWrapping = !InputWrapping;
        centralManager!.getWFCHandler().setInputChanged("Input Wrapping Change");
        centralManager!.getInputManager().restartSolution("Input Wrapping Change");
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void OnAnimate() {
        IsPlaying = !IsPlaying;
        centralManager!.getInputManager().animate();
    }

    public void OnRestart() {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getWFCHandler().updateWeights();

        centralManager!.getInputManager().restartSolution("Restart UI call");
    }

    public void OnWeightReset() {
        centralManager!.getWFCHandler().resetWeights();
    }

    public void OnRevert() {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getInputManager().revertStep();
    }

    public void OnAdvance() {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getInputManager().advanceStep();
    }

    public void OnSave() {
        centralManager!.getInputManager().placeMarker();
    }

    public void OnLoad() {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getInputManager().loadMarker();
    }

    public void OnExport() {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getInputManager().exportSolution();
    }

    public void OnInfoClick() {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getUIManager().showPopUp();
    }

    public void OnCloseClick() {
        centralManager!.getUIManager().hidePopUp();
    }

    public void OnPencilModeClick() {
        PencilModeEnabled = !PencilModeEnabled;
        EraseModeEnabled = false;
        PaintKeepModeEnabled = false;
        PaintEraseModeEnabled = false;
        centralManager!.getInputManager().resetOverwriteCache();
        centralManager!.getInputManager().updateMask();
    }

    public void OnEraseModeClick() {
        EraseModeEnabled = !EraseModeEnabled;
        PencilModeEnabled = false;
        PaintKeepModeEnabled = false;
        PaintEraseModeEnabled = false;
        centralManager!.getInputManager().resetOverwriteCache();
        centralManager!.getInputManager().updateMask();
    }

    public void OnPaintKeepModeClick() {
        PaintKeepModeEnabled = !PaintKeepModeEnabled;
        EraseModeEnabled = false;
        PencilModeEnabled = false;
        PaintEraseModeEnabled = false;
    }

    public void OnPaintEraseModeClick() {
        PaintEraseModeEnabled = !PaintEraseModeEnabled;
        EraseModeEnabled = false;
        PencilModeEnabled = false;
        PaintKeepModeEnabled = false;
    }

    public void OnApplyClick() {
        Color[,] mask = centralManager!.getInputManager().getMaskColours();
        if (!(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green)) {
            centralManager!.getUIManager().dispatchError(centralManager.getPaintingWindow());
            return;
        }
        setLoading(true);
        centralManager!.getMainWindowVM().StepAmount = 1;

        centralManager.getInputManager().resetOverwriteCache();
        centralManager.getInputManager().updateMask();
        centralManager.getWFCHandler().handlePaintBrush(mask);
    }

    public void OnCustomizeWindowSwitch(string param) {
        switch (param) {
            case "P":
                if (IsPlaying) {
                    OnAnimate();
                }

                centralManager!.getUIManager().switchWindow(Windows.PAINTING, false);
                break;
            case "M":
                Color[,] mask = centralManager!.getInputManager().getMaskColours();
                centralManager!.getInputManager().resetOverwriteCache();
                centralManager!.getUIManager().switchWindow(Windows.MAIN, !(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green));
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void setLoading(bool value) {
        IsLoading = value;
        centralManager?.getMainWindow().InvalidateVisual();
    }
}