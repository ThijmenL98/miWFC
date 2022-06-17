using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.DeBroglie.Topo;
using miWFC.Managers;
using miWFC.Utils;
using ReactiveUI;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private string _categoryDescription = "",
        _selectedInputImage = "",
        _stepAmountString = "Steps to take: 1",
        _itemDescription = "Placeholder";

    private Bitmap _inputImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _outputImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _itemOverlay
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _outputImageMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _outputPreviewMask
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _currentItemImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _currentHeatMap
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
        _itemEditorEnabled,
        _isPaintOverrideEnabled,
        _pencilModeEnabled,
        _eraseModeEnabled,
        _paintKeepModeEnabled,
        _paintEraseModeEnabled,
        _isRunning,
        _inItemMenu,
        _itemsMayAppearAnywhere,
        _itemIsDependent,
        _itemsInRange,
        _hardBrushEnabled = true;

    private ObservableCollection<ItemViewModel> _itemDataGrid = new();

    private ObservableCollection<MarkerViewModel> _markers = new();

    private ObservableCollection<TileViewModel> _patternTiles = new(), _paintTiles = new(), _helperTiles = new();

    private HoverableTextViewModel _selectedCategory = new();

#pragma warning disable CS8618
    private ItemType _selectedItemToAdd, _selectedItemDependency;
#pragma warning restore CS8618

    private int _stepAmount = 1,
        _animSpeed = 100,
        _imgOutWidth,
        _imgOutHeight,
        _patternSize = 3,
        _selectedTabIndex,
        _itemsToAddValue = 1,
        _itemsToAddLower = 1,
        _depMaxDistance = 2,
        _depMinDistance = 1,
        _itemsToAddUpper = 2,
        _heatmapValue = 50,
        _editingEntry = -2;

    private double _timeStampOffset, _timelineWidth = 600d;

    private CentralManager? centralManager;
    private Tuple<string, string>? lastOverlapSelection, lastSimpleSelection;

    private Tuple<int, int>[,] latestItemGrid = new Tuple<int, int>[0, 0];

    private Random r = new();

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
            if (centralManager != null) {
                OverlappingAdvancedEnabledIW = OverlappingAdvancedEnabled &&
                                               !centralManager!.getMainWindow().getInputControl().getCategory()
                                                   .Contains("Side");
            }

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

    public string ItemDescription {
        get => _itemDescription;
        set => this.RaiseAndSetIfChanged(ref _itemDescription, value);
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

    public bool ItemEditorEnabled {
        get => _itemEditorEnabled;
        set => this.RaiseAndSetIfChanged(ref _itemEditorEnabled, value);
    }

    public bool IsPaintOverrideEnabled {
        get => _isPaintOverrideEnabled;
        set => this.RaiseAndSetIfChanged(ref _isPaintOverrideEnabled, value);
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

    public int ItemsToAddValue {
        get => _itemsToAddValue;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddValue, value);
    }

    public int ItemsToAddUpper {
        get => _itemsToAddUpper;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddUpper, value);
    }

    public int ItemsToAddLower {
        get => _itemsToAddLower;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddLower, value);
    }

    public int DepMaxDistance {
        get => _depMaxDistance;
        set => this.RaiseAndSetIfChanged(ref _depMaxDistance, value);
    }

    public int DepMinDistance {
        get => _depMinDistance;
        set => this.RaiseAndSetIfChanged(ref _depMinDistance, value);
    }

    public int HeatmapValue {
        get => _heatmapValue;
        set => this.RaiseAndSetIfChanged(ref _heatmapValue, value);
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

    public Bitmap ItemOverlay {
        get => _itemOverlay;
        set => this.RaiseAndSetIfChanged(ref _itemOverlay, value);
    }

    public Bitmap OutputImageMask {
        get => _outputImageMask;
        set => this.RaiseAndSetIfChanged(ref _outputImageMask, value);
    }

    public Bitmap OutputPreviewMask {
        get => _outputPreviewMask;
        set => this.RaiseAndSetIfChanged(ref _outputPreviewMask, value);
    }

    public Bitmap CurrentItemImage {
        get => _currentItemImage;
        set => this.RaiseAndSetIfChanged(ref _currentItemImage, value);
    }

    public Bitmap CurrentHeatmap {
        get => _currentHeatMap;
        set => this.RaiseAndSetIfChanged(ref _currentHeatMap, value);
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

    public bool MainInfoPopupVisible {
        get => _mainInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _mainInfoPopupVisible, value);
    }

    public bool PaintInfoPopupVisible {
        get => _paintInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _paintInfoPopupVisible, value);
    }

    public bool ItemsInfoPopupVisible {
        get => _itemsInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _itemsInfoPopupVisible, value);
    }

    public bool HeatmapInfoPopupVisible {
        get => _heatmapInfoPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _heatmapInfoPopupVisible, value);
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

    public bool InItemMenu {
        get => _inItemMenu;
        set => this.RaiseAndSetIfChanged(ref _inItemMenu, value);
    }

    public bool ItemsMayAppearAnywhere {
        get => _itemsMayAppearAnywhere;
        set => this.RaiseAndSetIfChanged(ref _itemsMayAppearAnywhere, value);
    }

    public bool ItemIsDependent {
        get => _itemIsDependent;
        set => this.RaiseAndSetIfChanged(ref _itemIsDependent, value);
    }

    public bool ItemsInRange {
        get => _itemsInRange;
        set => this.RaiseAndSetIfChanged(ref _itemsInRange, value);
    }

    public bool HardBrushEnabled {
        get => _hardBrushEnabled;
        set => this.RaiseAndSetIfChanged(ref _hardBrushEnabled, value);
    }

    private ItemType SelectedItemToAdd {
        get => _selectedItemToAdd;
        set => this.RaiseAndSetIfChanged(ref _selectedItemToAdd, value);
    }

    private ItemType SelectedItemDependency {
        get => _selectedItemDependency;
        set => this.RaiseAndSetIfChanged(ref _selectedItemDependency, value);
    }

    public ObservableCollection<ItemViewModel> ItemDataGrid {
        get => _itemDataGrid;
        set => this.RaiseAndSetIfChanged(ref _itemDataGrid, value);
    }

    /*
     * Logic
     */

    public void setR(Random newRand) {
        r = newRand;
    }

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

        centralManager!.getWFCHandler().resetWeights(false);
        centralManager!.getWFCHandler().updateWeights();

        centralManager!.getInputManager().restartSolution("Restart UI call");
    }

    public void OnWeightReset() {
        centralManager!.getWFCHandler().resetWeights(force: true);
    }

    public void OnMaskReset() {
        centralManager!.getInputManager().resetOverwriteCache();
        centralManager!.getInputManager().updateMask();
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

    public void OnImport() {
        centralManager!.getInputManager().importSolution();
    }

    public void OnExport() {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getInputManager().exportSolution();
    }

    public void OnInfoClick(string param) {
        if (IsPlaying) {
            OnAnimate();
        }

        centralManager!.getUIManager().showPopUp(param);
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

    public async Task OnApplyClick() {
        Color[,] mask = centralManager!.getInputManager().getMaskColours();
        if (!(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green)) {
            centralManager!.getUIManager().dispatchError(centralManager.getPaintingWindow());
            return;
        }

        setLoading(true);
        centralManager!.getMainWindowVM().StepAmount = 1;

        centralManager.getInputManager().resetOverwriteCache();
        centralManager.getInputManager().updateMask();
        await centralManager.getWFCHandler().handlePaintBrush(mask);
    }

    public void resetDataGrid() {
        ItemDataGrid = new ObservableCollection<ItemViewModel>();
    }

    public void OnItemMenuApply() {
        ItemType itemType = centralManager!.getItemWindow().getItemAddMenu().getSelectedItemType();
        int amountUpper;
        int amountLower;

        if (ItemsInRange) {
            amountUpper = ItemsToAddUpper;
            amountLower = ItemsToAddLower;
        } else {
            amountUpper = ItemsToAddValue;
            amountLower = ItemsToAddValue;
        }

        bool anywhere = ItemsMayAppearAnywhere;
        List<TileViewModel> allowedTiles = new();

        if (!anywhere) {
            IEnumerable<bool> allowedCheck = centralManager!.getItemWindow().getItemAddMenu().getAllowedTiles();
            foreach ((bool allowed, int idx) in allowedCheck.Select((b, i) => (b, i))) {
                if (allowed) {
                    allowedTiles.Add(PaintTiles[idx]);
                }
            }
        } else {
            allowedTiles = new List<TileViewModel>(PaintTiles);
        }

        if (allowedTiles.Count == 0) {
            centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
            return;
        }

        bool isDependent = ItemIsDependent;
        Tuple<ItemType?, WriteableBitmap?, (int, int)>? depItem = null;
        if (isDependent) {
            ItemType depItemType = centralManager!.getItemWindow().getItemAddMenu().getDependencyItemType();
            (int, int) range = (DepMinDistance, DepMaxDistance);
            WriteableBitmap depItemBitmap = centralManager!.getItemWindow().getItemAddMenu().getItemImage(depItemType);
            depItem = new Tuple<ItemType?, WriteableBitmap?, (int, int)>(depItemType, depItemBitmap, range);
        }

        if (_editingEntry != -2) {
            OnRemoveItemEntryI(_editingEntry);
        }

        ItemDataGrid.Add(new ItemViewModel(itemType, (amountLower, amountUpper),
            new ObservableCollection<TileViewModel>(allowedTiles),
            centralManager!.getItemWindow().getItemAddMenu().getItemImage(itemType), depItem));

        InItemMenu = false;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;
        ItemIsDependent = false;
        DepMinDistance = 1;
        _editingEntry = -2;
        DepMaxDistance = 2;

        foreach (TileViewModel tvm in PaintTiles) {
            tvm.ItemAddChecked = false;
        }

        centralManager!.getItemWindow().getItemAddMenu().updateCheckBoxesLength();
        ItemOverlay = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        OnApplyItemsClick();
    }

    public void OnExitItemAddition() {
        InItemMenu = false;
        _editingEntry = -2;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;
        ItemIsDependent = false;
        DepMinDistance = 1;
        DepMaxDistance = 2;

        foreach (TileViewModel tvm in PaintTiles) {
            tvm.ItemAddChecked = false;
        }

        centralManager!.getItemWindow().getItemAddMenu().updateCheckBoxesLength();
    }

    public async void OnCustomizeWindowSwitch(string param) {
        switch (param) {
            case "P":
                if (IsPlaying) {
                    OnAnimate();
                }

                await centralManager!.getUIManager().switchWindow(Windows.PAINTING);
                break;
            case "M":
                Color[,] mask = centralManager!.getInputManager().getMaskColours();
                await centralManager!.getUIManager().switchWindow(Windows.MAIN,
                    !(mask[0, 0] == Colors.Red || mask[0, 0] == Colors.Green));
                centralManager!.getInputManager().resetOverwriteCache();
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

    public void setLoading(bool value) {
        IsLoading = value || centralManager!.getWFCHandler().IsBrushing();
        centralManager?.getMainWindow().InvalidateVisual();
    }

    public void OnAddItemEntry() {
        centralManager!.getItemWindow().getItemAddMenu().updateSelectedItemIndex();
        centralManager!.getItemWindow().getItemAddMenu().updateDependencyIndex();
        centralManager!.getMainWindowVM().ItemDescription = ItemType.itemTypes[0].Description;
        InItemMenu = true;
    }

    public void OnEditItemEntry() {
        DataGrid dg = centralManager!.getItemWindow().getDataGrid();
        int selectedIndex = dg.SelectedIndex;
        if (selectedIndex.Equals(-1)) {
            centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
            return;
        }

        InItemMenu = true;
        ItemViewModel itemSelected = ItemDataGrid[selectedIndex];

        ItemsMayAppearAnywhere = itemSelected.AllowedTiles.Count.Equals(PaintTiles.Count);
        ItemsInRange = itemSelected.Amount.Item1 != itemSelected.Amount.Item2;

        ItemsToAddValue = itemSelected.Amount.Item1;
        ItemsToAddLower = itemSelected.Amount.Item1;
        ItemsToAddUpper = itemSelected.Amount.Item2;

        centralManager!.getItemWindow().getItemAddMenu().updateSelectedItemIndex(itemSelected.ItemType.ID);

        ItemIsDependent = itemSelected.HasDependentItem;
        if (ItemIsDependent) {
            DepMinDistance = itemSelected.DependentItem.Item2.Item1;
            DepMaxDistance = itemSelected.DependentItem.Item2.Item2;
            centralManager!.getItemWindow().getItemAddMenu()
                .updateDependencyIndex(itemSelected.DependentItem.Item1!.ID);
        } else {
            DepMinDistance = 1;
            DepMaxDistance = 2;
            centralManager!.getItemWindow().getItemAddMenu().updateDependencyIndex();
        }


        foreach ((TileViewModel tvm, int index) in PaintTiles.Select((model, i) => (model, i))) {
            if (itemSelected.AllowedTiles.Select(at => at.PatternIndex).Contains(index)) {
                tvm.ItemAddChecked = true;
                centralManager!.getItemWindow().getItemAddMenu().forwardCheckChange(index, true);
            }
        }

        _editingEntry = selectedIndex;
    }

    public void OnRemoveItemEntry() {
        OnRemoveItemEntryI(-2);
    }

    public void OnRemoveItemEntryI(int selectedIndex) {
        Trace.WriteLine("Trying to delete");
        DataGrid dg = centralManager!.getItemWindow().getDataGrid();
        selectedIndex = selectedIndex == -2 ? dg.SelectedIndex : selectedIndex;
        if (selectedIndex < 0) {
            centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
            return;
        }

        ItemDataGrid.RemoveAt(selectedIndex);
        dg.SelectedIndex = -1;
    }

    public void OnApplyItemsClick() {
        if (ItemDataGrid.Count < 1) {
            WriteableBitmap newItemOverlayEmpty
                = Util.generateItemOverlay(new Tuple<int, int>[0, 0], ImageOutWidth, ImageOutHeight);
            Util.setLatestItemBitMap(newItemOverlayEmpty);
            ItemOverlay = newItemOverlayEmpty;
            return;
        }

        Tuple<int, int>[,] itemGrid = new Tuple<int, int>[ImageOutWidth, ImageOutHeight];

        for (int x = 0; x < ImageOutWidth; x++) {
            for (int y = 0; y < ImageOutHeight; y++) {
                itemGrid[x, y] = new Tuple<int, int>(-1, -1);
            }
        }

        Color[,] distinctColourCount = { };
        int[,] distinctIndexCount = new int[0, 0];

        int[] spacesLeft = new int[PaintTiles.Count];
        if (centralManager!.getWFCHandler().isOverlappingModel()) {
            distinctColourCount = centralManager!.getWFCHandler().getPropagatorOutputO().toArray2d();
            int width = distinctColourCount.GetLength(0);
            int height = distinctColourCount.GetLength(1);
            List<Color> distinctList = new(width * height);
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    distinctList.Add(distinctColourCount[i, j]);
                }
            }

            foreach (TileViewModel tvm in PaintTiles) {
                spacesLeft[tvm.PatternIndex] = distinctList.Count(c => c.Equals(tvm.PatternColour));
            }
        } else {
            distinctIndexCount = centralManager!.getWFCHandler().getPropagatorOutputA().toArray2d();
            int width = distinctIndexCount.GetLength(0);
            int height = distinctIndexCount.GetLength(1);
            List<int> distinctList = new(width * height);
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    distinctList.Add(distinctIndexCount[i, j]);
                }
            }

            foreach (TileViewModel tvm in PaintTiles) {
                spacesLeft[tvm.PatternIndex] = distinctList.Count(c => c.Equals(tvm.PatternIndex));
            }
        }

        int depItemCount = 1;

        foreach (ItemViewModel ivm in ItemDataGrid) {
            List<int> allowedAdd = ivm.AllowedTiles.Select(tvm => tvm.PatternIndex).ToList();
            int toAdd = r.Next(ivm.Amount.Item1, ivm.Amount.Item2 + 1);

            for (int i = 0; i < toAdd; i++) {
                bool added = false;
                int retry = 0;
                while (!added) {
                    retry++;
                    if (retry == 50) {
                        centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
                        return;
                    }

                    if (allowedAdd.Select(x => spacesLeft[x]).Sum() == 0) {
                        centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
                        return;
                    }

                    int randLoc = r.Next(ImageOutWidth * ImageOutHeight);
                    int xLoc = randLoc / ImageOutWidth;
                    int yLoc = randLoc % ImageOutWidth;
                    bool allowedAtLoc = false;
                    if (centralManager!.getWFCHandler().isOverlappingModel()) {
                        Color colorAtPos = distinctColourCount[xLoc, yLoc];
                        if (PaintTiles.Where(tvm => tvm.PatternColour.Equals(colorAtPos))
                            .Any(tvm => allowedAdd.Contains(tvm.PatternIndex))) {
                            allowedAtLoc = true;
                        }
                    } else {
                        int valAtPos = distinctIndexCount[xLoc, yLoc];
                        allowedAtLoc = allowedAdd.Contains(valAtPos);
                    }

                    if (allowedAtLoc) {
                        if (itemGrid[xLoc, yLoc].Item1 == -1) {
                            bool canContinueDependent = !ivm.HasDependentItem;
                            if (ivm.HasDependentItem) {
                                Tuple<ItemType, (int, int)> depItem = ivm.DependentItem!;
                                List<Tuple<int, int>> possibleCoordinates = new();
                                (int minDist, int maxDist) = depItem.Item2;

                                for (int xx = Math.Max(0, xLoc - maxDist);
                                     xx < Math.Min(ImageOutWidth - 1, xLoc + maxDist);
                                     xx++) {
                                    for (int yy = Math.Max(0, yLoc - maxDist);
                                         yy < Math.Min(ImageOutHeight - 1, yLoc + maxDist);
                                         yy++) {
                                        double depDist
                                            = Math.Sqrt((xx - xLoc) * (xx - xLoc) + (yy - yLoc) * (yy - yLoc));
                                        if (depDist <= maxDist && depDist >= minDist) {
                                            if (centralManager!.getWFCHandler().isOverlappingModel()) {
                                                Color colorAtPos = distinctColourCount[xx, yy];
                                                if (PaintTiles.Where(tvm => tvm.PatternColour.Equals(colorAtPos))
                                                        .Any(tvm => allowedAdd.Contains(tvm.PatternIndex)) &&
                                                    itemGrid[xx, yy].Item1 == -1) {
                                                    possibleCoordinates.Add(new Tuple<int, int>(xx, yy));
                                                }
                                            } else {
                                                int valAtPos = distinctIndexCount[xx, yy];
                                                if (allowedAdd.Contains(valAtPos) && itemGrid[xx, yy].Item1 == -1) {
                                                    possibleCoordinates.Add(new Tuple<int, int>(xx, yy));
                                                }
                                            }
                                        }
                                    }
                                }

                                canContinueDependent = possibleCoordinates.Count > 0;

                                if (canContinueDependent) {
                                    Tuple<int, int> depCoords = possibleCoordinates[r.Next(possibleCoordinates.Count)];
                                    itemGrid[depCoords.Item1, depCoords.Item2]
                                        = new Tuple<int, int>(depItem.Item1!.ID, depItemCount);

                                    if (centralManager!.getWFCHandler().isOverlappingModel()) {
                                        Color addedLoc = distinctColourCount[depCoords.Item1, depCoords.Item2];
                                        foreach (TileViewModel tvm in PaintTiles) {
                                            if (tvm.PatternColour.Equals(addedLoc)) {
                                                int cIdx = tvm.PatternIndex;
                                                spacesLeft[cIdx]--;
                                            }

                                            break;
                                        }
                                    } else {
                                        spacesLeft[distinctIndexCount[depCoords.Item1, depCoords.Item2]]--;
                                    }
                                }
                            }

                            if (canContinueDependent) {
                                itemGrid[xLoc, yLoc] = new Tuple<int, int>(ivm.ItemType.ID,
                                    ivm.HasDependentItem ? depItemCount : -1);

                                if (ivm.HasDependentItem) {
                                    depItemCount++;
                                }

                                if (centralManager!.getWFCHandler().isOverlappingModel()) {
                                    Color addedLoc = distinctColourCount[xLoc, yLoc];
                                    foreach (TileViewModel tvm in PaintTiles) {
                                        if (tvm.PatternColour.Equals(addedLoc)) {
                                            int cIdx = tvm.PatternIndex;
                                            spacesLeft[cIdx]--;
                                        }

                                        break;
                                    }
                                } else {
                                    spacesLeft[distinctIndexCount[xLoc, yLoc]]--;
                                }

                                added = true;
                            }
                        }
                    }
                }
            }
        }

        latestItemGrid = itemGrid;
        WriteableBitmap newItemOverlay = Util.generateItemOverlay(itemGrid, ImageOutWidth, ImageOutHeight);
        Util.setLatestItemBitMap(newItemOverlay);
        ItemOverlay = newItemOverlay;
    }

    public void OnMappingReset() {
        centralManager!.getWeightMapWindow().resetCurrentMapping();
    }

    public Tuple<int, int>[,] getLatestItemGrid() {
        return latestItemGrid;
    }
}