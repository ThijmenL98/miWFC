using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using miWFC.Utils;
using ReactiveUI;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace miWFC.ViewModels;

public class ItemViewModel : ReactiveObject {
    private readonly ItemType _itemType;
    private (int, int) _amount;
    private readonly ObservableCollection<TileViewModel> _allowedTiles;
    private readonly WriteableBitmap _itemIcon;
    private readonly WriteableBitmap? _depItemIcon;
    private readonly Tuple<ItemType?, (int, int)> _dependentItem;
    private readonly bool _hasDependentItem;

    private readonly string _itemName, _depItemString;
    private string _amountStr;

#pragma warning disable CS8618
    public ItemViewModel(ItemType itemType, (int, int) amount, ObservableCollection<TileViewModel> allowedTiles,
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
            DepItemString = $@"Appear distance Min: {dependentItem.Item3.Item1} - Max: {dependentItem.Item3.Item2}";
        } else {
            DependentItem = new Tuple<ItemType?, (int, int)>(null, (0, 0));
            DepItemIcon = null;
            HasDependentItem = false;
            DepItemString = "Nothing";
        }

        Item = itemType.DisplayName;
    }

    public string Item {
        get => _itemName;
        private init => this.RaiseAndSetIfChanged(ref _itemName, value);
    }

    public string DepItemString {
        get => _depItemString;
        private init => this.RaiseAndSetIfChanged(ref _depItemString, value);
    }

    public string AmountStr {
        get => _amountStr;
        set => this.RaiseAndSetIfChanged(ref _amountStr, value);
    }

    public ItemType ItemType {
        get => _itemType;
        private init => this.RaiseAndSetIfChanged(ref _itemType, value);
    }

    public (int, int) Amount {
        get => _amount;
        set => this.RaiseAndSetIfChanged(ref _amount, value);
    }

    public ObservableCollection<TileViewModel> AllowedTiles {
        get => _allowedTiles;
        private init => this.RaiseAndSetIfChanged(ref _allowedTiles, value);
    }

    public WriteableBitmap ItemIcon {
        get => _itemIcon;
        private init => this.RaiseAndSetIfChanged(ref _itemIcon, value);
    }

    public WriteableBitmap? DepItemIcon {
        get => _depItemIcon;
        private init => this.RaiseAndSetIfChanged(ref _depItemIcon, value);
    }

    public Tuple<ItemType?, (int, int)> DependentItem {
        get => _dependentItem;
        private init => this.RaiseAndSetIfChanged(ref _dependentItem, value);
    }

    public bool HasDependentItem {
        get => _hasDependentItem;
        private init => this.RaiseAndSetIfChanged(ref _hasDependentItem, value);
    }
}