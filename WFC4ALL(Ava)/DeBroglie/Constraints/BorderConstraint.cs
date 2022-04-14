using System;

namespace WFC4ALL.DeBroglie.Constraints; 

/// <summary>
///     Used by <see cref="BorderConstraint" /> to indicate what area affected.
/// </summary>
[Flags]
public enum BorderSides {
    NONE = 0,
    X_MIN = 0x01,
    X_MAX = 0x02,
    Y_MIN = 0x04,
    Y_MAX = 0x08,
    Z_MIN = 0x10,
    Z_MAX = 0x20,
    ALL = 0x3F
}

/// <summary>
///     BorderConstraint class restricts what tiles can be selected in various regions of the output.
///     For each affected location, BorderConstratin calls Select with the Tile specified.If the Ban field is set, then it
///     calls Ban instead of Select.
/// </summary>
public class BorderConstraint : ITileConstraint {
    /// <summary>
    ///     The tiles to select or ban fromthe  border area.
    /// </summary>
    public Tile[] Tiles { get; set; }

    /// <summary>
    ///     A set of flags specifying which sides of the output are affected by the constraint.
    /// </summary>
    public BorderSides Sides { get; set; } = BorderSides.ALL;

    /// <summary>
    ///     These locations are subtracted from the ones specified in <see cref="Sides" />. Defaults to empty.
    /// </summary>
    public BorderSides ExcludeSides { get; set; } = BorderSides.NONE;

    /// Inverts the area specified by
    /// <see cref="Sides" />
    /// and
    /// <see cref="ExcludeSides" />
    public bool InvertArea { get; set; }

    /// <summary>
    ///     If true, ban <see cref="Tile" /> from the area. Otherwise, select it (i.e. ban every other tile).
    /// </summary>
    public bool Ban { get; set; }

    public void Check(TilePropagator propagator) { }

    public void Init(TilePropagator propagator) {
        TilePropagatorTileSet tiles = propagator.CreateTileSet(Tiles);

        int width = propagator.Topology.Width;
        int height = propagator.Topology.Height;
        int depth = propagator.Topology.Depth;
        for (int x = 0; x < width; x++) {
            bool xmin = x == 0;
            bool xmax = x == width - 1;

            for (int y = 0; y < height; y++) {
                bool ymin = y == 0;
                bool ymax = y == height - 1;

                for (int z = 0; z < depth; z++) {
                    bool zmin = z == 0;
                    bool zmax = z == depth - 1;

                    bool match = (Match(Sides, xmin, xmax, ymin, ymax, zmin, zmax) &&
                        !Match(ExcludeSides, xmin, xmax, ymin, ymax, zmin, zmax)) != InvertArea;

                    if (match) {
                        if (Ban) {
                            propagator.ban(x, y, z, tiles);
                        } else {
                            propagator.@select(x, y, z, tiles);
                        }
                    }
                }
            }
        }
    }

    private bool Match(BorderSides sides, bool xmin, bool xmax, bool ymin, bool ymax, bool zmin, bool zmax) {
        return
            xmin && sides.HasFlag(BorderSides.X_MIN) ||
            xmax && sides.HasFlag(BorderSides.X_MAX) ||
            ymin && sides.HasFlag(BorderSides.Y_MIN) ||
            ymax && sides.HasFlag(BorderSides.Y_MAX) ||
            zmin && sides.HasFlag(BorderSides.Z_MIN) ||
            zmax && sides.HasFlag(BorderSides.Z_MAX);
    }
}