using System;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Constraints; 

public class MirrorYConstraint : SymmetryConstraint {
    private static readonly Rotation reflectY = new(180, true);

    public TileRotation TileRotation { get; set; }

    public override void Init(TilePropagator propagator) {
        if (TileRotation == null) {
            throw new ArgumentNullException(nameof(TileRotation));
        }

        DirectionSetType directionsType = propagator.Topology.AsGridTopology().Directions.Type;
        if (directionsType != DirectionSetType.CARTESIAN2D && directionsType != DirectionSetType.CARTESIAN3D) {
            throw new Exception($"MirrorYConstraint not supported on {directionsType}");
        }

        base.Init(propagator);
    }

    protected override bool TryMapIndex(TilePropagator propagator, int i, out int i2) {
        ITopology topology = propagator.Topology;
        topology.GetCoord(i, out int x, out int y, out int z);
        int y2 = topology.Height - 1 - y;
        i2 = topology.GetIndex(x, y2, z);
        return topology.ContainsIndex(i2);
    }

    protected override bool TryMapTile(Tile tile, out Tile tile2) {
        return TileRotation.Rotate(tile, reflectY, out tile2);
    }
}