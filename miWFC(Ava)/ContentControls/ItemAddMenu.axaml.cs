﻿using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using miWFC.Delegators;
using miWFC.Utils;
using miWFC.ViewModels.Structs;

// ReSharper disable SuggestBaseTypeForParameter

// ReSharper disable UnusedParameter.Local

namespace miWFC.ContentControls;

/// <summary>
///     Separated control for the item addition menu
/// </summary>
public partial class ItemAddMenu : UserControl {
    private bool[] allowedTiles;
    private CentralDelegator? centralDelegator;

    /*
     * Initializing Functions & Constructor
     */
    public ItemAddMenu() {
        InitializeComponent();
        allowedTiles = Array.Empty<bool>();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetCentralDelegator(CentralDelegator cd) {
        centralDelegator = cd;

        allowedTiles = new bool[centralDelegator!.GetMainWindowVM().PaintTiles.Count];
    }

    /// <summary>
    ///     Reset the allowed tiles array, which has a boolean for each tile whether they are allowed to host the item
    /// </summary>
    public void UpdateAllowedTiles() {
        allowedTiles = new bool[centralDelegator!.GetMainWindowVM().PaintTiles.Count];
    }

    /*
     * Getters
     */

    /// <summary>
    ///     Get the array of allowed tiles the main item is allowed to be hosted on
    /// </summary>
    /// <returns>Array of booleans, indexed by tile indices</returns>
    public IEnumerable<bool> GetAllowedTiles() {
        return allowedTiles;
    }

    /// <summary>
    ///     Get the Avalonia Control for the item name
    /// </summary>
    /// <returns>Avalonia Control</returns>
    public TextBox GetItemNameTB() {
        return this.Find<TextBox>("itemName");
    }

    /// <summary>
    ///     Get the Avalonia Control for the dependent item name
    /// </summary>
    /// <returns>Avalonia Control</returns>
    public TextBox GetDepItemNameTB() {
        return this.Find<TextBox>("depItemName");
    }

    /// <summary>
    ///     Get the Avalonia Control for the item colour
    /// </summary>
    /// <returns>Avalonia Control</returns>
    public TextBox GetItemColourTB() {
        return this.Find<TextBox>("itemColour");
    }

    /// <summary>
    ///     Get the Avalonia Control for the dependent item colour
    /// </summary>
    /// <returns>Avalonia Control</returns>
    public TextBox GetDepItemColourTB() {
        return this.Find<TextBox>("depItemColour");
    }

    /*
     * Setters
     */

    /// <summary>
    ///     Set the tile at index i to be allowed or not
    /// </summary>
    /// <param name="idx">Index</param>
    /// <param name="allowed">Whether to allow this tile to be the host of the item with index i</param>
    public void ForwardAllowedTileChange(int idx, bool allowed) {
        allowedTiles[idx] = allowed;
    }

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Instead of only allowing the user to click on the physical check box element, clicking on the tile image
    ///     itself also causes the checkbox to be toggled, this function forwards this UI click.
    /// </summary>
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
    ///     Callback when the amount of item appearances in the output has changed.
    ///     This function makes sure that the upper range does not drop below the lower range and vice versa.
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">NumericUpDownValueChangedEventArgs</param>
    private void AmountRange_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        if (e.Source is NumericUpDown source) {
            int oldUpper = centralDelegator!.GetMainWindowVM().ItemVM.ItemsToAddUpper;
            int oldLower = centralDelegator!.GetMainWindowVM().ItemVM.ItemsToAddLower;
            bool lowerAdapted = source.Name!.Equals("NUDLower");

            (int newLower, int newUpper) = Util.BalanceValues(oldLower, oldUpper,
                lowerAdapted
                    ? (int) this.Find<NumericUpDown>("NUDLower").Value - oldLower
                    : (int) this.Find<NumericUpDown>("NUDUpper").Value - oldUpper, lowerAdapted);

            centralDelegator!.GetMainWindowVM().ItemVM.ItemsToAddUpper = newUpper;
            centralDelegator!.GetMainWindowVM().ItemVM.ItemsToAddLower = newLower;

            this.Find<NumericUpDown>("NUDUpper").Value = newUpper;
            this.Find<NumericUpDown>("NUDLower").Value = newLower;
        }
    }

    /// <summary>
    ///     Callback when the distance of dependent item appearances has changed.
    ///     This function makes sure that the max distance does not drop below the min distance and vice versa.
    /// </summary>
    /// <param name="sender">UI Origin of function call</param>
    /// <param name="e">NumericUpDownValueChangedEventArgs</param>
    private void DependencyDistance_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (centralDelegator == null) {
            return;
        }

        if (e.Source is NumericUpDown source) {
            int oldMax = centralDelegator!.GetMainWindowVM().ItemVM.DepMaxDistance;
            int oldMin = centralDelegator!.GetMainWindowVM().ItemVM.DepMinDistance;
            bool minAdapted = source.Name!.Equals("NUDMinDist");

            (int newMin, int newMax) = Util.BalanceValues(oldMin, oldMax,
                minAdapted
                    ? (int) this.Find<NumericUpDown>("NUDMinDist").Value - oldMin
                    : (int) this.Find<NumericUpDown>("NUDMaxDist").Value - oldMax, minAdapted);

            centralDelegator!.GetMainWindowVM().ItemVM.DepMaxDistance = newMax;
            centralDelegator!.GetMainWindowVM().ItemVM.DepMinDistance = newMin;

            this.Find<NumericUpDown>("NUDMaxDist").Value = newMax;
            this.Find<NumericUpDown>("NUDMinDist").Value = newMin;
        }
    }
}