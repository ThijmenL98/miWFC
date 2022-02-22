using System;
using System.Collections.Generic;
using System.Linq;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie.Models
{
    /// <summary>
    /// Functions as AdjacentModel, but is more generic and will function for any toplogy.
    /// </summary>
    public class GraphAdjacentModel : TileModel
    {
        private readonly int directionsCount;
        private readonly int edgeLabelCount;
        private readonly Dictionary<Tile, int> tilesToPatterns;
        private readonly List<double> frequencies;
        // By Pattern, then edge-label
        private readonly List<HashSet<int>[]> propagator;
        private readonly (Direction, Direction, Rotation)[] edgeLabelInfo;

        public GraphAdjacentModel(GraphInfo graphInfo)
            :this(graphInfo.DirectionsCount, graphInfo.EdgeLabelCount)
        {
            edgeLabelInfo = graphInfo.EdgeLabelInfo;
        }


        public GraphAdjacentModel(int directionsCount, int edgeLabelCount)
        {
            this.directionsCount = directionsCount;
            this.edgeLabelCount = edgeLabelCount;
            // Tiles map 1:1 with patterns
            tilesToPatterns = new Dictionary<Tile, int>();
            frequencies = new List<double>();
            propagator = new List<HashSet<int>[]>();
        }

        internal override TileModelMapping getTileModelMapping(ITopology topology)
        {
            if (frequencies.Sum() == 0.0)
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
                PatternTopology = topology,
                PatternModel = patternModel,
                PatternsToTilesByOffset = patternsToTilesByOffset,
                TilesToPatternsByOffset = tilesToPatternsByOffset,
                TileCoordToPatternCoordIndexAndOffset = null,
            };
        }

        public override IEnumerable<Tile> Tiles => tilesToPatterns.Keys;

        public override void multiplyFrequency(Tile tile, double multiplier)
        {
            int pattern = tilesToPatterns[tile];
            frequencies[pattern] *= multiplier;
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
            foreach (Tile tile in Tiles)
            {
                setFrequency(tile, 1.0);
            }
        }

        private int getPattern(Tile tile)
        {
            int pattern;
            if (!tilesToPatterns.TryGetValue(tile, out pattern))
            {
                pattern = tilesToPatterns[tile] = tilesToPatterns.Count;
                frequencies.Add(0);
                propagator.Add(new HashSet<int>[edgeLabelCount]);
                for (int el = 0; el < edgeLabelCount; el++)
                {
                    propagator[pattern][el] = new HashSet<int>();
                }
            }
            return pattern;
        }

        public void addAdjacency(IList<Tile> src, IList<Tile> dest, Direction direction, TileRotation tileRotation)
        {
            foreach (Tile s in src)
            {
                foreach (Tile d in dest)
                {
                    addAdjacency(s, d, direction, tileRotation);
                }
            }
        }

        public void addAdjacency(Tile src, Tile dest, Direction direction, TileRotation tileRotation)
        {
            if(edgeLabelInfo == null)
            {
                throw new Exception("This method requires edgeLabelInfo configured");
            }
            List<(Direction, Direction, Rotation)> inverseDirectionItems = edgeLabelInfo.Where(x => x.Item3.IsIdentity && x.Item1 == direction).ToList();
            if(inverseDirectionItems.Count == 0)
            {
                throw new Exception($"Couldn't find identity edge label for direction {direction}");
            }
            Direction inverseDirection = inverseDirectionItems[0].Item2;
            for (int i = 0; i < edgeLabelInfo.Length; i++)
            {
                (Direction d, Direction id, Rotation r) = edgeLabelInfo[i];
                if (d == direction)
                {
                    Rotation rotation = r.inverse();
                    if (tileRotation.rotate(dest, rotation, out Tile rd))
                    {
                        addAdjacency(src, rd, (EdgeLabel)i);
                    }
                }
                if (d == inverseDirection)
                {
                    Rotation rotation = r.inverse();
                    if (tileRotation.rotate(src, rotation, out Tile rs))
                    {
                        addAdjacency(dest, rs, (EdgeLabel)i);
                    }
                }
            }
        }


        public void addAdjacency(IList<Tile> src, IList<Tile> dest, EdgeLabel edgeLabel)
        {
            foreach(Tile s in src)
            {
                foreach(Tile d in dest)
                {
                    addAdjacency(s, d, edgeLabel);
                }
            }
        }

        public void addAdjacency(Tile src, Tile dest, EdgeLabel edgeLabel)
        {
            int s = getPattern(src);
            int d = getPattern(dest);
            propagator[s][(int)edgeLabel].Add(d);
        }

        public bool isAdjacent(Tile src, Tile dest, EdgeLabel edgeLabel)
        {
            int srcPattern = getPattern(src);
            int destPattern = getPattern(dest);
            return propagator[srcPattern][(int)edgeLabel].Contains(destPattern);
        }
    }
}
