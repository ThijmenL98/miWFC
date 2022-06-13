using System.Collections.Generic;
using System.Linq;
using miWFC.DeBroglie.Topo;
using miWFC.DeBroglie.Trackers;

namespace miWFC.DeBroglie.Constraints.Path;

internal class PathView : IPathView {
    private readonly List<int> endPointIndices;
    private readonly SelectedTracker endPointSelectedTracker;
    private readonly ISet<Tile> endPointTiles;
    private readonly TilePropagatorTileSet endPointTileSet;

    private readonly TilePropagator propagator;
    private readonly SelectedTracker selectedTracker;
    private readonly ISet<Tile> tiles;
    private readonly TilePropagatorTileSet tileSet;

    private readonly bool hasEndPoints;

    public PathView(PathSpec spec, TilePropagator propagator) {
        if (spec.TileRotation != null) {
            tiles = new HashSet<Tile>(spec.TileRotation.RotateAll(spec.Tiles));
            endPointTiles = spec.RelevantTiles == null
                ? null
                : new HashSet<Tile>(spec.TileRotation.RotateAll(spec.RelevantTiles));
        } else {
            tiles = spec.Tiles;
            endPointTiles = spec.RelevantTiles;
        }

        tileSet = propagator.CreateTileSet(tiles);
        selectedTracker = propagator.CreateSelectedTracker(tileSet);

        Graph = PathConstraintUtils.CreateGraph(propagator.Topology);
        this.propagator = propagator;

        CouldBePath = new bool[propagator.Topology.IndexCount];
        MustBePath = new bool[propagator.Topology.IndexCount];

        hasEndPoints = spec.RelevantCells != null || spec.RelevantTiles != null;

        if (hasEndPoints) {
            CouldBeRelevant = new bool[propagator.Topology.IndexCount];
            MustBeRelevant = new bool[propagator.Topology.IndexCount];
            endPointIndices = spec.RelevantCells == null
                ? null
                : spec.RelevantCells.Select(p => propagator.Topology.GetIndex(p.X, p.Y, p.Z)).ToList();
            endPointTileSet = spec.RelevantTiles != null ? propagator.CreateTileSet(endPointTiles) : null;
            endPointSelectedTracker
                = spec.RelevantTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
        } else {
            CouldBeRelevant = CouldBePath;
            MustBeRelevant = MustBePath;
            endPointTileSet = tileSet;
        }
    }

    public PathConstraintUtils.SimpleGraph Graph { get; }

    public bool[] CouldBePath { get; }
    public bool[] MustBePath { get; }


    public bool[] CouldBeRelevant { get; }
    public bool[] MustBeRelevant { get; }


    public void Update() {
        ITopology topology = propagator.Topology;
        int indexCount = topology.IndexCount;
        for (int i = 0; i < indexCount; i++) {
            Quadstate ts = selectedTracker.GetQuadstate(i);
            CouldBePath[i] = ts.Possible();
            MustBePath[i] = ts.IsYes();
        }

        if (hasEndPoints) {
            if (endPointIndices != null) {
                foreach (int index in endPointIndices) {
                    CouldBeRelevant[index] = MustBeRelevant[index] = true;
                }
            }

            if (endPointSelectedTracker != null) {
                for (int i = 0; i < indexCount; i++) {
                    Quadstate ts = endPointSelectedTracker.GetQuadstate(i);

                    CouldBeRelevant[i] = ts.Possible();
                    MustBeRelevant[i] = ts.IsYes();
                }
            }
        }
    }

    public void SelectPath(int index) {
        propagator.Topology.GetCoord(index, out int x, out int y, out int z);
        propagator.@select(x, y, z, tileSet);
    }

    public void BanPath(int index) {
        propagator.Topology.GetCoord(index, out int x, out int y, out int z);
        propagator.ban(x, y, z, tileSet);
    }

    public void BanRelevant(int index) {
        propagator.Topology.GetCoord(index, out int x, out int y, out int z);
        propagator.ban(x, y, z, endPointTileSet);
    }
}