using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WFC4ALL.Managers;
using WFC4ALL.Utils;

namespace WFC4ALL.ContentControls;

public partial class ItemAddMenu : UserControl {
    private CentralManager? centralManager;

    private readonly ComboBox _itemsCB;

    private readonly HashSet<Tuple<int, int>> border;

    private readonly Dictionary<int, WriteableBitmap> imageCache;

    private const int Dimension = 17;
    private const double Radius = 6d;

    public ItemAddMenu() {
        InitializeComponent();

        imageCache = new Dictionary<int, WriteableBitmap>();
        border = new HashSet<Tuple<int, int>>();
        for (double i = 0; i < 360; i += 5) {
            int x1 = (int) (Radius * Math.Cos(i * Math.PI / 180d) + Dimension / 2d);
            int y1 = (int) (Radius * Math.Sin(i * Math.PI / 180d) + Dimension / 2d);
            border.Add(new Tuple<int, int>(x1, y1));
        }

        _itemsCB = this.Find<ComboBox>("itemTypesCB");

        _itemsCB.Items = ItemType.ItemTypes;
        _itemsCB.SelectedIndex = 0;
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;
    }

    public void updateIndex() {
        _itemsCB.SelectedIndex = 0;
    }

    private WriteableBitmap getItemImage(ItemType itemType) {
        // TODO : add numbers for dependencies if necessary
        
        if (imageCache.ContainsKey(itemType.ID)) { return imageCache[itemType.ID]; }

        WriteableBitmap outputBitmap = new(new PixelSize(Dimension, Dimension), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Premul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, Dimension, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < Dimension; x++) {
                    var distance = Math.Sqrt((Math.Pow(x - 8d, 2d) + Math.Pow(y - 8d, 2d)));
                    Color toSet;
                    if (distance < Radius + 0.5d) {
                        Tuple<int, int> key = new(x, (int) y);
                        toSet = border.Contains(key) ? Colors.Black : itemType.Color;
                    } else {
                        toSet = Colors.Transparent;
                    }

                    dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);
                }
            });
        }

        imageCache[itemType.ID] = outputBitmap;

        return outputBitmap;
    }

    private void OnItemChanged(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        int index = _itemsCB.SelectedIndex;
        if (index >= 0) {
            ItemType selection = ItemType.getItemTypeByID(index);
            centralManager!.getMainWindowVM().CurrentItemImage = getItemImage(selection);
        }
    }
}