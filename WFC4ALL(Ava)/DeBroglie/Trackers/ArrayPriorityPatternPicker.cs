using System;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class ArrayPriorityPatternPicker : IPatternPicker {
    private readonly WeightSetCollection weightSetCollection;
    private Wave wave;

    public ArrayPriorityPatternPicker(WeightSetCollection weightSetCollection) {
        this.weightSetCollection = weightSetCollection;
    }

    public void Init(WavePropagator wavePropagator) {
        wave = wavePropagator.Wave;
    }

    public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble) {
        FrequencySet fs = weightSetCollection.Get(index);

        // Run through the groups with descending prioirty
        for (int g = 0; g < fs.groups.Length; g++) {
            int[] patterns = fs.groups[g].Patterns;
            double[] frequencies = fs.groups[g].Frequencies;
            // Scan the group
            double s = 0.0;
            for (int i = 0; i < patterns.Length; i++) {
                if (wave.Get(index, patterns[i])) {
                    s += frequencies[i];
                }
            }

            if (s <= 0.0) {
                continue;
            }

            // There's at least one choice at this priority level,
            // pick one at random.
            double r = randomDouble() * s;
            for (int i = 0; i < patterns.Length; i++) {
                if (wave.Get(index, patterns[i])) {
                    r -= frequencies[i];
                }

                if (r <= 0) {
                    return patterns[i];
                }
            }

            return patterns[patterns.Length - 1];
        }

        return -1;
    }
}