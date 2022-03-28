using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace WFC4ALL.Utils; 

public static class Util {

    private static readonly XDocument xDoc = XDocument.Load("./Assets/samples.xml");

    /*
     * Pattern Adaptation Simple
     */

    public static Color[] imTile(Func<int, int, Color> f, int tilesize) {
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

    public static Bitmap getImageFromURI(string name) {
        return new Bitmap($"samples/{name}.png");
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

    public static Color[][] imageToColourArray(Bitmap bmp, out HashSet<Color> currentColors) {
        int width = bmp.Width;
        int height = bmp.Height;
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        byte[] bytes = new byte[height * data.Stride];
        try {
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        } finally {
            bmp.UnlockBits(data);
        }

        Color[][] result = new Color[height][];
        currentColors = new HashSet<Color>();
        for (int y = 0; y < height; ++y) {
            result[y] = new Color[width];
            for (int x = 0; x < width; ++x) {
                int offset = y * data.Stride + x * 3;
                Color c = Color.FromArgb(255, bytes[offset + 2], bytes[offset + 1], bytes[offset + 0]);
                result[y][x] = c;
                currentColors.Add(c);
            }
        }

        return result;
    }

    public static string[] getCategories(string modelType) {
        return modelType.Equals("overlapping")
            ? new[] {"Textures", "Shapes", "Knots", "Fonts", "Worlds Side-View", "Worlds Top-Down"}
            : new[] {"Worlds Top-Down", "Textures"};
    }
}