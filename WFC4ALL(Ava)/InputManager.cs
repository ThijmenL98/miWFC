using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using WFC4All.DeBroglie;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Rot;
using WFC4All.DeBroglie.Topo;
using WFC4ALL.ContentControls;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;

namespace WFC4All
{
    public class InputManager
    {
        private readonly XDocument xDoc;
        private Bitmap currentBitmap;
        private int currentStep, tileSize;
        private bool inputHasChanged;
        private Dictionary<int, Tuple<Color[], Tile>> tileCache;

        private XElement xRoot;

        private TilePropagator dbPropagator;
        private TileModel dbModel;
        private ITopoArray<Tile> tiles;

        private Bitmap latestOutput;

        private readonly MainWindowViewModel mainWindowVM;

        private OutputControl outputControl;
        private InputControl inputControl;

        public InputManager(MainWindowViewModel mainWindowVM, MainWindow mainWindow)
        {
            this.mainWindowVM = mainWindowVM;

            outputControl = mainWindow.getOutputControl();
            inputControl = mainWindow.getInputControl();

            tileSize = 0;
            tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
            currentStep = 0;
            xDoc = XDocument.Load("./Assets/samples.xml");
            currentBitmap = null;
            inputHasChanged = true;
            dbModel = null;
            dbPropagator = null;
            latestOutput = new Bitmap(1, 1);

            mainWindow.getInputControl().setCategories(getCategories("overlapping"));

            string[] inputImageDataSource = getImages("overlapping", "Textures"); // or "simpletiled"
            mainWindow.getInputControl().setInputImages(inputImageDataSource);

            (int[] patternSizeDataSource, int i) = getImagePatternDimensions(inputImageDataSource[0]);
            mainWindow.getInputControl().setPatternSizes(patternSizeDataSource, i);

        }

        /*
         * Functionality
         */

        public void execute()
        {
            try
            {
                int stepAmount = mainWindowVM.StepAmount;
                (Bitmap result2, bool _) = initAndRunWfcDB(true, stepAmount == 100 ? -1 : stepAmount);
                while (result2 == null)
                {
                    (result2, _) = initAndRunWfcDB(true, stepAmount == 100 ? -1 : stepAmount);
                }

                Avalonia.Media.Imaging.Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap((Image)result2);
                mainWindowVM.OutputImage = avaloniaBitmap;
            }
            catch (Exception exception)
            {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                string uRI = $"samples/NoResultFound.png";
                mainWindowVM.InputImage = new Avalonia.Media.Imaging.Bitmap(uRI);
            }
        }

        public Avalonia.Media.Imaging.Bitmap ConvertToAvaloniaBitmap(Image bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }
            System.Drawing.Bitmap bitmapTmp = new System.Drawing.Bitmap(bitmap);
            var bitmapdata = bitmapTmp.LockBits(new Rectangle(0, 0, bitmapTmp.Width, bitmapTmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Avalonia.Media.Imaging.Bitmap bitmap1 = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul,
                bitmapdata.Scan0,
                new Avalonia.PixelSize(bitmapdata.Width, bitmapdata.Height),
                new Avalonia.Vector(96, 96),
                bitmapdata.Stride);
            bitmapTmp.UnlockBits(bitmapdata);
            bitmapTmp.Dispose();
            return bitmap1;
        }

        public (Bitmap, bool) initAndRunWfcDB(bool reset, int steps)
        {
            //TODO
            //if (form.isChangingModels)
            //{
            //    return (new Bitmap(1, 1), true);
            //}

            Stopwatch sw = new();
            sw.Restart();

            if (reset || dbPropagator == null)
            {
                bool inputPaddingEnabled, outputPaddingEnabled;
                String category = inputControl.getCategory();
                String inputImage = inputControl.getInputImage();

                if (category.Equals("Textures"))
                {
                    inputPaddingEnabled = isOverlappingModel() && mainWindowVM.PaddingEnabled;
                    outputPaddingEnabled = inputPaddingEnabled;
                }
                else
                {

                    inputPaddingEnabled = category.Equals("Worlds Top-Down") || category.Equals("Worlds Side-View")
                        || category.Equals("Font") || category.Equals("Knots") && !inputImage.Equals("Nested") && !inputImage.Equals("NotKnot");
                    outputPaddingEnabled = category.Equals("Worlds Side-View") || inputImage.Equals("Font") || category.Equals("Knots")
                        && !inputImage.Equals("Nested");
                }

                if (inputHasChanged)
                {
                    //TODO
                    //form.displayLoading(true);
                    currentBitmap = getImage(inputControl.getInputImage());

                    //for (int i = 0; i < form.bitMaps.getPatternCount(); i++)
                    //{
                    //    foreach (Control item in form.patternPanel.Controls)
                    //    {
                    //        if (item.Name.Contains("patternPB_"))
                    //        {
                    //           Thread.CurrentThread.IsBackground = true;
                    //           form.patternPanel.Controls.Remove(item);
                    //            break;
                    //        }
                    //    }
                    //}

                    //form.bitMaps.reset();

                    if (isOverlappingModel())
                    {
                        ITopoArray<Color> dbSample
                            = TopoArray.create(imageToColourArray(currentBitmap),
                                inputPaddingEnabled); //TODO Input Padding
                        tiles = dbSample.toTiles();
                        dbModel = new OverlappingModel(inputControl.getPatternSize());

                        bool hasRotations = inputControl.getCategory().Equals("Worlds Top-Down")
                            || category.Equals("Knots") || category.Equals("Knots") || inputImage.Equals("Mazelike");
                        List<PatternArray> patternList
                            = ((OverlappingModel)dbModel).addSample(tiles, new TileRotation(hasRotations ? 4 : 1, false));

                        bool isCached = false;

                        foreach (PatternArray patternArray in patternList)
                        {
                            //TODO
                            //isCached = form.addPattern(patternArray, currentColors.ToList());
                            if (isCached)
                            {
                                break;
                            }
                        }

                        if (!isCached)
                        {
                            //TODO
                            //form.bitMaps.saveCache();
                        }
                    }
                    else
                    {
                        dbModel = new AdjacentModel();
                        xRoot = XDocument.Load($"samples/{inputImage}/data.xml").Root ?? new XElement("");

                        tileSize = int.Parse(xRoot.Attribute("size")?.Value ?? "16");

                        List<double> simpleWeights = new();

                        if (xRoot == null)
                        {
                            return (new Bitmap(0, 0), true);
                        }

                        tileCache = new Dictionary<int, Tuple<Color[], Tile>>();
                        bool isCached = false;

                        foreach (XElement xTile in xRoot.Element("tiles")?.Elements("tile")!)
                        {
                            Bitmap bitmap = new($"samples/{inputImage}/{xTile.Attribute("name")?.Value}.png");
                            int cardinality = char.Parse(xTile.Attribute("symmetry")?.Value ?? "X") switch
                            {
                                'L' => 4,
                                'T' => 4,
                                'I' => 2,
                                '\\' => 2,
                                'F' => 8,
                                _ => 1
                            };

                            Color[] cur = imTile((x, y) => bitmap.GetPixel(x, y), tileSize);
                            int val = tileCache.Count;
                            Tile curTile = new(val);
                            tileCache.Add(val, new Tuple<Color[], Tile>(cur, curTile));

                            for (int t = 1; t < cardinality; t++)
                            {
                                int myIdx = tileCache.Count;
                                Color[] curCard = t <= 3
                                    ? rotate(tileCache[val + t - 1].Item1.ToArray(), tileSize)
                                    : reflect(tileCache[val + t - 4].Item1.ToArray(), tileSize);
                                tileCache.Add(myIdx, new Tuple<Color[], Tile>(curCard, new Tile(myIdx)));
                            }

                            if (!isCached)
                            {
                                //TODO
                                //isCached = form.addPattern(bitmap);
                            }

                            for (int t = 0; t < cardinality; t++)
                            {
                                simpleWeights.Add(double.Parse(xTile.Attribute("weight")?.Value ?? "1.0"));
                            }
                        }

                        if (!isCached)
                        {
                            //TODO
                            //form.bitMaps.saveCache();
                        }

                        for (int i = 0; i < tileCache.Count; i++)
                        {
                            double refWeight = simpleWeights[i];
                            Tile refTile = tileCache.ElementAt(i).Value.Item2;
                            ((AdjacentModel)dbModel).setFrequency(refTile, refWeight);
                        }

                        int[][] values = new int[50][];

                        int j = 0;
                        foreach (XElement xTile in xRoot.Element("rows")?.Elements("row")!)
                        {
                            string[] row = xTile.Value.Split(',');
                            values[j] = new int[50];
                            for (int k = 0; k < 50; k++)
                            {
                                values[j][k] = int.Parse(row[k]);
                            }

                            j++;
                        }

                        ITopoArray<int> sample = TopoArray.create(values, false);
                        dbModel = new AdjacentModel(sample.toTiles());
                    }

#if (DEBUG)            
                    Trace.WriteLine(@$"Init took {sw.ElapsedMilliseconds}ms.");
#endif
                    sw.Restart();
                }

                //TODO Output Padding
                int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;

                GridTopology dbTopology = new(outputWidth, outputHeight, outputPaddingEnabled);
                int curSeed = Environment.TickCount;
                dbPropagator = new TilePropagator(dbModel, dbTopology, new TilePropagatorOptions
                {
                    BackTrackDepth = -1,
                    RandomDouble = new Random(curSeed).NextDouble,
                });
#if (DEBUG)
                Trace.WriteLine(@$"Assigning took {sw.ElapsedMilliseconds}ms.");
#endif
                sw.Restart();

                if (isOverlappingModel())
                {
                    if ("flowers".Equals(inputImage.ToLower()))
                    {
                        // Set the bottom last 2 rows to be the ground tile
                        dbPropagator?.select(0, outputHeight - 1, 0,
                            new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                        dbPropagator?.select(0, outputHeight - 2, 0,
                            new Tile(currentColors.ElementAt(currentColors.Count - 1)));

                        // And ban it elsewhere
                        for (int y = 0; y < outputHeight - 2; y++)
                        {
                            dbPropagator?.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                        }
                    }

                    if ("skyline".Equals(inputImage.ToLower()))
                    {
                        // Set the bottom last row to be the ground tile
                        dbPropagator?.select(0, outputHeight - 1, 0,
                            new Tile(currentColors.ElementAt(currentColors.Count - 1)));

                        // And ban it elsewhere
                        for (int y = 0; y < outputHeight - 1; y++)
                        {
                            dbPropagator?.ban(0, y, 0, new Tile(currentColors.ElementAt(currentColors.Count - 1)));
                        }
                    }
                }

                inputHasChanged = false;
            }

            //TODO
            //form.displayLoading(false);

            bool decided;
            (latestOutput, decided) = runWfcDB(steps);
            return (latestOutput, decided);
        }

        private (Bitmap, bool) runWfcDB(int steps)
        {
            Stopwatch sw = new();
            sw.Restart();
            Resolution dbStatus = Resolution.UNDECIDED;
            if (steps == -1)
            {
                dbStatus = dbPropagator.run();
            }
            else
            {
                for (int i = 0; i < steps; i++)
                {
                    dbStatus = dbPropagator.step();
                    currentStep++;
                }
            }

#if (DEBUG)
            Trace.WriteLine(@$"Stepping forward took {sw.ElapsedMilliseconds}ms.");
#endif
            sw.Restart();

            Bitmap outputBitmap;
            int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;

            if (isOverlappingModel())
            {
                outputBitmap = new Bitmap(outputWidth, outputHeight);
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < outputHeight; y++)
                {
                    for (int x = 0; x < outputWidth; x++)
                    {
                        Color cur = dbOutput.get(x, y); // TODO Check if colour is made from multiple?
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            }
            else
            {
                outputBitmap = new Bitmap(outputWidth * tileSize, outputHeight * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < outputHeight; y++)
                {
                    for (int x = 0; x < outputWidth; x++)
                    {
                        int value = dbOutput.get(x, y);
                        Color[] outputPattern = value >= 0
                            ? tileCache.ElementAt(value).Value.Item1
                            : Enumerable.Repeat(Color.Silver, tileSize * tileSize).ToArray();

                        for (int yy = 0; yy < tileSize; yy++)
                        {
                            for (int xx = 0; xx < tileSize; xx++)
                            {
                                Color cur = outputPattern[yy * tileSize + xx];
                                outputBitmap.SetPixel(x * tileSize + xx, y * tileSize + yy,
                                    cur);
                            }
                        }
                    }
                }
            }

#if (DEBUG)
            Trace.WriteLine(@$"Bitmap took {sw.ElapsedMilliseconds}ms. {dbStatus}");
#endif
            return (outputBitmap, dbStatus == Resolution.DECIDED);
        }

        public Bitmap stepBackWfc(int steps)
        {
            Stopwatch sw = new();
            sw.Restart();
            for (int i = 0; i < steps; i++)
            {
                dbPropagator.doBacktrack();
            }

#if (DEBUG)
            Trace.WriteLine(@$"Stepping back took {sw.ElapsedMilliseconds}ms.");
#endif
            sw.Restart();

            Bitmap outputBitmap;
            int outputHeight = mainWindowVM.ImageOutHeight, outputWidth = mainWindowVM.ImageOutWidth;

            if (isOverlappingModel())
            {
                outputBitmap = new Bitmap(outputWidth, outputHeight);
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < outputHeight; y++)
                {
                    for (int x = 0; x < outputWidth; x++)
                    {
                        Color cur = dbOutput.get(x, y);
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            }
            else
            {
                outputBitmap = new Bitmap(outputWidth * tileSize, outputHeight * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < outputHeight; y++)
                {
                    for (int x = 0; x < outputWidth; x++)
                    {
                        int value = dbOutput.get(x, y);
                        Color[] outputPattern = value >= 0
                            ? tileCache.ElementAt(value).Value.Item1
                            : Enumerable.Repeat(Color.Silver, tileSize * tileSize).ToArray();
                        for (int yy = 0; yy < tileSize; yy++)
                        {
                            for (int xx = 0; xx < tileSize; xx++)
                            {
                                Color cur = outputPattern[yy * tileSize + xx];
                                outputBitmap.SetPixel(x * tileSize + xx, y * tileSize + yy, cur);
                            }
                        }
                    }
                }
            }

#if (DEBUG)
            Trace.WriteLine(@$"Bitmap took {sw.ElapsedMilliseconds}ms.");
#endif

            return outputBitmap;
        }

        //TODO
        /*
        public Bitmap resizePixels(PictureBox pictureBox, Bitmap bitmap, int padding, Color borderColor,
            bool drawLines)
        {
            int w2 = pictureBox.Width, h2 = pictureBox.Height, w1 = bitmap.Width, h1 = bitmap.Height;

            Bitmap outputBM = new(pictureBox.Width, pictureBox.Height);
            double xRatio = w1 / (double)(w2 - padding * 2);
            double yRatio = h1 / (double)(h2 - padding * 2);

            try
            {
                for (int i = 0; i < h2 - padding; i++)
                {
                    int py = (int)Math.Floor(i * yRatio);
                    for (int j = 0; j < w2 - padding; j++)
                    {
                        int px = (int)Math.Floor(j * xRatio);
                        Color nextC;
                        if (px >= bitmap.Width || py >= bitmap.Height)
                        {
                            nextC = borderColor;
                        }
                        else if (drawLines && mainWindowVM.isOverlappingModel() && w1 != 2 &&
                          (i % ((h2 - padding) / h1) == 0 && i != 0 ||
                              j % ((w2 - padding) / w1) == 0 && j != 0))
                        {
                            Color c1 = Color.Gray;
                            Color c2 = bitmap.GetPixel(px, py);
                            nextC = Color.FromArgb((c1.A + c2.A) / 2, (c1.R + c2.R) / 2, (c1.G + c2.G) / 2,
                                (c1.B + c2.B) / 2);
                        }
                        else
                        {
                            nextC = bitmap.GetPixel(px, py);
                        }

                        outputBM.SetPixel(j + padding - padding, i + padding - padding, nextC);
                    }
                }
            }
            catch (DivideByZeroException) { }

            return outputBM;
        }*/

        private static HashSet<Color> currentColors;

        private static Color[][] imageToColourArray(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            byte[] bytes = new byte[height * data.Stride];
            try
            {
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            Color[][] result = new Color[height][];
            currentColors = new HashSet<Color>();
            for (int y = 0; y < height; ++y)
            {
                result[y] = new Color[width];
                for (int x = 0; x < width; ++x)
                {
                    int offset = y * data.Stride + x * 3;
                    Color c = Color.FromArgb(255, bytes[offset + 2], bytes[offset + 1], bytes[offset + 0]);
                    result[y][x] = c;
                    currentColors.Add(c);
                }
            }

            return result;
        }

        public Bitmap resizeBitmap(Bitmap source, float scale)
        {
            int width, height;
            if (isOverlappingModel())
            {
                width = source.Width * (int)scale;
                height = source.Height * (int)scale;
            }
            else
            {
                width = (int)(source.Width * scale);
                height = (int)(source.Height * scale);
            }

            Bitmap bmp = new(width, height);

            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.DarkGray);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, width, height));

            Pen semiTransPen = new(Color.FromArgb(100, 20, 20, 20), 1);

            if (isOverlappingModel())
            {
                for (int i = 0; i < source.Width; i++)
                {
                    // Vertical gridlines
                    g.DrawLine(semiTransPen, i * (int)scale, 0, i * (int)scale, source.Height * (int)scale);
                }

                for (int i = 0; i < source.Height; i++)
                {
                    // Horizontal gridlines
                    g.DrawLine(semiTransPen, 0, i * (int)scale, source.Width * (int)scale, i * (int)scale);
                }
            }

            g.Save();

            return bmp;
        }

        /*
         * Getters
         */

        public int getCurrentStep()
        {
            return currentStep;
        }

        public void setCurrentStep(int newCurStep)
        {
            currentStep = newCurStep;
        }

        public Bitmap getLatestOutput()
        {
            return latestOutput;
        }

        public bool isCollapsed()
        {
            return dbPropagator.Status == Resolution.DECIDED;
        }

        public bool isOverlappingModel()
        {
            return mainWindowVM.ModelSelectionText.Contains("Ti");
        }

        public Bitmap setTile(int a, int b, int toSet)
        {
            dbPropagator?.select(a, b, 0,
                new Tile(currentColors.ElementAt(toSet)));

            Bitmap outputBitmap;
            int outputWidth = mainWindowVM.ImageOutWidth, outputHeight = mainWindowVM.ImageOutHeight;

            if (isOverlappingModel())
            {
                outputBitmap = new Bitmap(outputWidth, outputHeight);
                ITopoArray<Color> dbOutput = dbPropagator.toValueArray<Color>();
                for (int y = 0; y < outputHeight; y++)
                {
                    for (int x = 0; x < outputWidth; x++)
                    {
                        Color cur = dbOutput.get(x, y); // TODO Check if colour is made from multiple?
                        outputBitmap.SetPixel(x, y, currentColors.Contains(cur) ? cur : Color.Silver);
                    }
                }
            }
            else
            {
                outputBitmap = new Bitmap(outputWidth * tileSize, outputHeight * tileSize);
                ITopoArray<int> dbOutput = dbPropagator.toValueArray(-1, -2);
                for (int y = 0; y < outputHeight; y++)
                {
                    for (int x = 0; x < outputWidth; x++)
                    {
                        int value = dbOutput.get(x, y);
                        Color[] outputPattern = value >= 0
                            ? tileCache.ElementAt(value).Value.Item1
                            : Enumerable.Repeat(Color.Silver, tileSize * tileSize).ToArray();

                        for (int yy = 0; yy < tileSize; yy++)
                        {
                            for (int xx = 0; xx < tileSize; xx++)
                            {
                                Color cur = outputPattern[yy * tileSize + xx];
                                outputBitmap.SetPixel(x * tileSize + xx, y * tileSize + yy,
                                    cur);
                            }
                        }
                    }
                }
            }

            return outputBitmap;
        }

        public (int[], int) getImagePatternDimensions(string imageName)
        {
            if (xDoc.Root == null)
            {
                return (Array.Empty<int>(), -1);
            }
            IEnumerable<XElement> xElements = xDoc.Root.Elements("simpletiled").Concat(xDoc.Root.Elements("overlapping"));
            IEnumerable<int> matchingElements = xElements.Where(x =>
                (x.Attribute("name")?.Value ?? "") == imageName).Select(t =>
                int.Parse(t.Attribute("patternSize")?.Value ?? "3"));

            List<int> patternDimensionsList = new();
            int j = 0;
            for (int i = 2; i < 6; i++)
            {
                if (i >= 4 && !matchingElements.Contains(5) && !matchingElements.Contains(4))
                {
                    break;
                }

                patternDimensionsList.Add(i);
                if (j == 0 && matchingElements.Contains(i))
                {
                    j = i;
                }
            }

            return (patternDimensionsList.ToArray(), j - 2);
        }

        public string[] getImages(string modelType, string category)
        {
            List<string> images = new();
            if (xDoc.Root != null)
            {
                images = xDoc.Root.Elements(modelType)
                    .Where(xElement => (xElement.Attribute("category")?.Value ?? "").Equals(category))
                    .Select(xElement => xElement.Attribute("name")?.Value ?? "").ToList();
            }

            images.Sort();

            return images.Distinct().ToArray();
        }

        public static Bitmap getImage(string name)
        {
            return new Bitmap($"samples/{name}.png");
        }

        public void updateInputImage(string newImage)
        {
            string uRI = $"samples/{newImage}.png";
            mainWindowVM.InputImage = new Avalonia.Media.Imaging.Bitmap(uRI);
        }

        //TODO Maybe re-enable
        // private bool transformationIsEnabled(int i) {
        //     if (i == 0) {
        //         return true;
        //     }
        //
        //     PictureBox currentPB = form.pbs[i - 1];
        //     return currentPB.BackColor.Equals(Color.LawnGreen);
        // }

        /*
         * Setters
         */

        public void setInputChanged(string source)
        {
            //TODO
            //if (!form.isChangingModels)
            //{
#if (DEBUG)
            Trace.WriteLine(@$"Input changed on {source}");
#endif
            inputHasChanged = true;
            //}
        }

        /*
         * Pattern Adaptation Simple
         */

        private static Color[] imTile(Func<int, int, Color> f, int tilesize)
        {
            Color[] result = new Color[tilesize * tilesize];
            for (int y = 0; y < tilesize; y++)
            {
                for (int x = 0; x < tilesize; x++)
                {
                    result[x + y * tilesize] = f(x, y);
                }
            }

            return result;
        }

        private static Color[] rotate(IReadOnlyList<Color> array, int tilesize)
        {
            return imTile((x, y) => array[tilesize - 1 - y + x * tilesize], tilesize);
        }

        private static Color[] reflect(IReadOnlyList<Color> array, int tilesize)
        {
            return imTile((x, y) => array[tilesize - 1 - x + y * tilesize], tilesize);
        }

        public static string[] getCategories(string modelType)
        {
            return modelType.Equals("overlapping")
                ? new[] { "Textures", "Shapes", "Knots", "Fonts", "Worlds Side-View", "Worlds Top-Down" }
                : new[] { "Worlds Top-Down", "Textures" };
        }
    }
}