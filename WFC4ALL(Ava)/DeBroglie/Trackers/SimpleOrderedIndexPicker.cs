using System;
using System.Collections.Generic;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class SimpleOrderedIndexPicker : IIndexPicker, IFilteredIndexPicker {
    private int indices;
    private bool[] mask;

    private Wave wave;

    public int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices) {
        foreach (int i in indices) {
            int c = wave.GetPatternCount(i);
            if (c <= 1) {
                continue;
            }

            return i;
        }

        return -1;
    }

    public void Init(WavePropagator wavePropagator) {
        wave = wavePropagator.Wave;

        mask = wavePropagator.Topology.Mask;

        indices = wave.Indicies;
    }

    public int GetRandomIndex(Func<double> randomDouble) {
        for (int i = 0; i < indices; i++) {
            if (mask != null && !mask[i]) {
                continue;
            }

            int c = wave.GetPatternCount(i);
            if (c <= 1) {
                continue;
            }

            return i;
        }

        return -1;
    }
}