using System;
using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Constraints.Path;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Trackers;

namespace WFC4ALL.DeBroglie.Constraints; 

[Obsolete("Use ConnectedConstraint instead")]
public class EdgedPathConstraint : ITileConstraint {
    private static readonly int[] empty = { };

    private SelectedTracker endPointSelectedTracker;

    private TilePropagatorTileSet endPointTileSet;

    private PathConstraintUtils.SimpleGraph graph;

    private SelectedTracker pathSelectedTracker;
    private TilePropagatorTileSet pathTileSet;

    private IDictionary<Direction, TilePropagatorTileSet> tilesByExit;
    private IDictionary<Direction, SelectedTracker> trackerByExit;

    public EdgedPathConstraint(IDictionary<Tile, ISet<Direction>> exits, Point[] endPoints = null,
        TileRotation tileRotation = null) {
        Exits = exits;
        EndPoints = endPoints;
        TileRotation = tileRotation;
    }

    private IDictionary<Tile, ISet<Direction>> actualExits { get; set; }


    /// <summary>
    ///     For each tile on the path, the set of direction values that paths exit out of this tile.
    /// </summary>
    public IDictionary<Tile, ISet<Direction>> Exits { get; set; }

    /// <summary>
    ///     Set of points that must be connected by paths.
    ///     If EndPoints and EndPointTiles are null, then EdgedPathConstraint ensures that all path cells
    ///     are connected.
    /// </summary>
    public Point[] EndPoints { get; set; }

    /// <summary>
    ///     Set of tiles that must be connected by paths.
    ///     If EndPoints and EndPointTiles are null, then EdgedPathConstraint ensures that all path cells
    ///     are connected.
    /// </summary>
    public ISet<Tile> EndPointTiles { get; set; }

    /// <summary>
    ///     If set, Exits is augmented with extra copies as dictated by the tile rotations
    /// </summary>
    public TileRotation TileRotation { get; set; }


    public void Init(TilePropagator propagator) {
        ISet<Tile> actualEndPointTiles;
        if (TileRotation != null) {
            actualExits = new Dictionary<Tile, ISet<Direction>>();
            foreach (KeyValuePair<Tile, ISet<Direction>> kv in Exits) {
                foreach (Rotation rot in TileRotation.RotationGroup) {
                    if (TileRotation.Rotate(kv.Key, rot, out Tile rtile)) {
                        Direction rotate(Direction d) {
                            return TopoArrayUtils.RotateDirection(propagator.Topology.AsGridTopology().Directions, d,
                                rot);
                        }

                        HashSet<Direction> rexits = new(kv.Value.Select(rotate));
                        actualExits[rtile] = rexits;
                    }
                }
            }

            actualEndPointTiles
                = EndPointTiles == null ? null : new HashSet<Tile>(TileRotation.RotateAll(EndPointTiles));
        } else {
            actualExits = Exits;
            actualEndPointTiles = EndPointTiles;
        }

        pathTileSet = propagator.CreateTileSet(Exits.Keys);
        pathSelectedTracker = propagator.CreateSelectedTracker(pathTileSet);
        endPointTileSet = EndPointTiles != null ? propagator.CreateTileSet(actualEndPointTiles) : null;
        endPointSelectedTracker = EndPointTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
        graph = CreateEdgedGraph(propagator.Topology);


        tilesByExit = actualExits
            .SelectMany(kv => kv.Value.Select(e => Tuple.Create(kv.Key, e)))
            .GroupBy(x => x.Item2, x => x.Item1)
            .ToDictionary(g => g.Key, propagator.CreateTileSet);

        trackerByExit = tilesByExit
            .ToDictionary(kv => kv.Key, kv => propagator.CreateSelectedTracker(kv.Value));

        Check(propagator, true);
    }

    public void Check(TilePropagator propagator) {
        Check(propagator, false);
    }

    private void Check(TilePropagator propagator, bool init) {
        ITopology topology = propagator.Topology;
        int indices = topology.Width * topology.Height * topology.Depth;

        int nodesPerIndex = topology.DirectionsCount + 1;

        // Initialize couldBePath and mustBePath based on wave possibilities
        bool[] couldBePath = new bool[indices * nodesPerIndex];
        bool[] mustBePath = new bool[indices * nodesPerIndex];
        bool[] exitMustBePath = new bool[indices * nodesPerIndex];
        foreach (KeyValuePair<Direction, SelectedTracker> kv in trackerByExit) {
            Direction exit = kv.Key;
            SelectedTracker tracker = kv.Value;
            for (int i = 0; i < indices; i++) {
                Quadstate ts = tracker.GetQuadstate(i);
                couldBePath[i * nodesPerIndex + 1 + (int) exit] = ts.Possible();
                // Cannot put this in mustBePath these points can be disconnected, depending on topology mask
                exitMustBePath[i * nodesPerIndex + 1 + (int) exit] = ts.IsYes();
            }
        }

        for (int i = 0; i < indices; i++) {
            Quadstate pathTs = pathSelectedTracker.GetQuadstate(i);
            couldBePath[i * nodesPerIndex] = pathTs.Possible();
            mustBePath[i * nodesPerIndex] = pathTs.IsYes();
        }

        // Select relevant cells, i.e. those that must be connected.
        bool hasEndPoints = EndPoints != null || EndPointTiles != null;
        bool[] relevant;
        if (!hasEndPoints) {
            // Basically equivalent to EndPoints = pathTileSet
            relevant = mustBePath;
        } else {
            relevant = new bool[indices * nodesPerIndex];

            int relevantCount = 0;
            if (EndPoints != null) {
                foreach (Point endPoint in EndPoints) {
                    int index = topology.GetIndex(endPoint.X, endPoint.Y, endPoint.Z);
                    relevant[index * nodesPerIndex] = true;
                    relevantCount++;
                }
            }

            if (EndPointTiles != null) {
                for (int i = 0; i < indices; i++) {
                    if (endPointSelectedTracker.IsSelected(i)) {
                        relevant[i * nodesPerIndex] = true;
                        relevantCount++;
                    }
                }
            }

            if (relevantCount == 0) {
                // Nothing to do.
                return;
            }
        }

        if (init) {
            for (int i = 0; i < indices; i++) {
                if (relevant[i * nodesPerIndex]) {
                    topology.GetCoord(i, out int x, out int y, out int z);
                    propagator.@select(x, y, z, pathTileSet);
                }
            }
        }

        bool[] walkable = couldBePath;

        PathConstraintUtils.AtrticulationPointsInfo info
            = PathConstraintUtils.GetArticulationPoints(graph, walkable, relevant);
        bool[] isArticulation = info.IsArticulation;

        if (info.ComponentCount > 1) {
            propagator.SetContradiction("Edged path constraint found multiple connected components.", this);
            return;
        }


        // All articulation points must be paths,
        // So ban any other possibilities
        for (int i = 0; i < indices; i++) {
            topology.GetCoord(i, out int x, out int y, out int z);
            if (isArticulation[i * nodesPerIndex] && !mustBePath[i * nodesPerIndex]) {
                propagator.@select(x, y, z, pathTileSet);
            }

            for (int d = 0; d < topology.DirectionsCount; d++) {
                if (isArticulation[i * nodesPerIndex + 1 + d] && !exitMustBePath[i * nodesPerIndex + 1 + d]) {
                    if (tilesByExit.TryGetValue((Direction) d, out TilePropagatorTileSet? exitTiles)) {
                        propagator.@select(x, y, z, exitTiles);
                    }
                }
            }
        }

        // Any path tiles / EndPointTiles not in the connected component aren't safe to add.
        if (info.ComponentCount > 0) {
            int?[] component = info.Component;
            TilePropagatorTileSet? actualEndPointTileSet = hasEndPoints ? endPointTileSet : pathTileSet;
            if (actualEndPointTileSet != null) {
                for (int i = 0; i < indices; i++) {
                    if (component[i * nodesPerIndex] == null) {
                        topology.GetCoord(i, out int x, out int y, out int z);
                        propagator.ban(x, y, z, actualEndPointTileSet);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Creates a grpah where each index in the original topology
    ///     has 1+n nodes in the graph - one for the initial index
    ///     and one for each direction leading out of it.
    /// </summary>
    private static PathConstraintUtils.SimpleGraph CreateEdgedGraph(ITopology topology) {
        int nodesPerIndex = topology.DirectionsCount + 1;

        int nodeCount = topology.IndexCount * nodesPerIndex;

        int[][] neighbours = new int[nodeCount][];

        int getNodeId(int index) {
            return index * nodesPerIndex;
        }

        int getDirNodeId(int index, Direction direction) {
            return index * nodesPerIndex + 1 + (int) direction;
        }

        foreach (int i in topology.GetIndices()) {
            List<int> n = new();
            for (int d = 0; d < topology.DirectionsCount; d++) {
                Direction direction = (Direction) d;
                if (topology.TryMove(i, direction, out int dest, out Direction inverseDir, out EdgeLabel _)) {
                    // The central node connects to the direction node
                    n.Add(getDirNodeId(i, direction));
                    // The diction node connects to the central node
                    // and the opposing direction node
                    neighbours[getDirNodeId(i, direction)] =
                        new[] {getNodeId(i), getDirNodeId(dest, inverseDir)};
                } else {
                    neighbours[getDirNodeId(i, direction)] = empty;
                }
            }

            neighbours[getNodeId(i)] = n.ToArray();
        }

        return new PathConstraintUtils.SimpleGraph {
            NodeCount = nodeCount,
            Neighbours = neighbours
        };
    }
}