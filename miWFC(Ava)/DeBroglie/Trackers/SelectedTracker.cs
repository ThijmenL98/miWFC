using System.Collections.Generic;
using System.Runtime.CompilerServices;
using miWFC.DeBroglie.Models;
using miWFC.DeBroglie.Topo;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Trackers; 

/// <summary>
///     Tracks the banned/selected status of each tile with respect to a tileset.
/// </summary>
public class SelectedTracker : ITracker {
    // Indexed by tile topology
    private readonly int[] patternCounts;

    private readonly TileModelMapping tileModelMapping;
    private readonly TilePropagator tilePropagator;

    private readonly TilePropagatorTileSet tileSet;

    private readonly WavePropagator wavePropagator;

    internal SelectedTracker(TilePropagator tilePropagator, WavePropagator wavePropagator,
        TileModelMapping tileModelMapping, TilePropagatorTileSet tileSet) {
        this.tilePropagator = tilePropagator;
        this.wavePropagator = wavePropagator;
        this.tileModelMapping = tileModelMapping;
        this.tileSet = tileSet;
        patternCounts = new int[tilePropagator.Topology.IndexCount];
    }

    void ITracker.DoBan(int patternIndex, int pattern) {
        if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null) {
            DoBan(patternIndex, pattern, patternIndex, 0);
        } else {
            foreach ((Point p, int index, int offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.get(
                         patternIndex)) {
                DoBan(patternIndex, pattern, index, offset);
            }
        }
    }

    void ITracker.Reset() {
        Wave wave = wavePropagator.Wave;
        foreach (int index in tilePropagator.Topology.GetIndices()) {
            tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int offset);
            ISet<int> patterns = tileModelMapping.GetPatterns(tileSet, offset);
            int count = 0;
            foreach (int p in patterns) {
                if (wave.Get(patternIndex, p)) {
                    count++;
                }
            }

            patternCounts[index] = count;
        }
    }


    void ITracker.UndoBan(int patternIndex, int pattern) {
        if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null) {
            UndoBan(patternIndex, pattern, patternIndex, 0);
        } else {
            foreach ((Point p, int index, int offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.get(
                         patternIndex)) {
                UndoBan(patternIndex, pattern, index, offset);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quadstate GetQuadstate(int index) {
        int selectedPatternCount = patternCounts[index];
        if (selectedPatternCount == 0) {
            return Quadstate.NO;
        }

        tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int offset);

        int totalPatternCount = wavePropagator.Wave.GetPatternCount(patternIndex);
        if (totalPatternCount == selectedPatternCount) {
            return Quadstate.YES;
        }

        return Quadstate.MAYBE;
    }

    public bool IsSelected(int index) {
        return GetQuadstate(index).IsYes();
    }

    private void DoBan(int patternIndex, int pattern, int index, int offset) {
        ISet<int> patterns = tileModelMapping.GetPatterns(tileSet, offset);
        if (patterns.Contains(pattern)) {
            patternCounts[index] -= 1;
        }
    }

    private void UndoBan(int patternIndex, int pattern, int index, int offset) {
        ISet<int> patterns = tileModelMapping.GetPatterns(tileSet, offset);
        if (patterns.Contains(pattern)) {
            patternCounts[index] += 1;
        }
    }
}