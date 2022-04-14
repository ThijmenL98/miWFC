using System;
using System.Collections.Generic;
using WFC4ALL.DeBroglie.Constraints.Path;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Trackers;

namespace WFC4ALL.DeBroglie.Constraints; 

/// <summary>
///     The PathConstraint checks that it is possible to connect several locations together via a continuous path of
///     adjacent tiles.
///     It does this by banning any tile placement that would make such a path impossible.
/// </summary>
[Obsolete("Use ConnectedConstraint instead")]
public class PathConstraint : ITileConstraint {
    private SelectedTracker endPointSelectedTracker;

    private TilePropagatorTileSet endPointTileSet;

    private PathConstraintUtils.SimpleGraph graph;

    private SelectedTracker selectedTracker;
    private TilePropagatorTileSet tileSet;


    public PathConstraint(ISet<Tile> tiles, Point[] endPoints = null, TileRotation tileRotation = null) {
        Tiles = tiles;
        EndPoints = endPoints;
        TileRotation = tileRotation;
    }

    /// <summary>
    ///     Set of patterns that are considered on the path
    /// </summary>
    public ISet<Tile> Tiles { get; set; }

    /// <summary>
    ///     Set of points that must be connected by paths.
    ///     If EndPoints and EndPointTiles are null, then PathConstraint ensures that all path cells
    ///     are connected.
    /// </summary>
    public Point[] EndPoints { get; set; }

    /// <summary>
    ///     Set of tiles that must be connected by paths.
    ///     If EndPoints and EndPointTiles are null, then PathConstraint ensures that all path cells
    ///     are connected.
    /// </summary>
    public ISet<Tile> EndPointTiles { get; set; }

    /// <summary>
    ///     If set, Tiles is augmented with extra copies as dictated by the tile rotations
    /// </summary>
    public TileRotation TileRotation { get; set; }

    public void Init(TilePropagator propagator) {
        ISet<Tile> actualTiles;
        ISet<Tile> actualEndPointTiles;
        if (TileRotation != null) {
            actualTiles = new HashSet<Tile>(TileRotation.RotateAll(Tiles));
            actualEndPointTiles
                = EndPointTiles == null ? null : new HashSet<Tile>(TileRotation.RotateAll(EndPointTiles));
        } else {
            actualTiles = Tiles;
            actualEndPointTiles = EndPointTiles;
        }

        tileSet = propagator.CreateTileSet(actualTiles);
        selectedTracker = propagator.CreateSelectedTracker(tileSet);
        endPointTileSet = EndPointTiles != null ? propagator.CreateTileSet(actualEndPointTiles) : null;
        endPointSelectedTracker = EndPointTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
        graph = PathConstraintUtils.CreateGraph(propagator.Topology);

        Check(propagator, true);
    }

    public void Check(TilePropagator propagator) {
        Check(propagator, false);
    }

    private void Check(TilePropagator propagator, bool init) {
        ITopology topology = propagator.Topology;
        int indices = topology.IndexCount;
        // Initialize couldBePath and mustBePath based on wave possibilities
        bool[] couldBePath = new bool[indices];
        bool[] mustBePath = new bool[indices];
        for (int i = 0; i < indices; i++) {
            Quadstate ts = selectedTracker.GetQuadstate(i);
            couldBePath[i] = ts.Possible();
            mustBePath[i] = ts.IsYes();
        }

        // Select relevant cells, i.e. those that must be connected.
        bool hasEndPoints = EndPoints != null || EndPointTiles != null;
        bool[] relevant;
        if (!hasEndPoints) {
            relevant = mustBePath;
        } else {
            relevant = new bool[indices];
            int relevantCount = 0;
            if (EndPoints != null) {
                foreach (Point endPoint in EndPoints) {
                    int index = topology.GetIndex(endPoint.X, endPoint.Y, endPoint.Z);
                    relevant[index] = true;
                    relevantCount++;
                }
            }

            if (EndPointTiles != null) {
                for (int i = 0; i < indices; i++) {
                    if (endPointSelectedTracker.IsSelected(i)) {
                        relevant[i] = true;
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
                if (relevant[i]) {
                    topology.GetCoord(i, out int x, out int y, out int z);
                    propagator.@select(x, y, z, tileSet);
                }
            }
        }

        bool[] walkable = couldBePath;

        PathConstraintUtils.AtrticulationPointsInfo info
            = PathConstraintUtils.GetArticulationPoints(graph, walkable, relevant);
        bool[] isArticulation = info.IsArticulation;

        if (info.ComponentCount > 1) {
            propagator.SetContradiction("Path constraint found multiple components", this);
            return;
        }

        // All articulation points must be paths,
        // So ban any other possibilities
        for (int i = 0; i < indices; i++) {
            if (isArticulation[i] && !mustBePath[i]) {
                topology.GetCoord(i, out int x, out int y, out int z);
                propagator.@select(x, y, z, tileSet);
            }
        }

        // Any path tiles / EndPointTiles not in the connected component aren't safe to add.
        if (info.ComponentCount > 0) {
            int?[] component = info.Component;
            TilePropagatorTileSet? actualEndPointTileSet = hasEndPoints ? endPointTileSet : tileSet;
            if (actualEndPointTileSet != null) {
                for (int i = 0; i < indices; i++) {
                    if (component[i] == null) {
                        topology.GetCoord(i, out int x, out int y, out int z);
                        propagator.ban(x, y, z, actualEndPointTileSet);
                    }
                }
            }
        }
    }
}