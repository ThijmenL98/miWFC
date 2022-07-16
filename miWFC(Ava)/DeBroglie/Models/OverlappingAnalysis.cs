using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using miWFC.DeBroglie.Rot;
using miWFC.DeBroglie.Topo;
using miWFC.Utils;

namespace miWFC.DeBroglie.Models;

/// <summary>
///     Contains utilities relevant to <see cref="OverlappingModel" />
/// </summary>
internal static class OverlappingAnalysis {
    public static IEnumerable<ITopoArray<Tile>> GetRotatedSamples(
        ITopoArray<Tile> sample,
        TileRotation tileRotation = null) {
        tileRotation = tileRotation ?? new TileRotation();

        foreach (Rotation rotation in tileRotation.RotationGroup) {
            yield return TopoArrayUtils.Rotate(sample, rotation, tileRotation);
        }
    }

    public static void GetPatterns(
        ITopoArray<Tile> sample,
        int nx,
        int ny,
        int nz,
        bool periodicX,
        bool periodicY,
        bool periodicZ,
        Dictionary<PatternArray, int> patternIndices,
        List<PatternArray> patternArrays,
        List<double> frequencies,
        List<Color[]> disabledPatterns) {
        int width = sample.Topology.Width;
        int height = sample.Topology.Height;
        int depth = sample.Topology.Depth;
        int maxx = periodicX ? width - 1 : width - nx;
        int maxy = periodicY ? height - 1 : height - ny;
        int maxz = periodicZ ? depth - 1 : depth - nz;

        for (int x = 0; x <= maxx; x++) {
            for (int y = 0; y <= maxy; y++) {
                for (int z = 0; z <= maxz; z++) {
                    PatternArray patternArray;
                    if (!TryExtract(sample, nx, ny, nz, x, y, z, out patternArray)) {
                        continue;
                    }

                    bool canContinue = true;
                    if (disabledPatterns.Count > 0) {
                        foreach (Color[] t in disabledPatterns) {
                            bool[] allSimilar = Enumerable.Repeat(true, 8).ToArray();
                            for (int xx = 0; xx < patternArray.Values.GetLength(0); xx++) {
                                for (int yy = 0; yy < patternArray.Values.GetLength(1); yy++) {
                                    allSimilar[0] = allSimilar[0] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(t[xx * patternArray.Values.GetLength(0) + yy]);
                                    allSimilar[1] = allSimilar[1] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(Util.Rotate(t, patternArray.Values.GetLength(0))[
                                            xx * patternArray.Values.GetLength(0) + yy]);
                                    allSimilar[2] = allSimilar[2] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(Util.Rotate(
                                            Util.Rotate(t, patternArray.Values.GetLength(0)),
                                            patternArray.Values.GetLength(0))[
                                            xx * patternArray.Values.GetLength(0) + yy]);
                                    allSimilar[3] = allSimilar[3] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(Util.Rotate(
                                            Util.Rotate(
                                                Util.Rotate(t, patternArray.Values.GetLength(0)),
                                                patternArray.Values.GetLength(0)), patternArray.Values.GetLength(0))[
                                            xx * patternArray.Values.GetLength(0) + yy]);

                                    allSimilar[4] = allSimilar[4] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(Util.Reflect(t, patternArray.Values.GetLength(0))[
                                            xx * patternArray.Values.GetLength(0) + yy]);
                                    allSimilar[5] = allSimilar[5] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(Util.Reflect(
                                            Util.Rotate(t, patternArray.Values.GetLength(0)),
                                            patternArray.Values.GetLength(0))[
                                            xx * patternArray.Values.GetLength(0) + yy]);
                                    allSimilar[6] = allSimilar[6] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(Util.Reflect(
                                            Util.Rotate(
                                                Util.Rotate(t, patternArray.Values.GetLength(0)),
                                                patternArray.Values.GetLength(0)), patternArray.Values.GetLength(0))[
                                            xx * patternArray.Values.GetLength(0) + yy]);
                                    allSimilar[7] = allSimilar[7] && patternArray.Values[xx, yy, 0].Value
                                        .Equals(Util.Reflect(
                                            Util.Rotate(Util.Rotate(
                                                Util.Rotate(t, patternArray.Values.GetLength(0)),
                                                patternArray.Values.GetLength(0)), patternArray.Values.GetLength(0)),
                                            patternArray.Values.GetLength(0))[
                                            xx * patternArray.Values.GetLength(0) + yy]);
                                }
                            }

                            if (allSimilar.Contains(true)) {
                                canContinue = false;
                                break;
                            }
                        }
                    }

                    if (!canContinue) {
                        continue;
                    }

                    int pattern;
                    if (!patternIndices.TryGetValue(patternArray, out pattern)) {
                        pattern = patternIndices[patternArray] = patternIndices.Count;
                        patternArrays.Add(patternArray);
                        frequencies.Add(1);
                    } else {
                        frequencies[pattern] += 1;
                    }
                }
            }
        }
    }

    public static PatternArray PatternEdge(PatternArray patternArray, int dx, int dy, int dz) {
        PatternArray a = patternArray;
        int edgeWidth = a.Width - Math.Abs(dx);
        int ix = Math.Max(0, dx);
        int edgeHeight = a.Height - Math.Abs(dy);
        int iy = Math.Max(0, dy);
        int edgeDepth = a.Depth - Math.Abs(dz);
        int iz = Math.Max(0, dz);
        PatternArray edge = new() {
            Values = new Tile[edgeWidth, edgeHeight, edgeDepth]
        };
        for (int x = 0; x < edgeWidth; x++) {
            for (int y = 0; y < edgeHeight; y++) {
                for (int z = 0; z < edgeDepth; z++) {
                    edge.Values[x, y, z] = patternArray.Values[x + ix, y + iy, z + iz];
                }
            }
        }

        return edge;
    }

    private static bool TryExtract(ITopoArray<Tile> sample, int nx, int ny, int nz, int x, int y, int z,
        out PatternArray pattern) {
        int width = sample.Topology.Width;
        int height = sample.Topology.Height;
        int depth = sample.Topology.Depth;
        Tile[,,] values = new Tile[nx, ny, nz];
        for (int tx = 0; tx < nx; tx++) {
            int sx = (x + tx) % width;
            for (int ty = 0; ty < ny; ty++) {
                int sy = (y + ty) % height;
                for (int tz = 0; tz < nz; tz++) {
                    int sz = (z + tz) % depth;
                    int index = sample.Topology.GetIndex(sx, sy, sz);
                    if (!sample.Topology.ContainsIndex(index)) {
                        pattern = default;
                        return false;
                    }

                    values[tx, ty, tz] = sample.get(sx, sy, sz);
                }
            }
        }

        pattern = new PatternArray {Values = values};
        return true;
    }
}

internal class PatternArrayComparer : IEqualityComparer<PatternArray> {
    public bool Equals(PatternArray a, PatternArray b) {
        int width = a.Width;
        int height = a.Height;
        int depth = a.Depth;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                for (int z = 0; z < depth; z++) {
                    if (a.Values[x, y, z] != b.Values[x, y, z]) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public int GetHashCode(PatternArray obj) {
        unchecked {
            int width = obj.Width;
            int height = obj.Height;
            int depth = obj.Depth;

            int hashCode = 13;
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    for (int z = 0; z < depth; z++) {
                        hashCode = (hashCode * 397) ^ obj.Values[x, y, z].GetHashCode();
                    }
                }
            }

            return hashCode;
        }
    }
}