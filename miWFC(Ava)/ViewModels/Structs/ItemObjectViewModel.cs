using System;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using miWFC.Utils;
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
    private readonly WriteableBitmap _itemIcon;

    private readonly string _itemName, _depItemString;
    private readonly ItemType _itemType;
    private readonly (int, int) _amount;
    private readonly string _amountStr;
    private readonly Tuple<ItemType?, (int, int)> _dependentItem;

    /*
     * Initializing Functions & Constructor
     */

#pragma warning disable CS8618
    public ItemObjectViewModel(ItemType itemType, (int, int) amount, ObservableCollection<TileViewModel> allowedTiles,
        WriteableBitmap itemIcon, Tuple<ItemType?, WriteableBitmap?, (int, int)>? dependentItem) {
#pragma warning restore CS8618
        ItemType = itemType;
        Amount = amount;

        AmountStr = amount.Item1 == amount.Item2 ? @$"{amount.Item2}" : @$"{amount.Item1} - {amount.Item2}";
        AllowedTiles = allowedTiles;
        ItemIcon = itemIcon;

        if (dependentItem != null) {
            DependentItem = new Tuple<ItemType?, (int, int)>(dependentItem.Item1, dependentItem.Item3);
            DepItemIcon = dependentItem.Item2;
            HasDependentItem = true;
            DepItemString = $@"Appearing distance: {dependentItem.Item3.Item1} to {dependentItem.Item3.Item2}";
        } else {
            DependentItem = new Tuple<ItemType?, (int, int)>(null, (0, 0));
            DepItemIcon = null;
            HasDependentItem = false;
            DepItemString = "Nothing";
        }

        Item = itemType.DisplayName;
    }

    /*
     * Getters & Setters
     */

    // Strings

    /// <summary>
    /// Name of the item
    /// </summary>
    private string Item {
        get => _itemName;
        init => this.RaiseAndSetIfChanged(ref _itemName, value);
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

    // Objects

    /// <summary>
    /// The item type enumerator object of the main item
    /// </summary>
    public ItemType ItemType {
        get => _itemType;
        private init => this.RaiseAndSetIfChanged(ref _itemType, value);
    }

    // Lists

    /// <summary>
    /// List of all tiles this item is allowed to be hosted on
    /// </summary>
    public ObservableCollection<TileViewModel> AllowedTiles {
        get => _allowedTiles;
        private init => this.RaiseAndSetIfChanged(ref _allowedTiles, value);
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
    /// T(x, (_, _)) - Represents the item type enumerator object of the dependent item
    /// T(_, (x, _)) - Represents the minimum distance of this dependent item appearing from the main item
    /// T(_, (_, x)) - Represents the maximum distance of this dependent item appearing from the main item
    /// </summary>
    public Tuple<ItemType?, (int, int)> DependentItem {
        get => _dependentItem;
        private init => this.RaiseAndSetIfChanged(ref _dependentItem, value);
    }

    /*
     * UI Callbacks
     */
}