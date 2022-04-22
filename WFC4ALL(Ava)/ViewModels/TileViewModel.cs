using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using WFC4ALL.Managers;

namespace WFC4ALL.ViewModels;

public class TileViewModel : ReactiveObject {
    private readonly Color _patternColour;
    private readonly WriteableBitmap _patternImage = null!;
    private readonly int _patternIndex, _patternRotation, _patternFlipping, _rawPatternIndex;
    private double _patternWeight;

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

    private long lastIncMillis = 0, lastDecMillis = 0;
    private int incCount = 0, decCount = 0;
    private double incAmount = 0.1d, decAmount = 0.1d;

    public void OnIncrement() {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long diff = now - lastIncMillis;
        lastIncMillis = now;

        if (diff < 400) {
            incCount++;
            incAmount = incCount switch {
                >= 18 when PatternWeight % 10 == 0 => 10d,
                > 9 when PatternWeight % 1 == 0 => 1d,
                _ => incAmount
            };
        } else {
            incAmount = 0.1d;
            incCount = 0;
        }

        PatternWeight += incAmount;
        PatternWeight = Math.Round(PatternWeight, 1);
    }

    public void OnDecrement() {
        if (PatternWeight > 0) {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long diff = now - lastDecMillis;
            lastDecMillis = now;

            if (diff < 400) {
                decCount++;
                decAmount = decCount switch {
                    >= 18 when PatternWeight % 10 == 0 => 10d,
                    > 9 when PatternWeight % 1 == 0 => 1d,
                    _ => decAmount
                };
            } else {
                decAmount = 0.1d;
                decCount = 0;
            }

            PatternWeight -= decAmount;
            PatternWeight = Math.Max(0, PatternWeight);
            PatternWeight = Math.Round(PatternWeight, 1);
        }
    }

    public void OnRotateClick() {
        RotateDisabled = !RotateDisabled;
    }

    public void OnFlipClick() {
        FlipDisabled = !FlipDisabled;
    }
}