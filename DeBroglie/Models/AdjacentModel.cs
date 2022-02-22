using System;
using System.Collections.Generic;
using System.Linq;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie.Models
{


    /// <summary>
    /// AdjacentModel constrains which tiles can be placed adjacent to which other ones. 
    /// It does so by maintaining for each tile, a list of tiles that can be placed next to it in each direction. 
    /// The list is always symmetric, i.e. if it is legal to place tile B directly above tile A, then it is legal to place A directly below B.
    /// </summary>
    public class AdjacentModel : TileModel
    {
        private DirectionSet directions;
        private readonly Dictionary<Tile, int> tilesToPatterns;
        private readonly List<double> frequencies;
        private readonly List<HashSet<int>[]> propagator;

        /// <summary>
        /// Constructs an AdjacentModel and initializees it with a given sample.
        /// </summary>
        public static AdjacentModel create<T>(T[,] sample, bool periodic)
        {
            return create(new TopoArray2D<T>(sample, periodic));
        }

        /// <summary>
        /// Constructs an AdjacentModel and initializees it with a given sample.
        /// </summary>
        public static AdjacentModel create<T>(ITopoArray<T> sample)
        {
            return new AdjacentModel(sample.toTiles());
        }


        /// <summary>
        /// Constructs an AdjacentModel.
        /// </summary>
        public AdjacentModel()
        {
            // Tiles map 1:1 with patterns
            tilesToPatterns = new Dictionary<Tile, int>();
            frequencies = new List<double>();
            propagator = new List<HashSet<int>[]>();
        }

        /// <summary>
        /// Constructs an AdjacentModel.
        /// </summary>
        public AdjacentModel(DirectionSet directions)
            : this()
        {
            setDirections(directions);
        }



        /// <summary>
        /// Constructs an AdjacentModel and initializees it with a given sample.
        /// </summary>
        public AdjacentModel(ITopoArray<Tile> sample)
            : this()
        {
            addSample(sample);
        }

        public override IEnumerable<Tile> Tiles => tilesToPatterns.Keys;

        /// <summary>
        /// Sets the directions of the Adjacent model, if it has not been set at construction.
        /// This specifies how many neighbours each tile has.
        /// Once set, it cannot be changed.
        /// </summary>
        /// <param name="newDirections"></param>
        public void setDirections(DirectionSet newDirections)
        {
            if (directions.Type != DirectionSetType.UNKNOWN && directions.Type != newDirections.Type)
            {
                throw new Exception($"Cannot set directions to {newDirections.Type}, it has already been set to {directions.Type}");
            }

            directions = newDirections;
        }

        private void requireDirections()
        {
            if(directions.Type == DirectionSetType.UNKNOWN)
            {
                throw new Exception("Directions must be set before calling this method");
            }
        }

        /// <summary>
        /// Finds a tile and all its rotations, and sets their total frequency.
        /// </summary>
        public void setFrequency(Tile tile, double frequency, TileRotation tileRotation)
        {
            List<Tile> rotatedTiles = tileRotation.rotateAll(tile).ToList();
            foreach (Tile rt in rotatedTiles)
            {
                int pattern = getPattern(rt);
                frequencies[pattern] = 0.0;
            }
            double incrementalFrequency = frequency / rotatedTiles.Count;
            foreach (Tile rt in rotatedTiles)
            {
                int pattern = getPattern(rt);
                frequencies[pattern] += incrementalFrequency;
            }
        }


        /// <summary>
        /// Sets the frequency of a given tile.
        /// </summary>
        public void setFrequency(Tile tile, double frequency)
        {
            int pattern = getPattern(tile);
            frequencies[pattern] = frequency;
        }

        /// <summary>
        /// Sets all tiles as equally likely to be picked
        /// </summary>
        public void setUniformFrequency()
        {
            foreach(Tile tile in Tiles)
            {
                setFrequency(tile, 1.0);
            }
        }

        /// <summary>
        /// Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified.
        /// Then it adds similar declarations for other rotations and reflections, as specified by rotations.
        /// </summary>
        public void addAdjacency(IList<Tile> src, IList<Tile> dest, Direction dir, TileRotation tileRotation = null)
        {
            requireDirections();
            int d = (int)dir;
            int x = directions.Dx[d];
            int y = directions.Dy[d];
            int z = directions.Dz[d];
            addAdjacency(src, dest, x, y, z, tileRotation);
        }

        /// <summary>
        /// Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified by (x, y, z).
        /// Then it adds similar declarations for other rotations and reflections, as specified by rotations.
        /// </summary>
        public void addAdjacency(IList<Tile> src, IList<Tile> dest, int x, int y, int z, TileRotation tileRotation = null)
        {
            requireDirections();

            tileRotation = tileRotation ?? new TileRotation();

            foreach (Rotation rotation in tileRotation.RotationGroup)
            {
                (int x2, int y2) = TopoArrayUtils.rotateVector(directions.Type, x, y, rotation);

                addAdjacency(
                    tileRotation.rotate(src, rotation).ToList(),
                    tileRotation.rotate(dest, rotation).ToList(),
                    x2, y2, z);
            }
        }

        /// <summary>
        /// Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified by (x, y, z).
        /// (x, y, z) must be a valid direction, which usually means a unit vector.
        /// </summary>
        public void addAdjacency(IList<Tile> src, IList<Tile> dest, int x, int y, int z)
        {
            requireDirections();
            addAdjacency(src, dest, directions.getDirection(x, y, z));
        }

        /// <summary>
        /// Declares that the tiles in dest can be placed adjacent to the tiles in src, in the direction specified by (x, y, z).
        /// (x, y, z) must be a valid direction, which usually means a unit vector.
        /// </summary>
        public void addAdjacency(IList<Tile> src, IList<Tile> dest, Direction dir)
        {
            requireDirections();

            foreach (Tile s in src)
            {
                foreach (Tile d in dest)
                {
                    addAdjacency(s, d, dir);
                }
            }
        }

        /// <summary>
        /// Declares that dest can be placed adjacent to src, in the direction specified by (x, y, z).
        /// (x, y, z) must be a valid direction, which usually means a unit vector.
        /// </summary>
        public void addAdjacency(Tile src, Tile dest, int x, int y, int z)
        {
            requireDirections();
            Direction d = directions.getDirection(x, y, z);
            addAdjacency(src, dest, d);
        }

        /// <summary>
        /// Declares that dest can be placed adjacent to src, in the direction specified.
        /// </summary>
        public void addAdjacency(Tile src, Tile dest, Direction d)
        {
            Direction id = directions.inverse(d);
            int srcPattern = getPattern(src);
            int destPattern = getPattern(dest);
            propagator[srcPattern][(int)d].Add(destPattern);
            propagator[destPattern][(int)id].Add(srcPattern);
        }

        public void addAdjacency(Adjacency adjacency)
        {
            addAdjacency(adjacency.Src, adjacency.Dest, adjacency.Direction);
        }

        public bool isAdjacent(Tile src, Tile dest, Direction d)
        {
            int srcPattern = getPattern(src);
            int destPattern = getPattern(dest);
            return propagator[srcPattern][(int)d].Contains(destPattern);
        }

        public void addSample(ITopoArray<Tile> sample, TileRotation tileRotation = null) {
            foreach (ITopoArray<Tile> s in OverlappingAnalysis.getRotatedSamples(sample, tileRotation))
            {
                addSample(s);
            }
        }

        public void addSample(ITopoArray<Tile> sample)
        {
            GridTopology topology = sample.Topology.asGridTopology();

            setDirections(topology.Directions);

            int width = topology.Width;
            int height = topology.Height;
            int depth = topology.Depth;
            int directionCount = topology.Directions.Count;

            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = topology.getIndex(x, y, z);
                        if (!topology.containsIndex(index)) {
                            continue;
                        }

                        // Find the pattern and update the frequency
                        int pattern = getPattern(sample.get(x, y, z));

                        frequencies[pattern] += 1;

                        // Update propagator
                        for (int d = 0; d < directionCount; d++)
                        {
                            int x2, y2, z2;
                            if (topology.tryMove(x, y, z, (Direction)d, out x2, out y2, out z2))
                            {
                                int pattern2 = getPattern(sample.get(x2, y2, z2));
                                propagator[pattern][d].Add(pattern2);
                            }
                        }
                    }
                }
            }
        }

        internal override TileModelMapping getTileModelMapping(ITopology topology)
        {
            GridTopology gridTopology = topology.asGridTopology();
            requireDirections();
            setDirections(gridTopology.Directions);

            if(frequencies.Sum() == 0.0)
            {
                throw new Exception("No tiles have assigned frequences.");
            }

            PatternModel patternModel = new PatternModel
            {
                Propagator = propagator.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(),
                Frequencies = frequencies.ToArray(),
            };
            Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>()
                {
                    {0, tilesToPatterns.ToLookup(x=>x.Key, x=>x.Value).ToDictionary(g=>g.Key, g=>(ISet<int>)new HashSet<int>(g)) }
                };
            Dictionary<int, IReadOnlyDictionary<int, Tile>> patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>
                {
                    {0, tilesToPatterns.ToDictionary(x => x.Value, x => x.Key)},
                };
            return new TileModelMapping
            {
                PatternTopology = gridTopology,
                PatternModel = patternModel,
                PatternsToTilesByOffset = patternsToTilesByOffset,
                TilesToPatternsByOffset = tilesToPatternsByOffset,
                TileCoordToPatternCoordIndexAndOffset = null,
            };
        }

        public override void multiplyFrequency(Tile tile, double multiplier)
        {
            int pattern = tilesToPatterns[tile];
            frequencies[pattern] *= multiplier;
        }

        private int getPattern(Tile tile)
        {
            int directionCount = directions.Count;

            int pattern;
            if (!tilesToPatterns.TryGetValue(tile, out pattern))
            {
                pattern = tilesToPatterns[tile] = tilesToPatterns.Count;
                frequencies.Add(0);
                propagator.Add(new HashSet<int>[directionCount]);
                for (int d = 0; d < directionCount; d++)
                {
                    propagator[pattern][d] = new HashSet<int>();
                }
            }
            return pattern;
        }

        public class Adjacency
        {
            public Tile[] Src { get; set; }
            public Tile[] Dest { get; set; }
            public Direction Direction { get; set; }
        }
    }
}
