﻿using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using miWFC.Managers;
using miWFC.Utils;
using miWFC.ViewModels.Structs;

// ReSharper disable SuggestBaseTypeForParameter

// ReSharper disable UnusedParameter.Local

namespace miWFC.ContentControls;

/// <summary>
/// Separated control for the item addition menu
/// </summary>
public partial class ItemAddMenu : UserControl {
    private readonly ComboBox _itemsCB, _depsCB;

    private CentralManager? centralManager;

    private bool[] allowedTiles;

    /*
     * Initializing Functions & Constructor
     */
    public ItemAddMenu() {
        InitializeComponent();
        
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

    public void SetCentralManager(CentralManager cm) {
        centralManager = cm;

        allowedTiles = new bool[centralManager!.GetMainWindowVM().PaintTiles.Count];
    }

    /// <summary>
    /// Reset the allowed tiles array, which has a boolean for each tile whether they are allowed to host the item
    /// </summary>
    public void UpdateAllowedTiles() {
        allowedTiles = new bool[centralManager!.GetMainWindowVM().PaintTiles.Count];
    }

    /*
     * Getters
     */

    /// <summary>
    /// Get the currently selected main item
    /// </summary>
    /// 
    /// <returns>The dependent item object</returns>
    public ItemType GetSelectedItemType() {
        return ItemType.GetItemTypeById(_itemsCB.SelectedIndex);
    }

    /// <summary>
    /// Get the currently selected dependent item
    /// </summary>
    /// 
    /// <returns>The dependent item object</returns>
    public ItemType GetDependencyItemType() {
        return ItemType.GetItemTypeById(_depsCB.SelectedIndex);
    }

    /// <summary>
    /// Get the array of allowed tiles the main item is allowed to be hosted on
    /// </summary>
    /// 
    /// <returns>Array of booleans, indexed by tile indices</returns>
    public IEnumerable<bool> GetAllowedTiles() {
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
    public void UpdateSelectedItemIndex(int idx = 0) {
        _itemsCB.SelectedIndex = idx;
    }

    /// <summary>
    /// Update the item dependencies combobox
    /// </summary>
    /// 
    /// <param name="idx">The to be selected index after initialization, default is the first item</param>
    public void UpdateDependencyIndex(int idx = 0) {
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
    public void ForwardAllowedTileChange(int idx, bool allowed) {
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
    private void OnItemChanged(object? sender, SelectionChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        int index = _itemsCB.SelectedIndex;
        if (index >= 0) {
            ItemType selection = ItemType.GetItemTypeById(index);
            centralManager!.GetMainWindowVM().ItemVM.CurrentItemImage = Util.GetItemImage(selection);
            centralManager!.GetMainWindowVM().ItemVM.ItemDescription = selection.Description;

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
    private void AmountRange_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (centralManager == null) {
            return;
        }

        if (e.Source is NumericUpDown source) {
            int oldUpper = centralManager!.GetMainWindowVM().ItemVM.ItemsToAddUpper;
            int oldLower = centralManager!.GetMainWindowVM().ItemVM.ItemsToAddLower;
            bool lowerAdapted = source.Name!.Equals("NUDLower");

            (int newLower, int newUpper) = Util.BalanceValues(oldLower, oldUpper,
                lowerAdapted
                    ? (int) this.Find<NumericUpDown>("NUDLower").Value - oldLower
                    : (int) this.Find<NumericUpDown>("NUDUpper").Value - oldUpper, lowerAdapted);

            centralManager!.GetMainWindowVM().ItemVM.ItemsToAddUpper = newUpper;
            centralManager!.GetMainWindowVM().ItemVM.ItemsToAddLower = newLower;

            this.Find<NumericUpDown>("NUDUpper").Value = newUpper;
            this.Find<NumericUpDown>("NUDLower").Value = newLower;
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
            int oldMax = centralManager!.GetMainWindowVM().ItemVM.DepMaxDistance;
            int oldMin = centralManager!.GetMainWindowVM().ItemVM.DepMinDistance;
            bool minAdapted = source.Name!.Equals("NUDMinDist");

            (int newMin, int newMax) = Util.BalanceValues(oldMin, oldMax,
                minAdapted
                    ? (int) this.Find<NumericUpDown>("NUDMinDist").Value - oldMin
                    : (int) this.Find<NumericUpDown>("NUDMaxDist").Value - oldMax, minAdapted);

            centralManager!.GetMainWindowVM().ItemVM.DepMaxDistance = newMax;
            centralManager!.GetMainWindowVM().ItemVM.DepMinDistance = newMin;

            this.Find<NumericUpDown>("NUDMaxDist").Value = newMax;
            this.Find<NumericUpDown>("NUDMinDist").Value = newMin;
        }
    }
}