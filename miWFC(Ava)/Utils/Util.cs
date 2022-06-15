using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace miWFC.Utils;

public static class Util {
    private static readonly XDocument xDoc = XDocument.Load(AppContext.BaseDirectory + "/Assets/samples.xml");

    private const int Dimension = 17;
    private const double Radius = 6d;

    public static int getDimension() {
        return Dimension;
    }

    private static WriteableBitmap latestItemBitmap
        = new(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    private static HashSet<Tuple<int, int>> getBorder() {
        HashSet<Tuple<int, int>> border = new();

        for (double i = 0; i < 360; i += 5) {
            int x1 = (int) (Radius * Math.Cos(i * Math.PI / 180d) + Dimension / 2d);
            int y1 = (int) (Radius * Math.Sin(i * Math.PI / 180d) + Dimension / 2d);
            border.Add(new Tuple<int, int>(x1, y1));
        }

        return border;
    }

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

    public static WriteableBitmap generateRawItemImage(int[,] itemGrid) {
        int xDimension = itemGrid.GetLength(0);
        int yDimension = itemGrid.GetLength(1);

        WriteableBitmap outputBitmap = new(new PixelSize(xDimension, yDimension), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, yDimension, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < xDimension; x++) {
                    int itemId = itemGrid[x, y];
                    if (itemId != -1) {
                        Color toSet = ItemType.getItemTypeByID(itemId).Color;
                        dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);
                    }
                }
            });
        }

        return outputBitmap;
    }

    public static Color[] getItemImageRaw(ItemType itemType, int index = -1) {
        bool singleDigit = false;
        bool[][] segments = Array.Empty<bool[]>();
        if (index != -1) {
            (singleDigit, segments) = getSegments(index);
        }

        Color[] rawColorData = new Color[Dimension * Dimension];
        int[] whiteBorders = {5, 7, 8, 9, 10};

        HashSet<Tuple<int, int>> border = getBorder();

        for (int x = 0; x < Dimension; x++) {
            for (int y = 0; y < Dimension; y++) {
                Color toSet;
                double distance = Math.Sqrt(Math.Pow(x - 8d, 2d) + Math.Pow(y - 8d, 2d));
                if (distance < Radius + 0.5d) {
                    Tuple<int, int> key = new(x, y);
                    toSet = border.Contains(key)
                        ? whiteBorders.Contains(itemType.ID) ? Colors.White : Colors.Black
                        : itemType.Color;
                } else {
                    toSet = Colors.Transparent;
                }

                if (index != -1) {
                    if (singleDigit) {
                        if (x is >= 7 and <= 9 && y is >= 6 and <= 10) {
                            int idx = (x - 7) % 3 + (y - 6) * 3;
                            if (segments[0][idx]) {
                                toSet = Colors.White;
                            }
                        }
                    } else {
                        // ReSharper disable once MergeIntoPattern
                        if (x >= 5 && x <= 11 && x != 8 && y is >= 6 and <= 10) {
                            if (x < 8) {
                                int idx = (x - 5) % 3 + (y - 6) * 3;
                                if (segments[1][idx]) {
                                    toSet = Colors.White;
                                }
                            } else {
                                int idx = (x - 9) % 3 + (y - 6) * 3;
                                if (segments[0][idx]) {
                                    toSet = Colors.White;
                                }
                            }
                        }
                    }
                }

                rawColorData[y * Dimension + x] = toSet;
            }
        }

        return rawColorData;
    }

    public static WriteableBitmap getLatestItemBitMap() {
        return latestItemBitmap;
    }

    public static void setLatestItemBitMap(WriteableBitmap newLatestItemBitmap) {
        latestItemBitmap = newLatestItemBitmap;
    }

    public static WriteableBitmap generateItemOverlay(int[,] itemGrid, int imgOutWidth, int imgOutHeight) {
        const int itemDimension = 17;
        int xDimension = imgOutWidth * itemDimension;
        int yDimension = imgOutHeight * itemDimension;

        WriteableBitmap outputBitmap = new(new PixelSize(xDimension, yDimension), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        Dictionary<int, Color[]> items = new();
        for (int x = 0; x < imgOutWidth; x++) {
            for (int y = 0; y < imgOutHeight; y++) {
                int itemId = itemGrid[x, y];
                if (itemId != -1 && !items.ContainsKey(itemId)) {
                    Color[] itemBitmap = getItemImageRaw(ItemType.getItemTypeByID(itemId));
                    items[itemId] = itemBitmap;
                }
            }
        }

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, yDimension, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < xDimension; x++) {
                    int itemId = itemGrid[x / itemDimension, y / itemDimension];
                    if (itemId != -1) {
                        Color toSet = items[itemId][y % itemDimension * itemDimension + x % itemDimension];
                        dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);
                    }
                }
            });
        }

        latestItemBitmap = outputBitmap;
        return outputBitmap;
    }

    private static (bool, bool[][]) getSegments(int i) {
        int digits = i <= 9 ? 1 : 2;
        bool[][] pixels = new bool[digits][];

        for (int d = 0; d < digits; d++) {
            pixels[d] = getSegment(d == 0 ? i % 10 : i / 10);
        }

        return (digits == 1, pixels);
    }

    private static bool[] getSegment(int i) {
        bool[] segment = {
            i != 1, i != 4, i != 1, i != 2 && i != 3 && i != 7, i == 1, i != 1 && i != 5 && i != 6, i != 1 && i != 7,
            i != 0 && i != 7, i != 1, i is 2 or 6 or 8 or 0, i == 1, i != 1 && i != 2, i != 4 && i != 7,
            i != 4 && i != 7,
            true
        };
        return segment;
    }
}