using System.Collections.Generic;
using WFC4ALL.DeBroglie.Rot;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Models; 

/// <summary>
///     Base class for the models used in generation.
/// </summary>
// A TileModel is a model with a well defined mapping from 
// "tiles" (arbitrary identifiers of distinct tiles)
// with patterns (dense integers that correspond to particular
// arrangements of tiles).
public abstract class TileModel {
    public abstract IEnumerable<Tile> Tiles { get; }

    /// <summary>
    ///     Extracts the actual model of patterns used.
    /// </summary>
    internal abstract TileModelMapping GetTileModelMapping(ITopology topology);

    /// <summary>
    ///     Scales the the occurency frequency of a given tile by the given multiplier.
    /// </summary>
    public abstract void MultiplyFrequency(Tile tile, double multiplier);

    /// <summary>
    ///     Scales the the occurency frequency of a given tile by the given multiplier,
    ///     including other rotations of the tile.
    /// </summary>
    public virtual void MultiplyFrequency(Tile tile, double multiplier, TileRotation tileRotation) {
        HashSet<Tile> rotatedTiles = new();
        foreach (Rotation rotation in tileRotation.RotationGroup) {
            if (tileRotation.Rotate(tile, rotation, out Tile result)) {
                if (rotatedTiles.Add(result)) {
                    MultiplyFrequency(result, multiplier);
                }
            }
        }
    }
}