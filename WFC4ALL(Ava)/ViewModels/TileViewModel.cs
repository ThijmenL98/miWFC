﻿using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using WFC4ALL.Managers;

namespace WFC4ALL.ViewModels;

public class TileViewModel : ReactiveObject {
    private readonly Color _patternColour;
    private readonly WriteableBitmap _patternImage = null!;
    private readonly int _patternIndex, _patternRotation, _patternFlipping, _rawPatternIndex;
    private double _patternWeight, _changeAmount = 1.0d;

    private readonly CentralManager? parentCM;
    private bool _flipDisabled, _rotateDisabled, _highlighted;
    
    /*
     * Used for input patterns
     */
    public TileViewModel(WriteableBitmap image, double weight, int index, int rawIndex, bool isF = false, CentralManager? cm = null) {
        PatternImage = image;
        PatternWeight = weight;
        PatternIndex = index;
        PatternRotation = 0;
        PatternFlipping = isF ? -1 : 1;
        RawPatternIndex = rawIndex;

        if (cm != null) {
            parentCM = cm;
        }

        FlipDisabled = false;
        RotateDisabled = false;
    }

    /*
     * Used for Adjacent Tiles
     */
    public TileViewModel(WriteableBitmap image, double weight, int index, int patternRotation, int patternFlipping) {
        PatternImage = image;
        PatternIndex = index;
        PatternWeight = weight;
        PatternRotation = patternRotation;
        PatternFlipping = patternFlipping;
    }

    /*
     * Used for Overlapping Tiles
     */
    public TileViewModel(WriteableBitmap image, int index, Color c) {
        PatternImage = image;
        PatternIndex = index;
        PatternColour = c;
        PatternRotation = 0;
        PatternFlipping = 1;
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
        switch ((int) ChangeAmount) {
            case 1:
                if (increment) {
                    ChangeAmount = 10;
                }
                break;
            case 10:
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

        PatternWeight = Math.Round(PatternWeight, 1);
    }

    public void OnRotateClick() {
        RotateDisabled = !RotateDisabled;
    }

    public void OnFlipClick() {
        FlipDisabled = !FlipDisabled;
    }
}