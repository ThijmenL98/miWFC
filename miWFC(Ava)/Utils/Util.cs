﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using miWFC.DeBroglie;
using miWFC.Managers;
using miWFC.ViewModels;

namespace miWFC.Utils;

public static class Util {
    private const int Dimension = 17;
    private const double Radius = 6d;
    private static readonly XDocument xDoc = XDocument.Load(AppContext.BaseDirectory + "/Assets/samples.xml");

    private static WriteableBitmap latestItemBitmap
        = new(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    public static int getDimension() {
        return Dimension;
    }

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
    }

    public static WriteableBitmap getImageFromPath(string path) {
        MemoryStream ms = new(File.ReadAllBytes(path));
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

    public static WriteableBitmap ColourArrayToImage(Color[,] colours) {
        int width = colours.GetLength(0);
        int height = colours.GetLength(1);

        WriteableBitmap outputBitmap = new(new PixelSize(width, height), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, height, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < width; x++) {
                    Color toSet = colours[x, (int) y];
                    dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);
                }
            });
        }

        return outputBitmap;
    }

    public static WriteableBitmap ValueArrayToImage(int[,] values, int tileSize,
        Dictionary<int, Tuple<Color[], Tile>> tiles) {
        int width = values.GetLength(0) * tileSize;
        int height = values.GetLength(1) * tileSize;

        WriteableBitmap outputBitmap = new(new PixelSize(width, height), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, height, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < width; x++) {
                    int value = values[(int) Math.Floor((double) x / tileSize),
                        (int) Math.Floor((double) y / tileSize)];

                    bool isCollapsed = value >= 0;
                    Color[]? outputPattern = isCollapsed ? tiles.ElementAt(value).Value.Item1 : null;
                    Color toSet = outputPattern?[y % tileSize * tileSize + x % tileSize] ?? Color.Parse("#00000000");

                    dest[x] = (uint) ((toSet.A << 24) + (toSet.R << 16) + (toSet.G << 8) + toSet.B);
                }
            });
        }

        return outputBitmap;
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

    public static T[,] RotateArrayClockwise<T>(T[,] src) {
        int width = src.GetUpperBound(0) + 1;
        int height = src.GetUpperBound(1) + 1;
        T[,] dst = new T[height, width];

        for (int row = 0; row < height; row++) {
            for (int col = 0; col < width; col++) {
                int newCol = height - (row + 1);

                dst[newCol, col] = src[col, row];
            }
        }

        return dst;
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

    public static WriteableBitmap generateRawItemImage(Tuple<int, int>[,] itemGrid) {
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
                    int itemId = itemGrid[x, y].Item1;
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
                                toSet = itemType.HasDarkText ? Colors.Black : Colors.White;
                            }
                        }
                    } else {
                        // ReSharper disable once MergeIntoPattern
                        if (x >= 5 && x <= 11 && x != 8 && y is >= 6 and <= 10) {
                            if (x < 8) {
                                int idx = (x - 5) % 3 + (y - 6) * 3;
                                if (segments[1][idx]) {
                                    toSet = itemType.HasDarkText ? Colors.Black : Colors.White;
                                }
                            } else {
                                int idx = (x - 9) % 3 + (y - 6) * 3;
                                if (segments[0][idx]) {
                                    toSet = itemType.HasDarkText ? Colors.Black : Colors.White;
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

    public static WriteableBitmap generateItemOverlay(Tuple<int, int>[,] itemGrid, int imgOutWidth, int imgOutHeight) {
        const int itemDimension = 17;
        int xDimension = imgOutWidth * itemDimension;
        int yDimension = imgOutHeight * itemDimension;

        WriteableBitmap outputBitmap = new(new PixelSize(xDimension, yDimension), new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        Dictionary<Tuple<int, int>, Color[]> items = new();
        for (int x = 0; x < imgOutWidth; x++) {
            for (int y = 0; y < imgOutHeight; y++) {
                int itemId = itemGrid[x, y].Item1;
                if (itemId != -1 && !items.ContainsKey(itemGrid[x, y])) {
                    Color[] itemBitmap = getItemImageRaw(ItemType.getItemTypeByID(itemId), itemGrid[x, y].Item2);
                    items[itemGrid[x, y]] = itemBitmap;
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
                    Tuple<int, int> curItem = itemGrid[x / itemDimension, y / itemDimension];
                    if (curItem.Item1 != -1) {
                        Color toSet = items[curItem][y % itemDimension * itemDimension + x % itemDimension];
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

    private static T[,] To2D<T>(IReadOnlyList<T[]> source) {
        try {
            int firstDim = source.Count;
            int secondDim = source.GroupBy(row => row.Length).Single().Key;

            T[,] result = new T[firstDim, secondDim];
            for (int i = 0; i < firstDim; ++i) {
                for (int j = 0; j < secondDim; ++j) {
                    result[i, j] = source[i][j];
                }
            }

            return result;
        } catch (InvalidOperationException) {
            throw new InvalidOperationException("The given jagged array is not rectangular.");
        }
    }

    public static T[,] TransposeMatrix<T>(T[,] matrix) {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);

        T[,] result = new T[columns, rows];

        for (int c = 0; c < columns; c++) {
            for (int r = 0; r < rows; r++) {
                result[c, r] = matrix[r, c];
            }
        }

        return result;
    }

    public static ObservableCollection<TemplateViewModel> GetTemplates(CentralManager centralManager) {
        string inputImage = centralManager.getMainWindowVM().InputImageSelection;
        bool isOverlapping = centralManager.getWFCHandler().isOverlappingModel();

        string baseDir = $"{AppContext.BaseDirectory}/Assets/Templates/";
        if (!Directory.Exists(baseDir)) {
            Directory.CreateDirectory(baseDir);
        }

        ObservableCollection<TemplateViewModel> templateList = new();

        string[] fileEntries = Directory.GetFiles(baseDir);
        foreach (string file in fileEntries) {
            if (Path.GetFileName(file).StartsWith(inputImage) && Path.GetExtension(file).Equals(".wfcPatt")) {
                WriteableBitmap tvmBitmap = getImageFromPath(file);
                if (isOverlapping) {
                    Color[][] tvmColourArray = imageToColourArray(tvmBitmap).Item1;
                    templateList.Add(new TemplateViewModel(tvmBitmap, To2D(tvmColourArray),
                        Path.GetFileName(file).Replace("_", "").Replace(inputImage, "").Replace(".wfcPatt", "")));
                } else {
                    int inputImageWidth = (int) tvmBitmap.Size.Width, inputImageHeight = (int) tvmBitmap.Size.Height;
                    int tileSize = centralManager.getWFCHandler().getTileSize();
                    int imageWidth = inputImageWidth / tileSize;
                    int imageHeight = inputImageHeight / tileSize;

                    byte[] input = File.ReadAllBytes(file);

                    IEnumerable<byte> trimmedInputB
                        = input.Skip(Math.Max(0,
                            input.Length - imageWidth * imageHeight));
                    byte[] inputBUntrimmed = trimmedInputB as byte[] ?? trimmedInputB.ToArray();
                    Split(inputBUntrimmed, imageWidth * imageHeight, out byte[] inputB, out byte[] _);

                    int[,] values = new int[imageWidth, imageHeight];

                    for (int x = 0; x < imageWidth; x++) {
                        for (int y = 0; y < imageHeight; y++) {
                            values[x, y] = inputB[x * imageHeight + y] - 2;
                        }
                    }

                    templateList.Add(new TemplateViewModel(tvmBitmap, values,
                        Path.GetFileName(file).Replace("_", "").Replace(inputImage, "").Replace(".wfcPatt", "")));
                }
            }
        }

        return templateList;
    }
}