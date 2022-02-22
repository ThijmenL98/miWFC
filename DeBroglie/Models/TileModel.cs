using System.Collections.Generic;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;

namespace WFC4All.DeBroglie.Models
{

    /// <summary>
    /// Base class for the models used in generation.
    /// </summary>
    // A TileModel is a model with a well defined mapping from 
    // "tiles" (arbitrary identifiers of distinct tiles)
    // with patterns (dense integers that correspond to particular
    // arrangements of tiles).
    public abstract class TileModel
    {
        /// <summary>
        /// Extracts the actual model of patterns used.
        /// </summary>
        internal abstract TileModelMapping getTileModelMapping(ITopology topology);

        public abstract IEnumerable<Tile> Tiles { get; }

        /// <summary>
        /// Scales the the occurency frequency of a given tile by the given multiplier.
        /// </summary>
        public abstract void multiplyFrequency(Tile tile, double multiplier);

        /// <summary>
        /// Scales the the occurency frequency of a given tile by the given multiplier, 
        /// including other rotations of the tile.
        /// </summary>
        public virtual void multiplyFrequency(Tile tile, double multiplier, TileRotation tileRotation)
        {
            HashSet<Tile> rotatedTiles = new HashSet<Tile>();
            foreach (Rotation rotation in tileRotation.RotationGroup)
            {
                if (tileRotation.rotate(tile, rotation, out Tile result))
                {
                    if(rotatedTiles.Add(result))
                    {
                        multiplyFrequency(result, multiplier);
                    }
                }
            }
        }
    }
}
