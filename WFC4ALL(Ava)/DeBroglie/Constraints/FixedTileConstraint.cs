using System;
using System.Collections.Generic;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Constraints; 

public class FixedTileConstraint : ITileConstraint {
    public Tile[] Tiles { get; set; }

    public Point? Point { get; set; }

    public void Check(TilePropagator propagator) { }

    public void Init(TilePropagator propagator) {
        TilePropagatorTileSet tileSet = propagator.CreateTileSet(Tiles);

        Point point = Point ?? GetRandomPoint(propagator, tileSet);

        propagator.@select(point.X, point.Y, point.Z, tileSet);
    }

    public Point GetRandomPoint(TilePropagator propagator, TilePropagatorTileSet tileSet) {
        ITopology topology = propagator.Topology;

        List<Point> points = new();
        for (int z = 0; z < topology.Depth; z++) {
            for (int y = 0; y < topology.Height; y++) {
                for (int x = 0; x < topology.Width; x++) {
                    if (topology.Mask != null) {
                        int index = topology.GetIndex(x, y, z);
                        if (!topology.Mask[index]) {
                            continue;
                        }
                    }

                    propagator.GetBannedSelected(x, y, z, tileSet, out bool isBanned, out bool _);
                    if (isBanned) {
                        continue;
                    }

                    points.Add(new Point(x, y, z));
                }
            }
        }

        // Choose a random point to select
        if (points.Count == 0) {
            throw new Exception($"No legal placement of {tileSet}");
        }

        int i = (int) (propagator.RandomDouble() * points.Count);

        return points[i];
    }
}