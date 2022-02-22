using System;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie.Trackers
{

    internal class EntropyTracker : ITracker
    {
        private readonly int patternCount;

        private readonly double[] frequencies;

        // Track some useful per-cell values
        private readonly EntropyValues[] entropyValues;

        // See the definition in EntropyValues
        private readonly double[] plogp;

        private readonly bool[] mask;

        private readonly int indices;

        private readonly Wave wave;

        public EntropyTracker(
            Wave wave,
            double[] frequencies,
            bool[] mask)
        {
            this.frequencies = frequencies;
            patternCount = frequencies.Length;
            this.mask = mask;

            this.wave = wave;
            indices = wave.Indicies;

            // Initialize plogp
            plogp = new double[patternCount];
            for (int pattern = 0; pattern < patternCount; pattern++)
            {
                double f = frequencies[pattern];
                double v = f > 0 ? f * Math.Log(f) : 0.0;
                plogp[pattern] = v;
            }

            entropyValues = new EntropyValues[indices];
        }

        public void doBan(int index, int pattern)
        {
            entropyValues[index].decrement(frequencies[pattern], plogp[pattern]);
        }

        public void reset()
        {
            // Assumes Reset is called on a truly new Wave.

            EntropyValues initial;
            initial.plogpSum = 0;
            initial.sum = 0;
            initial.entropy = 0;
            for (int pattern = 0; pattern < patternCount; pattern++)
            {
                double f = frequencies[pattern];
                double v = f > 0 ? f * Math.Log(f) : 0.0;
                initial.plogpSum += v;
                initial.sum += f;
            }
            initial.recomputeEntropy();
            for (int index = 0; index < indices; index++)
            {
                entropyValues[index] = initial;
            }
        }

        public void undoBan(int index, int pattern)
        {
            entropyValues[index].increment(frequencies[pattern], plogp[pattern]);
        }

        // Finds the cells with minimal entropy (excluding 0, decided cells)
        // and picks one randomly.
        // Returns -1 if every cell is decided.
        public int getRandomMinEntropyIndex(Func<double> randomDouble)
        {
            int selectedIndex = -1;
            // TODO: At the moment this is a linear scan, but potentially
            // could use some data structure
            double minEntropy = double.PositiveInfinity;
            int countAtMinEntropy = 0;
            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i]) {
                    continue;
                }

                int c = wave.getPatternCount(i);
                double e = entropyValues[i].entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (e < minEntropy)
                {
                    countAtMinEntropy = 1;
                    minEntropy = e;
                }
                else if (e == minEntropy)
                {
                    countAtMinEntropy++;
                }
            }
            int n = (int)(countAtMinEntropy * randomDouble());

            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i]) {
                    continue;
                }

                int c = wave.getPatternCount(i);
                double e = entropyValues[i].entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (e == minEntropy)
                {
                    if (n == 0)
                    {
                        selectedIndex = i;
                        break;
                    }
                    n--;
                }
            }
            return selectedIndex;
        }

        public int getRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            double s = 0.0;
            for (int pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.get(index, pattern))
                {
                    s += frequencies[pattern];
                }
            }
            double r = randomDouble() * s;
            for (int pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.get(index, pattern))
                {
                    r -= frequencies[pattern];
                }
                if (r <= 0)
                {
                    return pattern;
                }
            }
            return patternCount - 1;
        }

        /**
          * Struct containing the values needed to compute the entropy of all the cells.
          * This struct is updated every time the cell is changed.
          * p'(pattern) is equal to Frequencies[pattern] if the pattern is still possible, otherwise 0.
          */
        private struct EntropyValues
        {
            public double plogpSum;     // The sum of p'(pattern) * log(p'(pattern)).
            public double sum;          // The sum of p'(pattern).
            public double entropy;      // The entropy of the cell.

            public void recomputeEntropy()
            {
                entropy = Math.Log(sum) - plogpSum / sum;
            }

            public void decrement(double p, double plogp)
            {
                plogpSum -= plogp;
                sum -= p;
                recomputeEntropy();
            }

            public void increment(double p, double plogp)
            {
                plogpSum += plogp;
                sum += p;
                recomputeEntropy();
            }
        }
    }
}
