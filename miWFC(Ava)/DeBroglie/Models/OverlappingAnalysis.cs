using System;
using System.Collections.Generic;
using miWFC.DeBroglie.Rot;
using miWFC.DeBroglie.Topo;

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
        List<double> frequencies) {
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