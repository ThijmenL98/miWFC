using System.Collections.Generic;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Constraints
{
    /// <summary>
    /// This constriant forces particular tiles to not be placed near each other.
    /// It's useful for giving a more even distribution of tiles, similar to a Poisson disk sampling.
    /// </summary>
    public class SeparationConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileset;
        private SelectedChangeTracker changeTracker;
        private NearbyTracker nearbyTracker;

        /// <summary>
        /// Set of tiles, all of which should be separated from each other.
        /// </summary>
        public ISet<Tile> Tiles { get; set; }

        /// <summary>
        /// The minimum distance between two points.
        /// Measured using manhattan distance.
        /// </summary>
        public int MinDistance { get; set; }


        public void init(TilePropagator propagator)
        {
            tileset = propagator.createTileSet(Tiles);
            nearbyTracker = new NearbyTracker { minDistance = MinDistance, topology = propagator.Topology };
            changeTracker = propagator.createSelectedChangeTracker(tileset, nearbyTracker);

            // Review the initial state
            foreach(int index in propagator.Topology.getIndices())
            {
                if (changeTracker.getTristate(index).isYes())
                {
                    nearbyTracker.visitNearby(index, false);
                }
            }

            check(propagator);
        }

        public void check(TilePropagator propagator)
        {
            if (nearbyTracker.newlyVisited.Count == 0) {
                return;
            }

            ISet<int> newlyVisited = nearbyTracker.newlyVisited;
            nearbyTracker.newlyVisited = new HashSet<int>();

            foreach (int index in newlyVisited)
            {
                propagator.Topology.getCoord(index, out int x, out int y, out int z);
                propagator.ban(x, y, z, tileset);
            }
        }


        private class NearbyTracker : ITristateChanged
        {
            public ITopology topology;

            public readonly ISet<int> visited = new HashSet<int>();
            public ISet<int> newlyVisited = new HashSet<int>();

            public int minDistance;

            public void visitNearby(int index, bool undo)
            {
                // Dijkstra's with fixed weights is just a queue
                Queue<(int, int)> queue = new Queue<(int, int)>();
                queue.Enqueue((index, 0));

                while (queue.Count > 0)
                {
                    (int i, int dist) = queue.Dequeue();
                    if (dist < minDistance - 1)
                    {
                        for (int dir = 0; dir < topology.DirectionsCount; dir++)
                        {
                            if (topology.tryMove(i, (Direction)dir, out int i2))
                            {
                                if (!visited.Contains(i2))
                                {
                                    queue.Enqueue((i2, dist + 1));
                                }
                            }
                        }
                    }

                    if (undo)
                    {
                        visited.Remove(i);
                        newlyVisited.Remove(i);
                    }
                    else
                    {
                        visited.Add(i);
                        if (dist > 0)
                        {
                            newlyVisited.Add(i);
                        }

                    }
                }
            }

            public void reset(SelectedChangeTracker tracker)
            {
            }

            public void notify(int index, Tristate before, Tristate after)
            {
                if(after.isYes())
                {
                    visitNearby(index, false);
                }
                if(before.isYes())
                {
                    // Must be backtracking. 
                    // The main backtrack mechanism will handle undoing bans, and 
                    // undos are always in order, so we just need to reverse VisitNearby
                    visitNearby(index, true);
                }
            }
        }

    }
}
