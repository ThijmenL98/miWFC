using System.Collections.Generic;
using WFC4All.DeBroglie.Topo;

namespace WFC4All.DeBroglie.Constraints
{
    public class FixedTileConstraint : ITileConstraint
    {
        public Tile[] Tiles { get; set; }

        public Point? Point { get; set; }

        public void check(TilePropagator propagator)
        {
        }

        public void init(TilePropagator propagator)
        {
            TilePropagatorTileSet tileSet = propagator.createTileSet(Tiles);

            Point point = Point ?? getRandomPoint(propagator, tileSet);

            propagator.@select(point.x, point.y, point.z, tileSet);
        }

        public Point getRandomPoint(TilePropagator propagator, TilePropagatorTileSet tileSet)
        {
            ITopology topology = propagator.Topology;

            List<Point> points = new List<Point>();
            for (int z = 0; z < topology.Depth; z++)
            {
                for (int y = 0; y < topology.Height; y++)
                {
                    for (int x = 0; x < topology.Width; x++)
                    {
                        if (topology.Mask != null)
                        {
                            int index = topology.getIndex(x, y, z);
                            if (!topology.Mask[index]) {
                                continue;
                            }
                        }

                        propagator.getBannedSelected(x, y, z, tileSet, out bool isBanned, out bool _);
                        if (isBanned) {
                            continue;
                        }

                        points.Add(new Point(x, y, z));
                    }
                }
            }

            // Choose a random point to select
            if (points.Count == 0) {
                throw new System.Exception($"No legal placement of {tileSet}");
            }

            int i = (int)(propagator.RandomDouble() * points.Count);

            return points[i];

        }
    }
}
