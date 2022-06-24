using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace miWFC.Utils;

public static class Util {
    private static readonly XDocument xDoc = XDocument.Load(AppContext.BaseDirectory + "/Assets/samples.xml");

    /*
     * Pattern Adaptation Simple
     */

    private static Color[] imTile(Func<int, int, Color> f, int tilesize) {
        Color[] result = new Color[tilesize * tilesize];
        for (int y = 0; y < tilesize; y++) {
            for (int x = 0; x < tilesize; x++) {
                result[x + y * tilesize] = f(x, y);
            }
        }

        return result;
    }

    public static Color[] rotate(IReadOnlyList<Color> array, int tilesize) {
        return imTile((x, y) => array[tilesize - 1 - y + x * tilesize], tilesize);
    }

    public static Color[] reflect(IReadOnlyList<Color> array, int tilesize) {
        return imTile((x, y) => array[tilesize - 1 - x + y * tilesize], tilesize);
    }

    /*
     * Image Data Loading/Access
     */

    public static WriteableBitmap getImageFromURI(string name) {
        MemoryStream ms = new(File.ReadAllBytes($"{AppContext.BaseDirectory}/samples/{name}.png"));
        WriteableBitmap writeableBitmap = WriteableBitmap.Decode(ms);
        return writeableBitmap;
    } // ReSharper disable PossibleMultipleEnumeration
    public static (int[], int) getImagePatternDimensions(string imageName) {
        if (xDoc.Root == null) {
            return (Array.Empty<int>(), -1);
        }

        IEnumerable<XElement> xElements
            = xDoc.Root.Elements("simpletiled").Concat(xDoc.Root.Elements("overlapping"));
        IEnumerable<int> matchingElements = xElements.Where(x =>
            (x.Attribute("name")?.Value ?? "") == imageName).Select(t =>
            int.Parse(t.Attribute("patternSize")?.Value ?? "3", CultureInfo.InvariantCulture));

        List<int> patternDimensionsList = new();
        int j = 0;
        for (int i = 2; i < 6; i++) {
            if (i >= 4 && !matchingElements.Contains(5) && !matchingElements.Contains(4)) {
                break;
            }

            patternDimensionsList.Add(i);
            if (j == 0 && matchingElements.Contains(i)) {
                j = i;
            }
        }

        return (patternDimensionsList.ToArray(), j - 2);
    }

    public static string[] getModelImages(string modelType, string category) {
        List<string> images = new();
        if (xDoc.Root != null) {
            images = xDoc.Root.Elements(modelType)
                .Where(xElement => (xElement.Attribute("category")?.Value ?? "").Equals(category))
                .Select(xElement => xElement.Attribute("name")?.Value ?? "").ToList();
        }

        images.Sort();

        return images.Distinct().ToArray();
    }

    /*
     * Miscellaneous Util Functions
     */

    public static (Color[][], HashSet<Color>) imageToColourArray(WriteableBitmap bmp) {
        int width = (int) bmp.Size.Width;
        int height = (int) bmp.Size.Height;

        using ILockedFramebuffer? frameBuffer = bmp.Lock();

        Color[][] result = new Color[height][];
        ConcurrentDictionary<long, Color> currentColors = new();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, height, y => {
                uint* bytes = backBuffer + (int) y * stride / 4;
                result[y] = new Color[width];
                for (int x = 0; x < width; x++) {
                    Color c = Color.FromUInt32(bytes[x]);
                    result[y][x] = c;
                    currentColors[y * height + x] = c;
                }
            });
        }

        return (result, new HashSet<Color>(currentColors.Values.Distinct()));
    }

    public static string[] getCategories(string modelType) {
        return modelType.Equals("overlapping")
            ? new[] {"Textures", "Shapes", "Knots", "Fonts", "Worlds Side-View", "Worlds Top-Down"}
            : new[] {"Worlds Top-Down", "Textures"};
    }

    public static string getDescription(string category) {
        return category switch {
            "Textures" => "Surfaces of multi dimensional objects",
            "Shapes" => "Basic shapes and patterns",
            "Knots" => "Intertwined and/or tangled lines",
            "Fonts" => "Printable or displayable text characters",
            "Worlds Side-View" =>
                "Worlds as seen from the side (Seamless Output and Input Wrapping have been pre-set due to the nature of these images)",
            "Worlds Top-Down" => "(Game) Worlds as seen from above",
            _ => "???"
        };
    }

    public static Color[] extractColours(WriteableBitmap writeableBitmap) {
        (Color[][] colourArray, HashSet<Color> _) = imageToColourArray(writeableBitmap);
        return convert2DArrayTo1D(colourArray);
    }

    private static T[] convert2DArrayTo1D<T>(IEnumerable<T[]> array2D) {
        List<T> lst = new();
        foreach (T[] a in array2D) {
            lst.AddRange(a);
        }

        return lst.ToArray();
    }

    public static WriteableBitmap combineBitmaps(WriteableBitmap bottom, int tileSizeB, WriteableBitmap top,
        int tileSizeT, int imgInWidth, int imgInHeight) {
        int xDimension = imgInWidth * tileSizeB * tileSizeT;
        int yDimension = imgInHeight * tileSizeB * tileSizeT;

        WriteableBitmap outputBitmap
            = new(new PixelSize(imgInWidth * tileSizeB * tileSizeT, imgInHeight * tileSizeB * tileSizeT), Vector.One,
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();
        using ILockedFramebuffer? frameBufferBottom = bottom.Lock();
        using ILockedFramebuffer? frameBufferTop = top.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            uint* backBufferBottom = (uint*) frameBufferBottom.Address.ToPointer();
            uint* backBufferTop = (uint*) frameBufferTop.Address.ToPointer();

            int stride = frameBuffer.RowBytes;
            int strideBottom = frameBufferBottom.RowBytes;
            int strideTop = frameBufferTop.RowBytes;

            Parallel.For(0L, yDimension, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                uint* destBottom = backBufferBottom + (int) (y / tileSizeT) * strideBottom / 4;
                uint* destTop = backBufferTop + (int) (y / tileSizeB) * strideTop / 4;

                for (int x = 0; x < xDimension; x++) {
                    if (destTop[x / tileSizeB] >> 24 > 0) {
                        dest[x] = destTop[x / tileSizeB];
                    } else {
                        dest[x] = destBottom[x / tileSizeT];
                    }
                }
            });
        }

        return outputBitmap;
    }

    private static T[] Append<T>(T[] array, T item) {
        T[] result = new T[array.Length + 1];
        array.CopyTo(result, 0);
        result[array.Length] = item;
        return result;
    }

    public static async Task AppendPictureData(string fileName, int[,] appendData, bool mapData) {
        int[] output1D = new int[appendData.Length];
        Buffer.BlockCopy(appendData, 0, output1D, 0, appendData.Length * 4);
        await AppendPictureData(fileName, output1D, mapData);
    }

    public static async Task AppendPictureData(string fileName, IEnumerable<int> appendData, bool mapData) {
        byte[] data = await File.ReadAllBytesAsync(fileName);
        data = appendData.Aggregate(data, (current, val) => Append(current, (byte) (val + (mapData ? 2 : 0))));
        await File.WriteAllBytesAsync(fileName, data);
    }

    public static void Split<T>(T[] source, int index, out T[] first, out T[] last) {
        int len2 = source.Length - index;
        first = new T[index];
        last = new T[len2];
        Array.Copy(source, 0, first, 0, index);
        Array.Copy(source, index, last, 0, len2);
    }
    
    public static double RemapToByte(double value) {
        return Remap(value, 0d, 1d, 0d, 255d);
    }
    
    public static double RemapFromByte(double value) {
        return Remap(value, 0d, 255d, 0d, 1d);
    }
    
    private static double Remap(this double value, double from1, double to1, double from2, double to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}