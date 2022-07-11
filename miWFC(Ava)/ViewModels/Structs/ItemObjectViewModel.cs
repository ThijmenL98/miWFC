using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
// ReSharper disable UnusedMember.Local

namespace miWFC.ViewModels.Structs;

/// <summary>
/// View model for the items in the item addition side of the application
/// </summary>
public class ItemObjectViewModel : ReactiveObject {
    private readonly ObservableCollection<TileViewModel> _allowedTiles;
    private readonly WriteableBitmap? _depItemIcon;
    private readonly bool _hasDependentItem;
    private readonly WriteableBitmap _itemIcon, _itemLocationBM;

    private readonly string itemNameName, _depItemString;
    private readonly (int, int) _amount;
    private readonly string _amountStr;
    private readonly Tuple<string?, (int, int)> _dependentItem;
    private readonly Color _color;
    private readonly Color? _depColor;
    private bool[,] _appearanceRegion;

    /*
     * Initializing Functions & Constructor
     */

#pragma warning disable CS8618
    public ItemObjectViewModel(string itemNameName, (int, int) amount, ObservableCollection<TileViewModel> allowedTiles, Color c,
        WriteableBitmap itemIcon, Tuple<string?, WriteableBitmap?, Color?,(int, int)>? dependentItem, bool[,] appRegion, WriteableBitmap locMapping) {
#pragma warning restore CS8618
        Amount = amount;

        AppearanceRegion = appRegion;
        
        AmountStr = amount.Item1 == amount.Item2 ? @$"{amount.Item2}" : @$"{amount.Item1} - {amount.Item2}";
        AllowedTiles = allowedTiles;
        ItemIcon = itemIcon;

        MyColor = c;

        ItemLocationMapping = locMapping;

        if (dependentItem != null) {
            DependentItem = new Tuple<string?, (int, int)>(dependentItem.Item1, dependentItem.Item4);
            DepItemIcon = dependentItem.Item2;
            HasDependentItem = true;
            DepColor = dependentItem.Item3;
            DepItemString = $@"Appearing distance: {dependentItem.Item4.Item1} to {dependentItem.Item4.Item2}";
        } else {
            DependentItem = new Tuple<string?, (int, int)>(null, (0, 0));
            DepItemIcon = null;
            HasDependentItem = false;
            DepItemString = "Nothing";
        }

        ItemName = itemNameName;
    }

    /*
     * Getters & Setters
     */

    // Strings

    /// <summary>
    /// Name of the item
    /// </summary>
    public string ItemName {
        get => itemNameName;
        private init => this.RaiseAndSetIfChanged(ref itemNameName, value);
    }

    /// <summary>
    /// Name of the dependent item
    /// </summary>
    private string DepItemString {
        get => _depItemString;
        init => this.RaiseAndSetIfChanged(ref _depItemString, value);
    }

    /// <summary>
    /// String representation of the amount of appearance
    /// </summary>
    private string AmountStr {
        get => _amountStr;
        init => this.RaiseAndSetIfChanged(ref _amountStr, value);
    }

    // Numeric (Integer, Double, Float, Long ...)

    // Booleans

    /// <summary>
    /// Whether the item has an associated dependent item
    /// </summary>
    public bool HasDependentItem {
        get => _hasDependentItem;
        private init => this.RaiseAndSetIfChanged(ref _hasDependentItem, value);
    }

    // Images

    /// <summary>
    /// Image of the item
    /// </summary>
    private WriteableBitmap ItemIcon {
        get => _itemIcon;
        init => this.RaiseAndSetIfChanged(ref _itemIcon, value);
    }

    /// <summary>
    /// Image of the dependent item
    /// </summary>
    private WriteableBitmap? DepItemIcon {
        get => _depItemIcon;
        init => this.RaiseAndSetIfChanged(ref _depItemIcon, value);
    }

    /// <summary>
    /// Image of the locational mapping
    /// </summary>
    private WriteableBitmap ItemLocationMapping {
        get => _itemLocationBM;
        init => this.RaiseAndSetIfChanged(ref _itemLocationBM, value);
    }

    // Objects

    /// <summary>
    /// Colour of this item image
    /// </summary>
    public Color MyColor {
        get => _color;
        private init => this.RaiseAndSetIfChanged(ref _color, value);
    }

    /// <summary>
    /// Colour of the dependent item image
    /// </summary>
    public Color? DepColor {
        get => _depColor;
        private init => this.RaiseAndSetIfChanged(ref _depColor, value);
    }

    // Lists

    /// <summary>
    /// List of all tiles this item is allowed to be hosted on
    /// </summary>
    public ObservableCollection<TileViewModel> AllowedTiles {
        get => _allowedTiles;
        private init => this.RaiseAndSetIfChanged(ref _allowedTiles, value);
    }

    /// <summary>
    /// Where the items are allowed to appear
    /// </summary>
    public bool[,] AppearanceRegion {
        get => _appearanceRegion;
        private init => this.RaiseAndSetIfChanged(ref _appearanceRegion, value);
    }

    // Other

    /// <summary>
    /// Appearance amount tuple
    /// (x, _) - Represents the minimum amount of appearing
    /// (_, x) - Represents the maximum amount of appearing
    /// </summary>
    public (int, int) Amount {
        get => _amount;
        private init => this.RaiseAndSetIfChanged(ref _amount, value);
    }

    /// <summary>
    /// Storage of the dependent item, and the appearance distance
    /// T(x, (_, _)) - Represents the item name of the dependent item
    /// T(_, (x, _)) - Represents the minimum distance of this dependent item appearing from the main item
    /// T(_, (_, x)) - Represents the maximum distance of this dependent item appearing from the main item
    /// </summary>
    public Tuple<string?, (int, int)> DependentItem {
        get => _dependentItem;
        private init => this.RaiseAndSetIfChanged(ref _dependentItem, value);
    }

    /*
     * UI Callbacks
     */
}