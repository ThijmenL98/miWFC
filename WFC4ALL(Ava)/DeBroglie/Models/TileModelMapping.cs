﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Models;

public struct TileModelMapping {
    private static readonly ISet<int> emptyPatternSet = new HashSet<int>();

    public ITopology PatternTopology { get; set; }

    public PatternModel PatternModel { get; set; }

    public IDictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> TilesToPatternsByOffset { get; set; }

    public IDictionary<int, IReadOnlyDictionary<int, Tile>> PatternsToTilesByOffset { get; set; }

    // Null for 1:1 mappings
    public ITopoArray<(Point, int, int)> TileCoordToPatternCoordIndexAndOffset { get; set; }

    // Null for 1:1 mappings
    public ITopoArray<List<(Point, int, int)>> PatternCoordToTileCoordIndexAndOffset { get; set; }

    public void GetTileCoordToPatternCoord(int x, int y, int z, out int px, out int py, out int pz, out int offset) {
        if (TileCoordToPatternCoordIndexAndOffset == null) {
            px = x;
            py = y;
            pz = z;
            offset = 0;

            return;
        }

        (Point point, int index, int o) = TileCoordToPatternCoordIndexAndOffset.get(x, y, z);
        px = point.X;
        py = point.Y;
        pz = point.Z;
        offset = o;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetTileCoordToPatternCoord(int index, out int patternIndex, out int offset) {
        if (TileCoordToPatternCoordIndexAndOffset == null) {
            patternIndex = index;
            offset = 0;

            return;
        }

        (_, patternIndex, offset) = TileCoordToPatternCoordIndexAndOffset.get(index);
    }


    /// <summary>
    ///     Creates a set of tiles. This set can be used with some operations, and is marginally
    ///     faster than passing in a fresh list of tiles ever time.
    /// </summary>
    public TilePropagatorTileSet CreateTileSet(IEnumerable<Tile> tiles) {
        TilePropagatorTileSet set = new(tiles);
        // Quick optimization for size one sets
        if (set.Tiles.Count == 1) {
            Tile tile = set.Tiles.First();
            foreach (int o in TilesToPatternsByOffset.Keys) {
                set.OffsetToPatterns[o] = TilesToPatternsByOffset[o].TryGetValue(tile, out ISet<int>? patterns)
                    ? patterns
                    : emptyPatternSet;
            }
        }

        return set;
    }

    private static readonly ISet<int> empty = new HashSet<int>();

    private static ISet<int> GetPatterns(IReadOnlyDictionary<Tile, ISet<int>> tilesToPatterns, Tile tile) {
        return tilesToPatterns.TryGetValue(tile, out ISet<int>? ps) ? ps : empty;
    }

    /// <summary>
    ///     Gets the patterns associated with a set of tiles at a given offset.
    /// </summary>
    public ISet<int> GetPatterns(Tile tile, int offset) {
        return GetPatterns(TilesToPatternsByOffset[offset], tile);
    }

    /// <summary>
    ///     Gets the patterns associated with a set of tiles at a given offset.
    /// </summary>
    public ISet<int> GetPatterns(TilePropagatorTileSet tileSet, int offset) {
        if (!tileSet.OffsetToPatterns.TryGetValue(offset, out ISet<int>? patterns)) {
            IReadOnlyDictionary<Tile, ISet<int>> tilesToPatterns = TilesToPatternsByOffset[offset];
            patterns = new HashSet<int>(tileSet.Tiles.SelectMany(tile => GetPatterns(tilesToPatterns, tile)));
            tileSet.OffsetToPatterns[offset] = patterns;
        }
        return patterns;
    }
}