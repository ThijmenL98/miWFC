using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;

namespace WFC4All.DeBroglie.Constraints
{
    /// <summary>
    /// Maintain
    /// </summary>
    public class MirrorXConstraint : SymmetryConstraint
    {
        private readonly static Rotation reflectX = new(0, true);

        public TileRotation TileRotation { get; set; }

        public override void init(TilePropagator propagator)
        {
            propagator.Topology.asGridTopology();
            base.init(propagator);
        }

        protected override bool tryMapIndex(TilePropagator propagator, int i, out int i2)
        {
            ITopology topology = propagator.Topology;
            topology.getCoord(i, out int x, out int y, out int z);
            int x2 = topology.Width - 1 - x;
            i2 = topology.getIndex(x2, y, z);
            return topology.containsIndex(i2);
        }

        protected override bool tryMapTile(Tile tile, out Tile tile2)
        {
            return TileRotation.rotate(tile, reflectX, out tile2);
        }
    }
}
