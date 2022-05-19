using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using WFC4ALL.Utils;

namespace WFC4ALL.ContentControls; 

public partial class ItemAddMenu : UserControl {
    
    private readonly ComboBox _itemsCB;
    
    public ItemAddMenu() {
        InitializeComponent();
        
        _itemsCB = this.Find<ComboBox>("itemTypesCB");
        
        _itemsCB.Items = ItemType.ItemTypes;
        _itemsCB.SelectedIndex = 0;
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void updateIndex() {
        _itemsCB.SelectedIndex = 0;
    }

    public WriteableBitmap getItemImage(ItemType itemType) {
        //TODO
        return null;
    }
}