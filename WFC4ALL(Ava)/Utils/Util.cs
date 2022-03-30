using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace WFC4ALL.Utils;

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
    }

    // ReSharper disable PossibleMultipleEnumeration
    public static (int[], int) getImagePatternDimensions(string imageName) {
        if (xDoc.Root == null) {
            return (Array.Empty<int>(), -1);
        }

        IEnumerable<XElement> xElements
            = xDoc.Root.Elements("simpletiled").Concat(xDoc.Root.Elements("overlapping"));
        IEnumerable<int> matchingElements = xElements.Where(x =>
            (x.Attribute("name")?.Value ?? "") == imageName).Select(t =>
            int.Parse(t.Attribute("patternSize")?.Value ?? "3"));

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
                    currentColors[y] = c;
                }
            });
        }

        return (result, new HashSet<Color>(currentColors.Values));
    }

    public static string[] getCategories(string modelType) {
        return modelType.Equals("overlapping")
            ? new[] {"Textures", "Shapes", "Knots", "Fonts", "Worlds Side-View", "Worlds Top-Down"}
            : new[] {"Worlds Top-Down", "Textures"};
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
}