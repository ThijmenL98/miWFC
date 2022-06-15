using System;
using System.Collections.Generic;
using System.Linq;
using miWFC.DeBroglie.Models;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Trackers;

public class HeapEntropyTracker : ITracker, IIndexPicker {
    private const double Threshold = 1e-17;

    // Track some useful per-cell values
    private EntropyValues[] entropyValues;

    private double[] frequencies;

    private Heap<EntropyValues, double> heap;
    private int indexCount;

    private bool[] mask;
    private int patternCount;

    // See the definition in EntropyValues
    private double[] plogp;
    private Func<double> randomDouble;
    private ChangeTracker tracker;

    private Wave wave;

    public void Init(WavePropagator wavePropagator) {
        Init(wavePropagator.Wave, wavePropagator.Frequencies, wavePropagator.Topology.Mask,
            wavePropagator.RandomDouble);
        wavePropagator.AddTracker(this);
    }

    // Finds the cells with minimal entropy (excluding 0, decided cells)
    // and picks one randomly.
    // Returns -1 if every cell is decided.
    public int GetRandomIndex(Func<double> randomDouble) {
        if (tracker.ChangedCount > wave.Indicies * 0.5 && tracker.ChangedCount > 1) {
            // A lot of indices have changed
            // It's faster to rebuild the entire heap than sync it one at a time
            foreach (int index in tracker.GetChangedIndices()) {
                EntropyValues ev = entropyValues[index];
                ev.RecomputeEntropy();
            }

            IEnumerable<EntropyValues> items = Enumerable.Range(0, indexCount)
                .Where(index => mask == null || mask[index])
                .Where(index => {
                    int c = wave.GetPatternCount(index);
                    if (c <= 1) {
                        entropyValues[index].HeapIndex = -1;
                        return false;
                    }

                    return true;
                })
                .Select(index => entropyValues[index]);

            heap = new Heap<EntropyValues, double>(items);
        } else {
            // Sync heap with new values of entropy
            foreach (int index in tracker.GetChangedIndices()) {
                EntropyValues ev = entropyValues[index];
                ev.RecomputeEntropy();

                int c = wave.GetPatternCount(index);
                if (ev.HeapIndex == -1) {
                    if (c > 1) {
                        heap.Insert(ev);
                    }
                } else if (c <= 1) {
                    heap.Delete(ev);
                    ev.HeapIndex = -1;
                } else {
                    heap.ChangedKey(ev);
                }
            }
        }

        if (heap.Count == 0) {
            return -1;
        }

        EntropyValues item = heap.Peek();
        return item.Index;
    }

    public void DoBan(int index, int pattern) {
        EntropyValues ev = entropyValues[index];
        ev.Decrement(frequencies[pattern], plogp[pattern]);
        ((ITracker) tracker).DoBan(index, pattern);
    }

    public void Reset() {
        // Assumes Reset is called on a truly new Wave.

        EntropyValues initial = new();
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
        heap.Clear();
        for (int index = 0; index < indexCount; index++) {
            if (mask == null || mask[index]) {
                EntropyValues ev = entropyValues[index] = new EntropyValues(initial);
                ev.Index = index;
                ev.Tiebreaker = randomDouble() * 1e-10;
                heap.Insert(ev);
            }
        }

        ((ITracker) tracker).Reset();
    }

    public void UndoBan(int index, int pattern) {
        EntropyValues ev = entropyValues[index];
        ev.Increment(frequencies[pattern], plogp[pattern]);
        ((ITracker) tracker).UndoBan(index, pattern);
    }

    // For debugging
    public void Init(Wave wave, double[] frequencies, bool[] mask, Func<double> randomDouble) {
        this.frequencies = frequencies;
        patternCount = frequencies.Length;
        this.mask = mask;
        this.randomDouble = randomDouble;
        this.wave = wave;
        indexCount = wave.Indicies;

        // Initialize plogp
        plogp = new double[patternCount];
        for (int pattern = 0; pattern < patternCount; pattern++) {
            double f = frequencies[pattern];
            double v = f > 0 ? f * Math.Log(f) : 0.0;
            plogp[pattern] = v;
        }

        entropyValues = new EntropyValues[indexCount];

        heap = new Heap<EntropyValues, double>(indexCount);

        tracker = new ChangeTracker(new TileModelMapping(), wave.Indicies);

        Reset();
    }

    /**
     * Struct containing the values needed to compute the entropy of all the cells.
     * This struct is updated every time the cell is changed.
     * p'(pattern) is equal to Frequencies[pattern] if the pattern is still possible, otherwise 0.
     */
    private class EntropyValues : IHeapNode<double> {
        public double Entropy; // The entropy of the cell.
        public double PlogpSum; // The sum of p'(pattern) * log(p'(pattern)).
        public double Sum; // The sum of p'(pattern).

        public double Tiebreaker;
        //public double Priority => Entropy;

        public EntropyValues() { }

        public EntropyValues(EntropyValues other) {
            PlogpSum = other.PlogpSum;
            Sum = other.Sum;
            Entropy = other.Entropy;
        }

        public int Index { get; set; }

        public int HeapIndex { get; set; }

        public double Key => Entropy + Tiebreaker;

        public void RecomputeEntropy() {
            Entropy = Math.Log(Sum) - PlogpSum / Sum;
        }

        public void Decrement(double p, double plogp) {
            PlogpSum -= plogp;
            Sum -= p;
        }

        public void Increment(double p, double plogp) {
            PlogpSum += plogp;
            Sum += p;
        }
    }
}