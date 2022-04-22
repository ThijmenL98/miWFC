using System;
using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Models; 

/// <summary>
///     AdjacentModel constrains which tiles can be placed adjacent to which other ones.
///     It does so by maintaining for each tile, a list of tiles that can be placed next to it in each direction.
///     The list is always symmetric, i.e. if it is legal to place tile B directly above tile A, then it is legal to place
///     A directly below B.
/// </summary>
public class AdjacentModel : TileModel {
    private DirectionSet directions;
    private readonly List<double> frequencies;
    private readonly List<HashSet<int>[]> propagator;
    private readonly Dictionary<Tile, int> tilesToPatterns;


    /// <summary>
    ///     Constructs an AdjacentModel.
    /// </summary>
    public AdjacentModel() {
        // Tiles map 1:1 with patterns
        tilesToPatterns = new Dictionary<Tile, int>();
        frequencies = new List<double>();
        propagator = new List<HashSet<int>[]>();
    }

    /// <summary>
    ///     Constructs an AdjacentModel.
    /// </summary>
    public AdjacentModel(DirectionSet directions)
        : this() {
        SetDirections(directions);
    }


    /// <summary>
    ///     Constructs an AdjacentModel and initializees it with a given sample.
    /// </summary>
    public AdjacentModel(ITopoArray<Tile> sample)
        : this() {
        AddSample(sample);
    }

    public override IEnumerable<Tile> Tiles => tilesToPatterns.Keys;

    /// <summary>
    ///     Constructs an AdjacentModel and initializees it with a given sample.
    /// </summary>
    public static AdjacentModel Create<T>(T[,] sample, bool periodic) {
        return Create(new TopoArray2D<T>(sample, periodic));
    }

    /// <summary>
    ///     Constructs an AdjacentModel and initializees it with a given sample.
    /// </summary>
    public static AdjacentModel Create<T>(ITopoArray<T> sample) {
        return new AdjacentModel(sample.toTiles());
    }

    /// <summary>
    ///     Sets the directions of the Adjacent model, if it has not been set at construction.
    ///     This specifies how many neighbours each tile has.
    ///     Once set, it cannot be changed.
    /// </summary>
    /// <param name="directions"></param>
    public void SetDirections(DirectionSet directions) {
        if (this.directions.Type != DirectionSetType.UNKNOWN && this.directions.Type != directions.Type) {
            throw new Exception(
                $"Cannot set directions to {directions.Type}, it has already been set to {this.directions.Type}");
        }

        this.directions = directions;
    }

    private void RequireDirections() {
        if (directions.Type == DirectionSetType.UNKNOWN) {
            throw new Exception("Directions must be set before calling this method");
        }
    }

    /// <summary>
    ///     Finds a tile and all its rotations, and sets their total frequency.
    /// </summary>
    public void setFrequency(Tile tile, double frequency, TileRotation tileRotation) {
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
    public void setFrequency(Tile tile, double frequency) {
        int pattern = GetPattern(tile);
        frequencies[pattern] = frequency;
    }

    /// <summary>
    ///     Sets all tiles as equally likely to be picked
    /// </summary>
    public void SetUniformFrequency() {
        foreach (Tile tile in Tiles) {
            setFrequency(tile, 1.0);
        }
    }

    /// <summary>
    ///     Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified.
    ///     Then it adds similar declarations for other rotations and reflections, as specified by rotations.
    /// </summary>
    public void AddAdjacency(IList<Tile> src, IList<Tile> dest, Direction dir, TileRotation tileRotation = null) {
        RequireDirections();
        int d = (int) dir;
        int x = directions.DX[d];
        int y = directions.DY[d];
        int z = directions.DZ[d];
        AddAdjacency(src, dest, x, y, z, tileRotation);
    }

    /// <summary>
    ///     Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified by (x, y,
    ///     z).
    ///     Then it adds similar declarations for other rotations and reflections, as specified by rotations.
    /// </summary>
    public void AddAdjacency(IList<Tile> src, IList<Tile> dest, int x, int y, int z, TileRotation tileRotation = null) {
        RequireDirections();

        tileRotation = tileRotation ?? new TileRotation();

        foreach (Rotation rotation in tileRotation.RotationGroup) {
            (int x2, int y2) = TopoArrayUtils.RotateVector(directions.Type, x, y, rotation);

            AddAdjacency(
                tileRotation.Rotate(src, rotation).ToList(),
                tileRotation.Rotate(dest, rotation).ToList(),
                x2, y2, z);
        }
    }

    /// <summary>
    ///     Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified by (x, y,
    ///     z).
    ///     (x, y, z) must be a valid direction, which usually means a unit vector.
    /// </summary>
    public void AddAdjacency(IList<Tile> src, IList<Tile> dest, int x, int y, int z) {
        RequireDirections();
        AddAdjacency(src, dest, directions.GetDirection(x, y, z));
    }

    /// <summary>
    ///     Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified by (x, y,
    ///     z).
    ///     (x, y, z) must be a valid direction, which usually means a unit vector.
    /// </summary>
    public void AddAdjacency(IList<Tile> src, IList<Tile> dest, Direction dir) {
        RequireDirections();

        foreach (Tile s in src) {
            foreach (Tile d in dest) {
                AddAdjacency(s, d, dir);
            }
        }
    }

    /// <summary>
    ///     Declares that dest can be placed adjacent to src, in the direction specified by (x, y, z).
    ///     (x, y, z) must be a valid direction, which usually means a unit vector.
    /// </summary>
    public void AddAdjacency(Tile src, Tile dest, int x, int y, int z) {
        RequireDirections();
        Direction d = directions.GetDirection(x, y, z);
        AddAdjacency(src, dest, d);
    }

    /// <summary>
    ///     Declares that dest can be placed adjacent to src, in the direction specified.
    /// </summary>
    public void AddAdjacency(Tile src, Tile dest, Direction d) {
        Direction id = directions.Inverse(d);
        int srcPattern = GetPattern(src);
        int destPattern = GetPattern(dest);
        propagator[srcPattern][(int) d].Add(destPattern);
        propagator[destPattern][(int) id].Add(srcPattern);
    }

    public void AddAdjacency(Adjacency adjacency) {
        AddAdjacency(adjacency.Src, adjacency.Dest, adjacency.Direction);
    }

    public bool IsAdjacent(Tile src, Tile dest, Direction d) {
        int srcPattern = GetPattern(src);
        int destPattern = GetPattern(dest);
        return propagator[srcPattern][(int) d].Contains(destPattern);
    }

    public void AddSample(ITopoArray<Tile> sample, TileRotation tileRotation = null) {
        foreach (ITopoArray<Tile> s in OverlappingAnalysis.GetRotatedSamples(sample, tileRotation)) {
            AddSample(s);
        }
    }

    public void AddSample(ITopoArray<Tile> sample) {
        GridTopology topology = sample.Topology.AsGridTopology();

        SetDirections(topology.Directions);

        int width = topology.Width;
        int height = topology.Height;
        int depth = topology.Depth;
        int directionCount = topology.Directions.Count;

        for (int z = 0; z < depth; z++) {
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int index = topology.GetIndex(x, y, z);
                    if (!topology.ContainsIndex(index)) {
                        continue;
                    }

                    // Find the pattern and update the frequency
                    int pattern = GetPattern(sample.get(x, y, z));

                    frequencies[pattern] += 1;

                    // Update propagator
                    for (int d = 0; d < directionCount; d++) {
                        int x2, y2, z2;
                        if (topology.TryMove(x, y, z, (Direction) d, out x2, out y2, out z2)) {
                            int pattern2 = GetPattern(sample.get(x2, y2, z2));
                            propagator[pattern][d].Add(pattern2);
                        }
                    }
                }
            }
        }
    }

    internal override TileModelMapping GetTileModelMapping(ITopology topology) {
        GridTopology gridTopology = topology.AsGridTopology();
        RequireDirections();
        SetDirections(gridTopology.Directions);

        if (frequencies.Sum() == 0.0) {
            throw new Exception("No tiles have assigned frequences.");
        }

        PatternModel patternModel = new() {
            Propagator = propagator.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(),
            Frequencies = frequencies.ToArray()
        };
        Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> tilesToPatternsByOffset = new() {
            {0, tilesToPatterns.ToDictionary(kv => kv.Key, kv => (ISet<int>) new HashSet<int> {kv.Value})}
        };
        Dictionary<int, IReadOnlyDictionary<int, Tile>> patternsToTilesByOffset = new() {
            {0, tilesToPatterns.ToDictionary(x => x.Value, x => x.Key)}
        };
        return new TileModelMapping {
            PatternTopology = gridTopology,
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

    private int GetPattern(Tile tile) {
        int directionCount = directions.Count;

        int pattern;
        if (!tilesToPatterns.TryGetValue(tile, out pattern)) {
            pattern = tilesToPatterns[tile] = tilesToPatterns.Count;
            frequencies.Add(0);
            propagator.Add(new HashSet<int>[directionCount]);
            for (int d = 0; d < directionCount; d++) {
                propagator[pattern][d] = new HashSet<int>();
            }
        }

        return pattern;
    }

    public class Adjacency {
        public Tile[] Src { get; set; }
        public Tile[] Dest { get; set; }
        public Direction Direction { get; set; }
    }
}