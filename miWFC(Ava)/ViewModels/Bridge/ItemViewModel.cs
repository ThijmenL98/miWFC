using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.DeBroglie.Topo;
using miWFC.Delegators;
using miWFC.Utils;
using miWFC.ViewModels.Structs;
using ReactiveUI;

// ReSharper disable IntroduceOptionalParameters.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels.Bridge;

public class ItemViewModel : ReactiveObject {
    private readonly MainWindowViewModel mainWindowViewModel;

    private Bitmap _currentItemImage
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul),
        _regionImage
            = Util.CreateBitmapFromData(1, 1, 1, (_, _) => Util.positiveColour);

    private string _displayName = "", _dependentDisplayName = "", _itemColour = "", _depItemColour = "";

    private int _editingEntry = -2,
        _itemsToAddValue = 1,
        _itemsToAddLower = 1,
        _depMaxDistance = 5,
        _depMinDistance = 1,
        _itemsToAddUpper = 2;

    private bool _inItemMenu,
        _inRegionDefineMenu,
        _inAnyMenu,
        _itemsMayAppearAnywhere,
        _itemIsDependent,
        _itemsInRange,
        _itemEditorEnabled;

    private ObservableCollection<ItemObjectViewModel> _itemDataGrid = new();

    private Tuple<string, int>[,]? _latestItemGrid = new Tuple<string, int>[0, 0];
    private CentralDelegator? centralDelegator;

    /*
     * Initializing Functions & Constructor
     */

    public ItemViewModel(MainWindowViewModel mwvm) {
        mainWindowViewModel = mwvm;
    }

    /*
     * Getters & Setters
     */

    // Strings

    /// <summary>
    ///     User input name of the item to add
    /// </summary>
    public string DisplayName {
        get => _displayName;
        set {
            this.RaiseAndSetIfChanged(ref _displayName, value);
            if (_displayName == "") {
                throw new DataValidationException("Name cannot be empty!");
            }
        }
    }

    /// <summary>
    ///     Dependent item name through user input
    /// </summary>
    public string DependentItemName {
        get => _dependentDisplayName;
        set {
            this.RaiseAndSetIfChanged(ref _dependentDisplayName, value);
            if (_dependentDisplayName == "") {
                throw new DataValidationException("Name cannot be empty!");
            }
        }
    }

    /// <summary>
    ///     Item colour hex string
    /// </summary>
    public string ItemColour {
        get => _itemColour;
        set {
            this.RaiseAndSetIfChanged(ref _itemColour, value);
            if (!_itemColour.Equals("")) {
                string? validatedColor = Util.ValidateColor(_itemColour);
                if (validatedColor == null) {
                    throw new DataValidationException("Invalid colour hex!");
                }

                centralDelegator!.GetMainWindowVM().ItemVM.CurrentItemImage
                    = Util.GetItemImage(Color.Parse(validatedColor));
            } else {
                centralDelegator!.GetMainWindowVM().ItemVM.CurrentItemImage
                    = Util.GetItemImage(Colors.Red);
            }
        }
    }

    /// <summary>
    ///     Dependent Item colour hex string
    /// </summary>
    public string DepItemColour {
        get => _depItemColour;
        set {
            this.RaiseAndSetIfChanged(ref _depItemColour, value);
            if (!_depItemColour.Equals("")) {
                string? validatedColor = Util.ValidateColor(_depItemColour);
                if (validatedColor == null) {
                    throw new DataValidationException("Invalid colour hex!");
                }
            }
        }
    }

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    ///     Amount of this item to add to the world
    /// </summary>
    public int ItemsToAddValue {
        get => _itemsToAddValue;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddValue, value);
    }

    /// <summary>
    ///     Maximum amount of this item to add to the world
    /// </summary>
    public int ItemsToAddUpper {
        get => _itemsToAddUpper;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddUpper, value);
    }

    /// <summary>
    ///     Minimum amount of this item to add to the world
    /// </summary>
    public int ItemsToAddLower {
        get => _itemsToAddLower;
        set => this.RaiseAndSetIfChanged(ref _itemsToAddLower, value);
    }

    /// <summary>
    ///     Maximum distance of the dependent item to spawn from this item
    /// </summary>
    public int DepMaxDistance {
        get => _depMaxDistance;
        set => this.RaiseAndSetIfChanged(ref _depMaxDistance, value);
    }

    /// <summary>
    ///     Minimum distance of the dependent item to spawn from this item
    /// </summary>
    public int DepMinDistance {
        get => _depMinDistance;
        set => this.RaiseAndSetIfChanged(ref _depMinDistance, value);
    }

    // Booleans

    /// <summary>
    ///     Whether the item editor window is allowed to be opened (only on a fully collapsed Top-Down World)
    /// </summary>
    public bool ItemEditorEnabled {
        get => _itemEditorEnabled;
        set => this.RaiseAndSetIfChanged(ref _itemEditorEnabled, value);
    }

    /// <summary>
    ///     Whether the user is inside of the item addition menu
    /// </summary>
    public bool InItemMenu {
        get => _inItemMenu;
        set {
            this.RaiseAndSetIfChanged(ref _inItemMenu, value);
            InAnyMenu = InRegionDefineMenu || InItemMenu;
        }
    }

    /// <summary>
    ///     Whether the user is inside of the item appearance region menu menu
    /// </summary>
    public bool InRegionDefineMenu {
        get => _inRegionDefineMenu;
        set {
            this.RaiseAndSetIfChanged(ref _inRegionDefineMenu, value);
            InItemMenu = !InRegionDefineMenu;
            InAnyMenu = InRegionDefineMenu || InItemMenu;
        }
    }

    /// <summary>
    ///     Whether the user is inside of any submenu
    /// </summary>
    public bool InAnyMenu {
        get => _inAnyMenu;
        set => this.RaiseAndSetIfChanged(ref _inAnyMenu, value);
    }

    /// <summary>
    ///     Whether the current item may appear anywhere in the world
    /// </summary>
    public bool ItemsMayAppearAnywhere {
        get => _itemsMayAppearAnywhere;
        set => this.RaiseAndSetIfChanged(ref _itemsMayAppearAnywhere, value);
    }

    /// <summary>
    ///     Whether the current item has a dependent item
    /// </summary>
    public bool ItemIsDependent {
        get => _itemIsDependent;
        set => this.RaiseAndSetIfChanged(ref _itemIsDependent, value);
    }

    /// <summary>
    ///     Whether the current item has a fixed amount or a range amount (min-max)
    /// </summary>
    public bool ItemsInRange {
        get => _itemsInRange;
        set => this.RaiseAndSetIfChanged(ref _itemsInRange, value);
    }

    // Images

    /// <summary>
    ///     The image associated with this item
    /// </summary>
    public Bitmap CurrentItemImage {
        get => _currentItemImage;
        set => this.RaiseAndSetIfChanged(ref _currentItemImage, value);
    }

    public Bitmap RegionImage {
        get => _regionImage;
        set => this.RaiseAndSetIfChanged(ref _regionImage, value);
    }

    /// <summary>
    ///     The data grid on the right side of the window to show the user which items are being added
    /// </summary>
    public ObservableCollection<ItemObjectViewModel> ItemDataGrid {
        get => _itemDataGrid;
        set => this.RaiseAndSetIfChanged(ref _itemDataGrid, value);
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;
    }

    // Objects

    // Lists

    /// <summary>
    ///     Get the latest item grid to apply
    /// </summary>
    /// <returns></returns>
    public Tuple<string, int>[,]? GetLatestItemGrid() {
        return _latestItemGrid;
    }

    /// <summary>
    ///     Get all colours associated to each item name
    /// </summary>
    /// <returns>Colour dictionary</returns>
    public Dictionary<string, Color> GetItemColours() {
        Dictionary<string, Color> mainItems = new();

        foreach (ItemObjectViewModel model in ItemDataGrid) {
            string key = model.ItemName;
            Color val = model.MyColor;
            if (mainItems.ContainsKey(key) && mainItems[key].Equals(val)) {
                continue;
            }

            if (mainItems.ContainsKey(key)) {
                DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetItemNameTB(),
                    new DataValidationException($"Name already exists with a different colour: {mainItems[key]}"));
                return new Dictionary<string, Color>();
            }

            mainItems.Add(model.ItemName, model.MyColor);
        }

        Dictionary<string, Color> depItems = new();

        foreach (ItemObjectViewModel item in ItemDataGrid) {
            if (item.HasDependentItem) {
                string key = item.DependentItem.Item1!;
                Color val = (Color) item.DepColor!;
                if (depItems.ContainsKey(key) && depItems[key].Equals(val)) {
                    continue;
                }

                if (depItems.ContainsKey(key)) {
                    DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetDepItemNameTB(),
                        new DataValidationException($"Name already exists with a different colour: {depItems[key]}"));
                    return new Dictionary<string, Color>();
                }

                depItems.Add(key, val);
            }
        }

        Dictionary<string, Color> allItems
            = mainItems.Union(depItems).ToDictionary(pair => pair.Key, pair => pair.Value);

        return allItems;
    }

    /// <summary>
    ///     Get all unique item names
    /// </summary>
    /// <returns>List of item names</returns>
    public List<string> GetItemNames() {
        List<string> mainItems = ItemDataGrid.Select(item => item.ItemName).ToList();
        List<string> depItems = (from item in ItemDataGrid where item.HasDependentItem select item.DependentItem.Item1)
            .ToList()!;
        mainItems.AddRange(depItems);
        return mainItems;
    }

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Function called when applying the currently created item in the item addition menu
    /// </summary>
    public void AddItemToDataGrid() {
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
            IEnumerable<bool> allowedCheck = centralDelegator!.GetItemWindow().GetItemAddMenu().GetAllowedTiles();
            foreach ((bool allowed, int idx) in allowedCheck.Select((b, i) => (b, i))) {
                if (allowed) {
                    allowedTiles.Add(mainWindowViewModel.PaintTiles[idx]);
                }
            }
        } else {
            allowedTiles = new List<TileViewModel>(mainWindowViewModel.PaintTiles);
        }

        Color? itemColour;
        try {
            itemColour = ItemColour.Equals("") ? Colors.Red :
                ItemColour.StartsWith("#") ? Color.Parse(ItemColour) : Color.Parse("#" + ItemColour);
        } catch (FormatException) {
            itemColour = null;
        }

        if (itemColour == null) {
            try { 
                itemColour = ItemColour.Equals("") ? Colors.Red : Color.Parse(ItemColour);
            } catch (FormatException) {
                DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetItemColourTB(),
                    new DataValidationException("Invalid colour hex!"));
                centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(), null);
                return;
            }
        }

        if (allowedTiles.Count == 0 || DisplayName.Equals("") || string.IsNullOrWhiteSpace(DisplayName) ||
            (GetItemNames().Contains(DisplayName) && !GetItemColours()[DisplayName].Equals(itemColour))) {
            if (DisplayName.Equals("") || string.IsNullOrWhiteSpace(DisplayName)) {
                DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetItemNameTB(),
                    new DataValidationException("Name cannot be empty"));
            }

            if (GetItemNames().Contains(DisplayName) && !GetItemColours()[DisplayName].Equals(itemColour)) {
                DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetItemNameTB(),
                    new DataValidationException($"Name already exists with a different colour: {GetItemColours()[DisplayName]}"));
            }

            string? message = null;

            if (allowedTiles.Count == 0) {
                message = "No tiles have been selected";
            }

            centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(), message);
            return;
        }

        bool isDependent = ItemIsDependent;
        Tuple<string?, WriteableBitmap?, Color?, (int, int)>? depItem = null;
        if (isDependent) {
            Color? depColor;
            try {
                depColor = DepItemColour.Equals("") ? Colors.Red :
                    DepItemColour.StartsWith("#") ? Color.Parse(DepItemColour) : Color.Parse("#" + DepItemColour);
            } catch (FormatException) {
                depColor = null;
            }

            if (depColor == null) {
                try { 
                    depColor = DepItemColour.Equals("") ? Colors.Red : Color.Parse(DepItemColour);
                } catch (FormatException) {
                    DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetDepItemColourTB(),
                        new DataValidationException("Invalid colour hex!"));
                    centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(), null);
                    return;
                }
            }

            if (DependentItemName.Equals("") || string.IsNullOrWhiteSpace(DependentItemName) ||
                (GetItemNames().Contains(DependentItemName) &&
                 !GetItemColours()[DependentItemName].Equals(depColor))) {
                if (DependentItemName.Equals("") || string.IsNullOrWhiteSpace(DependentItemName)) {
                    DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetDepItemNameTB(),
                        new DataValidationException("Name cannot be empty"));
                }

                if (GetItemNames().Contains(DependentItemName) &&
                    !GetItemColours()[DependentItemName].Equals(itemColour)) {
                    DataValidationErrors.SetError(centralDelegator!.GetItemWindow().GetItemAddMenu().GetDepItemNameTB(),
                        new DataValidationException($"Name already exists with a different colour: {GetItemColours()[DependentItemName]}"));
                }

                centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(), null);
                return;
            }

            (int, int) range = (DepMinDistance, DepMaxDistance);
            WriteableBitmap depItemBitmap = Util.GetItemImage((Color) depColor);
            depItem = new Tuple<string?, WriteableBitmap?, Color?, (int, int)>(DependentItemName, depItemBitmap,
                depColor, range);
        }

        if (_editingEntry != -2) {
            RemoveIndexedItem(_editingEntry);
        }

        ObservableCollection<TileViewModel> myAllowedTiles = new(allowedTiles);

        foreach (ItemObjectViewModel itemObjectViewModel in ItemDataGrid) {
            if (itemObjectViewModel.ItemName.Equals(DisplayName) &&
                itemObjectViewModel.AllowedTiles.SequenceEqual(myAllowedTiles) &&
                itemObjectViewModel.AllowedTiles.Count == myAllowedTiles.Count) {
                if (itemObjectViewModel.HasDependentItem && isDependent &&
                    itemObjectViewModel.DependentItem.Item1!.Equals(depItem!.Item1) &&
                    itemObjectViewModel.DependentItem.Item2.Equals(depItem.Item4)) {
                    centralDelegator?.GetInterfaceHandler()
                        .DispatchError(centralDelegator!.GetItemWindow(), "Item already exists");
                    return;
                }

                if (!itemObjectViewModel.HasDependentItem && !isDependent) {
                    centralDelegator?.GetInterfaceHandler()
                        .DispatchError(centralDelegator!.GetItemWindow(), "Item already exists");
                    return;
                }
            }
        }

        bool[,] itemAllowanceMask = centralDelegator!.GetItemWindow().GetRegionDefineMenu().GetAllowanceMask();

        ItemDataGrid.Add(new ItemObjectViewModel(DisplayName, (amountLower, amountUpper),
            myAllowedTiles, (Color) itemColour,
            Util.GetItemImage((Color) itemColour), depItem, itemAllowanceMask,
            Util.CreateBitmapFromData(centralDelegator!.GetMainWindowVM().ImageOutWidth,
                centralDelegator!.GetMainWindowVM().ImageOutHeight, 1,
                (x, y) => itemAllowanceMask[x, y] ? Util.positiveColour : Util.negativeColour)));

        InItemMenu = false;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;
        ItemIsDependent = false;
        DepMinDistance = 1;
        _editingEntry = -2;
        DepMaxDistance = 5;

        try {
            DisplayName = "";
            ItemColour = "White";
            DependentItemName = "";
            DepItemColour = "White";
        } catch (DataValidationException) { }

        foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
            tvm.ItemAddChecked = false;
        }

        centralDelegator!.GetItemWindow().GetItemAddMenu().UpdateAllowedTiles();
        mainWindowViewModel.ItemOverlay
            = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        centralDelegator.GetItemWindow().GetRegionDefineMenu().ResetAllowanceMask();
        GenerateItemGrid();
    }

    /// <summary>
    ///     Function called when exiting the item addition menu
    /// </summary>
    public void ExitItemAddition() {
        InItemMenu = false;
        _editingEntry = -2;
        ItemsMayAppearAnywhere = false;
        ItemsToAddValue = 1;
        ItemIsDependent = false;
        DepMinDistance = 1;
        DepMaxDistance = 5;
        
        try {
            DisplayName = "";
            ItemColour = "White";
            DependentItemName = "";
            DepItemColour = "White";
        } catch (DataValidationException) { }

        foreach (TileViewModel tvm in mainWindowViewModel.PaintTiles) {
            tvm.ItemAddChecked = false;
        }

        centralDelegator!.GetItemWindow().GetItemAddMenu().UpdateAllowedTiles();
    }

    /// <summary>
    ///     Function called when creating a new item
    /// </summary>
    public void CreateNewItem() {
        centralDelegator!.GetItemWindow().GetRegionDefineMenu().ResetAllowanceMask();

        CurrentItemImage = Util.GetItemImage(Colors.Red);

        InItemMenu = true;
    }

    /// <summary>
    ///     Function called when editing the currently selected existing item from the data grid
    /// </summary>
    public void EditSelectedItem() {
        DataGrid dg = centralDelegator!.GetItemWindow().GetDataGrid();
        int selectedIndex = dg.SelectedIndex;
        if (selectedIndex.Equals(-1)) {
            centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(),
                "You need to select an item to edit first");
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

        DisplayName = itemSelected.ItemName;
        ItemColour = itemSelected.MyColor.ToString().ToUpper().Replace("#FF", "#");

        ItemIsDependent = itemSelected.HasDependentItem;
        if (ItemIsDependent) {
            DepMinDistance = itemSelected.DependentItem.Item2.Item1;
            DepMaxDistance = itemSelected.DependentItem.Item2.Item2;

            DependentItemName = itemSelected.DependentItem.Item1!;
            DepItemColour = ((Color) itemSelected.DepColor!).ToString().ToUpper().Replace("#FF", "#");
        } else {
            try {
                DepMinDistance = 1;
                DepMaxDistance = 5;

                DependentItemName = "";
                DepItemColour = "";
            } catch (DataValidationException) { }
        }

        foreach ((TileViewModel tvm, int index) in mainWindowViewModel.PaintTiles.Select((model, i) => (model, i))) {
            if (itemSelected.AllowedTiles.Select(at => at.PatternIndex).Contains(index)) {
                tvm.ItemAddChecked = true;
                centralDelegator!.GetItemWindow().GetItemAddMenu().ForwardAllowedTileChange(index, true);
            }
        }

        centralDelegator!.GetItemWindow().GetRegionDefineMenu().SetAllowanceMask(itemSelected.AppearanceRegion);

        _editingEntry = selectedIndex;
    }

    /// <summary>
    ///     Function called when removing the currently selected existing item from the data grid
    /// </summary>
    public void RemoveSelectedItem() {
        RemoveIndexedItem(-2);
    }

    /// <summary>
    ///     Function called when removing an existing item by index from the data grid
    /// </summary>
    /// <param name="selectedIndex"></param>
    public void RemoveIndexedItem(int selectedIndex) {
        DataGrid dg = centralDelegator!.GetItemWindow().GetDataGrid();
        selectedIndex = selectedIndex == -2 ? dg.SelectedIndex : selectedIndex;
        if (selectedIndex < 0) {
            centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(),
                "You need to select an item to delete first");
            return;
        }

        ItemDataGrid.RemoveAt(selectedIndex);
        dg.SelectedIndex = -1;
    }

    /// <summary>
    ///     Function called when applying the current items from the data grid
    /// </summary>
    public void GenerateItemGrid() {
        if (ItemDataGrid.Count < 1) {
            WriteableBitmap newItemOverlayEmpty
                = Util.GenerateItemOverlay(new Tuple<string, int>[0, 0], mainWindowViewModel.ImageOutWidth,
                    mainWindowViewModel.ImageOutHeight, GetItemColours());
            Util.SetLatestItemBitMap(newItemOverlayEmpty);
            mainWindowViewModel.ItemOverlay = newItemOverlayEmpty;
            return;
        }

        Tuple<string, int>[,]? itemGrid
            = new Tuple<string, int>[mainWindowViewModel.ImageOutWidth, mainWindowViewModel.ImageOutHeight];

        for (int x = 0; x < mainWindowViewModel.ImageOutWidth; x++) {
            for (int y = 0; y < mainWindowViewModel.ImageOutHeight; y++) {
                itemGrid[x, y] = new Tuple<string, int>("", -1);
            }
        }

        Color[,] distinctColourCount = { };
        int[,] distinctIndexCount = new int[0, 0];

        int[] spacesLeft = new int[mainWindowViewModel.PaintTiles.Count];
        if (centralDelegator!.GetWFCHandler().IsOverlappingModel()) {
            distinctColourCount = centralDelegator!.GetWFCHandler().GetPropagatorOutputO().toArray2d();
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
            distinctIndexCount = centralDelegator!.GetWFCHandler().GetPropagatorOutputA().toArray2d();
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

        itemGrid = FillItemGrid(itemGrid, spacesLeft, distinctColourCount, distinctIndexCount);
        if (itemGrid == null) {
            return;
        }

        _latestItemGrid = itemGrid;
        WriteableBitmap newItemOverlay = Util.GenerateItemOverlay(itemGrid, mainWindowViewModel.ImageOutWidth,
            mainWindowViewModel.ImageOutHeight, GetItemColours());
        Util.SetLatestItemBitMap(newItemOverlay);
        mainWindowViewModel.ItemOverlay = newItemOverlay;
    }

    /// <summary>
    ///     Fill the item grid with all found items, if possible, if not all items could be added, an error is returned
    /// </summary>
    /// <param name="itemGrid">Predefined item grid, empty</param>
    /// <param name="spacesLeft">Amount of spaces left available to place items on for each type of tile</param>
    /// <param name="distinctColourCount">List of distinct colours</param>
    /// <param name="distinctIndexCount">List of distinct indices to place on</param>
    /// <returns>Filled item grid, or null if not possible</returns>
    private Tuple<string, int>[,]? FillItemGrid(Tuple<string, int>[,] itemGrid, IList<int> spacesLeft,
        Color[,] distinctColourCount, int[,] distinctIndexCount) {
        int depItemCount = 1;

        foreach (ItemObjectViewModel ivm in ItemDataGrid) {
            List<int> allowedAdd = ivm.AllowedTiles.Select(tvm => tvm.PatternIndex).ToList();
            int toAdd = mainWindowViewModel.R.Next(ivm.Amount.Item1, ivm.Amount.Item2 + 1);

            for (int i = 0; i < toAdd; i++) {
                bool added = false;
                int retry = 0;
                List<(int, int)> locToSkip = new();
                int allowedRetries = mainWindowViewModel.ImageOutWidth *
                                     mainWindowViewModel.ImageOutHeight * 2;

                while (!added) {
                    retry++;
                    if (retry >= allowedRetries || locToSkip.Count == mainWindowViewModel.ImageOutWidth *
                        mainWindowViewModel.ImageOutHeight) {
                        if (i >= ivm.Amount.Item1) {
                            return itemGrid;
                        }

                        centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(),
                            $"Addition failed after timing out");
                        return null;
                    }

                    if (allowedAdd.Select(x => spacesLeft[x]).Sum() == 0) {
                        centralDelegator?.GetInterfaceHandler().DispatchError(centralDelegator!.GetItemWindow(),
                            "Item addition failed as no spaces were available");
                        return null;
                    }

                    int randLoc = mainWindowViewModel.R.Next(mainWindowViewModel.ImageOutWidth *
                                                             mainWindowViewModel.ImageOutHeight);
                    int xLoc = randLoc / mainWindowViewModel.ImageOutWidth;
                    int yLoc = randLoc % mainWindowViewModel.ImageOutWidth;

                    int whileRetries = allowedRetries;

                    while (whileRetries > 0) {
                        if (locToSkip.Contains((xLoc, yLoc))) {
                            randLoc = mainWindowViewModel.R.Next(mainWindowViewModel.ImageOutWidth *
                                                                 mainWindowViewModel.ImageOutHeight);
                            xLoc = randLoc / mainWindowViewModel.ImageOutWidth;
                            yLoc = randLoc % mainWindowViewModel.ImageOutWidth;
                        } else {
                            whileRetries = 0;
                        }

                        whileRetries--;
                    }

                    locToSkip.Add((xLoc, yLoc));

                    bool allowedAtLoc = false;
                    if (centralDelegator!.GetWFCHandler().IsOverlappingModel()) {
                        Color colorAtPos = distinctColourCount[xLoc, yLoc];
                        if (mainWindowViewModel.PaintTiles.Where(tvm => tvm.PatternColour.Equals(colorAtPos))
                                .Any(tvm => allowedAdd.Contains(tvm.PatternIndex)) &&
                            ivm.AppearanceRegion[xLoc, yLoc]) {
                            allowedAtLoc = true;
                        }
                    } else {
                        int valAtPos = distinctIndexCount[xLoc, yLoc];
                        allowedAtLoc = allowedAdd.Contains(valAtPos) && ivm.AppearanceRegion[xLoc, yLoc];
                    }

                    if (allowedAtLoc) {
                        (added, depItemCount) = TryPlaceItemAt(xLoc, yLoc, ivm, itemGrid, distinctColourCount,
                            allowedAdd,
                            distinctIndexCount, depItemCount, spacesLeft);
                    }
                }
            }
        }

        return itemGrid;
    }

    /// <summary>
    ///     Try to actually place an item at the desired position
    /// </summary>
    /// <param name="xLoc">X location to place at</param>
    /// <param name="yLoc">Y location to place at</param>
    /// <param name="ivm">Item object to place</param>
    /// <param name="itemGrid">Current item grid</param>
    /// <param name="distinctColourCount">List of distinct colours</param>
    /// <param name="allowedAdd">List of allowed tiles for the item to be placed on</param>
    /// <param name="distinctIndexCount">List of distinct tile indices to place on</param>
    /// <param name="depItemCount">ID count of the dependent item identifier</param>
    /// <param name="spacesLeft">Amount of spaces left for each tile type</param>
    /// <returns>Whether the placement of the item has succeeded and the new count of dependent items ids</returns>
    private (bool, int) TryPlaceItemAt(int xLoc, int yLoc, ItemObjectViewModel ivm, Tuple<string, int>[,] itemGrid,
        Color[,] distinctColourCount, ICollection<int> allowedAdd, int[,] distinctIndexCount, int depItemCount,
        IList<int> spacesLeft) {
        if (itemGrid[xLoc, yLoc].Item1.Equals("") && ivm.AppearanceRegion[xLoc, yLoc]) {
            bool canContinueDependent = !ivm.HasDependentItem;
            if (ivm.HasDependentItem) {
                (canContinueDependent, spacesLeft, itemGrid) = TryPlaceDependencyAt(xLoc, yLoc, ivm, itemGrid,
                    distinctColourCount, allowedAdd, distinctIndexCount, depItemCount, spacesLeft);
            }

            if (canContinueDependent) {
                itemGrid[xLoc, yLoc] = new Tuple<string, int>(ivm.ItemName,
                    ivm.HasDependentItem ? depItemCount : -1);

                if (ivm.HasDependentItem) {
                    depItemCount++;
                }

                if (centralDelegator!.GetWFCHandler().IsOverlappingModel()) {
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

                return (true, depItemCount);
            }
        }

        return (false, depItemCount);
    }

    /// <summary>
    ///     Check and place the dependent item if possible
    /// </summary>
    /// <param name="xLoc">X location to place at</param>
    /// <param name="yLoc">Y location to place at</param>
    /// <param name="ivm">Item object to place</param>
    /// <param name="itemGrid">Current item grid</param>
    /// <param name="distinctColourCount">List of distinct colours</param>
    /// <param name="allowedAdd">List of allowed tiles for the item to be placed on</param>
    /// <param name="distinctIndexCount">List of distinct tile indices to place on</param>
    /// <param name="depItemCount">ID count of the dependent item identifier</param>
    /// <param name="spacesLeft">Amount of spaces left for each tile type</param>
    /// <returns>Whether the dependent item was correctly placed, the updated space availability list and new item grid</returns>
    private (bool, IList<int>, Tuple<string, int>[,]) TryPlaceDependencyAt(int xLoc, int yLoc, ItemObjectViewModel ivm,
        Tuple<string, int>[,] itemGrid,
        Color[,] distinctColourCount, ICollection<int> allowedAdd
        , int[,] distinctIndexCount, int depItemCount, IList<int> spacesLeft) {
        Tuple<string, (int, int)> depItem = ivm.DependentItem!;
        List<Tuple<int, int>> possibleCoordinates = new();
        (int minDist, int maxDist) = depItem.Item2;

        for (int xx = Math.Max(0, xLoc - maxDist);
             xx < Math.Min(mainWindowViewModel.ImageOutWidth - 1, xLoc + maxDist);
             xx++) {
            for (int yy = Math.Max(0, yLoc - maxDist);
                 yy < Math.Min(mainWindowViewModel.ImageOutHeight - 1, yLoc + maxDist);
                 yy++) {
                double depDist = Math.Sqrt((xx - xLoc) * (xx - xLoc) + (yy - yLoc) * (yy - yLoc));
                if (depDist <= maxDist && depDist >= minDist) {
                    if (centralDelegator!.GetWFCHandler().IsOverlappingModel()) {
                        Color colorAtPos = distinctColourCount[xx, yy];
                        if (mainWindowViewModel.PaintTiles.Where(tvm => tvm.PatternColour.Equals(colorAtPos)).Any(tvm => allowedAdd.Contains(tvm.PatternIndex)) 
                            && itemGrid[xx, yy].Item1.Equals("") && ivm.AppearanceRegion[xx, yy]) {
                            possibleCoordinates.Add(new Tuple<int, int>(xx, yy));
                        }
                    } else {
                        int valAtPos = distinctIndexCount[xx, yy];
                        if (allowedAdd.Contains(valAtPos) && itemGrid[xx, yy].Item1.Equals("") &&
                            ivm.AppearanceRegion[xx, yy]) {
                            possibleCoordinates.Add(new Tuple<int, int>(xx, yy));
                        }
                    }
                }
            }
        }

        bool canContinueDependent = possibleCoordinates.Count > 0;

        if (canContinueDependent) {
            Tuple<int, int> depCoords = possibleCoordinates[mainWindowViewModel.R.Next(possibleCoordinates.Count)];
            itemGrid[depCoords.Item1, depCoords.Item2] = new Tuple<string, int>(depItem.Item1!, depItemCount);

            if (centralDelegator!.GetWFCHandler().IsOverlappingModel()) {
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

        return (canContinueDependent, spacesLeft, itemGrid);
    }

    /// <summary>
    ///     Function called to reset the items in the image
    /// </summary>
    public void ResetDataGrid() {
        ItemDataGrid = new ObservableCollection<ItemObjectViewModel>();
    }

    /// <summary>
    ///     Called when applying the region mask
    /// </summary>
    public void ApplyRegionEditor() {
        ExitRegionEditorWithReset(false);
    }

    /// <summary>
    ///     Function called when entering the item spawn region editor
    /// </summary>
    public void EnterRegionEditor() {
        InRegionDefineMenu = true;
    }

    /// <summary>
    ///     Function called when exiting the item spawn region editor
    /// </summary>
    public void ExitRegionEditor() {
        ExitRegionEditorWithReset(true);
    }

    /// <summary>
    ///     Function called when exiting the item spawn region editor
    /// </summary>
    /// <param name="resetMask">Whether to reset the mask</param>
    public void ExitRegionEditorWithReset(bool resetMask) {
        InRegionDefineMenu = false;
        if (resetMask) {
            centralDelegator!.GetItemWindow().GetRegionDefineMenu().ResetAllowanceMask();
        }
    }
}