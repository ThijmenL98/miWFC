using System.Collections.Generic;
using System.Linq;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Constraints
{
    /// <summary>
    /// Abstract constraint for any sort of global symmetry where 
    /// a choice of a tile in an index implies a selection for a specific index elsewhere.
    /// </summary>
    public abstract class SymmetryConstraint : ITileConstraint
    {
        protected abstract bool tryMapIndex(TilePropagator propagator, int i, out int i2);

        protected abstract bool tryMapTile(Tile tile, out Tile tile2);

        private ChangeTracker changeTracker;

        private static Tile sentinel = new(new object());

        public virtual void init(TilePropagator propagator)
        {
            changeTracker = propagator.createChangeTracker();

            TilePropagator p = propagator;
            ITopology topology = propagator.Topology;

            // Ban any tiles which don't have a symmetry
            foreach (int i in topology.getIndices())
            {
                if (tryMapIndex(p, i, out int i2))
                {
                    topology.getCoord(i, out int x, out int y, out int z);

                    propagator.select(x, y, z, propagator.TileModel.Tiles
                        .Where(tile => tryMapTile(tile, out Tile _)));
                }
            }

            // Ban tiles that interact badly with their own symmetry
            foreach (int i in topology.getIndices())
            {
                if (tryMapIndex(p, i, out int i2))
                {
                    topology.getCoord(i, out int x, out int y, out int z);

                    if (i2 == i)
                    {
                        // index maps to itself, so only allow tiles that map to themselves
                        IEnumerable<Tile> allowedTiles = propagator.TileModel.Tiles
                           .Where(tile => tryMapTile(tile, out Tile tile2) && tile == tile2);
                        propagator.select(x, y, z, allowedTiles);
                        continue;
                    }
                    
                    // TOODO: Support overlapped model?
                    if (propagator.TileModel is AdjacentModel adjacentModel)
                    {
                        for (int d = 0; d < topology.DirectionsCount; d++)
                        {
                            if (topology.tryMove(i, (Direction)d, out int dest) && dest == i2)
                            {
                                // index maps adjacent to itself, so only allow tiles that can be placed adjacent to themselves
                                List<Tile> allowedTiles = propagator.TileModel.Tiles
                                    .Where(tile => tryMapTile(tile, out Tile tile2) && adjacentModel.isAdjacent(tile, tile2, (Direction)d))
                                    .ToList();
                                propagator.select(x, y, z, allowedTiles);
                            }
                        }
                    }
                    if (propagator.TileModel is GraphAdjacentModel graphAdjacentModel)
                    {
                        Tile sentinel = new(new object());
                        for (int d = 0; d < topology.DirectionsCount; d++)
                        {
                            if (topology.tryMove(i, (Direction)d, out int dest, out Direction _, out EdgeLabel edgeLabel) && dest == i2)
                            {
                                // index maps adjacent to itself, so only allow tiles that can be placed adjacent to themselves
                                IEnumerable<Tile> allowedTiles = propagator.TileModel.Tiles
                                    .Where(tile => tryMapTile(tile, out Tile tile2) && graphAdjacentModel.isAdjacent(tile, tile2, edgeLabel));
                                propagator.select(x, y, z, allowedTiles);
                            }
                        }
                    }

                    topology.getCoord(i2, out int x2, out int y2, out int z2);
                }
            }
        }

        public void check(TilePropagator propagator)
        {
            ITopology topology = propagator.Topology;
            foreach (int i in changeTracker.getChangedIndices())
            {
                if (tryMapIndex(propagator, i, out int i2))
                {
                    topology.getCoord(i, out int x, out int y, out int z);
                    topology.getCoord(i2, out int x2, out int y2, out int z2);

                    foreach (Tile tile in propagator.TileModel.Tiles)
                    {
                        if (tryMapTile(tile, out Tile tile2))
                        {
                            if (propagator.isBanned(x, y, z, tile) && !propagator.isBanned(x2, y, z, tile2))
                            {
                                propagator.ban(x2, y, z, tile2);
                            }
                        }
                    }
                }
            }
        }
    }
}
