using System.Collections.Generic;
using System.Linq;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie.Models {
    /// <summary>
    /// OverlappingModel constrains that every n by n rectangle in the output is a copy of a rectangle taken from the sample.
    /// </summary>
    public class OverlappingModel : TileModel {
        private readonly int nx;
        private readonly int ny;
        private int nz;

        private readonly Dictionary<PatternArray, int> patternIndices;
        private readonly List<PatternArray> patternArrays;
        private readonly List<double> frequencies;
        private List<HashSet<int>[]> propagator;

        private IReadOnlyDictionary<int, Tile> patternsToTiles;
        private ILookup<Tile, int> tilesToPatterns;

        public static OverlappingModel create<T>(T[,] sample, int n, bool periodic, int symmetries) {
            ITopoArray<Tile> topArray = new TopoArray2D<T>(sample, periodic).toTiles();

            return new OverlappingModel(topArray, n, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1);
        }
        
        private OverlappingModel(ITopoArray<Tile> sample, int n, int rotationalSymmetry, bool reflectionalSymmetry)
            : this(n) {
            addSample(sample, new TileRotation(rotationalSymmetry, reflectionalSymmetry));
        }

        /// <summary>
        /// Shorthand for constructing an Overlapping model with an n by n square or n by n by cuboid.
        /// </summary>
        /// <param name="n"></param>
        public OverlappingModel(int n)
            : this(n, n, n) { }

        private OverlappingModel(int nx, int ny, int nz) {
            this.nx = nx;
            this.ny = ny;
            this.nz = nz;
            patternIndices = new Dictionary<PatternArray, int>(new PatternArrayComparer());
            frequencies = new List<double>();
            patternArrays = new List<PatternArray>();
            propagator = new List<HashSet<int>[]>();
        }

        public List<PatternArray> addSample(ITopoArray<Tile> sample, TileRotation tileRotation = null) {
            if (sample.Topology.Depth == 1) {
                nz = 1;
            }

            GridTopology topology = sample.Topology.asGridTopology();

            bool periodicX = topology.PeriodicX;
            bool periodicY = topology.PeriodicY;
            bool periodicZ = topology.PeriodicZ;

            foreach (ITopoArray<Tile> s in OverlappingAnalysis.getRotatedSamples(sample, tileRotation)) {
                OverlappingAnalysis.getPatterns(s, nx, ny, nz, periodicX, periodicY, periodicZ, patternIndices,
                    patternArrays, frequencies);
            }

            // Update the model based on the collected data
            DirectionSet directions = topology.Directions;

            // TOODO: Don't regenerate this from scratch every time
            propagator = new List<HashSet<int>[]>(patternArrays.Count);
            for (int p = 0; p < patternArrays.Count; p++) {
                propagator.Add(new HashSet<int>[directions.Count]);
                for (int d = 0; d < directions.Count; d++) {
                    HashSet<int> l = new();
                    for (int p2 = 0; p2 < patternArrays.Count; p2++) {
                        int dx = directions.Dx[d];
                        int dy = directions.Dy[d];
                        int dz = directions.Dz[d];
                        if (agrees(patternArrays[p], patternArrays[p2], dx, dy, dz)) {
                            l.Add(p2);
                        }
                    }

                    propagator[p][d] = l;
                }
            }

            patternsToTiles = patternArrays
                .Select((x, i) => new KeyValuePair<int, Tile>(i, x.values[0, 0, 0]))
                .ToDictionary(x => x.Key, x => x.Value);

            tilesToPatterns = patternsToTiles.ToLookup(x => x.Value, x => x.Key);

            return patternArrays;
        }

        public int Nx => nx;
        public int Ny => ny;
        public int Nz => nz;

        internal IReadOnlyList<PatternArray> PatternArrays => patternArrays;

        public override IEnumerable<Tile> Tiles => tilesToPatterns.Select(x => x.Key);

        /**
          * Return true if the pattern1 is compatible with pattern2
          * when pattern2 is at a distance (dy,dx) from pattern1.
          */
        private static bool agrees(PatternArray a, PatternArray b, int dx, int dy, int dz) {
            int xmin = dx < 0 ? 0 : dx;
            int xmax = dx < 0 ? dx + b.Width : a.Width;
            int ymin = dy < 0 ? 0 : dy;
            int ymax = dy < 0 ? dy + b.Height : a.Height;
            int zmin = dz < 0 ? 0 : dz;
            int zmax = dz < 0 ? dz + b.Depth : a.Depth;
            for (int x = xmin; x < xmax; x++) {
                for (int y = ymin; y < ymax; y++) {
                    for (int z = zmin; z < zmax; z++) {
                        if (a.values[x, y, z] != b.values[x - dx, y - dy, z - dz]) {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        internal override TileModelMapping getTileModelMapping(ITopology topology) {
            GridTopology gridTopology = topology.asGridTopology();
            PatternModel patternModel = new() {
                Propagator = propagator.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(),
                Frequencies = frequencies.ToArray(),
            };

            GridTopology patternTopology;
            Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> tilesToPatternsByOffset;
            Dictionary<int, IReadOnlyDictionary<int, Tile>> patternsToTilesByOffset;
            ITopoArray<(Point, int, int)> tileCoordToPatternCoordIndexAndOffset;
            ITopoArray<List<(Point, int, int)>> patternCoordToTileCoordIndexAndOffset;
            if (!(gridTopology.PeriodicX && gridTopology.PeriodicY && gridTopology.PeriodicZ)) {
                // Shrink the topology as patterns can cover multiple tiles.
                patternTopology = gridTopology.withSize(
                    gridTopology.PeriodicX ? topology.Width : topology.Width - Nx + 1,
                    gridTopology.PeriodicY ? topology.Height : topology.Height - Ny + 1,
                    gridTopology.PeriodicZ ? topology.Depth : topology.Depth - Nz + 1);


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
                    return ox + oy * Nx + oz * Nx * Ny;
                }

                (Point, int, int) map(Point t) {
                    overlapCoord(t.x, patternTopology.Width, out int px, out int ox);
                    overlapCoord(t.y, patternTopology.Height, out int py, out int oy);
                    overlapCoord(t.z, patternTopology.Depth, out int pz, out int oz);
                    int patternIndex = patternTopology.getIndex(px, py, pz);
                    return (new Point(px, py, pz), patternIndex, combineOffsets(ox, oy, oz));
                }

                (Point, int, int) rMap(Point t) {
                    overlapCoord(t.x, patternTopology.Width, out int px, out int ox);
                    overlapCoord(t.y, patternTopology.Height, out int py, out int oy);
                    overlapCoord(t.z, patternTopology.Depth, out int pz, out int oz);
                    int patternIndex = patternTopology.getIndex(px, py, pz);
                    return (new Point(px, py, pz), patternIndex, combineOffsets(ox, oy, oz));
                }

                tileCoordToPatternCoordIndexAndOffset = TopoArray.createByPoint(map, gridTopology);
                List<(Point, int, int)>[,,] patternCoordToTileCoordIndexAndOffsetValues
                    = new List<(Point, int, int)>[patternTopology.Width, patternTopology.Height, patternTopology.Depth];
                foreach (int index in topology.getIndices()) {
                    topology.getCoord(index, out int x, out int y, out int z);
                    (Point p, int patternIndex, int offset) = tileCoordToPatternCoordIndexAndOffset.get(index);
                    if (patternCoordToTileCoordIndexAndOffsetValues[p.x, p.y, p.z] == null) {
                        patternCoordToTileCoordIndexAndOffsetValues[p.x, p.y, p.z] = new List<(Point, int, int)>();
                    }

                    patternCoordToTileCoordIndexAndOffsetValues[p.x, p.y, p.z].Add((new Point(x, y, z), index, offset));
                }

                patternCoordToTileCoordIndexAndOffset
                    = TopoArray.create(patternCoordToTileCoordIndexAndOffsetValues, patternTopology);


                // Compute tilesToPatterns and patternsToTiles
                tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>();
                patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>();
                for (int ox = 0; ox < Nx; ox++) {
                    for (int oy = 0; oy < Ny; oy++) {
                        for (int oz = 0; oz < Nz; oz++) {
                            int o = combineOffsets(ox, oy, oz);
                            Dictionary<Tile, ISet<int>> tilesToPatterns = new();
                            tilesToPatternsByOffset[o] = tilesToPatterns;
                            Dictionary<int, Tile> patternsToTiles = new();
                            patternsToTilesByOffset[o] = patternsToTiles;
                            for (int pattern = 0; pattern < patternArrays.Count; pattern++) {
                                PatternArray patternArray = patternArrays[pattern];
                                Tile tile = patternArray.values[ox, oy, oz];
                                patternsToTiles[pattern] = tile;
                                if (!tilesToPatterns.TryGetValue(tile, out ISet<int> patternSet)) {
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
                    {0, patternsToTiles},
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
                // TOODO: This could probably do with some cleanup
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
                    return topology.Mask[topology.getIndex(x, y, z)];
                }

                bool getPatternTopologyMask(Point p) {
                    for (int oz = 0; oz < Nz; oz++) {
                        for (int oy = 0; oy < Ny; oy++) {
                            for (int ox = 0; ox < Nx; ox++) {
                                if (getTopologyMask(p.x + ox, p.y + oy, p.z + oz)) {
                                    return true;
                                }
                            }
                        }
                    }

                    return false;
                }

                ITopoArray<bool> patternMask = TopoArray.createByPoint(getPatternTopologyMask, patternTopology);
                patternTopology = patternTopology.withMask(patternMask);
            }

            return new TileModelMapping {
                PatternModel = patternModel,
                PatternsToTilesByOffset = patternsToTilesByOffset,
                TilesToPatternsByOffset = tilesToPatternsByOffset,
                PatternTopology = patternTopology,
                TileCoordToPatternCoordIndexAndOffset = tileCoordToPatternCoordIndexAndOffset,
                PatternCoordToTileCoordIndexAndOffset = patternCoordToTileCoordIndexAndOffset,
            };
        }

        public override void multiplyFrequency(Tile tile, double multiplier) {
            for (int p = 0; p < patternArrays.Count; p++) {
                PatternArray patternArray = patternArrays[p];
                for (int x = 0; x < patternArray.Width; x++) {
                    for (int y = 0; y < patternArray.Height; y++) {
                        for (int z = 0; z < patternArray.Depth; z++) {
                            if (patternArray.values[x, y, z] == tile) {
                                frequencies[p] *= multiplier;
                            }
                        }
                    }
                }
            }
        }
    }
}