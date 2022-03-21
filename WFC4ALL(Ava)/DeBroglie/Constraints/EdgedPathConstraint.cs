using System;
using System.Collections.Generic;
using System.Linq;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Constraints
{
    public class EdgedPathConstraint : ITileConstraint
    {
        private TilePropagatorTileSet pathTileSet;

        private SelectedTracker pathSelectedTracker;

        private TilePropagatorTileSet endPointTileSet;

        private SelectedTracker endPointSelectedTracker;

        private PathConstraintUtils.SimpleGraph graph;

        private IDictionary<Direction, TilePropagatorTileSet> tilesByExit;
        private IDictionary<Direction, SelectedTracker> trackerByExit;
        private IDictionary<Tile, ISet<Direction>> ActualExits { get; set; }


        /// <summary>
        /// For each tile on the path, the set of direction values that paths exit out of this tile.
        /// </summary>
        public IDictionary<Tile, ISet<Direction>> Exits { get; set; }

        /// <summary>
        /// Set of points that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then EdgedPathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public Point[] EndPoints { get; set; }

        /// <summary>
        /// Set of tiles that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then EdgedPathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public ISet<Tile> EndPointTiles { get; set; }

        /// <summary>
        /// If set, Exits is augmented with extra copies as dictated by the tile rotations
        /// </summary>
        public TileRotation TileRotation { get; set; }

        public EdgedPathConstraint(IDictionary<Tile, ISet<Direction>> exits, Point[] endPoints = null, TileRotation tileRotation = null)
        {
            Exits = exits;
            EndPoints = endPoints;
            TileRotation = tileRotation;
        }


        public void init(TilePropagator propagator)
        {
            pathTileSet = propagator.createTileSet(Exits.Keys);
            pathSelectedTracker = propagator.createSelectedTracker(pathTileSet);
            endPointTileSet = EndPointTiles != null ? propagator.createTileSet(EndPointTiles) : null;
            endPointSelectedTracker = EndPointTiles != null ? propagator.createSelectedTracker(endPointTileSet) : null;
            graph = createEdgedGraph(propagator.Topology);

            if (TileRotation != null)
            {
                ActualExits = new Dictionary<Tile, ISet<Direction>>();
                foreach (KeyValuePair<Tile, ISet<Direction>> kv in Exits)
                {
                    foreach (Rotation rot in TileRotation.RotationGroup)
                    {
                        if (TileRotation.rotate(kv.Key, rot, out Tile rtile))
                        {
                            Direction rotate(Direction d)
                            {
                                return TopoArrayUtils.rotateDirection(propagator.Topology.asGridTopology().Directions, d, rot);
                            }
                            HashSet<Direction> rexits = new(kv.Value.Select(rotate));
                            ActualExits[rtile] = rexits;
                        }
                    }
                }
            }
            else
            {
                ActualExits = Exits;
            }

            tilesByExit = ActualExits
                .SelectMany(kv => kv.Value.Select(e => Tuple.Create(kv.Key, e)))
                .GroupBy(x => x.Item2, x => x.Item1)
                .ToDictionary(g => g.Key, propagator.createTileSet);

            trackerByExit = tilesByExit
                .ToDictionary(kv => kv.Key, kv => propagator.createSelectedTracker(kv.Value));
        }

        public void check(TilePropagator propagator)
        {

            ITopology topology = propagator.Topology;
            int indices = topology.Width * topology.Height * topology.Depth;

            int nodesPerIndex = topology.DirectionsCount + 1;

            // Initialize couldBePath and mustBePath based on wave possibilities
            bool[] couldBePath = new bool[indices * nodesPerIndex];
            bool[] mustBePath = new bool[indices * nodesPerIndex];
            bool[] exitMustBePath = new bool[indices * nodesPerIndex];
            foreach (KeyValuePair<Direction, SelectedTracker> kv in trackerByExit)
            {
                Direction exit = kv.Key;
                SelectedTracker tracker = kv.Value;
                for (int i = 0; i < indices; i++)
                {
                    Tristate ts = tracker.getTristate(i);
                    couldBePath[i * nodesPerIndex + 1 + (int)exit] = ts.possible();
                    // Cannot put this in mustBePath these points can be disconnected, depending on topology mask
                    exitMustBePath[i * nodesPerIndex + 1 + (int)exit] = ts.isYes();
                }
            }
            for (int i = 0; i < indices; i++)
            {
                Tristate pathTs = pathSelectedTracker.getTristate(i);
                couldBePath[i * nodesPerIndex] = pathTs.possible();
                mustBePath[i * nodesPerIndex] = pathTs.isYes();
            }
            // Select relevant cells, i.e. those that must be connected.
            bool[] relevant;
            if (EndPoints == null && EndPointTiles == null)
            {
                relevant = mustBePath;
            }
            else
            {
                relevant = new bool[indices * nodesPerIndex];

                int relevantCount = 0;
                if (EndPoints != null)
                {
                    foreach (Point endPoint in EndPoints)
                    {
                        int index = topology.getIndex(endPoint.x, endPoint.y, endPoint.z);
                        relevant[index * nodesPerIndex] = true;
                        relevantCount++;
                    }
                }
                if (EndPointTiles != null)
                {
                    for (int i = 0; i < indices; i++)
                    {
                        if (endPointSelectedTracker.isSelected(i))
                        {
                            relevant[i * nodesPerIndex] = true;
                            relevantCount++;
                        }
                    }
                }
                if (relevantCount == 0)
                {
                    // Nothing to do.
                    return;
                }
            }
            bool[] walkable = couldBePath;

            bool[] component = EndPointTiles != null ? new bool[indices] : null;

            bool[] isArticulation = PathConstraintUtils.getArticulationPoints(graph, walkable, relevant, component);

            if (isArticulation == null)
            {
                propagator.setContradiction();
                return;
            }


            // All articulation points must be paths,
            // So ban any other possibilities
            for (int i = 0; i < indices; i++)
            {
                topology.getCoord(i, out int x, out int y, out int z);
                if (isArticulation[i * nodesPerIndex] && !mustBePath[i * nodesPerIndex])
                {
                    propagator.select(x, y, z, pathTileSet);
                }
                for (int d = 0; d < topology.DirectionsCount; d++)
                {
                    if(isArticulation[i * nodesPerIndex + 1 + d] && !exitMustBePath[i * nodesPerIndex + 1 + d])
                    {
                        propagator.select(x, y, z, tilesByExit[(Direction)d]);
                    }
                }
            }

            // Any EndPointTiles not in the connected component aren't safe to add
            if (EndPointTiles != null)
            {
                for (int i = 0; i < indices; i++)
                {
                    if (!component[i * nodesPerIndex])
                    {
                        topology.getCoord(i, out int x, out int y, out int z);
                        propagator.ban(x, y, z, endPointTileSet);
                    }
                }
            }
        }

        private static readonly int[] empty = { };

        /// <summary>
        /// Creates a grpah where each index in the original topology
        /// has 1+n nodes in the graph - one for the initial index
        /// and one for each direction leading out of it.
        /// </summary>
        private static PathConstraintUtils.SimpleGraph createEdgedGraph(ITopology topology)
        {
            int nodesPerIndex = topology.DirectionsCount + 1;

            int nodeCount = topology.IndexCount * nodesPerIndex;

            int[][] neighbours = new int[nodeCount][];

            int getNodeId(int index) => index * nodesPerIndex;

            int getDirNodeId(int index, Direction direction) => index * nodesPerIndex + 1 + (int)direction;

            foreach (int i in topology.getIndices())
            {
                List<int> n = new();
                for(int d=0;d<topology.DirectionsCount;d++)
                {
                    Direction direction = (Direction)d;
                    if (topology.tryMove(i, direction, out int dest, out Direction inverseDir, out EdgeLabel _))
                    {
                        // The central node connects to the direction node
                        n.Add(getDirNodeId(i, direction));
                        // The diction node connects to the central node
                        // and the opposing direction node
                        neighbours[getDirNodeId(i, direction)] =
                            new[] { getNodeId(i), getDirNodeId(dest, inverseDir) };
                    }
                    else
                    {
                        neighbours[getDirNodeId(i, direction)] = empty;
                    }
                }
                neighbours[getNodeId(i)] = n.ToArray();
            }

            return new PathConstraintUtils.SimpleGraph
            {
                NodeCount = nodeCount,
                Neighbours = neighbours,
            };
        }
    }
}
