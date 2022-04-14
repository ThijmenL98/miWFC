using System;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Constraints; 

/// <summary>
///     Maintain
/// </summary>
public class MirrorXConstraint : SymmetryConstraint {
    private static readonly Rotation reflectX = new(0, true);

    public TileRotation TileRotation { get; set; }

    public override void Init(TilePropagator propagator) {
        if (TileRotation == null) {
            throw new ArgumentNullException(nameof(TileRotation));
        }

        propagator.Topology.AsGridTopology();
        base.Init(propagator);
    }

    protected override bool TryMapIndex(TilePropagator propagator, int i, out int i2) {
        ITopology topology = propagator.Topology;
        topology.GetCoord(i, out int x, out int y, out int z);
        int x2 = topology.Width - 1 - x;
        i2 = topology.GetIndex(x2, y, z);
        return topology.ContainsIndex(i2);
    }

    protected override bool TryMapTile(Tile tile, out Tile tile2) {
        return TileRotation.Rotate(tile, reflectX, out tile2);
    }
}