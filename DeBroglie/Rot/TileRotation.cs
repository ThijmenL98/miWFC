using System;
using System.Collections.Generic;
using System.Linq;

namespace WFC4All.DeBroglie.Rot
{

    /// <summary>
    /// Describes which rotations and reflections are allowed, and
    /// and stores how to process each tile during a rotation.
    /// These are constructed with a <see cref="TileRotationBuilder"/>
    /// </summary>
    public class TileRotation
    {
        private readonly IDictionary<Tile, IDictionary<Rotation, Tile>> rotations;
        private readonly IDictionary<Tile, TileRotationTreatment> treatments;
        private readonly TileRotationTreatment defaultTreatment;
        private readonly RotationGroup rotationGroup;

        // Used by TileRotationBuilder
        internal TileRotation(
            IDictionary<Tile, IDictionary<Rotation, Tile>> rotations,
            IDictionary<Tile, TileRotationTreatment> treatments,
            TileRotationTreatment defaultTreatment, 
            RotationGroup rotationGroup)
        {
            this.rotations = rotations;
            this.treatments = treatments;
            this.defaultTreatment = defaultTreatment;
            this.rotationGroup = rotationGroup;
        }

        /// <summary>
        /// Constructs a TileRotation that allows rotations and reflections as passed in,
        /// but leaves all tiles unchanged when rotating.
        /// <paramref name="rotationalSymmetry"></paramref>
        /// </summary>
        /// <param name="rotationalSymmetry">Permits rotations of 360 / rotationalSymmetry</param>
        /// <param name="reflectionalSymmetry">If true, reflections in the x-axis are permited</param>
        public TileRotation(int rotationalSymmetry, bool reflectionalSymmetry)
        {
            treatments = new Dictionary<Tile, TileRotationTreatment>();
            defaultTreatment = TileRotationTreatment.UNCHANGED;
            rotationGroup = new RotationGroup(rotationalSymmetry, reflectionalSymmetry);
        }

        /// <summary>
        /// A TileRotation that permits no rotation at all.
        /// </summary>
        public TileRotation():this(1, false)
        {

        }

        public RotationGroup RotationGroup => rotationGroup;

        /// <summary>
        /// Attempts to reflect, then rotate clockwise, a given Tile.
        /// If there is a corresponding tile (possibly the same one), then it is set to result.
        /// Otherwise, false is returned.
        /// </summary>
        public bool rotate(Tile tile, Rotation rotation, out Tile result)
        {
            if(rotationGroup != null && tile.Value is RotatedTile rt)
            {
                rotation = rt.Rotation * rotation;
                tile = rt.Tile;
            }

            if(rotations != null && rotations.TryGetValue(tile, out IDictionary<Rotation, Tile> d))
            {
                if (d.TryGetValue(rotation, out result)) {
                    return true;
                }
            }

            // Transform not found, apply treatment
            if (!treatments.TryGetValue(tile, out TileRotationTreatment treatment)) {
                treatment = defaultTreatment;
            }

            switch (treatment)
            {
                case TileRotationTreatment.MISSING:
                    result = default(Tile);
                    return false;
                case TileRotationTreatment.UNCHANGED:
                    result = tile;
                    return true;
                case TileRotationTreatment.GENERATED:
                    if (rotation.IsIdentity) {
                        result = tile;
                    } else {
                        result = new Tile(new RotatedTile { Rotation = rotation, Tile = tile });
                    }

                    return true;
                default:
                    throw new Exception($"Unknown treatment {treatment}");
            }
        }

        /// <summary>
        /// Convenience method for calling Rotate on each tile in a list, skipping any that cannot be rotated.
        /// </summary>
        public IEnumerable<Tile> rotate(IEnumerable<Tile> tiles, Rotation rotation)
        {
            foreach(Tile tile in tiles)
            {
                if(rotate(tile, rotation, out Tile tile2))
                {
                    yield return tile2;
                }
            }
        }

        public IEnumerable<Tile> rotateAll(Tile tile)
        {
            foreach (Rotation rotation in RotationGroup)
            {
                if (rotate(tile, rotation, out Tile tile2))
                {
                    yield return tile2;
                }
            }
        }

        public IEnumerable<Tile> rotateAll(IEnumerable<Tile> tiles)
        {
            return tiles.SelectMany(tile => rotateAll(tile));
        }

        /// <summary>
        /// For a rotated tile, finds the canonical representation.
        /// Leaves all other tiles unchanged.
        /// </summary>
        public Tile canonicalize(Tile t)
        {
            if(t.Value is RotatedTile rt)
            {
                if (!rotate(rt.Tile, rt.Rotation, out Tile result)) {
                    throw new Exception($"No tile corresponds to {t}");
                }

                return result;
            }
            else
            {
                return t;
            }
        }

    }
}
