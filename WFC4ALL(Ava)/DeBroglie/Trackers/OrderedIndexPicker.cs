using System;
using System.Collections.Generic;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class OrderedIndexPicker : IIndexPicker, IFilteredIndexPicker {
    private readonly int[] indexOrder;

    private Wave wave;

    public OrderedIndexPicker(int[] indexOrder) {
        this.indexOrder = indexOrder;
    }

    public int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices) {
        HashSet<int> set = new(indices);
        foreach (int i in indices) {
            if (!set.Contains(i)) {
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

    public void Init(WavePropagator wavePropagator) {
        wave = wavePropagator.Wave;
    }


    public int GetRandomIndex(Func<double> randomDouble) {
        foreach (int i in indexOrder) {
            int c = wave.GetPatternCount(i);
            if (c <= 1) {
                continue;
            }

            return i;
        }

        return -1;
    }
}