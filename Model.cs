/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFC4All {
    internal abstract class Model {
        public enum Heuristic {
            ENTROPY,
            MRV,
            SCANLINE
        }

        protected static readonly int[] xChange = {-1, 0, 1, 0};
        protected static readonly int[] yChange = {0, 1, 0, -1};
        private static readonly int[] opposite = {2, 3, 0, 1};

        private readonly Heuristic heuristic;

        protected readonly int imageOutputWidth, imageOutputHeight, overlapTileDimension;
        protected readonly bool periodic;
        private int[][][] compatible;
        protected int[] observed;

        protected int[][][] propagator;

        private (int, int)[] stack;
        private int stackSize, observedSoFar;
        private double sumOfWeights, sumOfWeightLogWeights, startingEntropy;

        private int[] sumsOfOnes;
        private double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;
        protected int actionCount;
        protected bool[][] wave;
        private double[] weightLogWeights, distribution;

        protected double[] weights;

        private int currentStep;

        // <timeStamp, Stack<State<pos, pattern, compatibility array>>>
        private Dictionary<int, Stack<LoggedStep>> backTrackDict;

        protected Model(int outputWidth, int outputHeight, int overlapTileDimension, bool periodic,
            Heuristic heuristic) {
            imageOutputWidth = outputWidth;
            imageOutputHeight = outputHeight;
            this.overlapTileDimension = overlapTileDimension;
            this.periodic = periodic;
            this.heuristic = heuristic;
        }

        private void init() {
            backTrackDict = new Dictionary<int, Stack<LoggedStep>> {
                [0] = new Stack<LoggedStep>()
            };

            wave = new bool[imageOutputWidth * imageOutputHeight][];
            compatible = new int[wave.Length][][];
            for (int i = 0; i < wave.Length; i++) {
                wave[i] = new bool[actionCount];
                compatible[i] = new int[actionCount][];
                for (int t = 0; t < actionCount; t++) {
                    compatible[i][t] = new int[4];
                }
            }

            currentStep = 0;

            distribution = new double[actionCount];
            observed = new int[imageOutputWidth * imageOutputHeight];

            weightLogWeights = new double[actionCount];
            sumOfWeights = 0;
            sumOfWeightLogWeights = 0;

            for (int t = 0; t < actionCount; t++) {
                weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
                sumOfWeights += weights[t];
                sumOfWeightLogWeights += weightLogWeights[t];
            }

            startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

            sumsOfOnes = new int[imageOutputWidth * imageOutputHeight];
            sumsOfWeights = new double[imageOutputWidth * imageOutputHeight];
            sumsOfWeightLogWeights = new double[imageOutputWidth * imageOutputHeight];
            entropies = new double[imageOutputWidth * imageOutputHeight];

            stack = new (int, int)[wave.Length * actionCount];
            stackSize = 0;
        }

        public int run(int seed, int start, int steps) {
            if (wave == null) {
                init();
            }

            if (start == 0) {
                clear();
            }

            Random random = new Random(seed);

            int limit = start + steps;

            for (currentStep = start; currentStep < limit || limit < 0; currentStep++) {
                if (!backTrackDict.ContainsKey(currentStep)) {
                    backTrackDict[currentStep] = new Stack<LoggedStep>();
                }

                int node = nextUnobservedNode(random);
                if (node >= 0) {
                    observe(node, random);
                    if (!propagate()) {
                        if (!backtrack()) {
                            return 0;
                        }
                    }
                } else {
                    for (int i = 0; i < wave.Length; i++) {
                        for (int t = 0; t < actionCount; t++) {
                            if (wave[i][t]) {
                                observed[i] = t;
                                break;
                            }
                        }
                    }

                    return 2;
                }
            }

            return 1;
        }

        private bool backtrack() {
            Console.WriteLine("Starting backtracking for step " + currentStep);
            if (currentStep == 0) {
                return false;
            }

            Stack<LoggedStep> toUndoStack = backTrackDict[currentStep];
            while (toUndoStack.Count > 0) {
                LoggedStep toUndo = toUndoStack.Pop();
                if (!toUndo.wasObserved()) {
                    unBan(toUndo);
                } else {
                    Console.Write("Banning "+ toUndo.getPattern() + " at " + toUndo.getPos());
                    Console.WriteLine(" -> " + sumsOfOnes[toUndo.getPos()] + " patterns left.");
                    bool success = ban(toUndo.getPos(), toUndo.getPattern(), false, false);
                    if (!success) {
                        currentStep--;
                        Console.WriteLine("Backtracking again");
                        return backtrack();
                    }
                }
            }

            currentStep--;
            return true;
        }

        private int nextUnobservedNode(Random random) {
            if (heuristic == Heuristic.SCANLINE) {
                for (int i = observedSoFar; i < wave.Length; i++) {
                    if (!periodic && (i % imageOutputWidth + overlapTileDimension > imageOutputWidth ||
                        i / imageOutputWidth + overlapTileDimension > imageOutputHeight)) {
                        continue;
                    }

                    if (sumsOfOnes[i] > 1) {
                        observedSoFar = i + 1;
                        return i;
                    }
                }

                return -1;
            }

            double min = 1E+4;
            int argMin = -1;
            for (int i = 0; i < wave.Length; i++) {
                if (!periodic && (i % imageOutputWidth + overlapTileDimension > imageOutputWidth ||
                    i / imageOutputWidth + overlapTileDimension > imageOutputHeight)) {
                    continue;
                }

                int remainingValues = sumsOfOnes[i];
                double entropy = heuristic == Heuristic.ENTROPY ? entropies[i] : remainingValues;
                if (remainingValues > 1 && entropy <= min) {
                    double noise = 1E-6 * random.NextDouble();
                    if (entropy + noise < min) {
                        min = entropy + noise;
                        argMin = i;
                    }
                }
            }

            return argMin;
        }

        private void observe(int node, Random random) {
            bool[] w = wave[node];
            for (int t = 0; t < actionCount; t++) {
                distribution[t] = w[t] ? weights[t] : 0.0;
            }

            int r = distribution.Random(random.NextDouble());
            int[] comp = compatible[node][r];
            backTrackDict[currentStep].Push(new LoggedStep(node, r, comp.ToArray(), true));
            for (int t = 0; t < actionCount; t++) {
                if (w[t] != (t == r)) {
                    ban(node, t, false);
                }
            }
        }

        protected bool propagate() {
            bool successfulBans = true;
            while (stackSize > 0) {
                (int i1, int t1) = stack[stackSize - 1];
                stackSize--;

                int x1 = i1 % imageOutputWidth;
                int y1 = i1 / imageOutputWidth;

                for (int d = 0; d < 4; d++) {
                    int x2 = x1 + xChange[d];
                    int y2 = y1 + yChange[d];
                    if (!periodic && (x2 < 0 || y2 < 0 || x2 + overlapTileDimension > imageOutputWidth ||
                        y2 + overlapTileDimension > imageOutputHeight)) {
                        continue;
                    }

                    if (x2 < 0) {
                        x2 += imageOutputWidth;
                    } else if (x2 >= imageOutputWidth) {
                        x2 -= imageOutputWidth;
                    }

                    if (y2 < 0) {
                        y2 += imageOutputHeight;
                    } else if (y2 >= imageOutputHeight) {
                        y2 -= imageOutputHeight;
                    }

                    int i2 = x2 + y2 * imageOutputWidth;
                    int[] p = propagator[d][t1];
                    int[][] compat = compatible[i2];

                    foreach (int t2 in p) {
                        int[] comp = compat[t2];

                        comp[d]--;
                        if (comp[d] == 0) {
                            bool success = ban(i2, t2, false);
                            if (!success) {
                                successfulBans = false;
                            }
                        }
                    }
                }
            }

            return sumsOfOnes[0] > 0 && successfulBans;
        }

        protected bool ban(int position, int patternIdx, bool wasObserved, bool addBackTrack = true) {
            wave[position][patternIdx] = false;

            int[] comp = compatible[position][patternIdx];
            if (addBackTrack) {
                backTrackDict[currentStep].Push(new LoggedStep(position, patternIdx, comp.ToArray(), wasObserved));
            }

            for (int direction = 0; direction < 4; direction++) {
                comp[direction] = 0;
            }

            stack[stackSize] = (position, patternIdx);
            stackSize++;

            sumsOfOnes[position]--;

            sumsOfWeights[position] -= weights[patternIdx];
            sumsOfWeightLogWeights[position] -= weightLogWeights[patternIdx];

            double sum = sumsOfWeights[position];
            entropies[position] = Math.Log(sum) - sumsOfWeightLogWeights[position] / sum;

            return sumsOfOnes[position] != 0;
        }

        private void unBan(LoggedStep loggedStep) {
            int position = loggedStep.getPos();
            int patternIdx = loggedStep.getPattern();

            sumsOfOnes[position]++;
            sumsOfWeights[position] += weights[patternIdx];
            sumsOfWeightLogWeights[position] += weightLogWeights[patternIdx];

            double sum = sumsOfWeights[position];
            entropies[position] = Math.Log(sum) - sumsOfWeightLogWeights[position] / sum;

            for (int direction = 0; direction < 4; direction++) {
                compatible[position][patternIdx][direction] = loggedStep.getCompat().ToArray()[direction];
            }

            wave[position][patternIdx] = true;
        }

        protected virtual void clear() {
            for (int nodeIdx = 0; nodeIdx < wave.Length; nodeIdx++) {
                for (int patternIdx = 0; patternIdx < actionCount; patternIdx++) {
                    wave[nodeIdx][patternIdx] = true;
                    for (int direction = 0; direction < 4; direction++) {
                        compatible[nodeIdx][patternIdx][direction] = propagator[opposite[direction]][patternIdx].Length;
                    }
                }

                sumsOfOnes[nodeIdx] = weights.Length;
                sumsOfWeights[nodeIdx] = sumOfWeights;
                sumsOfWeightLogWeights[nodeIdx] = sumOfWeightLogWeights;
                entropies[nodeIdx] = startingEntropy;
                observed[nodeIdx] = -1;
            }

            observedSoFar = 0;
        }

        public abstract Bitmap graphics();
    }

    internal class LoggedStep {
        private readonly int position, pattern;
        private readonly int[] compatibilities;
        private readonly bool observed;

        public LoggedStep(int curPosition, int curPattern, int[] curCompatibilities, bool wasObserved) {
            position = curPosition;
            pattern = curPattern;
            compatibilities = curCompatibilities;
            observed = wasObserved;
        }

        public int getPos() {
            return position;
        }

        public int getPattern() {
            return pattern;
        }

        public int[] getCompat() {
            return compatibilities;
        }

        public bool wasObserved() {
            return observed;
        }
    }
}