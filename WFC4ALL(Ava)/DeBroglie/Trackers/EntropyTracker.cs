using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie.Trackers {
    internal class EntropyTracker : ITracker {
        private readonly int patternCount;

        private readonly double[] frequencies;

        // Track some useful per-cell values
        private readonly EntropyValues[] entropyValues;

        // See the definition in EntropyValues
        private readonly double[] plogp;

        private readonly bool[] mask;

        private readonly int indices;

        private readonly Wave wave;

        private List<int> cache = new();

        // private Queue<(int, int)> spiralIndices, hilbertIndices;

        private readonly int outputWidth, outputHeight;

        private int[] patternUsage;

        public EntropyTracker(
            Wave wave,
            double[] frequencies,
            bool[] mask,
            int outWidth,
            int outHeight) {
            this.frequencies = frequencies;
            patternCount = frequencies.Length;
            patternUsage = new int[patternCount];
            this.mask = mask;

            outputWidth = outWidth;
            outputHeight = outHeight;

            // generateSpiralCoords();
            // generateHilbertCoords();

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
        }

        public void doBan(int index, int pattern) {
            entropyValues[index].decrement(frequencies[pattern], plogp[pattern]);
        }

        public void reset() {
            // Assumes Reset is called on a truly new Wave.

            EntropyValues initial;
            initial.plogpSum = 0;
            initial.sum = 0;
            initial.entropy = 0;
            for (int pattern = 0; pattern < patternCount; pattern++) {
                double f = frequencies[pattern];
                double v = f > 0 ? f * Math.Log(f) : 0.0;
                initial.plogpSum += v;
                initial.sum += f;
            }

            initial.recomputeEntropy();
            for (int index = 0; index < indices; index++) {
                entropyValues[index] = initial;
            }

            cache = new List<int>();

            // generateSpiralCoords();
            // generateHilbertCoords();
            patternUsage = new int[patternCount];
        }

        public void undoBan(int index, int pattern) {
            entropyValues[index].increment(frequencies[pattern], plogp[pattern]);
            cache.Remove(index);
        }

        // Finds the cells with minimal entropy (excluding 0, decided cells)
        // and picks one randomly.
        // Returns -1 if every cell is decided.
        public int getRandomMinEntropyIndex(Func<double> randomDouble, bool isSimple) {
            int selectedIndex = -1;
            // TOODO: At the moment this is a linear scan, but potentially
            // could use some data structure
            double minEntropy = double.PositiveInfinity;
            int countAtMinEntropy = 0;
            for (int i = 0; i < indices; i++) {
                if (mask != null && !mask[i]) {
                    continue;
                }

                int c = wave.getPatternCount(i);
                double e = entropyValues[i].entropy;
                if (c <= 1) {
                    continue;
                }
                
                if (e < minEntropy) {
                    countAtMinEntropy = 1;
                    minEntropy = e;
                } else if (!isSimple && Math.Abs(e - minEntropy) < 0.001d) {
                    countAtMinEntropy++;
                }
            }

            int n = (int) (countAtMinEntropy * randomDouble());

            for (int i = 0; i < indices; i++) {
                if (mask != null && !mask[i]) {
                    continue;
                }

                int c = wave.getPatternCount(i);
                double e = entropyValues[i].entropy;
                if (c <= 1) {
                    continue;
                }

                if (Math.Abs(e - minEntropy) < 0.001d) {
                    if (n == 0) {
                        selectedIndex = i;
                        break;
                    }

                    n--;
                }
            }

            return selectedIndex;
        }

        // class RandomPool {
        //     Stack<int> pool;
        //
        //     public RandomPool(Random rng, IEnumerable<int> items) {
        //         var itemList = items.ToList();
        //         Shuffle(itemList, rng);
        //         pool = new Stack<int>(itemList);        
        //     }
        //
        //     public int Next() => pool.Count > 0 ? pool.Pop() : -1;
        // }

        public int getRandomIndex(Func<double> randomDouble) {
            while (true) {
                List<int> available = Enumerable.Range(0, indices).Except(cache).ToList();
                if (available.Count == 0) {
                    return -1;
                }

                int r = available.ElementAt((int) Math.Floor(randomDouble() * available.Count));
                if (wave.getPatternCount(r) <= 1) {
                    cache.Add(r);
                } else {
                    return r;
                }
            }
        }

        public int getLexicalIndex() {
            for (int i = 0; i < indices; i++) {
                if (wave.getPatternCount(i) <= 1) {
                    continue;
                }

                return i;
            }

            return -1;
        }

        // public int getSpiralIndex() {
        //     return getQueuedIndex(spiralIndices);
        // }
        //
        // public int getHilbertIndex() {
        //     return getQueuedIndex(hilbertIndices);
        // }

        private int getQueuedIndex(Queue<(int, int)> queue) {
            if (queue.Count == 0) {
                return -1;
            }
            (int, int) next = queue.Dequeue();
            int index = next.Item1 + next.Item2 * outputHeight;

            int c = wave.getPatternCount(index);
            while (c <= 1) {
                if (queue.Count == 0) {
                    return -1;
                }

                next = queue.Dequeue();
                index = next.Item1 + next.Item2 * outputHeight;
                c = wave.getPatternCount(index);
            }

            return index;
        }

        // private void generateSpiralCoords() {
        //     int x = (int) Math.Floor(outputWidth / 2d), y = (int) Math.Floor(outputHeight / 2d);
        //     spiralIndices = new Queue<(int, int)>();
        //     customEnqueue(x, y, spiralIndices);
        //
        //     int n = 1;
        //
        //     while (spiralIndices.Count < outputWidth * outputHeight) {
        //         if (n % 2 == 0) {
        //             customEnqueue(++x, y, spiralIndices);
        //             for (int m = 0; m < n; m++) {
        //                 customEnqueue(x, ++y, spiralIndices);
        //             }
        //
        //             for (int m = 0; m < n; m++) {
        //                 customEnqueue(--x, y, spiralIndices);
        //             }
        //         } else {
        //             customEnqueue(--x, y, spiralIndices);
        //             for (int m = 0; m < n; m++) {
        //                 customEnqueue(x, --y, spiralIndices);
        //             }
        //
        //             for (int m = 0; m < n; m++) {
        //                 customEnqueue(++x, y, spiralIndices);
        //             }
        //         }
        //
        //         n++;
        //     }
        // }
        //
        // private void generateHilbertCoords() {
        //     hilbertIndices = new Queue<(int, int)>();
        //     for (int i = 0; hilbertIndices.Count < outputWidth * outputHeight; i++) {
        //         int t = i, x = 0, y = 0;
        //         for (int s = 1; s < outputWidth * outputHeight; s *= 2) {
        //             int rx = 1 & (t / 2);
        //             int ry = 1 & (t ^ rx);
        //             hilbertRot(s, rx, ry, x, y, out x, out y);
        //             x += s * rx;
        //             y += s * ry;
        //             t /= 4;
        //         }
        //
        //         customEnqueue(x, y, hilbertIndices);
        //     }
        // }

        private static void hilbertRot(int n, int rx, int ry, int refX, int refY, out int x, out int y) {
            x = refX;
            y = refY;
            if (ry == 0) {
                if (rx == 1) {
                    x = n - 1 - x;
                    y = n - 1 - y;
                }

                (y, x) = (x, y);
            }
        }

        private void customEnqueue(int x, int y, Queue<(int, int)> queue) {
            if (x < 0 || y < 0) {
                return;
            }

            if (x >= outputWidth) {
                return;
            }

            if (y >= outputHeight) {
                return;
            }

            queue.Enqueue((x, y));
        }

        public int getWeightedPatternAt(int index, Func<double> randomDouble) {
            double s = 0.0;
                        
            Trace.WriteLine("Weights: " + string.Join(", ", frequencies));
            for (int pattern = 0; pattern < patternCount; pattern++) {
                if (wave.get(index, pattern)) {
                    s += frequencies[pattern];
                }
            }

            double r = randomDouble() * s;
            for (int pattern = 0; pattern < patternCount; pattern++) {
                if (wave.get(index, pattern)) {
                    r -= frequencies[pattern];
                }

                if (r <= 0) {
                    Trace.WriteLine("Chose a weight of: " + frequencies[pattern]);
                    return pattern;
                }
            }

            return patternCount - 1;
        }

        public int getLeastPatternAt(int index) {
            Random r = new();
            int lowestIndex = -1;
            for (int i = 0; i < patternCount; i++) {
                if (wave.get(index, i)) {
                    lowestIndex = i;
                    break;
                }
            }
            
            for (int i = 0; i < patternCount; i++) {
                int curValue = patternUsage[i];
                if (wave.get(index, i) && curValue < patternUsage[lowestIndex]) {
                    lowestIndex = i;
                }
            }            
            
            List<int> lowestIndices = new();
            
            // If there are more than 1 patterns with this least amount of usage, randomly select one of them.
            for (int i = 0; i < patternCount; i++) {
                if (patternUsage[i] == patternUsage[lowestIndex] && wave.get(index, i)) {
                    lowestIndices.Add(i);
                }
            }
            
            int next = lowestIndices[r.Next(lowestIndices.Count)];
            patternUsage[next]++;
            return next;
        }

        public int getRandomPatternAt(int index, Func<double> randomDouble) {
            double r = randomDouble() * wave.getPatternList(index).Count;
            for (int pattern = 0; pattern < patternCount; pattern++) {
                if (wave.get(index, pattern)) {
                    r--;
                }

                if (r <= 0) {
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
        private struct EntropyValues {
            public double plogpSum; // The sum of p'(pattern) * log(p'(pattern)).
            public double sum; // The sum of p'(pattern).
            public double entropy; // The entropy of the cell.

            public void recomputeEntropy() {
                entropy = Math.Log(sum) - plogpSum / sum;
            }

            public void decrement(double p, double plogp) {
                plogpSum -= plogp;
                sum -= p;
                recomputeEntropy();
            }

            public void increment(double p, double plogp) {
                plogpSum += plogp;
                sum += p;
                recomputeEntropy();
            }
        }

        public void updateZeroWeights(int index) {
            for (int pattern = 0; pattern < patternCount; pattern++) {
                if (frequencies[pattern] == 0) {
                    doBan(index, pattern);
                }
            }
        }
    }
}