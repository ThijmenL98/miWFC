using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.DeBroglie.Topo;
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels.Structs;
using ReactiveUI;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class ItemViewModel : ReactiveObject {
    private CentralManager? centralManager;
    private readonly MainWindowViewModel mainWindowViewModel;

    private Bitmap _currentItemImage
        = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    private string _itemDescription = "Placeholder";

    private bool _inItemMenu,
        _itemsMayAppearAnywhere,
        _itemIsDependent,
        _itemsInRange,
        _itemEditorEnabled;

    private int _editingEntry = -2,
        _itemsToAddValue = 1,
        _itemsToAddLower = 1,
        _depMaxDistance = 2,
        _depMinDistance = 1,
        _itemsToAddUpper = 2;
    
    private ItemType _selectedItemToAdd, _selectedItemDependency;
    
    private ObservableCollection<ItemObjectViewModel> _itemDataGrid = new();

    private Tuple<int, int>[,] _latestItemGrid = new Tuple<int, int>[0, 0];

    /*
     * Initializing Functions & Constructor
     */

    public ItemViewModel(MainWindowViewModel mwvm) {
        mainWindowViewModel = mwvm;
        
        _selectedItemToAdd = ItemType.getItemTypeByID(0);
        _selectedItemDependency = ItemType.getItemTypeByID(1);
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    /*
     * Getters & Setters
     */

    // Strings

    /// <summary>
    /// Description of this item
    /// </summary>
    public string ItemDescription {
        get => _itemDescription;
        set => this.RaiseAndSetIfChanged(ref _itemDescription, value);
    }

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    /// Amount of this item to add to the world
    /// </summary>
    public int ItemsToAddValue {
        get => _itemsToAddValue;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddValue, value);
    }

    /// <summary>
    /// Maximum amount of this item to add to the world
    /// </summary>
    public int ItemsToAddUpper {
        get => _itemsToAddUpper;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddUpper, value);
    }

    /// <summary>
    /// Minimum amount of this item to add to the world
    /// </summary>
    public int ItemsToAddLower {
        get => _itemsToAddLower;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddLower, value);
    }

    /// <summary>
    /// Maximum distance of the dependent item to spawn from this item
    /// </summary>
    public int DepMaxDistance {
        get => _depMaxDistance;
        set => this.RaiseAndSetIfChanged(ref _depMaxDistance, value);
    }

    /// <summary>
    /// Minimum distance of the dependent item to spawn from this item
    /// </summary>
    public int DepMinDistance {
        get => _depMinDistance;
        set => this.RaiseAndSetIfChanged(ref _depMinDistance, value);
    }

    // Booleans

    /// <summary>
    /// Whether the item editor window is allowed to be opened (only on a fully collapsed Top-Down World)
    /// </summary>
    public bool ItemEditorEnabled {
        get => _itemEditorEnabled;
        set => this.RaiseAndSetIfChanged(ref _itemEditorEnabled, value);
    }

    /// <summary>
    /// Whether the user is inside of the item addition menu
    /// </summary>
    public bool InItemMenu {
        get => _inItemMenu;
        set => this.RaiseAndSetIfChanged(ref _inItemMenu, value);
    }

    /// <summary>
    /// Whether the current item may appear anywhere in the world
    /// </summary>
    public bool ItemsMayAppearAnywhere {
        get => _itemsMayAppearAnywhere;
        set => this.RaiseAndSetIfChanged(ref _itemsMayAppearAnywhere, value);
    }

    /// <summary>
    /// Whether the current item has a dependent item
    /// </summary>
    public bool ItemIsDependent {
        get => _itemIsDependent;
        set => this.RaiseAndSetIfChanged(ref _itemIsDependent, value);
    }

    /// <summary>
    /// Whether the current item has a fixed amount or a range amount (min-max)
    /// </summary>
    public bool ItemsInRange {
        get => _itemsInRange;
        set => this.RaiseAndSetIfChanged(ref _itemsInRange, value);
    }

    // Images

    /// <summary>
    /// The image associated with this item
    /// </summary>
    public Bitmap CurrentItemImage {
        get => _currentItemImage;
        set => this.RaiseAndSetIfChanged(ref _currentItemImage, value);
    }

    // Objects

    // ReSharper disable once UnusedMember.Local
    /// <summary>
    /// The window's currently selected item
    /// </summary>
    private ItemType SelectedItemToAdd {
        get => _selectedItemToAdd;
        set => this.RaiseAndSetIfChanged(ref _selectedItemToAdd, value);
    }

    // ReSharper disable once UnusedMember.Local
    /// <summary>
    /// The window's currently selected dependent item
    /// </summary>
    private ItemType SelectedItemDependency {
        get => _selectedItemDependency;
        set => this.RaiseAndSetIfChanged(ref _selectedItemDependency, value);
    }

    // Lists

    /// <summary>
    /// Get the latest item grid to apply
    /// </summary>
    /// <returns></returns>
    public Tuple<int, int>[,] getLatestItemGrid() {
        return _latestItemGrid;
    }

    /// <summary>
    /// The data grid on the right side of the window to show the user which items are being added
    /// </summary>
    public ObservableCollection<ItemObjectViewModel> ItemDataGrid {
        get => _itemDataGrid;
        set => this.RaiseAndSetIfChanged(ref _itemDataGrid, value);
    }

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Function called when applying the currently created item in the item addition menu
    /// </summary>
    public void AddItemToDataGrid() {
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
                    allowedTiles.Add(mainWindowViewModel.PaintTiles[idx]);
                }
            }
        } else {
            allowedTiles = new List<TileViewModel>(mainWindowViewModel.PaintTiles);
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
            RemoveIndexedItem(_editingEntry);
        }

        ItemDataGrid.Add(new ItemObjectViewModel(itemType, (amountLower, amountUpper),
            new ObservableCollection<TileViewModel>(allowedTiles),
            centralManager!.getItemWindow().getItemAddMenu().getItemImage(itemType), depItem));

        InItemMenu = false;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;
        ItemIsDependent = false;
        DepMinDistance = 1;
        _editingEntry = -2;
        DepMaxDistance = 2;

        foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
            tvm.ItemAddChecked = false;
        }

        centralManager!.getItemWindow().getItemAddMenu().updateAllowedTiles();
        mainWindowViewModel.ItemOverlay
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        GenerateItemGrid();
    }

    /// <summary>
    /// Function called when exiting the item addition menu
    /// </summary>
    public void ExitItemAddition() {
        InItemMenu = false;
        _editingEntry = -2;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;
        ItemIsDependent = false;
        DepMinDistance = 1;
        DepMaxDistance = 2;

        foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
            tvm.ItemAddChecked = false;
        }

        centralManager!.getItemWindow().getItemAddMenu().updateAllowedTiles();
    }

    /// <summary>
    /// Function called when creating a new item
    /// </summary>
    public void CreateNewItem() {
        centralManager!.getItemWindow().getItemAddMenu().updateSelectedItemIndex();
        centralManager!.getItemWindow().getItemAddMenu().updateDependencyIndex();
        ItemDescription = ItemType.itemTypes[0].Description;
        InItemMenu = true;
    }

    /// <summary>
    /// Function called when editing the currently selected existing item from the data grid
    /// </summary>
    public void EditSelectedItem() {
        DataGrid dg = centralManager!.getItemWindow().getDataGrid();
        int selectedIndex = dg.SelectedIndex;
        if (selectedIndex.Equals(-1)) {
            centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
            return;
        }

        InItemMenu = true;
        ItemObjectViewModel itemSelected = ItemDataGrid[selectedIndex];

        ItemsMayAppearAnywhere
            = itemSelected.AllowedTiles.Count.Equals(mainWindowViewModel.PaintTiles.Count);
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

        foreach ((TileViewModel tvm, int index) in mainWindowViewModel.PaintTiles.Select((model, i) => (model, i))) {
            if (itemSelected.AllowedTiles.Select(at => at.PatternIndex).Contains(index)) {
                tvm.ItemAddChecked = true;
                centralManager!.getItemWindow().getItemAddMenu().forwardAllowedTileChange(index, true);
            }
        }

        _editingEntry = selectedIndex;
    }

    /// <summary>
    /// Function called when removing the currently selected existing item from the data grid
    /// </summary>
    public void RemoveSelectedItem() {
        RemoveIndexedItem(-2);
    }

    /// <summary>
    /// Function called when removing an existing item by index from the data grid
    /// </summary>
    /// <param name="selectedIndex"></param>
    public void RemoveIndexedItem(int selectedIndex) {
        DataGrid dg = centralManager!.getItemWindow().getDataGrid();
        selectedIndex = selectedIndex == -2 ? dg.SelectedIndex : selectedIndex;
        if (selectedIndex < 0) {
            centralManager?.getUIManager().dispatchError(centralManager!.getItemWindow());
            return;
        }

        ItemDataGrid.RemoveAt(selectedIndex);
        dg.SelectedIndex = -1;
    }

    /// <summary>
    /// Function called when applying the current items from the data grid
    /// </summary>
    public void GenerateItemGrid() {
        if (ItemDataGrid.Count < 1) {
            WriteableBitmap newItemOverlayEmpty
                = Util.generateItemOverlay(new Tuple<int, int>[0, 0], mainWindowViewModel.ImageOutWidth,
                    mainWindowViewModel.ImageOutHeight);
            Util.setLatestItemBitMap(newItemOverlayEmpty);
            mainWindowViewModel.ItemOverlay = newItemOverlayEmpty;
            return;
        }

        Tuple<int, int>[,] itemGrid
            = new Tuple<int, int>[mainWindowViewModel.ImageOutWidth, mainWindowViewModel.ImageOutHeight];

        for (int x = 0; x < mainWindowViewModel.ImageOutWidth; x++) {
            for (int y = 0; y < mainWindowViewModel.ImageOutHeight; y++) {
                itemGrid[x, y] = new Tuple<int, int>(-1, -1);
            }
        }

        Color[,] distinctColourCount = { };
        int[,] distinctIndexCount = new int[0, 0];

        int[] spacesLeft = new int[mainWindowViewModel.PaintTiles.Count];
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

            foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
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

            foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
                spacesLeft[tvm.PatternIndex] = distinctList.Count(c => c.Equals(tvm.PatternIndex));
            }
        }

        int depItemCount = 1;

        foreach (ItemObjectViewModel ivm in ItemDataGrid) {
            List<int> allowedAdd = ivm.AllowedTiles.Select(tvm => tvm.PatternIndex).ToList();
            int toAdd = mainWindowViewModel.R.Next(ivm.Amount.Item1, ivm.Amount.Item2 + 1);

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

                    int randLoc = mainWindowViewModel.R.Next(mainWindowViewModel.ImageOutWidth *
                                                             mainWindowViewModel.ImageOutHeight);
                    int xLoc = randLoc / mainWindowViewModel.ImageOutWidth;
                    int yLoc = randLoc % mainWindowViewModel.ImageOutWidth;
                    bool allowedAtLoc = false;
                    if (centralManager!.getWFCHandler().isOverlappingModel()) {
                        Color colorAtPos = distinctColourCount[xLoc, yLoc];
                        if (mainWindowViewModel.PaintTiles.Where(tvm => tvm.PatternColour.Equals(colorAtPos))
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
                                     xx < Math.Min(mainWindowViewModel.ImageOutWidth - 1, xLoc + maxDist);
                                     xx++) {
                                    for (int yy = Math.Max(0, yLoc - maxDist);
                                         yy < Math.Min(mainWindowViewModel.ImageOutHeight - 1, yLoc + maxDist);
                                         yy++) {
                                        double depDist
                                            = Math.Sqrt((xx - xLoc) * (xx - xLoc) + (yy - yLoc) * (yy - yLoc));
                                        if (depDist <= maxDist && depDist >= minDist) {
                                            if (centralManager!.getWFCHandler().isOverlappingModel()) {
                                                Color colorAtPos = distinctColourCount[xx, yy];
                                                if (mainWindowViewModel.PaintTiles.Where(tvm =>
                                                            tvm.PatternColour.Equals(colorAtPos))
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
                                    Tuple<int, int> depCoords
                                        = possibleCoordinates[mainWindowViewModel.R.Next(possibleCoordinates.Count)];
                                    itemGrid[depCoords.Item1, depCoords.Item2]
                                        = new Tuple<int, int>(depItem.Item1!.ID, depItemCount);

                                    if (centralManager!.getWFCHandler().isOverlappingModel()) {
                                        Color addedLoc = distinctColourCount[depCoords.Item1, depCoords.Item2];
                                        foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
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
                                    foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
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

        _latestItemGrid = itemGrid;
        WriteableBitmap newItemOverlay = Util.generateItemOverlay(itemGrid, mainWindowViewModel.ImageOutWidth,
            mainWindowViewModel.ImageOutHeight);
        Util.setLatestItemBitMap(newItemOverlay);
        mainWindowViewModel.ItemOverlay = newItemOverlay;
    }

    /// <summary>
    /// Function called to reset the items in the image
    /// </summary>
    public void ResetDataGrid() {
        ItemDataGrid = new ObservableCollection<ItemObjectViewModel>();
    }
}