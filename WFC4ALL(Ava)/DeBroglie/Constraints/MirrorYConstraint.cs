using System;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;

namespace WFC4All.DeBroglie.Constraints
{
    public class MirrorYConstraint : SymmetryConstraint
    {
        private readonly static Rotation reflectY = new(180, true);

        public TileRotation TileRotation { get; set; }

        public override void init(TilePropagator propagator)
        {
            DirectionSetType directionsType = propagator.Topology.asGridTopology().Directions.Type;
            if (directionsType != DirectionSetType.CARTESIAN2D && directionsType != DirectionSetType.CARTESIAN3D)
            {
                throw new Exception($"MirrorYConstraint not supported on {directionsType}");
            }
            base.init(propagator);
        }

        protected override bool tryMapIndex(TilePropagator propagator, int i, out int i2)
        {
            ITopology topology = propagator.Topology;
            topology.getCoord(i, out int x, out int y, out int z);
            int y2 = topology.Height - 1 - y;
            i2 = topology.getIndex(x, y2, z);
            return topology.containsIndex(i2);
        }

        protected override bool tryMapTile(Tile tile, out Tile tile2)
        {
            return TileRotation.rotate(tile, reflectY, out tile2);
        }
    }
}
