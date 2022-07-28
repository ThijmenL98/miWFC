using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Delegators;
using ReactiveUI;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace miWFC.ViewModels.Structs;

/// <summary>
///     View model for the tiles used in the application, whether it be an overlapping pattern or an adjacent tile
/// </summary>
public class TileViewModel : ReactiveObject {
    private readonly bool _mayFlip, _mayRotate;
    private readonly Color _patternColour;
    private readonly WriteableBitmap _patternImage = null!;
    private readonly int _patternIndex, _patternRotation, _patternFlipping, _rawPatternIndex;

    private readonly int cardin = -1;

    private readonly CentralDelegator? centralDelegator;

    private bool _flipDisabled,
        _rotateDisabled,
        _highlighted,
        _itemAddChecked,
        _dynamicWeight;

    private bool _mayTransform,
        _patternDisabled;

    private double _patternWeight;
    private string _patternWeightString;
    private int _userRotation, _finalRotation, _userFlipping = 1, _finalFlipping = 1;

    private double[,] _weightHeatmap = new double[0, 0];

    /*
     * Initializing Functions & Constructor
     */

    // Used for input patterns
    public TileViewModel(WriteableBitmap image, double weight, int index, int rawIndex, CentralDelegator cd,
        int card) {
        PatternImage = image;
        PatternWeight = weight;
        PatternIndex = index;
        PatternRotation = 0;
        PatternFlipping = card > 4 ? -1 : 1;
        RawPatternIndex = rawIndex;

        cardin = card;

        centralDelegator = cd;

        MayFlip = card > 4;
        MayRotate = card > 1;
        MayTransform = MayFlip || MayRotate;

        FlipDisabled = false;
        RotateDisabled = false;
        DynamicWeight = false;

        _patternWeightString = DynamicWeight ? "D" :
            _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
    }

    // Used for Adjacent Tiles
    public TileViewModel(WriteableBitmap image, double weight, int index, int patternRotation, int patternFlipping,
        CentralDelegator cd) {
        PatternImage = image;
        PatternIndex = index;
        RawPatternIndex = -1;
        PatternWeight = weight;
        PatternRotation = patternRotation;
        PatternFlipping = patternFlipping;

        centralDelegator = cd;
        DynamicWeight = false;

        _patternWeightString = DynamicWeight ? "D" :
            _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
    }

    // Used for Overlapping Tiles
    public TileViewModel(WriteableBitmap image, int index, Color c, CentralDelegator cd) {
        PatternImage = image;
        PatternIndex = index;
        PatternColour = c;
        PatternRotation = 0;
        PatternFlipping = 1;
        centralDelegator = cd;
        DynamicWeight = false;

        _patternWeightString = DynamicWeight ? "D" :
            _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
    }

    /*
     * Getters & Setters
     */

    // Strings

    /// <summary>
    ///     String representation of the pattern weight value
    /// </summary>
    public string PatternWeightString {
        get => _patternWeightString;
        set => this.RaiseAndSetIfChanged(ref _patternWeightString, value);
    }

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    ///     Index of the pattern
    /// </summary>
    public int PatternIndex {
        get => _patternIndex;
        init => this.RaiseAndSetIfChanged(ref _patternIndex, value);
    }

    /// <summary>
    ///     Index of the pattern, set to -1 if this pattern is a transformed version of a parent pattern
    /// </summary>
    public int RawPatternIndex {
        get => _rawPatternIndex;
        init => this.RaiseAndSetIfChanged(ref _rawPatternIndex, value);
    }

    /// <summary>
    ///     Final pattern rotation used in the UI to show to the user, sum of the user rotation and the pattern's inherent
    ///     rotation
    /// </summary>
    public int FinalRotation {
        get => _finalRotation;
        set => this.RaiseAndSetIfChanged(ref _finalRotation, value);
    }

    /// <summary>
    ///     Final pattern flipping used in the UI to show to the user, XOR of the user flipping and the pattern's inherent
    ///     flipping
    /// </summary>
    public int FinalFlipping {
        get => _finalFlipping;
        set => this.RaiseAndSetIfChanged(ref _finalFlipping, value);
    }

    /// <summary>
    ///     User defined pattern rotation
    /// </summary>
    public int UserRotation {
        get => _userRotation;
        set {
            this.RaiseAndSetIfChanged(ref _userRotation, value);
            FinalRotation = _userRotation + _patternRotation;
        }
    }

    /// <summary>
    ///     User defined pattern flipping value
    /// </summary>
    public int UserFlipping {
        get => _userFlipping;
        set {
            this.RaiseAndSetIfChanged(ref _userFlipping, value);
            FinalFlipping = _userFlipping * _patternFlipping;
        }
    }

    /// <summary>
    ///     The pattern's inherent rotation
    /// </summary>
    public int PatternRotation {
        get => _patternRotation + 90;
        init {
            this.RaiseAndSetIfChanged(ref _patternRotation, value);
            FinalRotation = _userRotation + _patternRotation;
        }
    }

    /// <summary>
    ///     The pattern's inherent flipping value
    /// </summary>
    public int PatternFlipping {
        get => _patternFlipping;
        init {
            this.RaiseAndSetIfChanged(ref _patternFlipping, value);
            FinalFlipping = _userFlipping * _patternFlipping;
        }
    }

    /// <summary>
    ///     Weight of the pattern, extracted from the input image or overwritten by the user
    /// </summary>
    public double PatternWeight {
        get => _patternWeight;
        set {
            this.RaiseAndSetIfChanged(ref _patternWeight, value);
            PatternWeightString = DynamicWeight ? "D" :
                _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
        }
    }

    // Booleans

    /// <summary>
    ///     Whether this tile has their rotation(s) locked in the output.
    /// </summary>
    public bool RotateDisabled {
        get => _rotateDisabled;
        set {
            this.RaiseAndSetIfChanged(ref _rotateDisabled, value);
            UserRotation = 0;
            FinalRotation = _userRotation + _patternRotation;
        }
    }

    /// <summary>
    ///     Whether this tile has their flipping locked in the output.
    /// </summary>
    public bool FlipDisabled {
        get => _flipDisabled;
        set => this.RaiseAndSetIfChanged(ref _flipDisabled, value);
    }

    /// <summary>
    ///     Whether this tile is highlighted in the painting editor, which is true if the user has this tile selected on
    ///     their brush. This is added to more transparently indicate whether the selected tile may be placed beforehand.
    /// </summary>
    public bool Highlighted {
        get => _highlighted;
        set => this.RaiseAndSetIfChanged(ref _highlighted, value);
    }

    /// <summary>
    ///     Whether this item is selected to be a host for a user created item in the output.
    /// </summary>
    public bool ItemAddChecked {
        get => _itemAddChecked;
        set => this.RaiseAndSetIfChanged(ref _itemAddChecked, value);
    }

    /// <summary>
    ///     Whether the tile is, by default, allowed to rotate, this is only true for tiles with a cardinality > 1
    /// </summary>
    public bool MayRotate {
        get => _mayRotate;
        init {
            this.RaiseAndSetIfChanged(ref _mayRotate, value);
            MayTransform = MayFlip || MayRotate;
        }
    }

    /// <summary>
    ///     Whether the tile is, by default, allowed to be flipped, this is only true for tiles with a cardinality > 4
    /// </summary>
    public bool MayFlip {
        get => _mayFlip;
        init {
            this.RaiseAndSetIfChanged(ref _mayFlip, value);
            MayTransform = MayFlip || MayRotate;
        }
    }

    /// <summary>
    ///     Whether the tile is, by default, allowed to be transformed, AND operation between MayRotate and MayFlip
    /// </summary>
    public bool MayTransform {
        get => _mayTransform;
        set => this.RaiseAndSetIfChanged(ref _mayTransform, value);
    }

    /// <summary>
    ///     Whether this pattern is disabled, hence forcibly prohibited from appearing in the output
    /// </summary>
    public bool PatternDisabled {
        get => _patternDisabled;
        set => this.RaiseAndSetIfChanged(ref _patternDisabled, value);
    }

    /// <summary>
    ///     Whether this tile has a dynamic weight, meaning it is based on a map of values instead of a single flat value
    /// </summary>
    public bool DynamicWeight {
        get => _dynamicWeight;
        set {
            this.RaiseAndSetIfChanged(ref _dynamicWeight, value);
            PatternWeightString = DynamicWeight ? "D" :
                _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
        }
    }

    // Images

    /// <summary>
    ///     Image representation of the pattern or tile
    /// </summary>
    public WriteableBitmap PatternImage {
        get => _patternImage;
        init => this.RaiseAndSetIfChanged(ref _patternImage, value);
    }

    // Objects

    /// <summary>
    ///     The hex-code colour of the tile in the overlapping mode
    /// </summary>
    public Color PatternColour {
        get => _patternColour;
        init => this.RaiseAndSetIfChanged(ref _patternColour, value);
    }

    // Lists

    /// <summary>
    ///     If the user has selected dynamic weight mapping, this weight heatmap is used, having a distinct value for each
    ///     coordinate in the output.
    /// </summary>
    public double[,] WeightHeatMap {
        get => _weightHeatmap;
        set => this.RaiseAndSetIfChanged(ref _weightHeatmap, value);
    }

    // Other

    /*
     * UI Callbacks
     */

    /// <summary>
    ///     Function to handle a weight increase
    /// </summary>
    public void OnIncrement() {
        HandleWeightChange(true);
    }

    /// <summary>
    ///     Function to handle a weight decrease
    /// </summary>
    public void OnDecrement() {
        HandleWeightChange(false);
    }

    /// <summary>
    ///     Actual logic behind changing the weight of this tile.
    /// </summary>
    /// <param name="increment">Whether to increment (inverse decrement) the weight</param>
    private void HandleWeightChange(bool increment) {
        switch (increment) {
            case false when !(PatternWeight > 0):
                return;
            case true:
                PatternWeight += centralDelegator!.GetMainWindow().ChangeAmount;
                break;
            default:
                PatternWeight -= centralDelegator!.GetMainWindow().ChangeAmount;
                PatternWeight = Math.Max(0, PatternWeight);
                break;
        }

        PatternWeight = Math.Min(Math.Round(PatternWeight, 1), 250d);

        if (!DynamicWeight) {
            int xDim = centralDelegator!.GetMainWindowVM().ImageOutWidth,
                yDim = centralDelegator!.GetMainWindowVM().ImageOutHeight;
            _weightHeatmap = new double[xDim, yDim];
            for (int i = 0; i < xDim; i++) {
                for (int j = 0; j < yDim; j++) {
                    _weightHeatmap[i, j] = PatternWeight;
                }
            }
        }
    }

    /// <summary>
    ///     Callback when clicking on the weight value of the tile, opening the weight mapping window.
    /// </summary>
    public async void DynamicWeightClick() {
        await centralDelegator!.GetInterfaceHandler().SwitchWindow(Windows.HEATMAP);

        int xDim = centralDelegator!.GetMainWindowVM().ImageOutWidth,
            yDim = centralDelegator!.GetMainWindowVM().ImageOutHeight;
        
        if (_weightHeatmap.Length == 0 || _weightHeatmap.Length != xDim * yDim || _weightHeatmap.GetLength(0) != xDim|| _weightHeatmap.GetLength(1) != yDim) {
            centralDelegator.GetWFCHandler().ResetWeights(force:true);
        }

        centralDelegator!.GetWeightMapWindow().SetSelectedTile(RawPatternIndex);
        centralDelegator!.GetWeightMapWindow().UpdateOutput(_weightHeatmap);
    }

    /// <summary>
    ///     Callback when clicking the button to toggle rotations
    /// </summary>
    public async void OnRotateClick() {
        RotateDisabled = !RotateDisabled;
        await centralDelegator!.GetOutputHandler().RestartSolution("Rotate toggle", true);
        centralDelegator!.GetWFCHandler().UpdateTransformations();
    }

    /// <summary>
    ///     Callback when clicking the button to toggle flipping
    /// </summary>
    public async void OnFlipClick() {
        FlipDisabled = !FlipDisabled;
        await centralDelegator!.GetOutputHandler().RestartSolution("Flip toggle", true);
        centralDelegator!.GetWFCHandler().UpdateTransformations();
    }

    /// <summary>
    ///     Forward the selection of the current pattern to be used as a host for the currently selected item to the menu
    /// </summary>
    public void ForwardSelectionToggle() {
        centralDelegator!.GetItemWindow().GetItemAddMenu().ForwardAllowedTileChange(PatternIndex, ItemAddChecked);
    }

    /// <summary>
    ///     Toggle whether the current pattern may appear in the output
    /// </summary>
    public void TogglePatternAppearance() {
        PatternDisabled = !PatternDisabled;
        centralDelegator!.GetWFCHandler().SetPatternDisabled(PatternDisabled, RawPatternIndex);
    }

    /// <summary>
    ///     Callback when rotating a rotationally locked tile to alter the output appearance
    /// </summary>
    public async void OnRotateUserRepresentation() {
        UserRotation += 90;

        if (cardin == 2) {
            UserRotation %= 180;
        } else {
            UserRotation %= 360;
        }

        await centralDelegator!.GetOutputHandler().RestartSolution("User rotate change", true);
        centralDelegator!.GetWFCHandler().UpdateTransformations();
    }

    /// <summary>
    ///     Callback when flipping a flip locked tile to alter the output appearance
    /// </summary>
    public async void OnFlipUserRepresentation() {
        if (UserFlipping == 1) {
            UserFlipping = -1;
        } else {
            UserFlipping = 1;
        }

        await centralDelegator!.GetOutputHandler().RestartSolution("User flip change", true);
        centralDelegator!.GetWFCHandler().UpdateTransformations();
    }
}