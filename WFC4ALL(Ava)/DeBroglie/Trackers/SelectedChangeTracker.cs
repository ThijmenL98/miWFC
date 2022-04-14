using System.Collections.Generic;
using WFC4ALL.DeBroglie.Models;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

public interface IQuadstateChanged {
    void Reset(SelectedChangeTracker tracker);

    void Notify(int index, Quadstate before, Quadstate after);
}

/// <summary>
///     Runs a callback when the banned/selected status of tile changes with respect to a tileset.
/// </summary>
public class SelectedChangeTracker : ITracker {
    private readonly IQuadstateChanged onChange;

    // Indexed by tile topology
    private readonly int[] patternCounts;

    private readonly TileModelMapping tileModelMapping;
    private readonly TilePropagator tilePropagator;

    private readonly TilePropagatorTileSet tileSet;

    private readonly Quadstate[] values;

    private readonly WavePropagator wavePropagator;

    internal SelectedChangeTracker(TilePropagator tilePropagator, WavePropagator wavePropagator,
        TileModelMapping tileModelMapping, TilePropagatorTileSet tileSet, IQuadstateChanged onChange) {
        this.tilePropagator = tilePropagator;
        this.wavePropagator = wavePropagator;
        this.tileModelMapping = tileModelMapping;
        this.tileSet = tileSet;
        this.onChange = onChange;
        patternCounts = new int[tilePropagator.Topology.IndexCount];
        values = new Quadstate[tilePropagator.Topology.IndexCount];
    }

    public void DoBan(int patternIndex, int pattern) {
        if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null) {
            DoBan(patternIndex, pattern, patternIndex, 0);
        } else {
            foreach ((Point p, int index, int offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.get(
                         patternIndex)) {
                DoBan(patternIndex, pattern, index, offset);
            }
        }
    }

    public void Reset() {
        Wave wave = wavePropagator.Wave;
        foreach (int index in tilePropagator.Topology.GetIndices()) {
            tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int offset);
            ISet<int> patterns = tileModelMapping.GetPatterns(tileSet, offset);
            int count = 0;
            foreach (int p in patterns) {
                if (patterns.Contains(p) && wave.Get(patternIndex, p)) {
                    count++;
                }
            }

            patternCounts[index] = count;
            values[index] = GetQuadstateInner(index);
        }

        onChange.Reset(this);
    }


    public void UndoBan(int patternIndex, int pattern) {
        if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null) {
            UndoBan(patternIndex, pattern, patternIndex, 0);
        } else {
            foreach ((Point p, int index, int offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.get(
                         patternIndex)) {
                UndoBan(patternIndex, pattern, index, offset);
            }
        }
    }

    private Quadstate GetQuadstateInner(int index) {
        int selectedPatternCount = patternCounts[index];

        tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int offset);

        int totalPatternCount = wavePropagator.Wave.GetPatternCount(patternIndex);

        if (totalPatternCount == 0) {
            return Quadstate.CONTRADICTION;
        }

        if (selectedPatternCount == 0) {
            return Quadstate.NO;
        }

        if (totalPatternCount == selectedPatternCount) {
            return Quadstate.YES;
        }

        return Quadstate.MAYBE;
    }

    public Quadstate GetQuadstate(int index) {
        return values[index];
    }

    public bool IsSelected(int index) {
        return GetQuadstate(index).IsYes();
    }

    private void DoBan(int patternIndex, int pattern, int index, int offset) {
        ISet<int> patterns = tileModelMapping.GetPatterns(tileSet, offset);
        if (patterns.Contains(pattern)) {
            patternCounts[index] -= 1;
        }

        DoNotify(index);
    }

    private void UndoBan(int patternIndex, int pattern, int index, int offset) {
        ISet<int> patterns = tileModelMapping.GetPatterns(tileSet, offset);
        if (patterns.Contains(pattern)) {
            patternCounts[index] += 1;
        }

        DoNotify(index);
    }

    private void DoNotify(int index) {
        Quadstate newValue = GetQuadstateInner(index);
        Quadstate oldValue = values[index];
        if (newValue != oldValue) {
            values[index] = newValue;
            onChange.Notify(index, oldValue, newValue);
        }
    }
}