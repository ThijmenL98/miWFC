using System.Collections.Generic;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Constraints
{
    /// <summary>
    /// The PathConstraint checks that it is possible to connect several locations together via a continuous path of adjacent tiles. 
    /// It does this by banning any tile placement that would make such a path impossible.
    /// </summary>
    public class PathConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileSet;

        private SelectedTracker selectedTracker;

        private TilePropagatorTileSet endPointTileSet;

        private SelectedTracker endPointSelectedTracker;

        private PathConstraintUtils.SimpleGraph graph;

        /// <summary>
        /// Set of patterns that are considered on the path
        /// </summary>
        public ISet<Tile> Tiles { get; set; }

        /// <summary>
        /// Set of points that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then PathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public Point[] EndPoints { get; set; }

        /// <summary>
        /// Set of tiles that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then PathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public ISet<Tile> EndPointTiles { get; set; }

        public PathConstraint(ISet<Tile> tiles, Point[] endPoints = null)
        {
            Tiles = tiles;
            EndPoints = endPoints;
        }

        public void init(TilePropagator propagator)
        {
            tileSet = propagator.createTileSet(Tiles);
            selectedTracker = propagator.createSelectedTracker(tileSet);
            endPointTileSet = EndPointTiles != null ? propagator.createTileSet(EndPointTiles) : null;
            endPointSelectedTracker = EndPointTiles != null ? propagator.createSelectedTracker(endPointTileSet) : null;
            graph = PathConstraintUtils.createGraph(propagator.Topology);
        }

        public void check(TilePropagator propagator)
        {
            ITopology topology = propagator.Topology;
            int indices = topology.Width * topology.Height * topology.Depth;
            // Initialize couldBePath and mustBePath based on wave possibilities
            bool[] couldBePath = new bool[indices];
            bool[] mustBePath = new bool[indices];
            for (int i = 0; i < indices; i++)
            {
                Tristate ts = selectedTracker.getTristate(i);
                couldBePath[i] = ts.possible();
                mustBePath[i] = ts.isYes();
            }

            // Select relevant cells, i.e. those that must be connected.
            bool[] relevant;
            if (EndPoints == null && EndPointTiles == null)
            {
                relevant = mustBePath;
            }
            else
            {
                relevant = new bool[indices];
                int relevantCount = 0;
                if (EndPoints != null)
                {
                    foreach (Point endPoint in EndPoints)
                    {
                        int index = topology.getIndex(endPoint.x, endPoint.y, endPoint.z);
                        relevant[index] = true;
                        relevantCount++;
                    }
                }
                if (EndPointTiles != null)
                {
                    for (int i = 0; i < indices; i++)
                    {
                        if (endPointSelectedTracker.isSelected(i))
                        {
                            relevant[i] = true;
                            relevantCount++;
                        }
                    }
                }
                if (relevantCount == 0)
                {
                    // Nothing to do.
                    return;
                }
            }
            bool[] walkable = couldBePath;

            bool[] component = EndPointTiles != null ? new bool[indices] : null;

            bool[] isArticulation = PathConstraintUtils.getArticulationPoints(graph, walkable, relevant, component);

            if (isArticulation == null)
            {
                propagator.setContradiction();
                return;
            }

            // All articulation points must be paths,
            // So ban any other possibilities
            for (int i = 0; i < indices; i++)
            {
                if (isArticulation[i] && !mustBePath[i])
                {
                    topology.getCoord(i, out int x, out int y, out int z);
                    propagator.@select(x, y, z, tileSet);
                }
            }

            // Any EndPointTiles not in the connected component aren't safe to add
            if (EndPointTiles != null)
            {
                for (int i = 0; i < indices; i++)
                {
                    if (!component[i])
                    {
                        topology.getCoord(i, out int x, out int y, out int z);
                        propagator.ban(x, y, z, endPointTileSet);
                    }
                }
            }
        }
    }
}
