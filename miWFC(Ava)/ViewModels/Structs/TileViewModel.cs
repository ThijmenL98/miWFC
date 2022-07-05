using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Managers;
using ReactiveUI;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace miWFC.ViewModels.Structs;

/// <summary>
/// View model for the tiles used in the application, whether it be an overlapping pattern or an adjacent tile
/// </summary>
public class TileViewModel : ReactiveObject {
    private readonly Color _patternColour;
    private readonly WriteableBitmap _patternImage = null!;
    private readonly int _patternIndex, _patternRotation, _patternFlipping, _rawPatternIndex;
    private int _userRotation, _finalRotation, _userFlipping = 1, _finalFlipping = 1;

    private readonly CentralManager? centralManager;

    private bool _flipDisabled,
        _rotateDisabled,
        _highlighted,
        _itemAddChecked,
        _dynamicWeight;

    private readonly bool _mayFlip, _mayRotate;

    private bool _mayTransform,
        _patternDisabled;

    private double _patternWeight, _changeAmount = 1.0d;
    private string _patternWeightString;

    private double[,] _weightHeatmap = new double[0, 0];

    private readonly int cardin = -1;

    /*
     * Initializing Functions & Constructor
     */

    // Used for input patterns
    public TileViewModel(WriteableBitmap image, double weight, int index, int rawIndex, CentralManager cm,
        int card) {
        PatternImage = image;
        PatternWeight = weight;
        PatternIndex = index;
        PatternRotation = 0;
        PatternFlipping = card > 4 ? -1 : 1;
        RawPatternIndex = rawIndex;

        cardin = card;

        centralManager = cm;

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
        CentralManager cm) {
        PatternImage = image;
        PatternIndex = index;
        RawPatternIndex = -1;
        PatternWeight = weight;
        PatternRotation = patternRotation;
        PatternFlipping = patternFlipping;

        centralManager = cm;
        DynamicWeight = false;

        _patternWeightString = DynamicWeight ? "D" :
            _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
    }

    // Used for Overlapping Tiles
    public TileViewModel(WriteableBitmap image, int index, Color c, CentralManager cm) {
        PatternImage = image;
        PatternIndex = index;
        PatternColour = c;
        PatternRotation = 0;
        PatternFlipping = 1;
        centralManager = cm;
        DynamicWeight = false;

        _patternWeightString = DynamicWeight ? "D" :
            _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
    }

    /*
     * Getters & Setters
     */

    // Strings

    /// <summary>
    /// String representation of the pattern weight value
    /// </summary>
    public string PatternWeightString {
        get => _patternWeightString;
        set => this.RaiseAndSetIfChanged(ref _patternWeightString, value);
    }
    
    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    /// Index of the pattern
    /// </summary>
    public int PatternIndex {
        get => _patternIndex;
        init => this.RaiseAndSetIfChanged(ref _patternIndex, value);
    }

    /// <summary>
    /// Index of the pattern, set to -1 if this pattern is a transformed version of a parent pattern
    /// </summary>
    public int RawPatternIndex {
        get => _rawPatternIndex;
        init => this.RaiseAndSetIfChanged(ref _rawPatternIndex, value);
    }

    /// <summary>
    /// Final pattern rotation used in the UI to show to the user, sum of the user rotation and the pattern's inherent
    /// rotation
    /// </summary>
    public int FinalRotation {
        get => _finalRotation;
        set => this.RaiseAndSetIfChanged(ref _finalRotation, value);
    }
    
    /// <summary>
    /// Final pattern flipping used in the UI to show to the user, XOR of the user flipping and the pattern's inherent
    /// flipping
    /// </summary>
    public int FinalFlipping {
        get => _finalFlipping;
        set => this.RaiseAndSetIfChanged(ref _finalFlipping, value);
    }

    /// <summary>
    /// User defined pattern rotation
    /// </summary>
    public int UserRotation {
        get => _userRotation;
        set {
            this.RaiseAndSetIfChanged(ref _userRotation, value);
            FinalRotation = _userRotation + _patternRotation;
        }
    }

    /// <summary>
    /// User defined pattern flipping value
    /// </summary>
    public int UserFlipping {
        get => _userFlipping;
        set {
            this.RaiseAndSetIfChanged(ref _userFlipping, value);
            FinalFlipping = _userFlipping * _patternFlipping;
        }
    }

    /// <summary>
    /// The pattern's inherent rotation
    /// </summary>
    public int PatternRotation {
        get => _patternRotation + 90;
        init {
            this.RaiseAndSetIfChanged(ref _patternRotation, value);
            FinalRotation = _userRotation + _patternRotation;
        }
    }

    /// <summary>
    /// The pattern's inherent flipping value
    /// </summary>
    public int PatternFlipping {
        get => _patternFlipping;
        init {
            this.RaiseAndSetIfChanged(ref _patternFlipping, value);
            FinalFlipping = _userFlipping * _patternFlipping;
        }
    }

    /// <summary>
    /// Weight of the pattern, extracted from the input image or overwritten by the user
    /// </summary>
    public double PatternWeight {
        get => _patternWeight;
        set {
            this.RaiseAndSetIfChanged(ref _patternWeight, value);
            PatternWeightString = DynamicWeight ? "D" :
                _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Current value the pattern's weight will increment or decrement on user input, this can be changed by the user
    /// </summary>
    public double ChangeAmount {
        get => _changeAmount;
        set => this.RaiseAndSetIfChanged(ref _changeAmount, value);
    }
    
    // Booleans

    /// <summary>
    /// Whether this tile has their rotation(s) locked in the output.
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
    /// Whether this tile has their flipping locked in the output.
    /// </summary>
    public bool FlipDisabled {
        get => _flipDisabled;
        set => this.RaiseAndSetIfChanged(ref _flipDisabled, value);
    }

    /// <summary>
    /// Whether this tile is highlighted in the painting editor, which is true if the user has this tile selected on
    /// their brush. This is added to more transparently indicate whether the selected tile may be placed beforehand.
    /// </summary>
    public bool Highlighted {
        get => _highlighted;
        set => this.RaiseAndSetIfChanged(ref _highlighted, value);
    }

    /// <summary>
    /// Whether this item is selected to be a host for a user created item in the output.
    /// </summary>
    public bool ItemAddChecked {
        get => _itemAddChecked;
        set => this.RaiseAndSetIfChanged(ref _itemAddChecked, value);
    }

    /// <summary>
    /// Whether the tile is, by default, allowed to rotate, this is only true for tiles with a cardinality > 1
    /// </summary>
    public bool MayRotate {
        get => _mayRotate;
        init {
            this.RaiseAndSetIfChanged(ref _mayRotate, value);
            MayTransform = MayFlip || MayRotate;
        }
    }

    /// <summary>
    /// Whether the tile is, by default, allowed to be flipped, this is only true for tiles with a cardinality > 4
    /// </summary>
    public bool MayFlip {
        get => _mayFlip;
        init {
            this.RaiseAndSetIfChanged(ref _mayFlip, value);
            MayTransform = MayFlip || MayRotate;
        }
    }

    /// <summary>
    /// Whether the tile is, by default, allowed to be transformed, AND operation between MayRotate and MayFlip
    /// </summary>
    public bool MayTransform {
        get => _mayTransform;
        set => this.RaiseAndSetIfChanged(ref _mayTransform, value);
    }

    /// <summary>
    /// Whether this pattern is disabled, hence forcibly prohibited from appearing in the output
    /// </summary>
    public bool PatternDisabled {
        get => _patternDisabled;
        set => this.RaiseAndSetIfChanged(ref _patternDisabled, value);
    }

    /// <summary>
    /// Whether this tile has a dynamic weight, meaning it is based on a map of values instead of a single flat value
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
    /// Image representation of the pattern or tile
    /// </summary>
    public WriteableBitmap PatternImage {
        get => _patternImage;
        init => this.RaiseAndSetIfChanged(ref _patternImage, value);
    }
    
    // Objects

    /// <summary>
    /// The hex-code colour of the tile in the overlapping mode
    /// </summary>
    public Color PatternColour {
        get => _patternColour;
        init => this.RaiseAndSetIfChanged(ref _patternColour, value);
    }
    
    // Lists

    /// <summary>
    /// If the user has selected dynamic weight mapping, this weight heatmap is used, having a distinct value for each
    /// coordinate in the output.
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
    /// Function to handle a weight increase
    /// </summary>
    public void OnIncrement() {
        handleWeightChange(true);
    }

    /// <summary>
    /// Function to handle a weight decrease
    /// </summary>
    public void OnDecrement() {
        handleWeightChange(false);
    }

    /// <summary>
    /// Function to handle an increase in the amount to change the actual weight with
    /// </summary>
    public void OnWeightIncrement() {
        handleWeightGapChange(true);
    }
    
    /// <summary>
    /// Function to handle an decrease in the amount to change the actual weight with
    /// </summary>
    public void OnWeightDecrement() {
        handleWeightGapChange(false);
    }

    /// <summary>
    /// Actual logic behind changing the amount to change the actual weight with, which is done in fixed steps:
    /// 1, 10, 20, 30...
    /// </summary>
    /// <param name="increment">Whether to increase or decrease the gap to change the actual weight with</param>
    private void handleWeightGapChange(bool increment) {
        switch (ChangeAmount) {
            case 1d:
                if (increment) {
                    ChangeAmount = 10;
                }

                break;
            case 10d:
                if (increment) {
                    ChangeAmount += 10;
                } else {
                    ChangeAmount = 1;
                }

                break;
            default:
                if (increment) {
                    ChangeAmount += 10;
                    ChangeAmount = Math.Min(ChangeAmount, 100d);
                } else {
                    ChangeAmount -= 10;
                }

                break;
        }

        ChangeAmount = Math.Round(ChangeAmount, 1);
    }

    /// <summary>
    /// Actual logic behind changing the weight of this tile.
    /// </summary>
    /// 
    /// <param name="increment">Whether to increment (inverse decrement) the weight</param>
    private void handleWeightChange(bool increment) {
        switch (increment) {
            case false when !(PatternWeight > 0):
                return;
            case true:
                PatternWeight += ChangeAmount;
                break;
            default:
                PatternWeight -= ChangeAmount;
                PatternWeight = Math.Max(0, PatternWeight);
                break;
        }

        PatternWeight = Math.Min(Math.Round(PatternWeight, 1), 250d);

        if (!DynamicWeight) {
            int xDim = centralManager!.getMainWindowVM().ImageOutWidth,
                yDim = centralManager!.getMainWindowVM().ImageOutHeight;
            _weightHeatmap = new double[xDim, yDim];
            for (int i = 0; i < xDim; i++) {
                for (int j = 0; j < yDim; j++) {
                    _weightHeatmap[i, j] = PatternWeight;
                }
            }
        }
    }

    /// <summary>
    /// Callback when clicking on the weight value of the tile, opening the weight mapping window.
    /// </summary>
    public async void DynamicWeightClick() {
        await centralManager!.getUIManager().switchWindow(Windows.HEATMAP);

        int xDim = centralManager!.getMainWindowVM().ImageOutWidth,
            yDim = centralManager!.getMainWindowVM().ImageOutHeight;
        if (_weightHeatmap.Length == 0 || _weightHeatmap.Length != xDim * yDim) {
            centralManager.getWFCHandler().resetWeights();
        }

        centralManager!.getWeightMapWindow().setSelectedTile(RawPatternIndex);
        centralManager!.getWeightMapWindow().updateOutput(_weightHeatmap);
    }

    /// <summary>
    /// Callback when clicking the button to toggle rotations
    /// </summary>
    public async void OnRotateClick() {
        RotateDisabled = !RotateDisabled;
        await centralManager!.getInputManager().restartSolution("Rotate toggle", true);
        centralManager!.getWFCHandler().updateTransformations();
    }

    /// <summary>
    /// Callback when clicking the button to toggle flipping
    /// </summary>
    public async void OnFlipClick() {
        FlipDisabled = !FlipDisabled;
        await centralManager!.getInputManager().restartSolution("Flip toggle", true);
        centralManager!.getWFCHandler().updateTransformations();
    }

    /// <summary>
    /// Forward the selection of the current pattern to be used as a host for the currently selected item to the menu
    /// </summary>
    public void ForwardSelectionToggle() {
        centralManager!.getItemWindow().getItemAddMenu().forwardAllowedTileChange(PatternIndex, ItemAddChecked);
    }

    /// <summary>
    /// Toggle whether the current pattern may appear in the output
    /// </summary>
    public void TogglePatternAppearance() {
        PatternDisabled = !PatternDisabled;
        centralManager!.getWFCHandler().setPatternDisabled(PatternDisabled, RawPatternIndex);
    }

    /// <summary>
    /// Callback when rotating a rotationally locked tile to alter the output appearance
    /// </summary>
    public async void OnRotateUserRepresentation() {
        UserRotation += 90;

        if (cardin == 2) {
            UserRotation %= 180;
        } else {
            UserRotation %= 360;
        }

        await centralManager!.getInputManager().restartSolution("User rotate change", true);
        centralManager!.getWFCHandler().updateTransformations();
    }
    
    /// <summary>
    /// Callback when flipping a flip locked tile to alter the output appearance
    /// </summary>
    public async void OnFlipUserRepresentation() {
        if (UserFlipping == 1) {
            UserFlipping = -1;
        } else {
            UserFlipping = 1;
        }

        await centralManager!.getInputManager().restartSolution("User flip change", true);
        centralManager!.getWFCHandler().updateTransformations();
    }
}