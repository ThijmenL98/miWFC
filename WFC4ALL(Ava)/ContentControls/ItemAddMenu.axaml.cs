using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WFC4ALL.Managers;
using WFC4ALL.Utils;
using WFC4ALL.ViewModels;

namespace WFC4ALL.ContentControls;

public partial class ItemAddMenu : UserControl {
    private CentralManager? centralManager;

    private readonly ComboBox _itemsCB;

    private readonly HashSet<Tuple<int, int>> border;

    private readonly Dictionary<int, WriteableBitmap> imageCache;

    private const int Dimension = 17;
    private const double Radius = 6d;

    private bool[] checkBoxes;

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

        checkBoxes = Array.Empty<bool>();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;

        checkBoxes = new bool[centralManager!.getMainWindowVM().PaintTiles.Count];
    }

    public void updateCheckBoxesLength() {
        checkBoxes = new bool[centralManager!.getMainWindowVM().PaintTiles.Count];
    }

    public void updateIndex(int idx = 0) {
        _itemsCB.SelectedIndex = idx;
    }

    public Color[] getItemImageRaw(ItemType itemType, int index = -1) {
        bool singleDigit = false;
        bool[][] segments = Array.Empty<bool[]>();
        if (index != -1) {
            (singleDigit, segments) = getSegments(index);
        }

        Color[] rawColorData = new Color[Dimension * Dimension];
        int[] whiteBorders = {5, 7, 8, 9, 10};

        for (int x = 0; x < Dimension; x++) {
            for (int y = 0; y < Dimension; y++) {
                Color toSet;
                double distance = Math.Sqrt(Math.Pow(x - 8d, 2d) + Math.Pow(y - 8d, 2d));
                if (distance < Radius + 0.5d) {
                    Tuple<int, int> key = new(x, y);
                    toSet = border.Contains(key) ? whiteBorders.Contains(itemType.ID) ? Colors.White : Colors.Black : itemType.Color;
                } else {
                    toSet = Colors.Transparent;
                }

                if (index != -1) {
                    if (singleDigit) {
                        if (x is >= 7 and <= 9 && y is >= 6 and <= 10) {
                            int idx = (x - 7) % 3 + (y - 6) * 3;
                            if (segments[0][idx]) {
                                toSet = Colors.White;
                            }
                        }
                    } else {
                        // ReSharper disable once MergeIntoPattern
                        if (x >= 5 && x <= 11 && x != 8 && y is >= 6 and <= 10) {
                            if (x < 8) {
                                int idx = (x - 5) % 3 + (y - 6) * 3;
                                if (segments[1][idx]) {
                                    toSet = Colors.White;
                                }
                            } else {
                                int idx = (x - 9) % 3 + (y - 6) * 3;
                                if (segments[0][idx]) {
                                    toSet = Colors.White;
                                }
                            }
                        }
                    }
                }

                rawColorData[y * Dimension + x] = toSet;
            }
        }

        return rawColorData;
    }

    public WriteableBitmap getItemImage(ItemType itemType, int index = -1) {
        if (imageCache.ContainsKey(itemType.ID)) {
            return imageCache[itemType.ID];
        }

        WriteableBitmap outputBitmap = new(new PixelSize(Dimension, Dimension), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();
        Color[] rawColours = getItemImageRaw(itemType, index);

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, Dimension, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < Dimension; x++) {
                    Color toSet = rawColours[y % Dimension * Dimension + x % Dimension];

                    dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);
                }
            });
        }

        imageCache[itemType.ID] = outputBitmap;

        return outputBitmap;
    }

    private static (bool, bool[][]) getSegments(int i) {
        int digits = i <= 9 ? 1 : 2;
        bool[][] pixels = new bool[digits][];

        for (int d = 0; d < digits; d++) {
            pixels[d] = getSegment(d == 0 ? i % 10 : i / 10);
        }

        return (digits == 1, pixels);
    }

    private static bool[] getSegment(int i) {
        bool[] segment = {
            i != 1, i != 4, i != 1, i != 2 && i != 3 && i != 7, i == 1, i != 1 && i != 5 && i != 6, i != 1 && i != 7,
            i != 0 && i != 7, i != 1, i is 2 or 6 or 8 or 0, i == 1, i != 1 && i != 2, i != 4 && i != 7,
            i != 4 && i != 7,
            true
        };
        return segment;
    }

    // ReSharper disable twice UnusedParameter.Local
    private void OnItemChanged(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        int index = _itemsCB.SelectedIndex;
        if (index >= 0) {
            ItemType selection = ItemType.getItemTypeByID(index);
            centralManager!.getMainWindowVM().CurrentItemImage = getItemImage(selection);
            centralManager!.getMainWindowVM().ItemDescription = selection.Description;
        }
    }

    public ItemType getItemType() {
        return ItemType.getItemTypeByID(_itemsCB.SelectedIndex);
    }

    public void forwardCheckChange(int i, bool allowed) {
        checkBoxes[i] = allowed;
    }

    public bool[] getAllowedTiles() {
        return checkBoxes;
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e) {
        TileViewModel? clickedTvm
            = ((sender as Border)?.Parent?.Parent as ContentPresenter)?.Content as TileViewModel ?? null;
        if (clickedTvm != null) {
            clickedTvm.ItemAddChecked = !clickedTvm.ItemAddChecked;
            clickedTvm.OnCheckChange();
        }
    }
}