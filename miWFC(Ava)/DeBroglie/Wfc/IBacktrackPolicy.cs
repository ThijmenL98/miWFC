using System;
using System.Collections.Generic;
using miWFC.DeBroglie.Trackers;

namespace miWFC.DeBroglie.Wfc;

public interface IBacktrackPolicy {
    void Init(WavePropagator wavePropagator);

    /// <summary>
    ///     0  = Give up
    ///     1  = Backtrack
    ///     >1 = Backjump
    /// </summary>
    int GetBackjump();
}

internal class ConstantBacktrackPolicy : IBacktrackPolicy {
    private readonly int amount;

    public ConstantBacktrackPolicy(int amount) {
        this.amount = amount;
    }

    public void Init(WavePropagator wavePropagator) { }

    public int GetBackjump() {
        return amount;
    }
}

// After 10 failed backtracks, backjumps by 4 (level 0) and repeats.
// Each subsequent level backtrackts twice as far, but waits twice as long to trigger.
// This means that after a backjump, we try exactly as hard the 2nd time as we did the first, including
// trying smaller backjumps.
// Whenever forward progress is made, all levels are reset.
internal class PatienceBackjumpPolicy : IBacktrackPolicy, IChoiceObserver {
    private long counter;
    private int depth;

    private List<Level> levels;
    private int maxDepth;
    private long start;

    public void Init(WavePropagator wavePropagator) {
        wavePropagator.AddChoiceObserver(this);
        counter = 0;
        depth = 0;
        maxDepth = 0;
        start = 0;
    }

    public int GetBackjump() {
        if (levels == null) {
            levels = new List<Level>();
        }

        // Find first non-expired level
        int i;
        for (i = 0; i < levels.Count; i++) {
            if (levels[i].Timeout > counter) {
                break;
            }
        }

        // Lazily add higher levels as needed
        if (levels.Count <= i) {
            levels.Add(CreateLevel(i));
        }

        if (i == 0) {
            return 1;
        }

        // Backjump to highest expired level
        int depthDelta = depth - levels[i - 1].Depth;

        // Reset any expired levels
        for (int j = 0; j < i; j++) {
            ResetLevel(j);
        }

        return depth - levels[i - 1].Depth;
    }

    public void MakeChoice(int index, int pattern) {
        depth++;
        if (depth > maxDepth) {
            maxDepth = depth;
            // Reset levels
            levels = null;
            start = counter;
        }
    }

    public void Backtrack() {
        counter++;
        depth--;
    }

    private Level CreateLevel(int level) {
        return new Level {
            Depth = maxDepth - 4 * (int) Math.Pow(2, level),
            Timeout = start + 10 * (long) Math.Pow(2, level)
        };
    }

    private void ResetLevel(int level) {
        levels[level].Timeout = counter + 10 * (long) Math.Pow(2, level);
    }

    private class Level {
        public int Depth;
        public long Timeout;
    }
}