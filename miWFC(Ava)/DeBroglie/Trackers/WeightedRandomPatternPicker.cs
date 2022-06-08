using System;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Trackers; 

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