using System;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers {
    internal class ArrayPriorityEntropyTracker : ITracker {
        private readonly FrequencySet[] frequencySets;

        // Track some useful per-cell values
        private readonly EntropyValues[] entropyValues;

        private readonly bool[] mask;

        private readonly int indices;

        private readonly Wave wave;

        public ArrayPriorityEntropyTracker(
            Wave wave,
            FrequencySet[] frequencySets,
            bool[] mask) {
            this.frequencySets = frequencySets;
            this.mask = mask;

            this.wave = wave;
            indices = wave.Indicies;

            entropyValues = new EntropyValues[indices];
        }

        public void doBan(int index, int pattern) {
            FrequencySet frequencySet = frequencySets[index];
            if (entropyValues[index].decrement(frequencySet.priorityIndices[pattern], frequencySet.frequencies[pattern],
                    frequencySet.plogp[pattern])) {
                priorityReset(index);
            }
        }

        public void reset() {
            // TOODO: Perf boost by assuming wave is truly fresh?
            EntropyValues initial;
            initial.priorityIndex = 0;
            initial.plogpSum = 0;
            initial.sum = 0;
            initial.count = 0;
            initial.entropy = 0;
            for (int index = 0; index < indices; index++) {
                entropyValues[index] = initial;
                priorityReset(index);
            }
        }

        // The priority has just changed, recompute
        private void priorityReset(int index) {
            FrequencySet frequencySet = frequencySets[index];
            ref EntropyValues v = ref entropyValues[index];
            v.plogpSum = 0;
            v.sum = 0;
            v.count = 0;
            v.entropy = 0;
            while (v.priorityIndex < frequencySet.Groups.Length) {
                ref FrequencySet.Group g = ref frequencySet.Groups[v.priorityIndex];
                for (int i = 0; i < g.patternCount; i++) {
                    if (wave.get(index, g.patterns[i])) {
                        v.sum += g.frequencies[i];
                        v.plogpSum += g.plogp[i];
                        v.count += 1;
                    }
                }

                if (v.count == 0) {
                    // Try again with the next priorityIndex
                    v.priorityIndex++;
                    continue;
                }

                v.recomputeEntropy();
                return;
            }
        }

        public void undoBan(int index, int pattern) {
            FrequencySet frequencySet = frequencySets[index];
            if (entropyValues[index].increment(frequencySet.priorityIndices[pattern], frequencySet.frequencies[pattern],
                    frequencySet.plogp[pattern])) {
                priorityReset(index);
            }
        }

        // Finds the cells with minimal entropy (excluding 0, decided cells)
        // and picks one randomly.
        // Returns -1 if every cell is decided.
        public int getRandomMinEntropyIndex(Func<double> randomDouble) {
            int selectedIndex = -1;
            // TOODO: At the moment this is a linear scan, but potentially
            // could use some data structure
            int minPriorityIndex = int.MaxValue;
            double minEntropy = double.PositiveInfinity;
            int countAtMinEntropy = 0;
            for (int i = 0; i < indices; i++) {
                if (mask != null && !mask[i]) {
                    continue;
                }

                int c = wave.getPatternCount(i);
                int pi = entropyValues[i].priorityIndex;
                double e = entropyValues[i].entropy;
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

                int c = wave.getPatternCount(i);
                int pi = entropyValues[i].priorityIndex;
                double e = entropyValues[i].entropy;
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

        public int getRandomPossiblePatternAt(int index, Func<double> randomDouble) {
            double s = 0.0;
            FrequencySet frequencySet = frequencySets[index];
            ref FrequencySet.Group g = ref frequencySet.Groups[entropyValues[index].priorityIndex];
            for (int i = 0; i < g.patternCount; i++) {
                int pattern = g.patterns[i];
                if (wave.get(index, pattern)) {
                    s += g.frequencies[i];
                }
            }

            double r = randomDouble() * s;
            for (int i = 0; i < g.patternCount; i++) {
                int pattern = g.patterns[i];
                if (wave.get(index, pattern)) {
                    r -= g.frequencies[i];
                }

                if (r <= 0) {
                    return pattern;
                }
            }

            return g.patterns[g.patterns.Count - 1];
        }

        /**
          * Struct containing the values needed to compute the entropy of all the cells.
          * This struct is updated every time the cell is changed.
          * p'(pattern) is equal to Frequencies[pattern] if the pattern is still possible, otherwise 0.
          */
        private struct EntropyValues {
            public int priorityIndex;
            public double plogpSum; // The sum of p'(pattern) * log(p'(pattern)).
            public double sum; // The sum of p'(pattern).
            public int count;
            public double entropy; // The entropy of the cell.

            public void recomputeEntropy() {
                entropy = Math.Log(sum) - plogpSum / sum;
            }

            public bool decrement(int priorityIndex, double p, double plogp) {
                if (priorityIndex == this.priorityIndex) {
                    plogpSum -= plogp;
                    sum -= p;
                    count--;
                    if (count == 0) {
                        this.priorityIndex++;
                        return true;
                    }

                    recomputeEntropy();
                }

                return false;
            }

            public bool increment(int priorityIndex, double p, double plogp) {
                if (priorityIndex == this.priorityIndex) {
                    plogpSum += plogp;
                    sum += p;
                    count++;
                    recomputeEntropy();
                }

                if (priorityIndex < this.priorityIndex) {
                    this.priorityIndex = priorityIndex;
                    return true;
                }

                return false;
            }
        }
    }
}