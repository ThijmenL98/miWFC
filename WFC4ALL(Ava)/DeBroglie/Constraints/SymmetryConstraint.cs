using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Models;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Trackers;

namespace WFC4ALL.DeBroglie.Constraints; 

/// <summary>
///     Abstract constraint for any sort of global symmetry where
///     a choice of a tile in an index implies a selection for a specific index elsewhere.
/// </summary>
public abstract class SymmetryConstraint : ITileConstraint {
    private static Tile sentinel = new(new object());

    private ChangeTracker changeTracker;

    public virtual void Init(TilePropagator propagator) {
        changeTracker = propagator.CreateChangeTracker();

        TilePropagator p = propagator;
        ITopology topology = propagator.Topology;

        // Ban any tiles which don't have a symmetry
        foreach (int i in topology.GetIndices()) {
            if (TryMapIndex(p, i, out int i2)) {
                topology.GetCoord(i, out int x, out int y, out int z);

                propagator.@select(x, y, z, propagator.TileModel.Tiles
                    .Where(tile => TryMapTile(tile, out Tile _)));
            }
        }

        // Ban tiles that interact badly with their own symmetry
        foreach (int i in topology.GetIndices()) {
            if (TryMapIndex(p, i, out int i2)) {
                topology.GetCoord(i, out int x, out int y, out int z);

                if (i2 == i) {
                    // index maps to itself, so only allow tiles that map to themselves
                    IEnumerable<Tile> allowedTiles = propagator.TileModel.Tiles
                        .Where(tile => TryMapTile(tile, out Tile tile2) && tile == tile2);
                    propagator.@select(x, y, z, allowedTiles);
                    continue;
                }


                // TODO: Support overlapped model?
                if (propagator.TileModel is AdjacentModel adjacentModel) {
                    for (int d = 0; d < topology.DirectionsCount; d++) {
                        if (topology.TryMove(i, (Direction) d, out int dest) && dest == i2) {
                            // index maps adjacent to itself, so only allow tiles that can be placed adjacent to themselves
                            List<Tile> allowedTiles = propagator.TileModel.Tiles
                                .Where(tile =>
                                    TryMapTile(tile, out Tile tile2)
                                    && adjacentModel.IsAdjacent(tile, tile2, (Direction) d))
                                .ToList();
                            propagator.@select(x, y, z, allowedTiles);
                        }
                    }
                }

                if (propagator.TileModel is GraphAdjacentModel graphAdjacentModel) {
                    Tile sentinel = new(new object());
                    for (int d = 0; d < topology.DirectionsCount; d++) {
                        if (topology.TryMove(i, (Direction) d, out int dest, out Direction _, out EdgeLabel edgeLabel)
                            && dest == i2) {
                            // index maps adjacent to itself, so only allow tiles that can be placed adjacent to themselves
                            IEnumerable<Tile> allowedTiles = propagator.TileModel.Tiles
                                .Where(tile =>
                                    TryMapTile(tile, out Tile tile2)
                                    && graphAdjacentModel.IsAdjacent(tile, tile2, edgeLabel));
                            propagator.@select(x, y, z, allowedTiles);
                        }
                    }
                }

                topology.GetCoord(i2, out int x2, out int y2, out int z2);
            }
        }
    }

    public void Check(TilePropagator propagator) {
        ITopology topology = propagator.Topology;
        foreach (int i in changeTracker.GetChangedIndices()) {
            if (TryMapIndex(propagator, i, out int i2)) {
                topology.GetCoord(i, out int x, out int y, out int z);
                topology.GetCoord(i2, out int x2, out int y2, out int z2);

                foreach (Tile tile in propagator.TileModel.Tiles) {
                    if (TryMapTile(tile, out Tile tile2)) {
                        if (propagator.IsBanned(x, y, z, tile) && !propagator.IsBanned(x2, y2, z2, tile2)) {
                            propagator.ban(x2, y2, z2, tile2);
                        }
                    }
                }
            }
        }
    }

    protected abstract bool TryMapIndex(TilePropagator propagator, int i, out int i2);

    protected abstract bool TryMapTile(Tile tile, out Tile tile2);
}