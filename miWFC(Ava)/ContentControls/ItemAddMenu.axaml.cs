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

/// <summary>
/// Separated control for the item addition menu
/// </summary>
public partial class ItemAddMenu : UserControl {
    private const int Dimension = 17;

    private readonly ComboBox _itemsCB, _depsCB;

    private readonly Dictionary<int, WriteableBitmap> imageCache;
    private CentralManager? centralManager;

    private bool[] allowedTiles;

    /*
     * Initializing Functions & Constructor
     */
    public ItemAddMenu() {
        InitializeComponent();

        imageCache = new Dictionary<int, WriteableBitmap>();

        _itemsCB = this.Find<ComboBox>("itemTypesCB");
        _depsCB = this.Find<ComboBox>("itemDependenciesCB");

        _itemsCB.Items = ItemType.itemTypes;
        _depsCB.Items = ItemType.itemTypes;

        _itemsCB.SelectedIndex = 0;
        _depsCB.SelectedIndex = 1;

        allowedTiles = Array.Empty<bool>();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void setCentralManager(CentralManager cm) {
        centralManager = cm;

        allowedTiles = new bool[centralManager!.getMainWindowVM().PaintTiles.Count];
    }

    /// <summary>
    /// Reset the allowed tiles array, which has a boolean for each tile whether they are allowed to host the item
    /// </summary>
    public void updateAllowedTiles() {
        allowedTiles = new bool[centralManager!.getMainWindowVM().PaintTiles.Count];
    }

    /*
     * Getters
     */

    /// <summary>
    /// Return the image form of an item
    /// </summary>
    /// 
    /// <param name="itemType">Currently selected item</param>
    /// <param name="index">Item dependency index, if this item is dependent on another item the index will be
    /// embedded within the image</param>
    /// 
    /// <returns>Item image</returns>
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
    }

    /// <summary>
    /// Get the currently selected main item
    /// </summary>
    /// 
    /// <returns>The dependent item object</returns>
    public ItemType getSelectedItemType() {
        return ItemType.getItemTypeByID(_itemsCB.SelectedIndex);
    }

    /// <summary>
    /// Get the currently selected dependent item
    /// </summary>
    /// 
    /// <returns>The dependent item object</returns>
    public ItemType getDependencyItemType() {
        return ItemType.getItemTypeByID(_depsCB.SelectedIndex);
    }

    /// <summary>
    /// Get the array of allowed tiles the main item is allowed to be hosted on
    /// </summary>
    /// 
    /// <returns>Array of booleans, indexed by tile indices</returns>
    public IEnumerable<bool> getAllowedTiles() {
        return allowedTiles;
    }

    /*
     * Setters
     */

    /// <summary>
    /// Update the currently selected item 
    /// </summary>
    /// 
    /// <param name="idx">Index, default is the first item</param>
    public void updateSelectedItemIndex(int idx = 0) {
        _itemsCB.SelectedIndex = idx;
    }

    /// <summary>
    /// Update the item dependencies combobox
    /// </summary>
    /// 
    /// <param name="idx">The to be selected index after initialization, default is the first item</param>
    public void updateDependencyIndex(int idx = 0) {
        _depsCB.SelectedIndex = idx;

        if (_itemsCB.SelectedIndex.Equals(idx)) {
            _depsCB.SelectedIndex = idx == 0 ? 1 : 0;
        }
    }

    /// <summary>
    /// Set the tile at index i to be allowed or not
    /// </summary>
    /// 
    /// <param name="idx">Index</param>
    /// <param name="allowed">Whether to allow this tile to be the host of the item with index i</param>
    public void forwardAllowedTileChange(int idx, bool allowed) {
        allowedTiles[idx] = allowed;
    }

    /*
     * UI Callbacks
     */

    /// <summary>
    /// Callback when the selected main item is changed. This updates the image shown to the user, as well as the
    /// description and resets the dependency index
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">SelectionChangedEventArgs</param>
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

            if (_depsCB.SelectedIndex.Equals(index)) {
                _depsCB.SelectedIndex = index == 0 ? 1 : 0;
            }
        }
    }

    /// <summary>
    /// Instead of only allowing the user to click on the physical check box element, clicking on the tile image
    /// itself also causes the checkbox to be toggled, this function forwards this UI click.
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">PointerReleasedEventArgs</param>
    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e) {
        TileViewModel? clickedTvm
            = ((sender as Border)?.Parent?.Parent as ContentPresenter)?.Content as TileViewModel ?? null;
        if (clickedTvm != null) {
            clickedTvm.ItemAddChecked = !clickedTvm.ItemAddChecked;
            clickedTvm.ForwardSelectionToggle();
        }
    }

    /// <summary>
    /// Callback when the amount of item appearances in the output has changed.
    /// This function makes sure that the upper range does not drop below the lower range and vice versa.
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">NumericUpDownValueChangedEventArgs</param>
    // ReSharper disable trice SuggestBaseTypeForParameter
    private void AmountRange_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        if (e.Source is NumericUpDown source) {
            switch (source.Name!) {
                case "NUDUpper":
                    centralManager!.getMainWindowVM().ItemsToAddUpper
                        = (int) this.Find<NumericUpDown>("NUDUpper").Value;

                    while (centralManager!.getMainWindowVM().ItemsToAddUpper <=
                           centralManager!.getMainWindowVM().ItemsToAddLower) {
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

                    if (centralManager!.getMainWindowVM().ItemsToAddUpper <=
                        centralManager!.getMainWindowVM().ItemsToAddLower) {
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

    /// <summary>
    /// Callback when the distance of dependent item appearances has changed.
    /// This function makes sure that the max distance does not drop below the min distance and vice versa.
    /// </summary>
    /// 
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">NumericUpDownValueChangedEventArgs</param>
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