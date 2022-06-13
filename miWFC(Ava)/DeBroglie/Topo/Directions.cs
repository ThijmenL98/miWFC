using System;
using System.Collections;
using System.Collections.Generic;

namespace miWFC.DeBroglie.Topo;

public enum Axis {
    X,
    Y,
    Z,

    // The "third" axis used for DirectionSet.Hexagonal2d 
    // it's redundant with X and Y, but still useful to refer to.
    W
}

public enum Direction {
    X_PLUS = 0,
    X_MINUS = 1,
    Y_PLUS = 2,
    Y_MINUS = 3,
    Z_PLUS = 4,
    Z_MINUS = 5,

    // Shared with Z, there's no DirectionSet that uses both.
    W_PLUS = 4,
    W_MINUS = 5
}

/// <summary>
///     DirectionType indicates what neighbors are considered adjacent to each tile.
/// </summary>
public enum DirectionSetType {
    UNKNOWN,
    CARTESIAN2D,
    HEXAGONAL2D,
    CARTESIAN3D,
    HEXAGONAL3D
}

public enum EdgeLabel { }

/// <summary>
///     Wrapper around DirectionsType supplying some convenience data.
/// </summary>
public struct DirectionSet : IEnumerable<Direction> {
    public int[] DX { get; private set; }
    public int[] DY { get; private set; }
    public int[] DZ { get; private set; }

    public int Count { get; private set; }

    public DirectionSetType Type { get; private set; }

    /// <summary>
    ///     The Directions associated with square grids.
    /// </summary>
    public static readonly DirectionSet cartesian2d = new() {
        DX = new[] {1, -1, 0, 0},
        DY = new[] {0, 0, 1, -1},
        DZ = new[] {0, 0, 0, 0},
        Count = 4,
        Type = DirectionSetType.CARTESIAN2D
    };

    /// <summary>
    ///     The Directions associated with hexagonal grids.
    ///     Conventially, x is treated as moving right, and y as moving down and left,
    ///     But the same Directions object will work just as well will several other conventions
    ///     as long as you are consistent.
    /// </summary>
    public static readonly DirectionSet hexagonal2d = new() {
        DX = new[] {1, -1, 0, 0, 1, -1},
        DY = new[] {0, 0, 1, -1, 1, -1},
        DZ = new[] {0, 0, 0, 0, 0, 0},
        Count = 6,
        Type = DirectionSetType.HEXAGONAL2D
    };

    /// <summary>
    ///     The Directions associated with grids of hexagon prisms.
    ///     x is right, and z as moving down and left, y is up (prism axis), and w is the same as one unity of x and z.
    ///     Note due to some stupid design descisions, you cannot use Direction.WPlus right now.
    /// </summary>
    public static readonly DirectionSet hexagonal3d = new() {
        //           X+  X-  Y+  Y-  Z+  Z-  W+  W-
        DX = new[] {1, -1, 0, 0, 0, 0, 1, -1},
        DY = new[] {0, 0, 1, -1, 0, 0, 0, 0},
        DZ = new[] {0, 0, 0, 0, 1, -1, 1, -1},
        Count = 8,
        Type = DirectionSetType.HEXAGONAL3D
    };

    /// <summary>
    ///     The Directions associated with cubic grids.
    /// </summary>
    public static readonly DirectionSet cartesian3d = new() {
        DX = new[] {1, -1, 0, 0, 0, 0},
        DY = new[] {0, 0, 1, -1, 0, 0},
        DZ = new[] {0, 0, 0, 0, 1, -1},
        Count = 6,
        Type = DirectionSetType.CARTESIAN3D
    };

    /// <summary>
    ///     Given a direction index, returns the direction index that makes the reverse movement.
    /// </summary>
    public Direction Inverse(Direction d) {
        return (Direction) ((int) d ^ 1);
    }

    public Direction GetDirection(int x, int y, int z = 0) {
        for (int d = 0; d < Count; d++) {
            if (x == DX[d] && y == DY[d] && z == DZ[d]) {
                return (Direction) d;
            }
        }

        throw new Exception($"No direction corresponds to ({x}, {y}, {z})");
    }

    public IEnumerator<Direction> GetEnumerator() {
        for (int d = 0; d < Count; d++) {
            yield return (Direction) d;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}