using System;
using System.Collections.Generic;
using System.Linq;
using miWFC.DeBroglie.Rot;
using miWFC.DeBroglie.Topo;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Models;

/// <summary>
///     OverlappingModel constrains that every n by n rectangle in the output is a copy of a rectangle taken from the
///     sample.
/// </summary>
public class OverlappingModel : TileModel {
    private readonly List<double> frequencies;
    private readonly List<PatternArray> patternArrays;

    private readonly Dictionary<PatternArray, int> patternIndices;
    private List<double> originalFrequencies;

    private IReadOnlyDictionary<int, Tile> patternsToTiles;
    private List<int[][]> propagator;
    private DirectionSet sampleTopologyDirections;
    private ILookup<Tile, int> tilesToPatterns;


    public OverlappingModel(ITopoArray<Tile> sample, int n, int rotationalSymmetry, bool reflectionalSymmetry)
        : this(n) {
        addSample(sample, new TileRotation(rotationalSymmetry, reflectionalSymmetry));
    }

    /// <summary>
    ///     Shorthand for constructing an Overlapping model with an n by n square or n by n by cuboid.
    /// </summary>
    /// <param name="n"></param>
    public OverlappingModel(int n)
        : this(n, n, n) { }

    public OverlappingModel(int nx, int ny, int nz) {
        NX = nx;
        NY = ny;
        NZ = nz;
        patternIndices = new Dictionary<PatternArray, int>(new PatternArrayComparer());
        frequencies = new List<double>();
        originalFrequencies = new List<double>();
        patternArrays = new List<PatternArray>();
        propagator = new List<int[][]>();
    }

    public int NX { get; }

    public int NY { get; }

    public int NZ { get; private set; }

    internal IReadOnlyList<PatternArray> PatternArrays => patternArrays;

    public override IEnumerable<Tile> Tiles => tilesToPatterns.Select(x => x.Key);

    public static OverlappingModel Create<T>(T[,] sample, int n, bool periodic, int symmetries) {
        ITopoArray<Tile> topArray = new TopoArray2D<T>(sample, periodic).toTiles();

        return new OverlappingModel(topArray, n, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1);
    }

    public Tuple<List<PatternArray>, List<double>>
        addSample(ITopoArray<Tile> sample, TileRotation tileRotation = null) {
        if (sample.Topology.Depth == 1) {
            NZ = 1;
        }

        GridTopology topology = sample.Topology.AsGridTopology();

        bool periodicX = topology.PeriodicX;
        bool periodicY = topology.PeriodicY;
        bool periodicZ = topology.PeriodicZ;

        foreach (ITopoArray<Tile> s in OverlappingAnalysis.GetRotatedSamples(sample, tileRotation)) {
            OverlappingAnalysis.GetPatterns(s, NX, NY, NZ, periodicX, periodicY, periodicZ, patternIndices,
                patternArrays, frequencies);
        }

        originalFrequencies = new List<double>(frequencies);

        sampleTopologyDirections = topology.Directions;
        propagator = null; // Mark as dirty

        return new Tuple<List<PatternArray>, List<double>>(patternArrays, frequencies);
    }

    private void Build() {
        if (propagator != null) {
            return;
        }

        // Update the model based on the collected data
        DirectionSet directions = sampleTopologyDirections;

        // Collect all the pattern edges
        Dictionary<Direction, Dictionary<PatternArray, int[]>> patternIndicesByEdge = new();
        Dictionary<(Direction, int), PatternArray> edgesByPatternIndex = new();
        for (int d = 0; d < directions.Count; d++) {
            int dx = directions.DX[d];
            int dy = directions.DY[d];
            int dz = directions.DZ[d];
            Dictionary<PatternArray, HashSet<int>> edges = new(new PatternArrayComparer());
            for (int p = 0; p < patternArrays.Count; p++) {
                PatternArray edge = OverlappingAnalysis.PatternEdge(patternArrays[p], dx, dy, dz);
                if (!edges.TryGetValue(edge, out HashSet<int>? l)) {
                    l = edges[edge] = new HashSet<int>();
                }

                l.Add(p);
                edgesByPatternIndex[((Direction) d, p)] = edge;
            }

            patternIndicesByEdge[(Direction) d] = edges
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.OrderBy(y => y).ToArray(),
                    new PatternArrayComparer());
        }

        // Setup propagator
        int[] empty = new int[0];
        propagator = new List<int[][]>(patternArrays.Count);
        for (int p = 0; p < patternArrays.Count; p++) {
            propagator.Add(new int[directions.Count][]);
            for (int d = 0; d < directions.Count; d++) {
                Direction dir = (Direction) d;
                Direction invDir = directions.Inverse(dir);
                PatternArray edge = edgesByPatternIndex[(dir, p)];
                if (patternIndicesByEdge[invDir].TryGetValue(edge, out int[]? otherPatterns)) {
                    propagator[p][d] = otherPatterns;
                } else {
                    propagator[p][d] = empty;
                }
            }
        }

        patternsToTiles = patternArrays
            .Select((x, i) => new KeyValuePair<int, Tile>(i, x.Values[0, 0, 0]))
            .ToDictionary(x => x.Key, x => x.Value);

        tilesToPatterns = patternsToTiles.ToLookup(x => x.Value, x => x.Key);
    }

    internal override TileModelMapping GetTileModelMapping(ITopology topology) {
        Build();

        GridTopology gridTopology = topology.AsGridTopology();
        PatternModel patternModel = new() {
            Propagator = propagator.Select(x => x.Select(y => y).ToArray()).ToArray(),
            Frequencies = frequencies.ToArray()
        };

        GridTopology patternTopology;
        Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> tilesToPatternsByOffset;
        Dictionary<int, IReadOnlyDictionary<int, Tile>> patternsToTilesByOffset;
        ITopoArray<(Point, int, int)> tileCoordToPatternCoordIndexAndOffset;
        ITopoArray<List<(Point, int, int)>> patternCoordToTileCoordIndexAndOffset;
        if (!(gridTopology.PeriodicX && gridTopology.PeriodicY && gridTopology.PeriodicZ)) {
            // Shrink the topology as patterns can cover multiple tiles.
            patternTopology = gridTopology.WithSize(
                gridTopology.PeriodicX ? topology.Width : topology.Width - NX + 1,
                gridTopology.PeriodicY ? topology.Height : topology.Height - NY + 1,
                gridTopology.PeriodicZ ? topology.Depth : topology.Depth - NZ + 1);

            if (patternTopology.Width <= 0) {
                throw new Exception($"Sample width {topology.Width} not wide enough for overlap of {NX}");
            }

            if (patternTopology.Height <= 0) {
                throw new Exception($"Sample width {topology.Height} not wide enough for overlap of {NY}");
            }

            if (patternTopology.Depth <= 0) {
                throw new Exception($"Sample width {topology.Depth} not wide enough for overlap of {NZ}");
            }


            void overlapCoord(int x, int width, out int px, out int ox) {
                if (x < width) {
                    px = x;
                    ox = 0;
                } else {
                    px = width - 1;
                    ox = x - px;
                }
            }

            int combineOffsets(int ox, int oy, int oz) {
                return ox + oy * NX + oz * NX * NY;
            }

            (Point, int, int) map(Point t) {
                overlapCoord(t.X, patternTopology.Width, out int px, out int ox);
                overlapCoord(t.Y, patternTopology.Height, out int py, out int oy);
                overlapCoord(t.Z, patternTopology.Depth, out int pz, out int oz);
                int patternIndex = patternTopology.GetIndex(px, py, pz);
                return (new Point(px, py, pz), patternIndex, combineOffsets(ox, oy, oz));
            }

            /*
            (Point, int, int) RMap(Point t)
            {
                OverlapCoord(t.X, patternTopology.Width, out var px, out var ox);
                OverlapCoord(t.Y, patternTopology.Height, out var py, out var oy);
                OverlapCoord(t.Z, patternTopology.Depth, out var pz, out var oz);
                var patternIndex = patternTopology.GetIndex(px, py, pz);
                return (new Point(px, py, pz), patternIndex, CombineOffsets(ox, oy, oz));
            }
            */

            tileCoordToPatternCoordIndexAndOffset = TopoArray.CreateByPoint(map, gridTopology);
            List<(Point, int, int)>[,,] patternCoordToTileCoordIndexAndOffsetValues
                = new List<(Point, int, int)>[patternTopology.Width, patternTopology.Height, patternTopology.Depth];
            foreach (int index in topology.GetIndices()) {
                topology.GetCoord(index, out int x, out int y, out int z);
                (Point p, int patternIndex, int offset) = tileCoordToPatternCoordIndexAndOffset.get(index);
                if (patternCoordToTileCoordIndexAndOffsetValues[p.X, p.Y, p.Z] == null) {
                    patternCoordToTileCoordIndexAndOffsetValues[p.X, p.Y, p.Z] = new List<(Point, int, int)>();
                }

                patternCoordToTileCoordIndexAndOffsetValues[p.X, p.Y, p.Z].Add((new Point(x, y, z), index, offset));
            }

            patternCoordToTileCoordIndexAndOffset
                = TopoArray.create(patternCoordToTileCoordIndexAndOffsetValues, patternTopology);


            // Compute tilesToPatterns and patternsToTiles
            tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>();
            patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>();
            for (int ox = 0; ox < NX; ox++) {
                for (int oy = 0; oy < NY; oy++) {
                    for (int oz = 0; oz < NZ; oz++) {
                        int o = combineOffsets(ox, oy, oz);
                        Dictionary<Tile, ISet<int>> tilesToPatterns = new();
                        tilesToPatternsByOffset[o] = tilesToPatterns;
                        Dictionary<int, Tile> patternsToTiles = new();
                        patternsToTilesByOffset[o] = patternsToTiles;
                        for (int pattern = 0; pattern < patternArrays.Count; pattern++) {
                            PatternArray patternArray = patternArrays[pattern];
                            Tile tile = patternArray.Values[ox, oy, oz];
                            patternsToTiles[pattern] = tile;
                            if (!tilesToPatterns.TryGetValue(tile, out ISet<int>? patternSet)) {
                                patternSet = tilesToPatterns[tile] = new HashSet<int>();
                            }

                            patternSet.Add(pattern);
                        }
                    }
                }
            }
        } else {
            patternTopology = gridTopology;
            tileCoordToPatternCoordIndexAndOffset = null;
            patternCoordToTileCoordIndexAndOffset = null;
            tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> {
                {0, tilesToPatterns.ToDictionary(g => g.Key, g => (ISet<int>) new HashSet<int>(g))}
            };
            patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>> {
                {0, patternsToTiles}
            };
        }

        // Masks interact a bit weirdly with the overlapping model
        // We choose a pattern mask that is a expansion of the topology mask
        // i.e. a pattern location is masked out if all the tile locations it covers is masked out.
        // This makes the propagator a bit conservative - it'll always preserve the overlapping property
        // but might ban some layouts that make sense.
        // The alternative is to contract the mask - that is more permissive, but sometimes will
        // violate the overlapping property.
        // (passing the mask verbatim is unacceptable as does not lead to symmetric behaviour)
        // See TestTileMaskWithThinOverlapping for an example of the problem, and
        // https://github.com/BorisTheBrave/DeBroglie/issues/7 for a possible solution.
        if (topology.Mask != null) {
            // BORIS_TODO: This could probably do with some cleanup
            bool getTopologyMask(int x, int y, int z) {
                if (!gridTopology.PeriodicX && x >= topology.Width) {
                    return false;
                }

                if (!gridTopology.PeriodicY && y >= topology.Height) {
                    return false;
                }

                if (!gridTopology.PeriodicZ && z >= topology.Depth) {
                    return false;
                }

                x = x % topology.Width;
                y = y % topology.Height;
                z = z % topology.Depth;
                return topology.Mask[topology.GetIndex(x, y, z)];
            }

            bool getPatternTopologyMask(Point p) {
                for (int oz = 0; oz < NZ; oz++) {
                    for (int oy = 0; oy < NY; oy++) {
                        for (int ox = 0; ox < NX; ox++) {
                            if (getTopologyMask(p.X + ox, p.Y + oy, p.Z + oz)) {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            ITopoArray<bool> patternMask = TopoArray.CreateByPoint(getPatternTopologyMask, patternTopology);
            patternTopology = patternTopology.WithMask(patternMask);
        }


        return new TileModelMapping {
            PatternModel = patternModel,
            PatternsToTilesByOffset = patternsToTilesByOffset,
            TilesToPatternsByOffset = tilesToPatternsByOffset,
            PatternTopology = patternTopology,
            TileCoordToPatternCoordIndexAndOffset = tileCoordToPatternCoordIndexAndOffset,
            PatternCoordToTileCoordIndexAndOffset = patternCoordToTileCoordIndexAndOffset
        };
    }

    public override void MultiplyFrequency(Tile tile, double multiplier) {
        for (int p = 0; p < patternArrays.Count; p++) {
            PatternArray patternArray = patternArrays[p];
            for (int x = 0; x < patternArray.Width; x++) {
                for (int y = 0; y < patternArray.Height; y++) {
                    for (int z = 0; z < patternArray.Depth; z++) {
                        if (patternArray.Values[x, y, z] == tile) {
                            frequencies[p] = Math.Max(originalFrequencies[p] * multiplier, 0.00000000001d);
                        }
                    }
                }
            }
        }
    }

    public List<double> getFrequency(Tile tile) {
        List<double> weights = new();
        for (int p = 0; p < patternArrays.Count; p++) {
            PatternArray patternArray = patternArrays[p];
            for (int x = 0; x < patternArray.Width; x++) {
                for (int y = 0; y < patternArray.Height; y++) {
                    for (int z = 0; z < patternArray.Depth; z++) {
                        if (patternArray.Values[x, y, z] == tile) {
                            weights.Add(frequencies[p]);
                        }
                    }
                }
            }
        }

        return weights;
    }
}