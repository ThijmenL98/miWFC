using System;
using System.Collections.Generic;
using System.Linq;

namespace WFC4All.DeBroglie.Rot
{
    /// <summary>
    /// Builds a <see cref="TileRotation"/>.
    /// This class lets you specify some transformations between tiles via rotation and reflection.
    /// It then infers the full set of rotations possible, and informs you if there are contradictions.
    /// 
    /// As an example of inference, if a square tile 1 transforms to tile 2 when rotated clockwise, and tile 2 transforms to itself when reflected in the x-axis,
    /// then we can infer that tile 1 must transform to tile 1 when reflected in the y-axis.
    /// </summary>
    public class TileRotationBuilder
    {
        private readonly Dictionary<Tile, SubGroup> tileToSubGroup = new Dictionary<Tile, SubGroup>();

        private readonly RotationGroup rotationGroup;

        private readonly TileRotationTreatment defaultTreatment;

        public TileRotationBuilder(RotationGroup rotationGroup, TileRotationTreatment defaultTreatment = TileRotationTreatment.UNCHANGED)
        {
            this.rotationGroup = rotationGroup;
            this.defaultTreatment = defaultTreatment;
        }

        public TileRotationBuilder(int rotationalSymmetry, bool reflectionalSymmetry, TileRotationTreatment defaultTreatment = TileRotationTreatment.UNCHANGED)
        {
            rotationGroup = new RotationGroup(rotationalSymmetry, reflectionalSymmetry);
            this.defaultTreatment = defaultTreatment;
        }

        public RotationGroup RotationGroup => rotationGroup;

        /// <summary>
        /// Indicates that if you reflect then rotate clockwise the src tile as indicated, then you get the dest tile.
        /// </summary>
        public void add(Tile src, Rotation rotation, Tile dest)
        {
            rotationGroup.checkContains(rotation);
            getGroup(src, out SubGroup srcSg);
            getGroup(dest, out SubGroup destSg);
            // Groups need merging
            if(srcSg != destSg)
            {
                Rotation srcR = srcSg.getRotations(src)[0];
                Rotation destR = destSg.getRotations(dest)[0];

                // Arrange destRG so that it is relatively rotated
                // to srcRG as specified by r.
                destSg.permute(rot => destR.inverse() * srcR * rotation * rot);

                // Attempt to copy over tiles
                srcSg.Entries.AddRange(destSg.Entries);
                foreach (KeyValuePair<Rotation, Tile> kv in destSg.Tiles)
                {
                    set(srcSg, kv.Key, kv.Value, $"record rotation from {src} to {dest} by {rotation}");
                    tileToSubGroup[kv.Value] = srcSg;
                }
            }
            srcSg.Entries.Add(new Entry
            {
                Src = src,
                Rotation = rotation,
                Dest = dest,
            });
            expand(srcSg);
        }

        private bool set(SubGroup sg, Rotation rotation, Tile tile, string action)
        {
            if(sg.Tiles.TryGetValue(rotation, out Tile current))
            {
                if(current != tile)
                {
                    throw new Exception($"Cannot {action}: conflict between {current} and {tile}");
                }
                return false;
            }
            sg.Tiles[rotation] = tile;
            return true;
        }

        public void setTreatment(Tile tile, TileRotationTreatment treatment)
        {
            getGroup(tile, out SubGroup rg);
            if(rg.Treatment != null && rg.Treatment !=treatment)
            {
                throw new Exception($"Cannot set {tile} treatment, inconsistent with {rg.Treatment} of {rg.TreatmentSetBy}");
            }
            rg.Treatment = treatment;
            rg.TreatmentSetBy = tile;
        }

        /// <summary>
        /// Declares that a tile is symetric, and therefore transforms to iteself.
        /// This is a shorthand for calling Add(tile,..., tile) for specific rotations.
        /// </summary>
        public void addSymmetry(Tile tile, TileSymmetry ts)
        {
            // I've listed the subgroups in the order found here:
            // https://groupprops.subwiki.org/wiki/Subgroup_structure_of_dihedral_group:D8
            switch (ts)
            {
                case TileSymmetry.F:
                    getGroup(tile, out SubGroup _);
                    break;
                case TileSymmetry.N:
                    add(tile, new Rotation(2 * 90, false), tile);
                    break;

                case TileSymmetry.T:
                    add(tile, new Rotation(0 * 90, true), tile);
                    break;
                case TileSymmetry.L:
                    add(tile, new Rotation(1 * 90, true), tile);
                    break;
                case TileSymmetry.E:
                    add(tile, new Rotation(2 * 90, true), tile);
                    break;
                case TileSymmetry.Q:
                    add(tile, new Rotation(3 * 90, true), tile);
                    break;

                case TileSymmetry.I:
                    add(tile, new Rotation(0 * 90, true), tile);
                    add(tile, new Rotation(2 * 90, false), tile);
                    break;
                case TileSymmetry.SLASH:
                    add(tile, new Rotation(1 * 90, true), tile);
                    add(tile, new Rotation(2 * 90, false), tile);
                    break;

                case TileSymmetry.CYCLIC:
                    add(tile, new Rotation(1 * 90, false), tile);
                    break;

                case TileSymmetry.X:
                    add(tile, new Rotation(0 * 90, true), tile);
                    add(tile, new Rotation(1 * 90, false), tile);
                    break;
            }
        }

        /// <summary>
        /// Extracts the full set of rotations
        /// </summary>
        /// <returns></returns>
        public TileRotation build()
        {
            // For a given tile (found in a given rotation group)
            // Find the full set of tiles it rotates to.
            IDictionary<Rotation, Tile> getDict(Tile t, SubGroup sg)
            {
                TileRotationTreatment treatment = sg.Treatment ?? defaultTreatment;
                if(treatment == TileRotationTreatment.GENERATED)
                {
                    sg = clone(sg);
                    generate(sg);
                }
                Rotation r1 = sg.getRotations(t)[0];
                Dictionary<Rotation, Tile> result = new Dictionary<Rotation, Tile>();
                foreach(Rotation r2 in rotationGroup)
                {
                    if (!sg.Tiles.TryGetValue(r2, out Tile dest))
                    {
                        continue;
                    }
                    result[r1.inverse() * r2] = dest;
                }
                return result;
            }
            return new TileRotation(
                tileToSubGroup.ToDictionary(kv => kv.Key, kv => getDict(kv.Key, kv.Value)),
                tileToSubGroup.Where(kv=>kv.Value.Treatment.HasValue).ToDictionary(kv => kv.Key, kv => kv.Value.Treatment.Value),
                defaultTreatment,
                rotationGroup);
        }

        // Gets the rotation group containing Tile, creating it if it doesn't exist
        private void getGroup(Tile tile, out SubGroup sg)
        {
            if(tileToSubGroup.TryGetValue(tile, out sg))
            {
                return;
            }

            sg = new SubGroup();
            sg.Tiles[new Rotation()] = tile;
            tileToSubGroup[tile] = sg;
        }

        // Ensures that rg.Tiles is fully filled in
        // according to rg.Entries.
        private void expand(SubGroup sg)
        {
            bool expanded;
            do
            {
                expanded = false;
                foreach (Entry entry in sg.Entries)
                {
                    foreach (KeyValuePair<Rotation, Tile> kv in sg.Tiles.ToList())
                    {
                        if (kv.Value == entry.Src)
                        {
                            expanded = expanded || set(sg, kv.Key * entry.Rotation, entry.Dest, "resolve conflicting rotations");
                        }
                        if (kv.Value == entry.Dest)
                        {
                            expanded = expanded || set(sg, kv.Key * entry.Rotation.inverse(), entry.Src, "resolve conflicting rotations");
                        }
                    }
                }
            } while (expanded);
        }

        private SubGroup clone(SubGroup sg)
        {
            return new SubGroup
            {
                Entries = sg.Entries.ToList(),
                Tiles = sg.Tiles.ToDictionary(x => x.Key, x => x.Value),
                Treatment = sg.Treatment,
                TreatmentSetBy = sg.TreatmentSetBy,
            };
        }

        // Fills all remaining slots with RotatedTile
        // Care is taken that as few distinct RotatedTiles are used as possible
        // If there's two possible choices, prefernce is given to rotations over reflections.
        private void generate(SubGroup sg)
        {
            start:
            // This doesn't use rotationGroup.Rotations as the order is well defined;
            for (int refl = 0; refl < (rotationGroup.ReflectionalSymmetry ? 2 : 1); refl++)
            {
                for (int rot = 0; rot < 360; rot += rotationGroup.SmallestAngle)
                {
                    Rotation rotation = new Rotation(rot, refl > 0);
                    if (sg.Tiles.ContainsKey(rotation)) {
                        continue;
                    }

                    // Found an empty spot, figure out what to rotate from
                    for (int refl2 = 0; refl2 < (rotationGroup.ReflectionalSymmetry ? 2 : 1); refl2++)
                    {
                        for (int rot2 = 0; rot2 < 360; rot2 += rotationGroup.SmallestAngle)
                        {
                            Rotation rotation2 = new Rotation(rot2, refl2 > 0 != refl > 0);
                            if (!sg.Tiles.TryGetValue(rotation2, out Tile srcTile)) {
                                continue;
                            }

                            // Don't allow RotatedTiles to nest.
                            if(srcTile.Value is RotatedTile rt)
                            {
                                srcTile = rt.Tile;
                                Rotation rtRotation = rt.Rotation;
                                rotation2 = rtRotation.inverse() * rotation2;
                            }

                            Rotation srcToDest = rotation2.inverse() * rotation;

                            Tile destTile;
                            if (srcToDest.ReflectX == false && srcToDest.RotateCw == 0)
                            {
                                destTile = srcTile;
                            }
                            else
                            {
                                destTile = new Tile(new RotatedTile
                                {
                                    Tile = srcTile,
                                    Rotation = srcToDest,
                                });
                            }

                            // Found where we want to rotate from
                            sg.Entries.Add(new Entry
                            {
                                Src = srcTile,
                                Rotation = srcToDest,
                                Dest = destTile,
                            });
                            expand(sg);
                            goto start;
                        }
                    }
                }
            }
        }




        /// <summary>
        /// Stores a set of tiles related to each other by transformations.
        /// If we have two key value pairs (k1, v1) and (k2, v2) in Tiles, then 
        /// we can apply rortaion (k1.Inverse() * k2) to rotate v1 to v2.
        /// </summary>
        private class SubGroup
        {
            public List<Entry> Entries { get; set; } = new List<Entry>();
            public Dictionary<Rotation, Tile> Tiles { get; set; } = new Dictionary<Rotation, Tile>();
            public TileRotationTreatment? Treatment { get; set; }
            public Tile TreatmentSetBy { get; set; }


            // A tile may appear multiple times in a rotation group if it is symmetric in some way.
            public List<Rotation> getRotations(Tile tile)
            {
                return Tiles.Where(kv => kv.Value == tile).Select(x => x.Key).ToList();
            }
        
            public void permute(Func<Rotation, Rotation> f)
            {
                Tiles = Tiles.ToDictionary(kv => f(kv.Key), kv => kv.Value);
            }
        }

        private class Entry
        {
            public Tile Src { get; set; }
            public Rotation Rotation { get; set; }
            public Tile Dest { get; set; }
        }
    }
}
