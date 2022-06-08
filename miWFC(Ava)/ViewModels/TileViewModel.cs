using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using miWFC.Managers;
using ReactiveUI;

namespace miWFC.ViewModels;

public class TileViewModel : ReactiveObject {
    private readonly Color _patternColour;
    private readonly WriteableBitmap _patternImage = null!;
    private readonly int _patternIndex, _patternRotation, _patternFlipping, _rawPatternIndex;
    private double _patternWeight, _changeAmount = 1.0d;

    private readonly CentralManager? parentCM;
    private bool _flipDisabled, _rotateDisabled, _highlighted, _itemAddChecked;

    /*
     * Used for input patterns
     */
    public TileViewModel(WriteableBitmap image, double weight, int index, int rawIndex, CentralManager cm,
        bool isF = false) {
        PatternImage = image;
        PatternWeight = weight;
        PatternIndex = index;
        PatternRotation = 0;
        PatternFlipping = isF ? -1 : 1;
        RawPatternIndex = rawIndex;

        parentCM = cm;

        FlipDisabled = false;
        RotateDisabled = false;
    }

    /*
     * Used for Adjacent Tiles
     */
    public TileViewModel(WriteableBitmap image, double weight, int index, int patternRotation, int patternFlipping,
        CentralManager cm) {
        PatternImage = image;
        PatternIndex = index;
        PatternWeight = weight;
        PatternRotation = patternRotation;
        PatternFlipping = patternFlipping;

        parentCM = cm;
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
        parentCM = cm;
    }

    public WriteableBitmap PatternImage {
        get => _patternImage;
        private init => this.RaiseAndSetIfChanged(ref _patternImage, value);
    }

    public double PatternWeight {
        get => _patternWeight;
        set => this.RaiseAndSetIfChanged(ref _patternWeight, value);
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
        bool isOverlapping = parentCM != null && parentCM!.getWFCHandler().isOverlappingModel();
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

        PatternWeight = Math.Min(isOverlapping ? Math.Min(1d, PatternWeight) : Math.Round(PatternWeight, 1), 100d);
        double change = oldWeight - PatternWeight;

        if (isOverlapping && change != 0d) {
            parentCM!.getWFCHandler().propagateWeightChange(PatternIndex, change);
        }
    }

    public void OnRotateClick() {
        RotateDisabled = !RotateDisabled;
    }

    public void OnFlipClick() {
        FlipDisabled = !FlipDisabled;
    }

    public void OnCheckChange() {
        parentCM!.getItemWindow().getItemAddMenu().forwardCheckChange(PatternIndex, ItemAddChecked);
    }
}