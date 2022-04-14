using System;
using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Models; 

/// <summary>
///     Functions as AdjacentModel, but is more generic and will function for any toplogy.
/// </summary>
public class GraphAdjacentModel : TileModel {
    private readonly int directionsCount;
    private readonly int edgeLabelCount;
    private readonly (Direction, Direction, Rotation)[] edgeLabelInfo;

    private readonly List<double> frequencies;

    // By Pattern, then edge-label
    private readonly List<HashSet<int>[]> propagator;
    private readonly Dictionary<Tile, int> tilesToPatterns;

    public GraphAdjacentModel(GraphInfo graphInfo)
        : this(graphInfo.DirectionsCount, graphInfo.EdgeLabelCount) {
        edgeLabelInfo = graphInfo.EdgeLabelInfo;
    }


    public GraphAdjacentModel(int directionsCount, int edgeLabelCount) {
        this.directionsCount = directionsCount;
        this.edgeLabelCount = edgeLabelCount;
        // Tiles map 1:1 with patterns
        tilesToPatterns = new Dictionary<Tile, int>();
        frequencies = new List<double>();
        propagator = new List<HashSet<int>[]>();
    }

    public override IEnumerable<Tile> Tiles => tilesToPatterns.Keys;

    internal override TileModelMapping GetTileModelMapping(ITopology topology) {
        if (frequencies.Sum() == 0.0) {
            throw new Exception("No tiles have assigned frequences.");
        }

        PatternModel patternModel = new() {
            Propagator = propagator.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(),
            Frequencies = frequencies.ToArray()
        };
        Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> tilesToPatternsByOffset = new() {
            {
                0,
                tilesToPatterns.ToLookup(x => x.Key, x => x.Value)
                    .ToDictionary(g => g.Key, g => (ISet<int>) new HashSet<int>(g))
            }
        };
        Dictionary<int, IReadOnlyDictionary<int, Tile>> patternsToTilesByOffset = new() {
            {0, tilesToPatterns.ToDictionary(x => x.Value, x => x.Key)}
        };
        return new TileModelMapping {
            PatternTopology = topology,
            PatternModel = patternModel,
            PatternsToTilesByOffset = patternsToTilesByOffset,
            TilesToPatternsByOffset = tilesToPatternsByOffset,
            TileCoordToPatternCoordIndexAndOffset = null
        };
    }

    public override void MultiplyFrequency(Tile tile, double multiplier) {
        int pattern = tilesToPatterns[tile];
        frequencies[pattern] *= multiplier;
    }

    /// <summary>
    ///     Finds a tile and all its rotations, and sets their total frequency.
    /// </summary>
    public void SetFrequency(Tile tile, double frequency, TileRotation tileRotation) {
        List<Tile> rotatedTiles = tileRotation.RotateAll(tile).ToList();
        foreach (Tile rt in rotatedTiles) {
            int pattern = GetPattern(rt);
            frequencies[pattern] = 0.0;
        }

        double incrementalFrequency = frequency / rotatedTiles.Count;
        foreach (Tile rt in rotatedTiles) {
            int pattern = GetPattern(rt);
            frequencies[pattern] += incrementalFrequency;
        }
    }

    /// <summary>
    ///     Sets the frequency of a given tile.
    /// </summary>
    public void SetFrequency(Tile tile, double frequency) {
        int pattern = GetPattern(tile);
        frequencies[pattern] = frequency;
    }

    /// <summary>
    ///     Sets all tiles as equally likely to be picked
    /// </summary>
    public void SetUniformFrequency() {
        foreach (Tile tile in Tiles) {
            SetFrequency(tile, 1.0);
        }
    }

    private int GetPattern(Tile tile) {
        int pattern;
        if (!tilesToPatterns.TryGetValue(tile, out pattern)) {
            pattern = tilesToPatterns[tile] = tilesToPatterns.Count;
            frequencies.Add(0);
            propagator.Add(new HashSet<int>[edgeLabelCount]);
            for (int el = 0; el < edgeLabelCount; el++) {
                propagator[pattern][el] = new HashSet<int>();
            }
        }

        return pattern;
    }

    public void AddAdjacency(IList<Tile> src, IList<Tile> dest, Direction direction, TileRotation tileRotation) {
        foreach (Tile s in src) {
            foreach (Tile d in dest) {
                AddAdjacency(s, d, direction, tileRotation);
            }
        }
    }

    public void AddAdjacency(Tile src, Tile dest, Direction direction, TileRotation tileRotation) {
        if (edgeLabelInfo == null) {
            throw new Exception("This method requires edgeLabelInfo configured");
        }

        List<(Direction, Direction, Rotation)> inverseDirectionItems
            = edgeLabelInfo.Where(x => x.Item3.IsIdentity && x.Item1 == direction).ToList();
        if (inverseDirectionItems.Count == 0) {
            throw new Exception($"Couldn't find identity edge label for direction {direction}");
        }

        Direction inverseDirection = inverseDirectionItems[0].Item2;
        for (int i = 0; i < edgeLabelInfo.Length; i++) {
            (Direction d, Direction id, Rotation r) = edgeLabelInfo[i];
            if (d == direction) {
                Rotation rotation = r.Inverse();
                if (tileRotation.Rotate(dest, rotation, out Tile rd)) {
                    AddAdjacency(src, rd, (EdgeLabel) i);
                }
            }

            if (d == inverseDirection) {
                Rotation rotation = r.Inverse();
                if (tileRotation.Rotate(src, rotation, out Tile rs)) {
                    AddAdjacency(dest, rs, (EdgeLabel) i);
                }
            }
        }
    }


    public void AddAdjacency(IList<Tile> src, IList<Tile> dest, EdgeLabel edgeLabel) {
        foreach (Tile s in src) {
            foreach (Tile d in dest) {
                AddAdjacency(s, d, edgeLabel);
            }
        }
    }

    public void AddAdjacency(Tile src, Tile dest, EdgeLabel edgeLabel) {
        int s = GetPattern(src);
        int d = GetPattern(dest);
        propagator[s][(int) edgeLabel].Add(d);
    }

    public bool IsAdjacent(Tile src, Tile dest, EdgeLabel edgeLabel) {
        int srcPattern = GetPattern(src);
        int destPattern = GetPattern(dest);
        return propagator[srcPattern][(int) edgeLabel].Contains(destPattern);
    }
}