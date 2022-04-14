using System;
using System.Collections.Generic;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class EntropyTracker : ITracker, IIndexPicker, IFilteredIndexPicker {
    // Track some useful per-cell values
    private EntropyValues[] entropyValues;

    private double[] frequencies;

    private int indices;

    private bool[] mask;
    private int patternCount;

    // See the definition in EntropyValues
    private double[] plogp;

    private Wave wave;

    public int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices) {
        int selectedIndex = -1;
        double minEntropy = double.PositiveInfinity;
        int countAtMinEntropy = 0;
        foreach (int i in indices) {
            int c = wave.GetPatternCount(i);
            double e = entropyValues[i].Entropy;
            if (c <= 1) {
                continue;
            }

            if (e < minEntropy) {
                countAtMinEntropy = 1;
                minEntropy = e;
            } else if (e == minEntropy) {
                countAtMinEntropy++;
            }
        }

        int n = (int) (countAtMinEntropy * randomDouble());

        foreach (int i in indices) {
            int c = wave.GetPatternCount(i);
            double e = entropyValues[i].Entropy;
            if (c <= 1) {
                continue;
            }

            if (e == minEntropy) {
                if (n == 0) {
                    selectedIndex = i;
                    break;
                }

                n--;
            }
        }

        return selectedIndex;
    }

    public void Init(WavePropagator wavePropagator) {
        Init(wavePropagator.Wave, wavePropagator.Frequencies, wavePropagator.Topology.Mask);
        wavePropagator.AddTracker(this);
    }

    // Finds the cells with minimal entropy (excluding 0, decided cells)
    // and picks one randomly.
    // Returns -1 if every cell is decided.
    public int GetRandomIndex(Func<double> randomDouble) {
        int selectedIndex = -1;
        double minEntropy = double.PositiveInfinity;
        int countAtMinEntropy = 0;
        for (int i = 0; i < indices; i++) {
            if (mask != null && !mask[i]) {
                continue;
            }

            int c = wave.GetPatternCount(i);
            double e = entropyValues[i].Entropy;
            if (c <= 1) {
                continue;
            }

            if (e < minEntropy) {
                countAtMinEntropy = 1;
                minEntropy = e;
            } else if (e == minEntropy) {
                countAtMinEntropy++;
            }
        }

        int n = (int) (countAtMinEntropy * randomDouble());

        for (int i = 0; i < indices; i++) {
            if (mask != null && !mask[i]) {
                continue;
            }

            int c = wave.GetPatternCount(i);
            double e = entropyValues[i].Entropy;
            if (c <= 1) {
                continue;
            }

            if (e == minEntropy) {
                if (n == 0) {
                    selectedIndex = i;
                    break;
                }

                n--;
            }
        }

        return selectedIndex;
    }

    public void DoBan(int index, int pattern) {
        entropyValues[index].Decrement(frequencies[pattern], plogp[pattern]);
    }

    public void Reset() {
        // Assumes Reset is called on a truly new Wave.

        EntropyValues initial;
        initial.PlogpSum = 0;
        initial.Sum = 0;
        initial.Entropy = 0;
        for (int pattern = 0; pattern < patternCount; pattern++) {
            double f = frequencies[pattern];
            double v = f > 0 ? f * Math.Log(f) : 0.0;
            initial.PlogpSum += v;
            initial.Sum += f;
        }

        initial.RecomputeEntropy();
        for (int index = 0; index < indices; index++) {
            entropyValues[index] = initial;
        }
    }

    public void UndoBan(int index, int pattern) {
        entropyValues[index].Increment(frequencies[pattern], plogp[pattern]);
    }

    // For debugging
    public void Init(Wave wave, double[] frequencies, bool[] mask) {
        this.frequencies = frequencies;
        patternCount = frequencies.Length;
        this.mask = mask;

        this.wave = wave;
        indices = wave.Indicies;

        // Initialize plogp
        plogp = new double[patternCount];
        for (int pattern = 0; pattern < patternCount; pattern++) {
            double f = frequencies[pattern];
            double v = f > 0 ? f * Math.Log(f) : 0.0;
            plogp[pattern] = v;
        }

        entropyValues = new EntropyValues[indices];

        Reset();
    }

    /**
     * Struct containing the values needed to compute the entropy of all the cells.
     * This struct is updated every time the cell is changed.
     * p'(pattern) is equal to Frequencies[pattern] if the pattern is still possible, otherwise 0.
     */
    private struct EntropyValues {
        public double PlogpSum; // The sum of p'(pattern) * log(p'(pattern)).
        public double Sum; // The sum of p'(pattern).
        public double Entropy; // The entropy of the cell.

        public void RecomputeEntropy() {
            Entropy = Math.Log(Sum) - PlogpSum / Sum;
        }

        public void Decrement(double p, double plogp) {
            PlogpSum -= plogp;
            Sum -= p;
            RecomputeEntropy();
        }

        public void Increment(double p, double plogp) {
            PlogpSum += plogp;
            Sum += p;
            RecomputeEntropy();
        }
    }
}