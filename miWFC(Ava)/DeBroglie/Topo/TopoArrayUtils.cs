using System;
using miWFC.DeBroglie.Rot;

namespace miWFC.DeBroglie.Topo;

public static class TopoArrayUtils {
    public delegate bool TileRotate<T>(T tile, out T result);

    public static ValueTuple<int, int> RotateVector(DirectionSetType type, int x, int y, Rotation rotation) {
        if (type == DirectionSetType.CARTESIAN2D ||
            type == DirectionSetType.CARTESIAN3D) {
            return SquareRotateVector(x, y, rotation);
        }

        if (type == DirectionSetType.HEXAGONAL2D) {
            return HexRotateVector(x, y, rotation);
        }

        throw new Exception($"Unknown directions type {type}");
    }

    public static ValueTuple<int, int> SquareRotateVector(int x, int y, Rotation rotation) {
        if (rotation.ReflectX) {
            x = -x;
        }

        switch (rotation.RotateCw) {
            case 0 * 90:
                return (x, y);
            case 1 * 90:
                return (-y, x);
            case 2 * 90:
                return (-x, -y);
            case 3 * 90:
                return (y, -x);
            default:
                throw new Exception($"Unexpected angle {rotation.RotateCw}");
        }
    }

    public static ValueTuple<int, int> HexRotateVector(int x, int y, Rotation rotation) {
        int microRotate = rotation.RotateCw / 60 % 3;
        bool rotate180 = rotation.RotateCw / 60 % 2 == 1;
        return HexRotateVector(x, y, microRotate, rotate180, rotation.ReflectX);
    }

    private static ValueTuple<int, int> HexRotateVector(int x, int y, int microRotate, bool rotate180, bool reflectX) {
        if (reflectX) {
            x = -x + y;
        }

        int q = x - y;
        int r = -x;
        int s = y;
        int q2 = q;
        switch (microRotate) {
            case 0:
                break;
            case 1:
                q = s;
                s = r;
                r = q2;
                break;
            case 2:
                q = r;
                r = s;
                s = q2;
                break;
        }

        if (rotate180) {
            q = -q;
            r = -r;
            s = -s;
        }

        x = -r;
        y = s;
        return (x, y);
    }

    public static Direction RotateDirection(DirectionSet directions, Direction direction, Rotation rotation) {
        int x = directions.DX[(int) direction];
        int y = directions.DY[(int) direction];
        int z = directions.DZ[(int) direction];

        (x, y) = RotateVector(directions.Type, x, y, rotation);

        return directions.GetDirection(x, y, z);
    }

    public static ITopoArray<Tile> Rotate(ITopoArray<Tile> original, Rotation rotation,
        TileRotation tileRotation = null) {
        GridTopology gridTopology = original.Topology.AsGridTopology();
        DirectionSetType type = gridTopology.Directions.Type;
        if (type == DirectionSetType.CARTESIAN2D ||
            type == DirectionSetType.CARTESIAN3D) {
            return SquareRotate(original, rotation, tileRotation);
        }

        if (type == DirectionSetType.HEXAGONAL2D) {
            return HexRotate(original, rotation, tileRotation);
        }

        throw new Exception($"Unknown directions type {type}");
    }

    public static ITopoArray<T> Rotate<T>(ITopoArray<T> original, Rotation rotation, TileRotate<T> tileRotate = null) {
        GridTopology topology = original.Topology.AsGridTopology();
        DirectionSetType type = topology.Directions.Type;
        if (type == DirectionSetType.CARTESIAN2D ||
            type == DirectionSetType.CARTESIAN3D) {
            return SquareRotate(original, rotation, tileRotate);
        }

        if (type == DirectionSetType.HEXAGONAL2D) {
            return HexRotate(original, rotation, tileRotate);
        }

        throw new Exception($"Unknown directions type {type}");
    }


    public static ITopoArray<Tile> SquareRotate(ITopoArray<Tile> original, Rotation rotation,
        TileRotation? tileRotation = null) {
        bool tileRotate(Tile tile, out Tile result) {
            return tileRotation.Rotate(tile, rotation, out result);
        }

        return SquareRotate<Tile>(original, rotation, tileRotation == null ? null : tileRotate);
    }

    public static ITopoArray<T> SquareRotate<T>(ITopoArray<T> original, Rotation rotation,
        TileRotate<T> tileRotate = null) {
        if (rotation.IsIdentity) {
            return original;
        }

        ValueTuple<int, int> mapCoord(int x, int y) {
            return SquareRotateVector(x, y, rotation);
        }

        return RotateInner(original, mapCoord, tileRotate);
    }

    public static ITopoArray<Tile> HexRotate(ITopoArray<Tile> original, Rotation rotation,
        TileRotation? tileRotation = null) {
        bool tileRotate(Tile tile, out Tile result) {
            return tileRotation.Rotate(tile, rotation, out result);
        }

        return HexRotate<Tile>(original, rotation, tileRotation == null ? null : tileRotate);
    }

    public static ITopoArray<T> HexRotate<T>(ITopoArray<T> original, Rotation rotation,
        TileRotate<T> tileRotate = null) {
        if (rotation.IsIdentity) {
            return original;
        }

        int microRotate = rotation.RotateCw / 60 % 3;
        bool rotate180 = rotation.RotateCw / 60 % 2 == 1;

        // Actually do a reflection/rotation
        ValueTuple<int, int> mapCoord(int x, int y) {
            return HexRotateVector(x, y, microRotate, rotate180, rotation.ReflectX);
        }

        return RotateInner(original, mapCoord, tileRotate);
    }


    private static ITopoArray<T> RotateInner<T>(ITopoArray<T> original, Func<int, int, ValueTuple<int, int>> mapCoord,
        TileRotate<T> tileRotate = null) {
        GridTopology originalTopology = original.Topology.AsGridTopology();

        // Find new bounds
        (int x1, int y1) = mapCoord(0, 0);
        (int x2, int y2) = mapCoord(originalTopology.Width - 1, 0);
        (int x3, int y3) = mapCoord(originalTopology.Width - 1, originalTopology.Height - 1);
        (int x4, int y4) = mapCoord(0, originalTopology.Height - 1);

        int minx = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
        int maxx = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
        int miny = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
        int maxy = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

        // Arrange so that co-ordinate transfer is into the rect bounced by width, height
        int offsetx = -minx;
        int offsety = -miny;
        int width = maxx - minx + 1;
        int height = maxy - miny + 1;
        int depth = originalTopology.Depth;

        bool[] mask = new bool[width * height * depth];
        GridTopology topology = new(originalTopology.Directions, width, height, originalTopology.Depth, false, false,
            false, mask);
        T[,,] values = new T[width, height, depth];

        // Copy from original to values based on the rotation, setting up the mask as we go.
        for (int z = 0; z < originalTopology.Depth; z++) {
            for (int y = 0; y < originalTopology.Height; y++) {
                for (int x = 0; x < originalTopology.Width; x++) {
                    (int newX, int newY) = mapCoord(x, y);
                    newX += offsetx;
                    newY += offsety;
                    int newIndex = topology.GetIndex(newX, newY, z);
                    T? newValue = original.get(x, y, z);
                    bool hasNewValue = true;
                    if (tileRotate != null) {
                        hasNewValue = tileRotate(newValue, out newValue);
                    }

                    values[newX, newY, z] = newValue;
                    mask[newIndex] = hasNewValue && originalTopology.ContainsIndex(originalTopology.GetIndex(x, y, z));
                }
            }
        }

        return new TopoArray3D<T>(values, topology);
    }
}