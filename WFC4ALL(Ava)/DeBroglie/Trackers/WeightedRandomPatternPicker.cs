using System;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class WeightedRandomPatternPicker : IPatternPicker {
    private double[] frequencies;
    private Wave wave;

    public void Init(WavePropagator wavePropagator) {
        wave = wavePropagator.Wave;
        frequencies = wavePropagator.Frequencies;
    }

    public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble) {
        return RandomPickerUtils.GetRandomPossiblePattern(wave, randomDouble, index, frequencies);
    }
}