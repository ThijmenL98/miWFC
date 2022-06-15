using System.Collections.ObjectModel;
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

    private readonly string _itemName, _constraints;
    private string _amountStr;

#pragma warning disable CS8618
    public ItemViewModel(ItemType itemType, (int, int) amount, ObservableCollection<TileViewModel> allowedTiles,
        WriteableBitmap itemIcon) {
#pragma warning restore CS8618
        ItemType = itemType;
        Amount = amount;

        AmountStr = amount.Item1 == amount.Item2 ? @$"{amount.Item2}" : @$"{amount.Item1} - {amount.Item2}";
        AllowedTiles = allowedTiles;
        ItemIcon = itemIcon;

        Item = itemType.DisplayName;
        Constraints = "Allowed on: ";
    }

    public string Item {
        get => _itemName;
        private init => this.RaiseAndSetIfChanged(ref _itemName, value);
    }

    public string Constraints {
        get => _constraints;
        private init => this.RaiseAndSetIfChanged(ref _constraints, value);
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
}