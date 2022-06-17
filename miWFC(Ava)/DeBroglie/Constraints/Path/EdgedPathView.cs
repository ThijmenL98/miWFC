using System;
using System.Collections.Generic;
using System.Linq;
using miWFC.DeBroglie.Rot;
using miWFC.DeBroglie.Topo;
using miWFC.DeBroglie.Trackers;

namespace miWFC.DeBroglie.Constraints.Path;

internal class EdgedPathView : IPathView {
    private static readonly int[] empty = { };

    private readonly List<int> endPointIndices;
    private readonly SelectedTracker endPointSelectedTracker;
    private readonly ISet<Tile> endPointTiles;
    private readonly TilePropagatorTileSet endPointTileSet;
    private readonly IDictionary<Tile, ISet<Direction>> exits;
    private readonly bool hasEndPoints;
    private readonly TilePropagatorTileSet pathTileSet;

    private readonly TilePropagator propagator;

    private readonly ITopology topology;

    public EdgedPathView(EdgedPathSpec spec, TilePropagator propagator) {
        if (spec.TileRotation != null) {
            exits = new Dictionary<Tile, ISet<Direction>>();
            foreach (KeyValuePair<Tile, ISet<Direction>> kv in spec.Exits) {
                foreach (Rotation rot in spec.TileRotation.RotationGroup) {
                    if (spec.TileRotation.Rotate(kv.Key, rot, out Tile rtile)) {
                        Direction rotate(Direction d) {
                            return TopoArrayUtils.RotateDirection(propagator.Topology.AsGridTopology().Directions, d,
                                rot);
                        }

                        HashSet<Direction> rexits = new(kv.Value.Select(rotate));
                        exits[rtile] = rexits;
                    }
                }
            }

            endPointTiles = spec.RelevantTiles == null
                ? null
                : new HashSet<Tile>(spec.TileRotation.RotateAll(spec.RelevantTiles));
        } else {
            exits = spec.Exits;
            endPointTiles = spec.RelevantTiles;
        }

        pathTileSet = propagator.CreateTileSet(exits.Keys);
        PathSelectedTracker = propagator.CreateSelectedTracker(pathTileSet);

        Graph = CreateEdgedGraph(propagator.Topology);
        this.propagator = propagator;
        topology = propagator.Topology;

        int nodesPerIndex = GetNodesPerIndex();

        CouldBePath = new bool[propagator.Topology.IndexCount * nodesPerIndex];
        MustBePath = new bool[propagator.Topology.IndexCount * nodesPerIndex];

        TileSetByExit = exits
            .SelectMany(kv => kv.Value.Select(e => Tuple.Create(kv.Key, e)))
            .GroupBy(x => x.Item2, x => x.Item1)
            .ToDictionary(g => g.Key, propagator.CreateTileSet);

        TrackerByExit = TileSetByExit
            .ToDictionary(kv => kv.Key, kv => propagator.CreateSelectedTracker(kv.Value));

        hasEndPoints = spec.RelevantCells != null || spec.RelevantTiles != null;

        if (hasEndPoints) {
            CouldBeRelevant = new bool[propagator.Topology.IndexCount * nodesPerIndex];
            MustBeRelevant = new bool[propagator.Topology.IndexCount * nodesPerIndex];
            endPointIndices = spec.RelevantCells == null
                ? null
                : spec.RelevantCells.Select(p => propagator.Topology.GetIndex(p.X, p.Y, p.Z)).ToList();
            endPointTileSet = endPointTiles != null ? propagator.CreateTileSet(endPointTiles) : null;
            endPointSelectedTracker = endPointTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
        } else {
            CouldBeRelevant = CouldBePath;
            MustBeRelevant = MustBePath;
            endPointTileSet = pathTileSet;
        }
    }

    public SelectedTracker PathSelectedTracker { get; }

    public Dictionary<Direction, SelectedTracker> TrackerByExit { get; }

    public Dictionary<Direction, TilePropagatorTileSet> TileSetByExit { get; }


    public PathConstraintUtils.SimpleGraph Graph { get; }

    public bool[] CouldBePath { get; }
    public bool[] MustBePath { get; }
    public bool[] CouldBeRelevant { get; }
    public bool[] MustBeRelevant { get; }

    public void Update() {
        int nodesPerIndex = GetNodesPerIndex();
        int indexCount = topology.IndexCount;


        foreach (KeyValuePair<Direction, SelectedTracker> kv in TrackerByExit) {
            Direction exit = kv.Key;
            SelectedTracker tracker = kv.Value;
            for (int i = 0; i < indexCount; i++) {
                Quadstate ts = tracker.GetQuadstate(i);
                CouldBePath[i * nodesPerIndex + 1 + (int) exit] = ts.Possible();
                MustBePath[i * nodesPerIndex + 1 + (int) exit] = ts.IsYes();
            }
        }

        for (int i = 0; i < indexCount; i++) {
            Quadstate pathTs = PathSelectedTracker.GetQuadstate(i);
            CouldBePath[i * nodesPerIndex] = pathTs.Possible();
            MustBePath[i * nodesPerIndex] = pathTs.IsYes();
        }

        if (hasEndPoints) {
            if (endPointIndices != null) {
                foreach (int index in endPointIndices) {
                    CouldBeRelevant[index * nodesPerIndex] = MustBeRelevant[index * nodesPerIndex] = true;
                }
            }

            if (endPointSelectedTracker != null) {
                for (int i = 0; i < indexCount; i++) {
                    Quadstate ts = endPointSelectedTracker.GetQuadstate(i);
                    CouldBeRelevant[i * nodesPerIndex] = ts.Possible();
                    MustBeRelevant[i * nodesPerIndex] = ts.IsYes();
                }
            }
        }
    }

    public void SelectPath(int node) {
        (int index, Direction? dir) = Unpack(node, GetNodesPerIndex());
        topology.GetCoord(index, out int x, out int y, out int z);
        if (dir == null) {
            propagator.select(x, y, z, pathTileSet);
        } else {
            if (MustBePath[node]) {
                return;
            }

            if (TileSetByExit.TryGetValue((Direction) dir, out TilePropagatorTileSet? exitTiles)) {
                propagator.select(x, y, z, exitTiles);
            }
        }
    }

    public void BanPath(int node) {
        (int index, Direction? dir) = Unpack(node, GetNodesPerIndex());
        topology.GetCoord(index, out int x, out int y, out int z);
        if (dir == null) {
            propagator.ban(x, y, z, pathTileSet);
        } else {
            if (TileSetByExit.TryGetValue((Direction) dir, out TilePropagatorTileSet? exitTiles)) {
                propagator.ban(x, y, z, exitTiles);
            }
        }
    }

    public void BanRelevant(int node) {
        (int index, Direction? dir) = Unpack(node, GetNodesPerIndex());
        topology.GetCoord(index, out int x, out int y, out int z);
        if (dir == null) {
            propagator.ban(x, y, z, endPointTileSet);
        }
    }

    private int GetNodesPerIndex() {
        return topology.DirectionsCount + 1;
    }

    private (int, Direction?) Unpack(int node, int nodesPerIndex) {
        int index = node / nodesPerIndex;
        int d = node - index * nodesPerIndex;
        if (d == 0) {
            return (index, null);
        }

        return (index, (Direction?) (d - 1));
    }

    private int Pack(int index, Direction? dir, int nodesPerIndex) {
        return index * nodesPerIndex + (dir == null ? 0 : 1 + (int) dir);
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
                // The central node connects to the direction node
                n.Add(getDirNodeId(i, direction));
                if (topology.TryMove(i, direction, out int dest, out Direction inverseDir, out EdgeLabel _)) {
                    // The diction node connects to the central node
                    // and the opposing direction node
                    neighbours[getDirNodeId(i, direction)] =
                        new[] {getNodeId(i), getDirNodeId(dest, inverseDir)};
                } else {
                    // Dead end
                    neighbours[getDirNodeId(i, direction)] =
                        new[] {getNodeId(i)};
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