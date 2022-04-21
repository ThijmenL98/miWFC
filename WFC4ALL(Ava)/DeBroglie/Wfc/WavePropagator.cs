using System;
using System.Collections.Generic;
using System.Diagnostics;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Trackers;

namespace WFC4ALL.DeBroglie.Wfc;

public class WavePropagatorOptions {
    public IBacktrackPolicy BacktrackPolicy { get; set; }
    public IWaveConstraint[] Constraints { get; set; }
    public Func<double> RandomDouble { get; set; }
    public IIndexPicker IndexPicker { get; set; }
    public IPatternPicker PatternPicker { get; set; }
    public bool Clear { get; set; } = true;
    public ModelConstraintAlgorithm ModelConstraintAlgorithm { get; set; }
}

/// <summary>
///     WavePropagator holds a wave, and supports updating it's possibilities
///     according to the model constraints.
/// </summary>
public class WavePropagator {
    private readonly IWaveConstraint[] constraints;

    private readonly IIndexPicker indexPicker;

    private readonly int outWidth, outHeight;
    private readonly IPatternPicker patternPicker;

    // In

    // Used for backtracking
    private Deque<IndexPatternItem> backtrackItems;
    private Deque<int> backtrackItemsLengths;
    private readonly IBacktrackPolicy backtrackPolicy;
    private List<IChoiceObserver> choiceObservers;

    public string _contradictionReason;
    public object _contradictionSource;

    // We evaluate constraints at the last possible minute, instead of eagerly like the model,
    // As they can potentially be expensive.
    private bool deferredConstraintsStep;

    // Used for MaxBacktrackDepth
    private int droppedBacktrackItemsCount;

    // Basic parameters

    // From model

    private readonly IPatternModelConstraint patternModelConstraint;

    private Deque<IndexPatternItem> prevChoices;

    // The overall status of the propagator, always kept up to date

    private List<ITracker> trackers;

    // Main data tracking what we've decided so far

    public WavePropagator(
        PatternModel model,
        ITopology topology,
        int outputWidth,
        int outputHeight,
        WavePropagatorOptions options) {
        PatternCount = model.PatternCount;
        Frequencies = model.Frequencies;

        outWidth = outputWidth;
        outHeight = outputHeight;

        IndexCount = topology.IndexCount;
        backtrackPolicy = options.BacktrackPolicy;
        constraints = options.Constraints ?? new IWaveConstraint[0];
        Topology = topology;
        RandomDouble = options.RandomDouble ?? new Random().NextDouble;
        indexPicker = options.IndexPicker ?? new EntropyTracker();
        patternPicker = options.PatternPicker ?? new WeightedRandomPatternPicker();

        switch (options.ModelConstraintAlgorithm) {
            case ModelConstraintAlgorithm.ONE_STEP:
                patternModelConstraint = new OneStepPatternModelConstraint(this, model);
                break;
            case ModelConstraintAlgorithm.DEFAULT:
            case ModelConstraintAlgorithm.AC4:
                patternModelConstraint = new Ac4PatternModelConstraint(this, model);
                break;
            case ModelConstraintAlgorithm.AC3:
                patternModelConstraint = new Ac3PatternModelConstraint(this, model);
                break;
            default:
                throw new Exception();
        }

        if (options.Clear) {
            Clear();
        }
    }

    public Resolution Status { get; private set; }

    public string ContradictionReason => _contradictionReason;
    public object ContradictionSource => _contradictionSource;
    public int BacktrackCount { get; private set; }

    public int BackjumpCount { get; private set; }

    /// <summary>
    ///     Returns the only possible value of a cell if there is only one,
    ///     otherwise returns -1 (multiple possible) or -2 (none possible)
    /// </summary>
    public int GetDecidedPattern(int index) {
        int decidedPattern = (int) Resolution.CONTRADICTION;
        for (int pattern = 0; pattern < PatternCount; pattern++) {
            if (Wave.Get(index, pattern)) {
                if (decidedPattern == (int) Resolution.CONTRADICTION) {
                    decidedPattern = pattern;
                } else {
                    return (int) Resolution.UNDECIDED;
                }
            }
        }

        return decidedPattern;
    }

    private void InitConstraints() {
        foreach (IWaveConstraint constraint in constraints) {
            constraint.Init(this);
            if (Status != Resolution.UNDECIDED) {
                return;
            }

            patternModelConstraint.Propagate();
            if (Status != Resolution.UNDECIDED) {
                return;
            }
        }
    }

    public void StepConstraints() {
        // TODO: Do we need to worry about evaluating constraints multiple times?
        foreach (IWaveConstraint constraint in constraints) {
            constraint.Check(this);
            if (Status != Resolution.UNDECIDED) {
                return;
            }

            patternModelConstraint.Propagate();
            if (Status != Resolution.UNDECIDED) {
                return;
            }
        }

        deferredConstraintsStep = false;
    }

    /**
         * Resets the wave to it's original state
         */
    public Resolution Clear() {
        Wave = new Wave(Frequencies.Length, IndexCount);

        backtrackItems = new Deque<IndexPatternItem>();
        backtrackItemsLengths = new Deque<int>();
        backtrackItemsLengths.Push(0);
        prevChoices = new Deque<IndexPatternItem>();

        Status = Resolution.UNDECIDED;
        _contradictionReason = null;
        _contradictionSource = null;
        trackers = new List<ITracker>();
        choiceObservers = new List<IChoiceObserver>();
        indexPicker.Init(this);
        patternPicker.Init(this);
        backtrackPolicy?.Init(this);

        patternModelConstraint.Clear();

        if (Status == Resolution.CONTRADICTION) {
            return Status;
        }

        InitConstraints();

        return Status;
    }

    /**
         * Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
         */
    public void SetContradiction() {
        Status = Resolution.CONTRADICTION;
    }

    /**
         * Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
         */
    public void SetContradiction(string reason, object source) {
        Status = Resolution.CONTRADICTION;
        _contradictionReason = reason;
        _contradictionSource = source;
    }

    /**
         * Removes pattern as a possibility from index
         */
    public Resolution ban(int x, int y, int z, int pattern) {
        int index = Topology.GetIndex(x, y, z);
        if (Wave.Get(index, pattern)) {
            deferredConstraintsStep = true;
            if (InternalBan(index, pattern)) {
                return Status = Resolution.CONTRADICTION;
            }
        }

        patternModelConstraint.Propagate();
        return Status;
    }

    public void PushSelection(int px, int py, int pz, int pattern) {
        int index = Topology.GetIndex(px, py, pz);
        PushSelection(index, pattern);
    }

    public void PushSelection(int index, int pattern) {
        backtrackItemsLengths.Push(droppedBacktrackItemsCount + backtrackItems.Count);
        prevChoices.Push(new IndexPatternItem {Index = index, Pattern = pattern});
        backtrackItems.Push(new IndexPatternItem {Index = index, Pattern = pattern});
    }

    /**
         * Make some progress in the WaveFunctionCollapseAlgorithm
         */
    public Resolution Step() {
        // This will be true if the user has called Ban, etc, since the last step.
        if (deferredConstraintsStep) {
            StepConstraints();
        }

        // If we're already in a final state. skip making an observiation.
        if (Status == Resolution.UNDECIDED) {
            // Pick a index to use
            int index = indexPicker.GetRandomIndex(RandomDouble);

            if (index != -1) {
                // Pick a tile to select at that index
                int pattern = patternPicker.GetRandomPossiblePatternAt(index, RandomDouble);

                RecordBacktrack(index, pattern);

                // Use the pick
                if (InternalSelect(index, pattern)) {
                    Status = Resolution.CONTRADICTION;
                }
            }

            // Re-evaluate status
            if (Status == Resolution.UNDECIDED) {
                patternModelConstraint.Propagate();
            }

            if (Status == Resolution.UNDECIDED) {
                StepConstraints();
            }

            // If we've made all possible choices, and found no contradictions,
            // then we've succeeded.
            if (index == -1 && Status == Resolution.UNDECIDED) {
                Status = Resolution.DECIDED;
                return Status;
            }
        }

        TryBacktrackUntilNoContradiction();

        return Status;
    }

    /**
         * Make some progress in the WaveFunctionCollapseAlgorithm
         */
    public Resolution StepWith(int index, int pattern) {
        // This will be true if the user has called Ban, etc, since the last step.
        if (deferredConstraintsStep) {
            StepConstraints();
        }

        // If we're already in a final state. skip making an observiation.
        if (Status == Resolution.UNDECIDED) {
            RecordBacktrack(index, pattern);

            // Use the pick
            if (InternalSelect(index, pattern)) {
                Status = Resolution.CONTRADICTION;
            }

            // Re-evaluate status
            if (Status == Resolution.UNDECIDED) {
                patternModelConstraint.Propagate();
            }

            if (Status == Resolution.UNDECIDED) {
                StepConstraints();
            }

            // If we've made all possible choices, and found no contradictions,
            // then we've succeeded.
            if (index == -1 && Status == Resolution.UNDECIDED) {
                Status = Resolution.DECIDED;
                return Status;
            }
        }

        TryBacktrackUntilNoContradiction();

        return Status;
    }

    private void RecordBacktrack(int index, int pattern) {
        backtrackItemsLengths.Push(droppedBacktrackItemsCount + backtrackItems.Count);
        prevChoices.Push(new IndexPatternItem {Index = index, Pattern = pattern});

        foreach (IChoiceObserver co in choiceObservers) {
            co.MakeChoice(index, pattern);
        }
    }

    public void TryBacktrackUntilNoContradiction() {
        while (Status == Resolution.CONTRADICTION) {
            int backjumpAmount = backtrackPolicy?.GetBackjump() ?? 1;

            for (int i = 0; i < backjumpAmount; i++) {
                if (backtrackItemsLengths.Count == 1) {
                    // We've backtracked as much as we can, but 
                    // it's still not possible. That means it is imposible
                    return;
                }

                // Actually undo various bits of state
                DoBacktrack();

                IndexPatternItem item = prevChoices.Pop();
                Status = Resolution.UNDECIDED;
                _contradictionReason = null;
                _contradictionSource = null;

                foreach (IChoiceObserver co in choiceObservers) {
                    co.Backtrack();
                }

                if (backjumpAmount == 1) {
                    BacktrackCount++;

                    // Mark the given choice as impossible
                    if (InternalBan(item.Index, item.Pattern)) {
                        Status = Resolution.CONTRADICTION;
                    }
                }
            }

            if (backjumpAmount > 1) {
                BackjumpCount++;
            }

            // Revalidate status.
            if (Status == Resolution.UNDECIDED) {
                patternModelConstraint.Propagate();
            }

            if (Status == Resolution.UNDECIDED) {
                StepConstraints();
            }
        }
    }

    public void DoCustomBacktrack() {
        if (backtrackItemsLengths.Count == 1) {
            // We've backtracked as much as we can, but 
            // it's still not possible. That means it is imposible
            return;
        }

        // Actually undo various bits of state
        DoBacktrack();

        Status = Resolution.UNDECIDED;
        
        // Revalidate status.
        if (Status == Resolution.UNDECIDED) {
            patternModelConstraint.Propagate();
        }

        if (Status == Resolution.UNDECIDED) {
            StepConstraints();
        }
    }

    // Actually does the work of undoing what was previously recorded
    public void DoBacktrack() {
        int targetLength = backtrackItemsLengths.Pop() - droppedBacktrackItemsCount;
        // Undo each item
        while (backtrackItems.Count > targetLength) {
            IndexPatternItem item = backtrackItems.Pop();
            int index = item.Index;
            int pattern = item.Pattern;

            // Also add the possibility back
            // as it is removed in InternalBan
            Wave.AddPossibility(index, pattern);
            // Update trackers
            foreach (ITracker tracker in trackers) {
                tracker.UndoBan(index, pattern);
            }

            // Next, undo the decremenents done in Propagate
            patternModelConstraint.UndoBan(index, pattern);
        }
    }

    public void AddTracker(ITracker tracker) {
        trackers.Add(tracker);
    }

    public void RemoveTracker(ITracker tracker) {
        trackers.Remove(tracker);
    }

    public void AddChoiceObserver(IChoiceObserver co) {
        choiceObservers.Add(co);
    }

    public void RemoveChoiceObserver(IChoiceObserver co) {
        choiceObservers.Remove(co);
    }

    /**
         * Rpeatedly step until the status is Decided or Contradiction
         */
    public Resolution Run() {
        while (true) {
            Step();
            if (Status != Resolution.UNDECIDED) {
                return Status;
            }
        }
    }

    /**
     * Returns the array of decided patterns, writing
     * -1 or -2 to indicate cells that are undecided or in contradiction.
     */
    public ITopoArray<int> ToTopoArray() {
        return TopoArray.CreateByIndex(GetDecidedPattern, Topology);
    }

    /**
         * Returns an array where each cell is a list of remaining possible patterns.
         */
    public ITopoArray<ISet<int>> ToTopoArraySets() {
        return TopoArray.CreateByIndex(index => {
            HashSet<int> hs = new();
            for (int pattern = 0; pattern < PatternCount; pattern++) {
                if (Wave.Get(index, pattern)) {
                    hs.Add(pattern);
                }
            }

            return (ISet<int>) hs;
        }, Topology);
    }

    public IEnumerable<int> GetPossiblePatterns(int index) {
        for (int pattern = 0; pattern < PatternCount; pattern++) {
            if (Wave.Get(index, pattern)) {
                yield return pattern;
            }
        }
    }

    // This is only exposed publically
    // in case users write their own constraints, it's not 
    // otherwise useful.

    #region Internal API

    public Wave Wave { get; private set; }

    public int IndexCount { get; }

    public ITopology Topology { get; }

    public Func<double> RandomDouble { get; }

    public int PatternCount { get; }

    public double[] Frequencies { get; }

    /**
         * Requires that index, pattern is possible
         */
    public bool InternalBan(int index, int pattern) {
        // Record information for backtracking
        backtrackItems.Push(new IndexPatternItem {
            Index = index,
            Pattern = pattern
        });

        patternModelConstraint.DoBan(index, pattern);

        // Update the wave
        bool isContradiction = Wave.RemovePossibility(index, pattern);

        // Update trackers
        foreach (ITracker tracker in trackers) {
            tracker.DoBan(index, pattern);
        }

        return isContradiction;
    }

    public bool InternalSelect(int index, int chosenPattern) {
        // Simple, inefficient way
        if (!Optimizations.QuickSelect) {
            for (int pattern = 0; pattern < PatternCount; pattern++) {
                if (pattern == chosenPattern) {
                    continue;
                }

                if (Wave.Get(index, pattern)) {
                    if (InternalBan(index, pattern)) {
                        return true;
                    }
                }
            }

            return false;
        }

        bool isContradiction = false;

        patternModelConstraint.DoSelect(index, chosenPattern);

        for (int pattern = 0; pattern < PatternCount; pattern++) {
            if (pattern == chosenPattern) {
                continue;
            }

            if (Wave.Get(index, pattern)) {
                // This is mostly a repeat of InternalBan, as except for patternModelConstraint,
                // Selects are just seen as a set of bans


                // Record information for backtracking
                backtrackItems.Push(new IndexPatternItem {
                    Index = index,
                    Pattern = pattern
                });

                // Don't update patternModelConstraint here, it's been done above in DoSelect

                // Update the wave
                isContradiction = isContradiction || Wave.RemovePossibility(index, pattern);

                // Update trackers
                foreach (ITracker tracker in trackers) {
                    tracker.DoBan(index, pattern);
                }
            }
        }

        return false;
    }

    #endregion
}