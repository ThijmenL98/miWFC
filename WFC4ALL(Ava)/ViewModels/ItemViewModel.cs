using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using ReactiveUI;
using WFC4ALL.Utils;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace WFC4ALL.ViewModels; 

public class ItemViewModel : ReactiveObject {

    private readonly ItemType _itemType;
    private int _amount;
    private readonly ObservableCollection<TileViewModel> _allowedTiles;

    private readonly string _itemName, _constraints;
    
#pragma warning disable CS8618
    public ItemViewModel(ItemType itemType, int amount, ObservableCollection<TileViewModel> allowedTiles) {
#pragma warning restore CS8618
        Trace.WriteLine(@$"To add: {itemType.DisplayName} {amount} {string.Join(", ", allowedTiles.Select(model => model.PatternIndex))}");

        ItemType = itemType;
        Amount = amount;
        AllowedTiles = allowedTiles;

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
    
    public ItemType ItemType {
        get => _itemType;
        private init => this.RaiseAndSetIfChanged(ref _itemType, value);
    }

    public int Amount {
        get => _amount;
        set => this.RaiseAndSetIfChanged(ref _amount, value);
    }

    public ObservableCollection<TileViewModel> AllowedTiles {
        get => _allowedTiles;
        private init => this.RaiseAndSetIfChanged(ref _allowedTiles, value);
    }
}