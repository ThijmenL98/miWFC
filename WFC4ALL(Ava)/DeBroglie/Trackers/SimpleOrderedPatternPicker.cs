using System;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class SimpleOrderedPatternPicker : IPatternPicker {
    private int patternCount;
    private Wave wave;

    public void Init(WavePropagator wavePropagator) {
        wave = wavePropagator.Wave;
        patternCount = wavePropagator.PatternCount;
    }

    public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble) {
        for (int pattern = 0; pattern < patternCount; pattern++) {
            if (wave.Get(index, pattern)) {
                return pattern;
            }
        }

        return -1;
    }
}