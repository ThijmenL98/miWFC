using System;
using System.Collections.Generic;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Wfc {
    /// <summary>
    /// WavePropagator holds a wave, and supports updating it's possibilities 
    /// according to the model constraints.
    /// </summary>
    internal class WavePropagator {
        // Main data tracking what we've decided so far
        private Wave wave;

        private readonly PatternModelConstraint patternModelConstraint;

        // From model
        private readonly int patternCount;
        private readonly double[] frequencies;

        // Used for backtracking
        private Deque<IndexPatternItem> backtrackItems;
        private Deque<int> backtrackItemsLengths;
        private Deque<IndexPatternItem> prevChoices;
        private int droppedBacktrackItemsCount;
        private int backtrackCount; // Purely informational

        // Basic parameters
        private readonly int indexCount;
        private readonly bool backtrack;
        private readonly int backtrackDepth;
        private readonly IWaveConstraint[] constraints;
        private readonly Func<double> randomDouble;
        private readonly FrequencySet[] frequencySets;
        private readonly int selectionHeuristic, patternHeuristic;

        // We evaluate constraints at the last possible minute, instead of eagerly like the model,
        // As they can potentially be expensive.
        private bool deferredConstraintsStep;

        // The overall status of the propagator, always kept up to date
        private Resolution status;

        private readonly ITopology topology;
        private int directionsCount;

        private List<ITracker> trackers;

        private IPickHeuristic pickHeuristic;

        private readonly int outWidth, outHeight;

        public WavePropagator(
            PatternModel model,
            ITopology topology,
            int outputWidth,
            int outputHeight,
            int selectionHeuristic,
            int patternHeuristic,
            int backtrackDepth = 0,
            IWaveConstraint[] constraints = null,
            Func<double> randomDouble = null,
            FrequencySet[] frequencySets = null,
            bool clear = true) {
            patternCount = model.PatternCount;
            frequencies = model.Frequencies;

            outWidth = outputWidth;
            outHeight = outputHeight;

            indexCount = topology.IndexCount;
            backtrack = backtrackDepth != 0;
            this.backtrackDepth = backtrackDepth;
            this.constraints = constraints ?? new IWaveConstraint[0];
            this.topology = topology;
            this.randomDouble = randomDouble ?? new Random(1).NextDouble;
            this.frequencySets = frequencySets;
            this.selectionHeuristic = selectionHeuristic;
            this.patternHeuristic = patternHeuristic;
            directionsCount = topology.DirectionsCount;

            patternModelConstraint = new PatternModelConstraint(this, model);

            if (clear) {
                this.clear();
            }
        }

        // This is only exposed publically
        // in case users write their own constraints, it's not 
        // otherwise useful.

        #region Internal API

        public Wave Wave => wave;
        public int IndexCount => indexCount;
        public ITopology Topology => topology;
        public Func<double> RandomDouble => randomDouble;

        public int PatternCount => patternCount;
        public double[] Frequencies => frequencies;

        /**
         * Requires that index, pattern is possible
         */
        public bool internalBan(int index, int pattern) {
            // Record information for backtracking
            if (backtrack) {
                backtrackItems.push(new IndexPatternItem {
                    Index = index,
                    Pattern = pattern,
                });
            }

            patternModelConstraint.doBan(index, pattern);

            // Update the wave
            bool isContradiction = wave.removePossibility(index, pattern);

            // Update trackers
            foreach (ITracker tracker in trackers) {
                tracker.doBan(index, pattern);
            }

            return isContradiction;
        }

        public bool internalSelect(int index, int chosenPattern) {
            for (int pattern = 0; pattern < patternCount; pattern++) {
                if (pattern == chosenPattern) {
                    continue;
                }

                if (wave.get(index, pattern)) {
                    if (internalBan(index, pattern)) {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion


        private void observe(out int index, out int pattern) {
            pickHeuristic.pickObservation(out index, out pattern);
            if (index == -1) {
                return;
            }

            // Decide on the given cell
            if (internalSelect(index, pattern)) {
                status = Resolution.CONTRADICTION;
            }
        }

        private void observeWith(int index, int pattern) {
            if (index == -1) {
                return;
            }

            // Decide on the given cell
            if (internalSelect(index, pattern)) {
                status = Resolution.CONTRADICTION;
            }
        }

        // Returns the only possible value of a cell if there is only one,
        // otherwise returns -1 (multiple possible) or -2 (none possible)
        private int getDecidedCell(int index) {
            int decidedPattern = (int) Resolution.CONTRADICTION;
            for (int pattern = 0; pattern < patternCount; pattern++) {
                if (wave.get(index, pattern)) {
                    if (decidedPattern == (int) Resolution.CONTRADICTION) {
                        decidedPattern = pattern;
                    } else {
                        return (int) Resolution.UNDECIDED;
                    }
                }
            }

            return decidedPattern;
        }

        private void initConstraints() {
            foreach (IWaveConstraint constraint in constraints) {
                constraint.init(this);
                if (status != Resolution.UNDECIDED) {
                    return;
                }

                patternModelConstraint.propagate();
                if (status != Resolution.UNDECIDED) {
                    return;
                }
            }

            return;
        }

        private void stepConstraints() {
            // TOODO: Do we need to worry about evaluating constraints multiple times?
            foreach (IWaveConstraint constraint in constraints) {
                constraint.check(this);
                if (status != Resolution.UNDECIDED) {
                    return;
                }

                patternModelConstraint.propagate();
                if (status != Resolution.UNDECIDED) {
                    return;
                }
            }

            deferredConstraintsStep = false;
        }

        public Resolution Status => status;
        public int BacktrackCount => backtrackCount;

        /**
         * Resets the wave to it's original state
         */
        public Resolution clear() {
            wave = new Wave(frequencies.Length, indexCount);

            if (backtrack) {
                backtrackItems = new Deque<IndexPatternItem>();
                backtrackItemsLengths = new Deque<int>();
                backtrackItemsLengths.push(0);
                prevChoices = new Deque<IndexPatternItem>();
            }

            status = Resolution.UNDECIDED;
            trackers = new List<ITracker>();
            if (frequencySets != null) {
                ArrayPriorityEntropyTracker entropyTracker = new(wave, frequencySets, topology.Mask);
                entropyTracker.reset();
                addTracker(entropyTracker);
                pickHeuristic = new ArrayPriorityEntropyHeuristic(entropyTracker, randomDouble);
            } else {
                EntropyTracker entropyTracker = new(wave, frequencies, topology.Mask, outWidth, outHeight);
                entropyTracker.reset();
                addTracker(entropyTracker);
                pickHeuristic
                    = new EntropyHeuristic(entropyTracker, randomDouble, selectionHeuristic, patternHeuristic);
            }

            patternModelConstraint.clear();

            if (status == Resolution.CONTRADICTION) {
                return status;
            }

            initConstraints();

            return status;
        }

        /**
         * Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
         */
        public void setContradiction() {
            status = Resolution.CONTRADICTION;
        }

        /**
         * Removes pattern as a possibility from index
         */
        public Resolution ban(int x, int y, int z, int pattern) {
            int index = topology.getIndex(x, y, z);
            if (wave.get(index, pattern)) {
                deferredConstraintsStep = true;
                if (internalBan(index, pattern)) {
                    return status = Resolution.CONTRADICTION;
                }
            }

            patternModelConstraint.propagate();
            return status;
        }

        /**
         * Make some progress in the WaveFunctionCollapseAlgorithm
         */
        public Resolution step() {
            int index;

            // This will true if the user has called Ban, etc, since the last step.
            if (deferredConstraintsStep) {
                stepConstraints();
            }

            // If we're already in a final state. skip making an observiation, 
            // and jump to backtrack handling / return.
            if (status != Resolution.UNDECIDED) {
                index = 0;
                goto restart;
            }

            // Record state before making a choice
            if (backtrack) {
                backtrackItemsLengths.push(droppedBacktrackItemsCount + backtrackItems.Count);
                // Clean up backtracks if they are too long
                while (backtrackDepth != -1 && backtrackItemsLengths.Count > backtrackDepth) {
                    int newDroppedCount = backtrackItemsLengths.unshift();
                    prevChoices.unshift();
                    backtrackItems.dropFirst(newDroppedCount - droppedBacktrackItemsCount);
                    droppedBacktrackItemsCount = newDroppedCount;
                }
            }

            // Pick a tile and Select a pattern from it.
            observe(out index, out int pattern);

            // Record what was selected for backtracking purposes
            if (index != -1 && backtrack) {
                prevChoices.push(new IndexPatternItem {Index = index, Pattern = pattern});
            }

            // After a backtrack we resume here
            restart:

            if (status == Resolution.UNDECIDED) {
                patternModelConstraint.propagate();
            }

            if (status == Resolution.UNDECIDED) {
                stepConstraints();
            }

            // Are all things are fully chosen?
            if (index == -1 && status == Resolution.UNDECIDED) {
                status = Resolution.DECIDED;
                return status;
            }

            if (backtrack && status == Resolution.CONTRADICTION) {
                // After back tracking, it's no logner the case things are fully chosen
                index = 0;

                // Actually backtrack
                while (true) {
                    if (backtrackItemsLengths.Count == 1) {
                        // We've backtracked as much as we can, but 
                        // it's still not possible. That means it is imposible
                        return Resolution.CONTRADICTION;
                    }

                    doBacktrack();
                    IndexPatternItem item = prevChoices.pop();
                    backtrackCount++;
                    status = Resolution.UNDECIDED;
                    // Mark the given choice as impossible
                    if (internalBan(item.Index, item.Pattern)) {
                        status = Resolution.CONTRADICTION;
                    }

                    if (status == Resolution.UNDECIDED) {
                        patternModelConstraint.propagate();
                    }

                    if (status == Resolution.CONTRADICTION) {
                        // If still in contradiction, repeat backtracking

                        continue;
                    } else {
                        // Include the last ban as part of the previous backtrack
                        backtrackItemsLengths.pop();
                        backtrackItemsLengths.push(droppedBacktrackItemsCount + backtrackItems.Count);
                    }

                    goto restart;
                }
            }

            return status;
        }

        public Resolution stepWith(int index, int pattern) {
            // This will true if the user has called Ban, etc, since the last step.
            if (deferredConstraintsStep) {
                stepConstraints();
            }

            // If we're already in a final state. skip making an observiation, 
            // and jump to backtrack handling / return.
            if (status != Resolution.UNDECIDED) {
                goto restart;
            }

            // Record state before making a choice
            if (backtrack) {
                backtrackItemsLengths.push(droppedBacktrackItemsCount + backtrackItems.Count);
                // Clean up backtracks if they are too long
                while (backtrackDepth != -1 && backtrackItemsLengths.Count > backtrackDepth) {
                    int newDroppedCount = backtrackItemsLengths.unshift();
                    prevChoices.unshift();
                    backtrackItems.dropFirst(newDroppedCount - droppedBacktrackItemsCount);
                    droppedBacktrackItemsCount = newDroppedCount;
                }
            }

            // Pick a tile and Select a pattern from it.
            observeWith(index, pattern);

            // Record what was selected for backtracking purposes
            if (index != -1 && backtrack) {
                prevChoices.push(new IndexPatternItem {Index = index, Pattern = pattern});
            }

            // After a backtrack we resume here
            restart:

            if (status == Resolution.UNDECIDED) {
                patternModelConstraint.propagate();
            }

            if (status == Resolution.UNDECIDED) {
                stepConstraints();
            }

            // Are all things are fully chosen?
            if (index == -1 && status == Resolution.UNDECIDED) {
                status = Resolution.DECIDED;
                return status;
            }

            if (backtrack && status == Resolution.CONTRADICTION) {
                // After back tracking, it's no logner the case things are fully chosen
                index = 0;

                // Actually backtrack
                while (true) {
                    if (backtrackItemsLengths.Count == 1) {
                        // We've backtracked as much as we can, but 
                        // it's still not possible. That means it is imposible
                        return Resolution.CONTRADICTION;
                    }

                    doBacktrack();
                    IndexPatternItem item = prevChoices.pop();
                    backtrackCount++;
                    status = Resolution.UNDECIDED;
                    // Mark the given choice as impossible
                    if (internalBan(item.Index, item.Pattern)) {
                        status = Resolution.CONTRADICTION;
                    }

                    if (status == Resolution.UNDECIDED) {
                        patternModelConstraint.propagate();
                    }

                    if (status == Resolution.CONTRADICTION) {
                        // If still in contradiction, repeat backtracking

                        continue;
                    } else {
                        // Include the last ban as part of the previous backtrack
                        backtrackItemsLengths.pop();
                        backtrackItemsLengths.push(droppedBacktrackItemsCount + backtrackItems.Count);
                    }

                    goto restart;
                }
            }

            return status;
        }

        public Resolution stepBack() {
            if (backtrackItemsLengths.Count == 1) {
                return Resolution.CONTRADICTION;
            }

            doBacktrack();
            if (prevChoices.Count > 0) {
                prevChoices.pop();
            }

            backtrackCount++;
            status = Resolution.UNDECIDED;
            return status;
        }

        public Resolution propagateSingle() {
            if (status == Resolution.UNDECIDED) {
                patternModelConstraint.propagate();
            }

            if (status == Resolution.UNDECIDED) {
                stepConstraints();
            }

            // Are all things are fully chosen?
            if (status == Resolution.UNDECIDED) {
                status = Resolution.DECIDED;
                return status;
            }

            return status;
        }

        private void doBacktrack() {
            int targetLength = backtrackItemsLengths.pop() - droppedBacktrackItemsCount;
            // Undo each item
            while (backtrackItems.Count > targetLength) {
                IndexPatternItem item = backtrackItems.pop();
                int index = item.Index;
                int pattern = item.Pattern;

                // Also add the possibility back
                // as it is removed in InternalBan
                wave.addPossibility(index, pattern);
                // Update trackers
                foreach (ITracker tracker in trackers) {
                    tracker.undoBan(index, pattern);
                }

                // Next, undo the decremenents done in Propagate
                patternModelConstraint.undoBan(item);
            }
        }

        public void addTracker(ITracker tracker) {
            trackers.Add(tracker);
        }

        public void removeTracker(ITracker tracker) {
            trackers.Remove(tracker);
        }

        /**
         * Rpeatedly step until the status is Decided or Contradiction
         */
        public Resolution run() {
            while (true) {
                step();
                if (status != Resolution.UNDECIDED) {
                    return status;
                }
            }
        }

        /**
         * Returns the array of decided patterns, writing
         * -1 or -2 to indicate cells that are undecided or in contradiction.
         */
        public ITopoArray<int> toTopoArray() {
            return TopoArray.createByIndex(getDecidedCell, topology);
        }

        /**
         * Returns an array where each cell is a list of remaining possible patterns.
         */
        public ITopoArray<ISet<int>> toTopoArraySets() {
            return TopoArray.createByIndex(index => {
                HashSet<int> hs = new HashSet<int>();
                for (int pattern = 0; pattern < patternCount; pattern++) {
                    if (wave.get(index, pattern)) {
                        hs.Add(pattern);
                    }
                }

                return (ISet<int>) hs;
            }, topology);
        }
    }
}