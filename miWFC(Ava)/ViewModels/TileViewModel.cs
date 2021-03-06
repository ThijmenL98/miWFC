using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Managers;
using ReactiveUI;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace miWFC.ViewModels;

public class TileViewModel : ReactiveObject {
    private readonly Color _patternColour;
    private readonly WriteableBitmap _patternImage = null!;
    private readonly int _patternIndex, _patternRotation, _patternFlipping, _rawPatternIndex;

    private readonly CentralManager? centralManager;

    private bool _flipDisabled,
        _rotateDisabled,
        _highlighted,
        _itemAddChecked,
        _dynamicWeight,
        _mayRotate,
        _mayFlip,
        _mayTransform;

    private double _patternWeight, _changeAmount = 1.0d;
    private string _patternWeightString;

    private double[,] _weightHeatmap = new double[0, 0];

    /*
     * Used for input patterns
     */
    public TileViewModel(WriteableBitmap image, double weight, int index, int rawIndex, CentralManager cm,
        int card) {
        PatternImage = image;
        PatternWeight = weight;
        PatternIndex = index;
        PatternRotation = 0;
        PatternFlipping = -1;
        RawPatternIndex = rawIndex;

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

    /*
     * Used for Adjacent Tiles
     */
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

    /*
     * Used for Overlapping Tiles
     */
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

    public WriteableBitmap PatternImage {
        get => _patternImage;
        private init => this.RaiseAndSetIfChanged(ref _patternImage, value);
    }

    public double PatternWeight {
        get => _patternWeight;
        set {
            this.RaiseAndSetIfChanged(ref _patternWeight, value);
            PatternWeightString = DynamicWeight ? "D" :
                _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
        }
    }

    public string PatternWeightString {
        get => _patternWeightString;
        set => this.RaiseAndSetIfChanged(ref _patternWeightString, value);
    }

    public double ChangeAmount {
        get => _changeAmount;
        set => this.RaiseAndSetIfChanged(ref _changeAmount, value);
    }

    public int PatternIndex {
        get => _patternIndex;
        private init => this.RaiseAndSetIfChanged(ref _patternIndex, value);
    }

    public int RawPatternIndex {
        get => _rawPatternIndex;
        private init => this.RaiseAndSetIfChanged(ref _rawPatternIndex, value);
    }

    public Color PatternColour {
        get => _patternColour;
        private init => this.RaiseAndSetIfChanged(ref _patternColour, value);
    }

    public int PatternRotation {
        get => _patternRotation + 90;
        private init => this.RaiseAndSetIfChanged(ref _patternRotation, value);
    }

    public int PatternFlipping {
        get => _patternFlipping;
        private init => this.RaiseAndSetIfChanged(ref _patternFlipping, value);
    }

    public bool RotateDisabled {
        get => _rotateDisabled;
        set => this.RaiseAndSetIfChanged(ref _rotateDisabled, value);
    }

    public bool FlipDisabled {
        get => _flipDisabled;
        set => this.RaiseAndSetIfChanged(ref _flipDisabled, value);
    }

    public bool Highlighted {
        get => _highlighted;
        set => this.RaiseAndSetIfChanged(ref _highlighted, value);
    }

    public bool ItemAddChecked {
        get => _itemAddChecked;
        set => this.RaiseAndSetIfChanged(ref _itemAddChecked, value);
    }

    public bool MayRotate {
        get => _mayRotate;
        set {
            this.RaiseAndSetIfChanged(ref _mayRotate, value);
            MayTransform = MayFlip || MayRotate;
        }
    }

    public bool MayFlip {
        get => _mayFlip;
        set {
            this.RaiseAndSetIfChanged(ref _mayFlip, value);
            MayTransform = MayFlip || MayRotate;
        }
    }

    public bool MayTransform {
        get => _mayTransform;
        set => this.RaiseAndSetIfChanged(ref _mayTransform, value);
    }

    public bool DynamicWeight {
        get => _dynamicWeight;
        set {
            this.RaiseAndSetIfChanged(ref _dynamicWeight, value);
            PatternWeightString = DynamicWeight ? "D" :
                _patternWeight == 0d ? "~0" : _patternWeight.ToString(CultureInfo.InvariantCulture);
        }
    }

    public double[,] WeightHeatMap {
        get => _weightHeatmap;
        set => this.RaiseAndSetIfChanged(ref _weightHeatmap, value);
    }

    /*
     * Button callbacks
     */

    public void OnIncrement() {
        handleWeightChange(true);
    }

    public void OnDecrement() {
        handleWeightChange(false);
    }

    public void OnWeightIncrement() {
        handleWeightGapChange(true);
    }

    public void OnWeightDecrement() {
        handleWeightGapChange(false);
    }

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

    private void handleWeightChange(bool increment) {
        bool isOverlapping = centralManager != null && centralManager!.getWFCHandler().isOverlappingModel();
        double oldWeight = PatternWeight;
        switch (increment) {
            case false when !(PatternWeight > 0):
                return;
            case true:
                PatternWeight += isOverlapping ? ChangeAmount / 100d : ChangeAmount;
                break;
            default:
                PatternWeight -= isOverlapping ? ChangeAmount / 100d : ChangeAmount;
                PatternWeight = Math.Max(0, PatternWeight);
                break;
        }

        PatternWeight = isOverlapping ? Math.Min(1d, PatternWeight) : Math.Min(Math.Round(PatternWeight, 1), 250d);
        double change = oldWeight - PatternWeight;

        if (isOverlapping && change != 0d) {
            centralManager!.getWFCHandler().propagateWeightChange(PatternIndex, change);
        }

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

    public void OnRotateClick() {
        RotateDisabled = !RotateDisabled;
        centralManager!.getInputManager().restartSolution("Rotate toggle", true);
        centralManager!.getWFCHandler().updateTransformations();
        Trace.WriteLine("We have banned rotations");
    }

    public void OnFlipClick() {
        FlipDisabled = !FlipDisabled;
        centralManager!.getInputManager().restartSolution("Flip toggle", true);
        centralManager!.getWFCHandler().updateTransformations();
    }

    public void OnCheckChange() {
        centralManager!.getItemWindow().getItemAddMenu().forwardCheckChange(PatternIndex, ItemAddChecked);
    }
}