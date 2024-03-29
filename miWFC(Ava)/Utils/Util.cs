﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData;
using miWFC.DeBroglie;
using miWFC.ViewModels.Structs;

// ReSharper disable PossibleMultipleEnumeration

namespace miWFC.Utils;

/// <summary>
///     Util function, multiple static functions to handle common calculations
/// </summary>
public static class Util {
    /*
     * Constants
     */

    private const int Dimension = 17;
    private const double Radius = 6d;
    private static readonly XDocument xDoc = XDocument.Load(AppContext.BaseDirectory + "/Assets/samples.xml");

    private static WriteableBitmap latestItemBitmap
        = new(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    /*
     * Public Constants
     */

    public static readonly Color negativeColour = Color.Parse("#FF0064");
    public static readonly Color positiveColour = Color.Parse("#00FF69");

    /*
     * Functions related to pattern initialization
     */

    /// <summary>
    ///     Converter that converts a mapping coordinates to colour function into an actual colour array based on these coords
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <param name="tilesize">Size (both width and height) of the tile</param>
    /// <returns>Linear array representation of the tile</returns>
    private static Color[] ImTile(Func<int, int, Color> f, int tilesize) {
        Color[] result = new Color[tilesize * tilesize];
        for (int y = 0; y < tilesize; y++) {
            for (int x = 0; x < tilesize; x++) {
                result[x + y * tilesize] = f(x, y);
            }
        }

        return result;
    }

    /// <summary>
    ///     Converter which rotates a linear colour array clockwise
    /// </summary>
    /// <param name="array">Array to rotate</param>
    /// <param name="tilesize">Size (both width and height) of the tile</param>
    /// <returns>Rotated array</returns>
    public static Color[] Rotate(IReadOnlyList<Color> array, int tilesize) {
        return ImTile((x, y) => array[tilesize - 1 - y + x * tilesize], tilesize);
    }

    /// <summary>
    ///     Converter which flips a linear colour array
    /// </summary>
    /// <param name="array">Array to flip</param>
    /// <param name="tilesize">Size (both width and height) of the tile</param>
    /// <returns>Flipped array</returns>
    public static Color[] Reflect(IReadOnlyList<Color> array, int tilesize) {
        return ImTile((x, y) => array[tilesize - 1 - x + y * tilesize], tilesize);
    }

    /*
     * Input handling
     */

    /// <summary>
    ///     Get all input images associated with the selected category
    /// </summary>
    /// <param name="modelType">overlapping or simpletiled model</param>
    /// <param name="category">category of images</param>
    /// <returns>List of images associated with the input category</returns>
    public static string[] GetModelImages(string modelType, string category) {
        List<string> images = new();
        if (!category.Equals("Custom")) {
            if (xDoc.Root != null) {
                images = xDoc.Root.Elements(modelType)
                    .Where(xElement => (xElement.Attribute("category")?.Value ?? "").Equals(category))
                    .Select(xElement => xElement.Attribute("name")?.Value ?? "").ToList();
            }

            images.Sort();
        } else {
            try {
                images.AddRange(
                    from file in Directory.GetFiles($"{AppContext.BaseDirectory}/samples/Custom", "*.png",
                        SearchOption.AllDirectories)
                    select Path.GetFileName(file.Replace(".png", "")));
            } catch (DirectoryNotFoundException) { }
        }

        return images.Distinct().ToArray();
    }

    /// <summary>
    ///     Forwarding function based on a sample input image to getImageFromPath(string)
    /// </summary>
    /// 
    /// <param name="name">Name of the input sample</param>
    /// <param name="category">Name of the input category</param>
    /// <param name="isCustomSideView">Whether the currently selected input image is custom AND a side view image</param>
    /// 
    /// <returns>The image</returns>
    public static WriteableBitmap GetSampleFromPath(string name, string category, out bool isCustomSideView) {
        bool isCustom = category.Equals("Custom");
        string subFolder = isCustom ? "Custom" : "Default";

        if (isCustom) {
            isCustomSideView = File.Exists($"{AppContext.BaseDirectory}/samples/{subFolder}/SideView/{name}.png");
            subFolder += isCustomSideView ? "/SideView" : "/TopDown";
        } else {
            isCustomSideView = false;
        }

        return GetImageFromPath($"{AppContext.BaseDirectory}/samples/{subFolder}/{name}.png");
    }

    /// <summary>
    ///     Return an image from a machine path
    /// </summary>
    /// <param name="path">Path location of the image</param>
    /// <returns>WriteableBitmap</returns>
    private static WriteableBitmap GetImageFromPath(string path) {
        MemoryStream ms = new(File.ReadAllBytes(path));
        WriteableBitmap writeableBitmap = WriteableBitmap.Decode(ms);
        return writeableBitmap;
    }

    /// <summary>
    ///     Get the allowed tile sizes for the given input image
    /// </summary>
    /// <param name="imageName">Name of the input image</param>
    /// <returns>The pattern sizes, and the amount of pattern sizes found (minus 2 default sizes, 2 and 3)</returns>
    public static (int[], int) GetImagePatternDimensions(string imageName) {
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

        return (patternDimensionsList.ToArray(), j == 0 ? 1 : j - 2);
    }

    /// <summary>
    ///     Get all categories based on the selected model
    /// </summary>
    /// <param name="modelType">Selected model</param>
    /// <returns>List of categories</returns>
    public static string[] GetCategories(string modelType) {
        return modelType.Equals("overlapping")
            ? new[] {"Textures", "Shapes", "Knots", "Fonts", "Worlds Side-View", "Worlds Top-Down", "Custom"}
            : new[] {"Worlds Top-Down", "Textures"};
    }

    /// <summary>
    ///     Get a description of each category
    /// </summary>
    /// <param name="category">Category to get a description of</param>
    /// <returns>Description</returns>
    public static string GetDescription(string category) {
        return category switch {
            "Textures" => "Surface detailing to apply to 2D/3D models",
            "Shapes" => "Surfaces of multidimensional objects",
            "Knots" => "Intertwined and/or tangled lines",
            "Fonts" => "Printable or displayable text characters",
            "Worlds Side-View" =>
                "(Game) Worlds as seen from the side (Seamless Output and Input Wrapping have been pre-set due to the nature of these images)",
            "Worlds Top-Down" => "(Game) Worlds as seen from above",
            "Custom" => "Images uploaded by you yourself!",
            _ => "???"
        };
    }

    /*
     * Bitmap Logic
     */

    /// <summary>
    ///     Function that reads an input image and converts it into an matrix of colours, and a list of distinct colours
    /// </summary>
    /// <param name="bmp">Input Image</param>
    /// <returns>(matrix of colours, list of distinct colours)</returns>
    public static (Color[][], HashSet<Color>) ImageToColourArray(WriteableBitmap bmp) {
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
                    c = c.A <= 30 ? Color.FromArgb(30, c.R, c.G, c.B) : c;
                    result[y][x] = c;
                    currentColors[y * height + x] = c;
                }
            });
        }

        return (result, new HashSet<Color>(currentColors.Values.Distinct()));
    }

    /// <summary>
    ///     Return the image form of an item
    /// </summary>
    /// <param name="itemColor">Currently selected item colour</param>
    /// <param name="index">
    ///     Item dependency index, if this item is dependent on another item the index will be
    ///     embedded within the image
    /// </param>
    /// <returns>Item image</returns>
    public static WriteableBitmap GetItemImage(Color itemColor, int index = -1) {
        Color[] rawColours = GetItemImageRaw(itemColor, index);
        WriteableBitmap outputBitmap = CreateBitmapFromData(Dimension, Dimension, 1,
            (x, y) => rawColours[y % Dimension * Dimension + x % Dimension]);
        return outputBitmap;
    }

    /// <summary>
    ///     Function to convert a matrix of colours to a bitmap
    /// </summary>
    /// <param name="colours">Matrix of colours</param>
    /// <returns>Bitmap</returns>
    public static WriteableBitmap ColourArrayToImage(Color[,] colours) {
        int width = colours.GetLength(0);
        int height = colours.GetLength(1);

        WriteableBitmap outputBitmap = CreateBitmapFromData(width, height, 1, (x, y) => colours[x, y]);

        return outputBitmap;
    }

    /// <summary>
    ///     Function that converts a matrix of tile indices to a bitmap
    /// </summary>
    /// <param name="values">Matrix of tile indices</param>
    /// <param name="tileSize">Tile Dimension</param>
    /// <param name="tiles">Tiles allowed to place</param>
    /// <returns>Bitmap</returns>
    public static WriteableBitmap ValueArrayToImage(int[,] values, int tileSize,
        Dictionary<int, Tuple<Color[], Tile>> tiles) {
        int width = values.GetLength(0) * tileSize;
        int height = values.GetLength(1) * tileSize;

        WriteableBitmap outputBitmap = CreateBitmapFromData(width, height, 1, (x, y) => {
            int value = values[(int) Math.Floor((double) x / tileSize),
                (int) Math.Floor((double) y / tileSize)];

            bool isCollapsed = value >= 0;
            Color[]? outputPattern = isCollapsed ? tiles.ElementAt(value).Value.Item1 : null;
            return outputPattern?[y % tileSize * tileSize + x % tileSize] ?? Color.Parse("#00000000");
        });

        return outputBitmap;
    }

    /// <summary>
    ///     Convert a bitmap into a 1D Array of Colours
    /// </summary>
    /// <param name="writeableBitmap">Input Bitmap</param>
    /// <returns>1D array representation in Colours of the input bitmap</returns>
    public static Color[] ExtractColours(WriteableBitmap writeableBitmap) {
        (Color[][] colourArray, HashSet<Color> _) = ImageToColourArray(writeableBitmap);
        return Convert2DArrayTo1D(colourArray);
    }

    /// <summary>
    ///     Convert two bitmaps into a single bitmap, overlaying with the use of transparency
    /// </summary>
    /// <param name="bottom">Bottom Bitmap</param>
    /// <param name="tileSizeB">Size of cells in the bottom bitmap</param>
    /// <param name="top">Overlaid Bitmap</param>
    /// <param name="tileSizeT">Size of cells in the top bitmap</param>
    /// <param name="imgInWidth">Width of the largest input bitmap</param>
    /// <param name="imgInHeight">Height of the largest input bitmap</param>
    /// <returns>Combined Bitmap</returns>
    public static WriteableBitmap CombineBitmaps(WriteableBitmap bottom, int tileSizeB, WriteableBitmap top,
        int tileSizeT, int imgInWidth, int imgInHeight) {
        int xDimension = imgInWidth * tileSizeB * tileSizeT;
        int yDimension = imgInHeight * tileSizeB * tileSizeT;

        WriteableBitmap outputBitmap
            = new(new PixelSize(xDimension, yDimension), Vector.One,
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

    /// <summary>
    ///     Create item grid image, with a single pixel representing a single output cell
    /// </summary>
    /// <param name="itemGrid">Grid of items</param>
    /// <param name="itemColours">Colours associated to each name</param>
    /// <returns>Bitmap</returns>
    public static WriteableBitmap GenerateRawItemImage(Tuple<string, int>[,]? itemGrid,
        Dictionary<string, Color> itemColours) {
        if (itemGrid == null) {
            return new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        }

        int xDimension = itemGrid.GetLength(0);
        int yDimension = itemGrid.GetLength(1);

        WriteableBitmap outputBitmap = CreateBitmapFromData(xDimension, yDimension, 1, (x, y) => {
            string itemName = itemGrid[x, y].Item1;
            return !itemName.Equals("") ? itemColours[itemName] : Colors.Transparent;
        });

        return outputBitmap;
    }

    /// <summary>
    ///     Generate a bitmap that only has the items, rest being transparent
    /// </summary>
    /// <param name="itemGrid">Grid of items to place</param>
    /// <param name="imgOutWidth">Width of the current solution</param>
    /// <param name="imgOutHeight">Height of the current solution</param>
    /// <param name="itemColours">Colours of each item name</param>
    /// <returns>Bitmap</returns>
    public static WriteableBitmap GenerateItemOverlay(Tuple<string, int>[,]? itemGrid, int imgOutWidth,
        int imgOutHeight, Dictionary<string, Color> itemColours) {
        if (itemGrid == null) {
            return new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        }

        const int itemDimension = 17;
        int xDimension = imgOutWidth * itemDimension;
        int yDimension = imgOutHeight * itemDimension;

        Dictionary<Tuple<string, int>, Color[]> items = new();
        for (int x = 0; x < imgOutWidth; x++) {
            for (int y = 0; y < imgOutHeight; y++) {
                string itemName = itemGrid[x, y].Item1;
                if (!itemName.Equals("") && !items.ContainsKey(itemGrid[x, y])) {
                    Color[] itemBitmap = GetItemImageRaw(itemColours[itemName], itemGrid[x, y].Item2);
                    items[itemGrid[x, y]] = itemBitmap;
                }
            }
        }

        WriteableBitmap outputBitmap = CreateBitmapFromData(xDimension, yDimension, 1, (x, y) => {
            Tuple<string, int> curItem = itemGrid[x / itemDimension, y / itemDimension];
            return !curItem.Item1.Equals("")
                ? items[curItem][y % itemDimension * itemDimension + x % itemDimension]
                : Colors.Transparent;
        });

        latestItemBitmap = outputBitmap;
        return outputBitmap;
    }

    /// <summary>
    ///     Return the latest generated item bitmap
    /// </summary>
    /// <returns>WriteableBitmap</returns>
    public static WriteableBitmap GetLatestItemBitMap() {
        return latestItemBitmap;
    }

    /// <summary>
    ///     Set the latest generated item bitmap
    /// </summary>
    /// <param name="newLatestItemBitmap">Latest item bitmap</param>
    public static void SetLatestItemBitMap(WriteableBitmap newLatestItemBitmap) {
        latestItemBitmap = newLatestItemBitmap;
    }


    /// <summary>
    ///     Get the index of the most occuring colour in an image's bottom vertical row
    /// </summary>
    /// <param name="inputBitmap">Input bitmap</param>
    /// <returns>index of the most occuring colour in an image's bottom vertical row</returns>
    public static int CalculateGroundTileIndex(WriteableBitmap inputBitmap) {
        (Color[][] colourArray, HashSet<Color> allowedColours) = ImageToColourArray(inputBitmap);
        List<Color> colorCount = new();
        for (int y = 0; y < colourArray[0].Length; y++) {
            colorCount.Add(colourArray[^1][y]);
        }

        Color most = colorCount.GroupBy(i => i).OrderByDescending(grp => grp.Count())
            .Select(grp => grp.Key).First();
        return allowedColours.IndexOf(most);
    }

    /*
     * Item related functions
     */

    /// <summary>
    ///     Create a colour array representation of the item including a possible dependent item linking value
    /// </summary>
    /// <param name="itemColor">Item colour to create an image for</param>
    /// <param name="index">Index of the dependent item</param>
    /// <returns>Bitmap</returns>
    private static Color[] GetItemImageRaw(Color itemColor, int index = -1) {
        bool singleDigit = false;
        bool[][] segments = Array.Empty<bool[]>();
        if (index != -1) {
            (singleDigit, segments) = GetSegments(index);
        }

        Color[] rawColorData = new Color[Dimension * Dimension];
        HashSet<Tuple<int, int>> border = GetBorder();

        Color contrastColour = itemColor.R * 0.299d + itemColor.G * 0.587d + itemColor.B * 0.114d > 150d
            ? Color.Parse("#424242")
            : Colors.White;

        for (int x = 0; x < Dimension; x++) {
            for (int y = 0; y < Dimension; y++) {
                Color toSet;
                double distance = Math.Sqrt(Math.Pow(x - 8d, 2d) + Math.Pow(y - 8d, 2d));
                if (distance < Radius + 0.5d) {
                    Tuple<int, int> key = new(x, y);
                    toSet = border.Contains(key)
                        ? contrastColour
                        : itemColor;
                } else {
                    toSet = Colors.Transparent;
                }

                if (index != -1) {
                    if (singleDigit) {
                        if (x is >= 7 and <= 9 && y is >= 6 and <= 10) {
                            int idx = (x - 7) % 3 + (y - 6) * 3;
                            if (segments[0][idx]) {
                                toSet = contrastColour;
                            }
                        }
                    } else {
                        if (x is >= 5 and <= 11 && x != 8 && y is >= 6 and <= 10) {
                            if (x < 8) {
                                int idx = (x - 5) % 3 + (y - 6) * 3;
                                if (segments[1][idx]) {
                                    toSet = contrastColour;
                                }
                            } else {
                                int idx = (x - 9) % 3 + (y - 6) * 3;
                                if (segments[0][idx]) {
                                    toSet = contrastColour;
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

    /// <summary>
    ///     Get the circular border of the item image representation
    /// </summary>
    /// <returns>Set of coordinates that are part of the border</returns>
    private static HashSet<Tuple<int, int>> GetBorder() {
        HashSet<Tuple<int, int>> border = new();

        for (double i = 0; i < 360; i += 5) {
            int x1 = (int) (Radius * Math.Cos(i * Math.PI / 180d) + Dimension / 2d);
            int y1 = (int) (Radius * Math.Sin(i * Math.PI / 180d) + Dimension / 2d);
            border.Add(new Tuple<int, int>(x1, y1));
        }

        return border;
    }

    /// <summary>
    ///     Get the boolean 3x5 segments related to the input integer
    /// </summary>
    /// <param name="i">Integer to represent</param>
    /// <returns>Segments</returns>
    private static (bool, bool[][]) GetSegments(int i) {
        int digits = i <= 9 ? 1 : 2;
        bool[][] pixels = new bool[digits][];

        for (int d = 0; d < digits; d++) {
            pixels[d] = GetSegment(d == 0 ? i % 10 : i / 10);
        }

        return (digits == 1, pixels);
    }

    /// <summary>
    ///     Get a single 3x5 segment related to the input integer
    /// </summary>
    /// <param name="i">Integer to represent</param>
    /// <returns>Segment</returns>
    private static bool[] GetSegment(int i) {
        bool[] segment = {
            i != 1, i != 4, i != 1, i != 2 && i != 3 && i != 7, i == 1, i != 1 && i != 5 && i != 6, i != 1 && i != 7,
            i != 0 && i != 7, i != 1, i is 2 or 6 or 8 or 0, i == 1, i != 1 && i != 2, i != 4 && i != 7,
            i != 4 && i != 7,
            true
        };
        return segment;
    }

    /*
     * Template related functions
     */

    /// <summary>
    ///     Get the user created templates from this input image
    /// </summary>
    /// <param name="inputImage">Input image</param>
    /// <param name="isOverlapping">Whether the input image is overlapping or not</param>
    /// <param name="tileSize">The size of the tiles</param>
    /// <returns></returns>
    public static ObservableCollection<TemplateViewModel> GetTemplates(string inputImage, bool isOverlapping,
        int tileSize) {
        string baseDir = $"{AppContext.BaseDirectory}/Assets/Templates/";
        if (!Directory.Exists(baseDir)) {
            Directory.CreateDirectory(baseDir);
        }

        ObservableCollection<TemplateViewModel> templateList = new();

        string[] fileEntries = Directory.GetFiles(baseDir);
        foreach (string file in fileEntries) {
            if (Path.GetFileName(file).StartsWith(inputImage) && Path.GetExtension(file).Equals(".wfcPatt")) {
                WriteableBitmap tvmBitmap = GetImageFromPath(file);
                if (isOverlapping) {
                    Color[][] tvmColourArray = ImageToColourArray(tvmBitmap).Item1;
                    templateList.Add(new TemplateViewModel(tvmBitmap, Convert1DArrayTo2D(tvmColourArray),
                        Path.GetFileName(file).Replace("_", "").Replace(inputImage, "").Replace(".wfcPatt", "")));
                } else {
                    int inputImageWidth = (int) tvmBitmap.Size.Width, inputImageHeight = (int) tvmBitmap.Size.Height;
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

    /*
     * Array Logic
     */

    /// <summary>
    ///     Conversion function to change a 2 dimensional array to a single dimensional array
    /// </summary>
    /// <param name="source">Source array</param>
    /// <typeparam name="T">Type of the array</typeparam>
    /// <returns>1D array representation</returns>
    private static T[] Convert2DArrayTo1D<T>(IEnumerable<T[]> source) {
        List<T> lst = new();
        foreach (T[] a in source) {
            lst.AddRange(a);
        }

        return lst.ToArray();
    }

    /// <summary>
    ///     Conversion function to change a single dimensional array to a 2 dimensional array
    /// </summary>
    /// <param name="source">Source array</param>
    /// <typeparam name="T">Type of the array</typeparam>
    /// <returns>2D array representation</returns>
    private static T[,] Convert1DArrayTo2D<T>(IReadOnlyList<T[]> source) {
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

    /// <summary>
    ///     Rotate a 2D array of values clockwise
    /// </summary>
    /// <param name="src">2D array to rotate</param>
    /// <typeparam name="T">Type of the array</typeparam>
    /// <returns>Rotated array</returns>
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

    /// <summary>
    ///     Append an item to a fixed-length array, increasing its size
    /// </summary>
    /// <param name="array">Array to append to</param>
    /// <param name="item">Item to append with</param>
    /// <typeparam name="T">Type of the array</typeparam>
    /// <returns>Appended array</returns>
    private static T[] Append<T>(T[] array, T item) {
        T[] result = new T[array.Length + 1];
        array.CopyTo(result, 0);
        result[array.Length] = item;
        return result;
    }

    /// <summary>
    ///     Split an array into two separate arrays based on an index separator
    /// </summary>
    /// <param name="source">Array to split</param>
    /// <param name="index">Index to split at, also the starting index of the second array</param>
    /// <param name="first">Out: Left side of the array</param>
    /// <param name="last">Out: Right side of the array</param>
    /// <typeparam name="T">Type of the array</typeparam>
    public static void Split<T>(T[] source, int index, out T[] first, out T[] last) {
        int len2 = source.Length - index;
        first = new T[index];
        last = new T[len2];
        Array.Copy(source, 0, first, 0, index);
        Array.Copy(source, index, last, 0, len2);
    }

    /*
     * PNG Logic
     */

    /// <summary>
    ///     Create a bitmap from the given conversion function
    /// </summary>
    /// <param name="imageWidth">Width of the image</param>
    /// <param name="imageHeight">Height of the image</param>
    /// <param name="tileSize">Size of the cells in pixels</param>
    /// <param name="conversionFunction">Function that maps a coordinate in the image to a colour</param>
    /// <returns>Image</returns>
    public static WriteableBitmap CreateBitmapFromData(int imageWidth, int imageHeight, int tileSize,
        Func<int, int, Color> conversionFunction) {
        return CreateBitmapFromDataFull(imageWidth, imageHeight, tileSize, conversionFunction).Item1;
    }

    /// <summary>
    ///     Create a bitmap from the given conversion function, including the count of nontransparent pixels
    /// </summary>
    /// <param name="imageWidth">Width of the image</param>
    /// <param name="imageHeight">Height of the image</param>
    /// <param name="tileSize">Size of the cells in pixels</param>
    /// <param name="conversionFunction">Function that maps a coordinate in the image to a colour</param>
    /// <returns>(w, i) -> w = Image, i = count</returns>
    public static (WriteableBitmap, int) CreateBitmapFromDataFull(int imageWidth, int imageHeight, int tileSize,
        Func<int, int, Color> conversionFunction) {
        WriteableBitmap outputBitmap = new(new PixelSize(imageWidth * tileSize, imageHeight * tileSize),
            new Vector(96, 96),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using ILockedFramebuffer? frameBuffer = outputBitmap.Lock();

        int collapseCount = 0;

        unsafe {
            uint* backBuffer = (uint*) frameBuffer.Address.ToPointer();
            int stride = frameBuffer.RowBytes;

            Parallel.For(0L, imageHeight * tileSize, y => {
                uint* dest = backBuffer + (int) y * stride / 4;
                for (int x = 0; x < imageWidth * tileSize; x++) {
                    Color toPlace = conversionFunction(x, (int) y);
                    dest[x] = (uint) ((toPlace.A << 24) + (toPlace.R << 16) + (toPlace.G << 8) + toPlace.B);

                    if (toPlace.A >= 20) {
                        Interlocked.Increment(ref collapseCount);
                    }
                }
            });
        }

        return (outputBitmap, collapseCount);
    }

    /// <summary>
    ///     Extended version of AppendPictureData(string, IEnumerable, bool) which first converts a 2D integer matrix to 1D
    /// </summary>
    /// <param name="fileName">Name of the file to append data to</param>
    /// <param name="appendData">Data to append</param>
    /// <param name="mapData">
    ///     Whether the data to be added is map data, which adds 2 to each integer,
    ///     as -1 and -2 cannot be converted to an unsigned byte
    /// </param>
    public static async Task AppendPictureData(string fileName, int[,] appendData, bool mapData) {
        int[] output1D = new int[appendData.Length];
        Buffer.BlockCopy(appendData, 0, output1D, 0, appendData.Length * 4);
        await AppendPictureData(fileName, output1D, mapData);
    }

    /// <summary>
    ///     Append data to a png, hidden, and not affecting the image itself
    /// </summary>
    /// <param name="fileName">Name of the file to append data to</param>
    /// <param name="appendData">Data to append</param>
    /// <param name="mapData">
    ///     Whether the data to be added is map data, which adds 2 to each integer,
    ///     as -1 and -2 cannot be converted to an unsigned byte
    /// </param>
    public static async Task AppendPictureData(string fileName, IEnumerable<int> appendData, bool mapData) {
        byte[] data = await File.ReadAllBytesAsync(fileName);
        data = appendData.Aggregate(data, (current, val) => Append(current, (byte) (val + (mapData ? 2 : 0))));
        await File.WriteAllBytesAsync(fileName, data);
    }

    /*
     * Math extensions
     */

    /// <summary>
    ///     Balancing function that makes sure that the lower is never bigger than or equal to the upper value
    /// </summary>
    /// <param name="lower">The lower bound</param>
    /// <param name="upper">The upper bound</param>
    /// <param name="change">Amount of value to change</param>
    /// <param name="lowerAdapted">Whether the lower or upper bound is changed</param>
    /// <returns>Balanced values (lower, higher)</returns>
    public static (int, int) BalanceValues(int lower, int upper, int change, bool lowerAdapted) {
        int newLower, newUpper;

        if (lowerAdapted) {
            newLower = Math.Max(1, lower + change);
            newUpper = upper - newLower < 1 ? newLower + 1 : upper;
        } else {
            newUpper = Math.Max(upper + change, 2);
            newLower = newUpper - lower < 1 ? newUpper - 1 : lower;
        }

        return (newLower, newUpper);
    }

    /// <summary>
    ///     Function to validate the input string as a colour
    /// </summary>
    /// <param name="s">Input string to validate as colour</param>
    /// <returns>S formatted if s was a string, otherwise null</returns>
    public static string? ValidateColor(string s) {
        if (!s.StartsWith("#")) {
            try {
                Color cRaw = Color.Parse(s);
                Color c = Color.FromRgb(cRaw.R, cRaw.G, cRaw.B);
                s = c.ToString().ToUpper().Replace("#FF", "#");
                return s;
            } catch (Exception) {
                // ignored
            }

            s = "#" + s;
        }

        try {
            Color cRaw = Color.Parse(s);
            Color c = Color.FromRgb(cRaw.R, cRaw.G, cRaw.B);
            s = c.ToString().ToUpper().Replace("#FF", "#");
            return s;
        } catch (Exception) {
            return null;
        }
    }
}