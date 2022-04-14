﻿using System.Collections.Generic;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Trackers;

namespace WFC4ALL.DeBroglie.Constraints; 

/// <summary>
///     This constriant forces particular tiles to not be placed near each other.
///     It's useful for giving a more even distribution of tiles, similar to a Poisson disk sampling.
/// </summary>
public class SeparationConstraint : ITileConstraint {
    private SelectedChangeTracker changeTracker;
    private NearbyTracker nearbyTracker;
    private TilePropagatorTileSet tileset;

    /// <summary>
    ///     Set of tiles, all of which should be separated from each other.
    /// </summary>
    public ISet<Tile> Tiles { get; set; }

    /// <summary>
    ///     The minimum distance between two points.
    ///     Measured using manhattan distance.
    /// </summary>
    public int MinDistance { get; set; }


    public void Init(TilePropagator propagator) {
        tileset = propagator.CreateTileSet(Tiles);
        nearbyTracker = new NearbyTracker {MinDistance = MinDistance, Topology = propagator.Topology};
        changeTracker = propagator.CreateSelectedChangeTracker(tileset, nearbyTracker);

        // Review the initial state
        foreach (int index in propagator.Topology.GetIndices()) {
            if (changeTracker.GetQuadstate(index).IsYes()) {
                nearbyTracker.VisitNearby(index, false);
            }
        }

        Check(propagator);
    }

    public void Check(TilePropagator propagator) {
        if (nearbyTracker.NewlyVisited.Count == 0) {
            return;
        }

        ISet<int> newlyVisited = nearbyTracker.NewlyVisited;
        nearbyTracker.NewlyVisited = new HashSet<int>();

        foreach (int index in newlyVisited) {
            propagator.Topology.GetCoord(index, out int x, out int y, out int z);
            propagator.ban(x, y, z, tileset);
        }
    }


    internal class NearbyTracker : IQuadstateChanged {
        public int MinDistance;

        public ISet<int> NewlyVisited = new HashSet<int>();
        public ITopology Topology;

        public void Reset(SelectedChangeTracker tracker) { }

        public void Notify(int index, Quadstate before, Quadstate after) {
            bool a = after == Quadstate.YES || after == Quadstate.CONTRADICTION;
            bool b = before == Quadstate.YES || before == Quadstate.CONTRADICTION;
            if (a && !b) {
                VisitNearby(index, false);
            }

            if (b && !a) {
                // Must be backtracking. 
                // The main backtrack mechanism will handle undoing bans, and 
                // undos are always in order, so we just need to reverse VisitNearby
                VisitNearby(index, true);
            }
        }

        public void VisitNearby(int index, bool undo) {
            // Dijkstra's with fixed weights is just a queue
            Queue<(int, int)> queue = new();
            HashSet<int> visited = new();

            void visit(int i2, int dist) {
                if (visited.Add(i2)) {
                    queue.Enqueue((i2, dist));

                    if (undo) {
                        NewlyVisited.Remove(i2);
                    } else {
                        if (dist != 0) {
                            NewlyVisited.Add(i2);
                        }
                    }
                }
            }

            visit(index, 0);

            while (queue.Count > 0) {
                (int i, int dist) = queue.Dequeue();
                if (dist < MinDistance - 1) {
                    for (int dir = 0; dir < Topology.DirectionsCount; dir++) {
                        if (Topology.TryMove(i, (Direction) dir, out int i2)) {
                            visit(i2, dist + 1);
                        }
                    }
                }
            }
        }
    }
}