using System;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

/// <summary>
///     An <see cref="IRandomPicker" /> that picks cells based on min entropy heuristic.
///     It's slower than <see cref="EntropyTracker" /> but supports two extra features:
///     * The frequencies can be set on a per cell basis.
///     * In addition to frequency, priority can be set. Only tiles of the highest priority for a given cell are considered
///     available.
/// </summary>
internal class ArrayPriorityEntropyTracker : ITracker, IIndexPicker, IPatternPicker {
    private readonly WeightSetCollection weightSetCollection;

    // Track some useful per-cell values
    private EntropyValues[] entropyValues;

    private int indices;

    private bool[] mask;

    private Wave wave;

    public ArrayPriorityEntropyTracker(WeightSetCollection weightSetCollection) {
        this.weightSetCollection = weightSetCollection;
    }

    public void Init(WavePropagator wavePropagator) {
        mask = wavePropagator.Topology.Mask;
        wave = wavePropagator.Wave;
        indices = wave.Indicies;
        entropyValues = new EntropyValues[indices];

        Reset();
        wavePropagator.AddTracker(this);
    }

    // Finds the cells with minimal entropy (excluding 0, decided cells)
    // and picks one randomly.
    // Returns -1 if every cell is decided.
    public int GetRandomIndex(Func<double> randomDouble) {
        int selectedIndex = -1;
        // TODO: At the moment this is a linear scan, but potentially
        // could use some data structure
        int minPriorityIndex = int.MaxValue;
        double minEntropy = double.PositiveInfinity;
        int countAtMinEntropy = 0;
        for (int i = 0; i < indices; i++) {
            if (mask != null && !mask[i]) {
                continue;
            }

            int c = wave.GetPatternCount(i);
            int pi = entropyValues[i].PriorityIndex;
            double e = entropyValues[i].Entropy;
            if (c <= 1) {
                continue;
            }

            if (pi < minPriorityIndex || pi == minPriorityIndex && e < minEntropy) {
                countAtMinEntropy = 1;
                minEntropy = e;
                minPriorityIndex = pi;
            } else if (pi == minPriorityIndex && e == minEntropy) {
                countAtMinEntropy++;
            }
        }

        int n = (int) (countAtMinEntropy * randomDouble());

        for (int i = 0; i < indices; i++) {
            if (mask != null && !mask[i]) {
                continue;
            }

            int c = wave.GetPatternCount(i);
            int pi = entropyValues[i].PriorityIndex;
            double e = entropyValues[i].Entropy;
            if (c <= 1) {
                continue;
            }

            if (pi == minPriorityIndex && e == minEntropy) {
                if (n == 0) {
                    selectedIndex = i;
                    break;
                }

                n--;
            }
        }

        return selectedIndex;
    }

    // Don't run init twice
    void IPatternPicker.Init(WavePropagator wavePropagator) { }

    public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble) {
        FrequencySet frequencySet = weightSetCollection.Get(index);
        ref FrequencySet.Group g = ref frequencySet.groups[entropyValues[index].PriorityIndex];
        return RandomPickerUtils.GetRandomPossiblePattern(wave, randomDouble, index, g.Frequencies, g.Patterns);
    }


    public void DoBan(int index, int pattern) {
        FrequencySet frequencySet = weightSetCollection.Get(index);
        if (entropyValues[index].Decrement(frequencySet.PriorityIndices[pattern], frequencySet.Frequencies[pattern],
                frequencySet.Plogp[pattern])) {
            PriorityReset(index);
        }
    }

    public void Reset() {
        // TODO: Perf boost by assuming wave is truly fresh?
        EntropyValues initial;
        initial.PriorityIndex = 0;
        initial.PlogpSum = 0;
        initial.Sum = 0;
        initial.Count = 0;
        initial.Entropy = 0;
        for (int index = 0; index < indices; index++) {
            entropyValues[index] = initial;
            if (weightSetCollection.Get(index) != null) {
                PriorityReset(index);
            }
        }
    }

    public void UndoBan(int index, int pattern) {
        FrequencySet frequencySet = weightSetCollection.Get(index);
        if (entropyValues[index].Increment(frequencySet.PriorityIndices[pattern], frequencySet.Frequencies[pattern],
                frequencySet.Plogp[pattern])) {
            PriorityReset(index);
        }
    }

    // The priority has just changed, recompute
    private void PriorityReset(int index) {
        FrequencySet frequencySet = weightSetCollection.Get(index);
        ref EntropyValues v = ref entropyValues[index];
        v.PlogpSum = 0;
        v.Sum = 0;
        v.Count = 0;
        v.Entropy = 0;
        while (v.PriorityIndex < frequencySet.groups.Length) {
            ref FrequencySet.Group g = ref frequencySet.groups[v.PriorityIndex];
            for (int i = 0; i < g.PatternCount; i++) {
                if (wave.Get(index, g.Patterns[i])) {
                    v.Sum += g.Frequencies[i];
                    v.PlogpSum += g.Plogp[i];
                    v.Count += 1;
                }
            }

            if (v.Count == 0) {
                // Try again with the next priorityIndex
                v.PriorityIndex++;
                continue;
            }

            v.RecomputeEntropy();
            return;
        }
    }

    /**
     * Struct containing the values needed to compute the entropy of all the cells.
     * This struct is updated every time the cell is changed.
     * p'(pattern) is equal to Frequencies[pattern] if the pattern is still possible, otherwise 0.
     */
    private struct EntropyValues {
        public int PriorityIndex;
        public double PlogpSum; // The sum of p'(pattern) * log(p'(pattern)).
        public double Sum; // The sum of p'(pattern).
        public int Count;
        public double Entropy; // The entropy of the cell.

        public void RecomputeEntropy() {
            Entropy = Math.Log(Sum) - PlogpSum / Sum;
        }

        public bool Decrement(int priorityIndex, double p, double plogp) {
            if (priorityIndex == PriorityIndex) {
                PlogpSum -= plogp;
                Sum -= p;
                Count--;
                if (Count == 0) {
                    PriorityIndex++;
                    return true;
                }

                RecomputeEntropy();
            }

            return false;
        }

        public bool Increment(int priorityIndex, double p, double plogp) {
            if (priorityIndex == PriorityIndex) {
                PlogpSum += plogp;
                Sum += p;
                Count++;
                RecomputeEntropy();
            }

            if (priorityIndex < PriorityIndex) {
                PriorityIndex = priorityIndex;
                return true;
            }

            return false;
        }
    }
}