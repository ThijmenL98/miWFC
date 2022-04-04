using System;
using WFC4All.DeBroglie.Rot;

namespace WFC4All.DeBroglie.Topo {
    public static class TopoArrayUtils {
        public static ValueTuple<int, int> rotateVector(DirectionSetType type, int x, int y, Rotation rotation) {
            if (type == DirectionSetType.CARTESIAN2D ||
                type == DirectionSetType.CARTESIAN3D) {
                return squareRotateVector(x, y, rotation);
            }

            if (type == DirectionSetType.HEXAGONAL2D) {
                return hexRotateVector(x, y, rotation);
            }

            throw new Exception($"Unknown directions type {type}");
        }

        private static ValueTuple<int, int> squareRotateVector(int x, int y, Rotation rotation) {
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

        private static ValueTuple<int, int> hexRotateVector(int x, int y, Rotation rotation) {
            int microRotate = rotation.RotateCw / 60 % 3;
            bool rotate180 = rotation.RotateCw / 60 % 2 == 1;
            return hexRotateVector(x, y, microRotate, rotate180, rotation.ReflectX);
        }

        private static ValueTuple<int, int> hexRotateVector(int x, int y, int microRotate, bool rotate180,
            bool reflectX) {
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

        public static Direction rotateDirection(DirectionSet directions, Direction direction, Rotation rotation) {
            int x = directions.Dx[(int) direction];
            int y = directions.Dy[(int) direction];
            int z = directions.Dz[(int) direction];

            (x, y) = rotateVector(directions.Type, x, y, rotation);

            return directions.getDirection(x, y, z);
        }


        public delegate bool TileRotate<T>(T tile, out T result);

        public static ITopoArray<Tile> rotate(ITopoArray<Tile> original, Rotation rotation,
            TileRotation? tileRotation = null) {
            GridTopology gridTopology = original.Topology.asGridTopology();
            DirectionSetType type = gridTopology.Directions.Type;
            if (type is DirectionSetType.CARTESIAN2D or DirectionSetType.CARTESIAN3D) {
                return squareRotate(original, rotation, tileRotation);
            }

            if (type == DirectionSetType.HEXAGONAL2D) {
                return hexRotate(original, rotation, tileRotation);
            }

            throw new Exception($"Unknown directions type {type}");
        }

        public static ITopoArray<T> rotate<T>(ITopoArray<T> original, Rotation rotation,
            TileRotate<T>? tileRotate = null) {
            GridTopology topology = original.Topology.asGridTopology();
            DirectionSetType type = topology.Directions.Type;
            if (type == DirectionSetType.CARTESIAN2D ||
                type == DirectionSetType.CARTESIAN3D) {
                return squareRotate(original, rotation, tileRotate);
            }

            if (type == DirectionSetType.HEXAGONAL2D) {
                return hexRotate(original, rotation, tileRotate);
            }

            throw new Exception($"Unknown directions type {type}");
        }


        private static ITopoArray<Tile> squareRotate(ITopoArray<Tile> original, Rotation rotation,
            TileRotation? tileRotation = null) {
            bool tileRotate(Tile tile, out Tile result) {
                return tileRotation.rotate(tile, rotation, out result);
            }

            return squareRotate(original, rotation, tileRotation == null ? null : (TileRotate<Tile>) tileRotate);
        }

        private static ITopoArray<T> squareRotate<T>(ITopoArray<T> original, Rotation rotation,
            TileRotate<T>? tileRotate = null) {
            if (rotation.IsIdentity) {
                return original;
            }

            ValueTuple<int, int> mapCoord(int x, int y) {
                return squareRotateVector(x, y, rotation);
            }

            return rotateInner(original, mapCoord, tileRotate);
        }

        private static ITopoArray<Tile> hexRotate(ITopoArray<Tile> original, Rotation rotation, object tileRotation1,
            TileRotation? tileRotation = null) {
            bool tileRotate(Tile tile, out Tile result) {
                return tileRotation.rotate(tile, rotation, out result);
            }

            return hexRotate(original, rotation, (tileRotation == null ? null : tileRotate)!);
        }

        private static ITopoArray<T> hexRotate<T>(ITopoArray<T> original, Rotation rotation,
            TileRotate<T>? tileRotate = null) {
            if (rotation.IsIdentity) {
                return original;
            }

            int microRotate = rotation.RotateCw / 60 % 3;
            bool rotate180 = rotation.RotateCw / 60 % 2 == 1;

            // Actually do a reflection/rotation
            ValueTuple<int, int> mapCoord(int x, int y) {
                return hexRotateVector(x, y, microRotate, rotate180, rotation.ReflectX);
            }

            return rotateInner(original, mapCoord, tileRotate);
        }


        private static ITopoArray<T> rotateInner<T>(ITopoArray<T> original,
            Func<int, int, ValueTuple<int, int>> mapCoord, TileRotate<T>? tileRotate = null) {
            GridTopology originalTopology = original.Topology.asGridTopology();

            // Find new bounds
            (int x1, int y1) = mapCoord(0, 0);
            (int x2, int y2) = mapCoord(originalTopology.Width - 1, 0);
            (int x3, int y3) = mapCoord(originalTopology.Width - 1, originalTopology.Height - 1);
            (int x4, int y4) = mapCoord(0, originalTopology.Height - 1);

            int minX = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
            int maxX = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
            int minY = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
            int maxY = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

            // Arrange so that co-ordinate transfer is into the rect bounced by width, height
            int offSetX = -minX;
            int offSetY = -minY;
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            int depth = originalTopology.Depth;

            bool[] mask = new bool[width * height * depth];
            GridTopology topology = new(originalTopology.Directions, width, height, originalTopology.Depth, false,
                false, false, mask);
            T[,,] values = new T[width, height, depth];

            // Copy from original to values based on the rotation, setting up the mask as we go.
            for (int z = 0; z < originalTopology.Depth; z++) {
                for (int y = 0; y < originalTopology.Height; y++) {
                    for (int x = 0; x < originalTopology.Width; x++) {
                        (int newX, int newY) = mapCoord(x, y);
                        newX += offSetX;
                        newY += offSetY;
                        int newIndex = topology.getIndex(newX, newY, z);
                        T newValue = original.get(x, y, z);
                        bool hasNewValue = true;
                        if (tileRotate != null) {
                            hasNewValue = tileRotate(newValue, out newValue);
                        }

                        values[newX, newY, z] = newValue;
                        mask[newIndex] = hasNewValue &&
                                         originalTopology.containsIndex(originalTopology.getIndex(x, y, z));
                    }
                }
            }

            return new TopoArray3D<T>(values, topology);
        }
    }
}