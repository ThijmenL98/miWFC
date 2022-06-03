using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.Managers;
using WFC4ALL.Utils;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace WFC4ALL.ViewModels;

public class MainWindowViewModel : ViewModelBase {
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
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    private bool _isPlaying,
        _seamlessOutput,
        _inputWrapping,
        _instantCollapse,
        _mainInfoPopupVisible,
        _isLoading,
        _advancedEnabled,
        _simpleModel,
        _advancedOverlapping,
        simpleAdvanced,
        _advancedOverlappingIW,
        _itemEditorEnabled,
        _pencilModeEnabled,
        _eraseModeEnabled,
        _paintKeepModeEnabled,
        _paintEraseModeEnabled,
        _isRunning,
        _inItemMenu,
        _itemsMayAppearAnywhere,
        _isEditing;

    private ObservableCollection<MarkerViewModel> _markers = new();

    private ObservableCollection<TileViewModel> _patternTiles = new(), _paintTiles = new(), _helperTiles = new();

    private ObservableCollection<ItemViewModel> _itemDataGrid = new();

    private HoverableTextViewModel _selectedCategory = new();

    private Random r = new();

    private int[,] latestItemGrid = {{-1}};

    private string _categoryDescription = "",
        _selectedInputImage = "",
        _stepAmountString = "Steps to take: 1",
        _itemDescription = "Placeholder";

    private int _stepAmount = 1,
        _animSpeed = 100,
        _imgOutWidth,
        _imgOutHeight,
        _patternSize = 3,
        _selectedTabIndex,
        _itemsToAddValue = 1;

    private double _timeStampOffset, _timelineWidth = 600d;

    private CentralManager? centralManager;
    private Tuple<string, string>? lastOverlapSelection, lastSimpleSelection;

#pragma warning disable CS8618
    private ItemType _selectedItemToAdd;
#pragma warning restore CS8618

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

    private ItemType SelectedItemToAdd {
        get => _selectedItemToAdd;
        set => this.RaiseAndSetIfChanged(ref _selectedItemToAdd, value);
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

        centralManager!.getWFCHandler().updateWeights();

        centralManager!.getInputManager().restartSolution("Restart UI call");
    }

    public void OnWeightReset() {
        centralManager!.getWFCHandler().resetWeights();
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
        ItemType itemType = centralManager!.getItemWindow().getItemAddMenu().getItemType();
        int amount = ItemsToAddValue;
        bool anywhere = ItemsMayAppearAnywhere;
        List<TileViewModel> allowedTiles = new();

        if (!anywhere) {
            bool[] allowedCheck = centralManager!.getItemWindow().getItemAddMenu().getAllowedTiles();
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

        foreach (ItemViewModel ivm in ItemDataGrid) {
            List<int> mySet = new(allowedTiles.Select(model => model.PatternIndex));
            List<int> compared = new(ivm.AllowedTiles.Select(model => model.PatternIndex));
            if (ivm.ItemType.ID.Equals(itemType.ID) && mySet.All(compared.Contains)
                                                    && mySet.Count.Equals(compared.Count)) {
                if (_isEditing) {
                    ivm.Amount = amount;
                } else {
                    ivm.Amount += amount;
                }

                InItemMenu = false;
                return;
            }
        }

        ItemDataGrid.Add(new ItemViewModel(itemType, amount, new ObservableCollection<TileViewModel>(allowedTiles),
            centralManager!.getItemWindow().getItemAddMenu().getItemImage(itemType)));

        InItemMenu = false;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;

        foreach (TileViewModel tvm in PaintTiles) {
            tvm.ItemAddChecked = false;
        }

        centralManager!.getItemWindow().getItemAddMenu().updateCheckBoxesLength();
        ItemOverlay = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
    }

    public void OnExitItemAddition() {
        InItemMenu = false;
        _isEditing = false;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;

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
        centralManager!.getItemWindow().getItemAddMenu().updateIndex();
        centralManager!.getMainWindowVM().ItemDescription = ItemType.ItemTypes[0].Description;
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
        ItemsToAddValue = itemSelected.Amount;
        centralManager!.getItemWindow().getItemAddMenu().updateIndex(itemSelected.ItemType.ID);

        foreach ((TileViewModel tvm, int index) in PaintTiles.Select((model, i) => (model, i))) {
            if (itemSelected.AllowedTiles.Select(at => at.PatternIndex).Contains(index)) {
                tvm.ItemAddChecked = true;
                centralManager!.getItemWindow().getItemAddMenu().forwardCheckChange(index, true);
            }
        }

        _isEditing = true;
    }

    public void OnRemoveItemEntry() {
        DataGrid dg = centralManager!.getItemWindow().getDataGrid();
        int selectedIndex = dg.SelectedIndex;
        if (selectedIndex.Equals(-1)) {
            centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
            return;
        }

        ItemDataGrid.RemoveAt(selectedIndex);
        dg.SelectedIndex = -1;
    }

    public void OnApplyItemsClick() {
        if (ItemDataGrid.Count < 1) {
            centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
            return;
        }

        int[,] itemGrid = new int[ImageOutWidth, ImageOutHeight];

        for (int x = 0; x < ImageOutWidth; x++) {
            for (int y = 0; y < ImageOutHeight; y++) {
                itemGrid[x, y] = -1;
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

        foreach (ItemViewModel ivm in ItemDataGrid) {
            List<int> allowedAdd = ivm.AllowedTiles.Select(tvm => tvm.PatternIndex).ToList();
            for (int i = 0; i < ivm.Amount; i++) {
                bool added = false;
                while (!added) {
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
                        if (itemGrid[xLoc, yLoc] == -1) {
                            itemGrid[xLoc, yLoc] = ivm.ItemType.ID;
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

        latestItemGrid = itemGrid;
        WriteableBitmap newItemOverlay = Util.generateItemOverlay(itemGrid, ImageOutWidth, ImageOutHeight);
        Util.setLatestItemBitMap(newItemOverlay);
        ItemOverlay = newItemOverlay;
    }

    public int[,] getLatestItemGrid() {
        return latestItemGrid;
    }
}