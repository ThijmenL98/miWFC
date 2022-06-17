using System;
using System.Collections.Generic;
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
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels;

namespace miWFC.ContentControls;

public partial class ItemAddMenu : UserControl {
    private const int Dimension = 17;

    private readonly ComboBox _itemsCB, _depsCB;

    private readonly Dictionary<int, WriteableBitmap> imageCache;
    private CentralManager? centralManager;

    private bool[] checkBoxes;

    public ItemAddMenu() {
        InitializeComponent();

        imageCache = new Dictionary<int, WriteableBitmap>();

        _itemsCB = this.Find<ComboBox>("itemTypesCB");
        _depsCB = this.Find<ComboBox>("itemDependenciesCB");

        _itemsCB.Items = ItemType.itemTypes;
        _depsCB.Items = ItemType.itemTypes;

        _itemsCB.SelectedIndex = 0;
        _depsCB.SelectedIndex = 1;

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

    public void updateSelectedItemIndex(int idx = 0) {
        _itemsCB.SelectedIndex = idx;
    }

    public void updateDependencyIndex(int idx = 0) {
        _depsCB.SelectedIndex = idx;

        if (_itemsCB.SelectedIndex.Equals(idx)) {
            _depsCB.SelectedIndex = idx == 0 ? 1 : 0;
        }
    }

    public WriteableBitmap getItemImage(ItemType itemType, int index = -1) {
        if (imageCache.ContainsKey(itemType.ID)) {
            return imageCache[itemType.ID];
        }

        WriteableBitmap outputBitmap = new(new PixelSize(Dimension, Dimension), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();
        Color[] rawColours = Util.getItemImageRaw(itemType, index);

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
    } // ReSharper disable twice UnusedParameter.Local
    private void OnItemChanged(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        int index = _itemsCB.SelectedIndex;
        if (index >= 0) {
            ItemType selection = ItemType.getItemTypeByID(index);
            centralManager!.getMainWindowVM().CurrentItemImage = getItemImage(selection);
            centralManager!.getMainWindowVM().ItemDescription = selection.Description;

            if (_depsCB.SelectedIndex.Equals(index)) {
                _depsCB.SelectedIndex = index == 0 ? 1 : 0;
            }
        }
    }

    // ReSharper disable twice UnusedParameter.Local
    private void OnDependencyChanged(object? sender, SelectionChangedEventArgs e) {
        updateDependencyIndex(_depsCB.SelectedIndex);
        // TODO
    }

    public ItemType getSelectedItemType() {
        return ItemType.getItemTypeByID(_itemsCB.SelectedIndex);
    }

    public ItemType getDependencyItemType() {
        return ItemType.getItemTypeByID(_depsCB.SelectedIndex);
    }

    public void forwardCheckChange(int i, bool allowed) {
        checkBoxes[i] = allowed;
    }

    public IEnumerable<bool> getAllowedTiles() {
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

    private void AmountRange_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        if (e.Source is NumericUpDown source) {
            switch (source.Name!) {
                case "NUDUpper":
                    centralManager!.getMainWindowVM().ItemsToAddUpper
                        = (int) this.Find<NumericUpDown>("NUDUpper").Value;

                    while (centralManager!.getMainWindowVM().ItemsToAddUpper
                           <= centralManager!.getMainWindowVM().ItemsToAddLower) {
                        centralManager!.getMainWindowVM().ItemsToAddLower--;
                        if (centralManager!.getMainWindowVM().ItemsToAddLower <= 0) {
                            centralManager!.getMainWindowVM().ItemsToAddLower = 1;
                            centralManager!.getMainWindowVM().ItemsToAddUpper++;
                        }
                    }

                    break;

                case "NUDLower":
                    centralManager!.getMainWindowVM().ItemsToAddLower
                        = (int) this.Find<NumericUpDown>("NUDLower").Value;

                    if (centralManager!.getMainWindowVM().ItemsToAddLower <= 0) {
                        centralManager!.getMainWindowVM().ItemsToAddLower = 1;
                    }

                    if (centralManager!.getMainWindowVM().ItemsToAddUpper
                        <= centralManager!.getMainWindowVM().ItemsToAddLower) {
                        centralManager!.getMainWindowVM().ItemsToAddUpper++;
                        if (centralManager!.getMainWindowVM().ItemsToAddLower <= 0) {
                            centralManager!.getMainWindowVM().ItemsToAddLower = 1;
                            centralManager!.getMainWindowVM().ItemsToAddUpper++;
                        }
                    }

                    break;
            }
        }
    }

    private void DependencyDistance_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        if (e.Source is NumericUpDown source) {
            switch (source.Name!) {
                case "NUDMaxDist":
                    centralManager!.getMainWindowVM().DepMaxDistance
                        = (int) this.Find<NumericUpDown>("NUDMaxDist").Value;

                    while (centralManager!.getMainWindowVM().DepMaxDistance
                           <= centralManager!.getMainWindowVM().DepMinDistance) {
                        centralManager!.getMainWindowVM().DepMinDistance--;
                        if (centralManager!.getMainWindowVM().DepMinDistance <= 0) {
                            centralManager!.getMainWindowVM().DepMinDistance = 1;
                            centralManager!.getMainWindowVM().DepMaxDistance++;
                        }
                    }

                    break;

                case "NUDMinDist":
                    centralManager!.getMainWindowVM().DepMinDistance
                        = (int) this.Find<NumericUpDown>("NUDMinDist").Value;

                    if (centralManager!.getMainWindowVM().DepMinDistance <= 0) {
                        centralManager!.getMainWindowVM().DepMinDistance = 1;
                    }

                    if (centralManager!.getMainWindowVM().DepMaxDistance
                        <= centralManager!.getMainWindowVM().DepMinDistance) {
                        centralManager!.getMainWindowVM().DepMaxDistance++;
                        if (centralManager!.getMainWindowVM().DepMinDistance <= 0) {
                            centralManager!.getMainWindowVM().DepMinDistance = 1;
                            centralManager!.getMainWindowVM().DepMaxDistance++;
                        }
                    }

                    break;
            }
        }
    }
}