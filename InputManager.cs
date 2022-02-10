using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WFC4All {
    public class InputManager {
        private static XDocument xDoc;

        public InputManager() {
            xDoc = XDocument.Load("samples.xml");
        }

        public static Bitmap runWfc(Form1 form) {
            Stopwatch sw = Stopwatch.StartNew();

            Random random = new Random();

            XElement xElem = xDoc.Root.Elements("overlapping", "simpletiled").Where(x => x.Get<string>("name") == form.getSelectedInput()).ElementAtOrDefault(0);

            Model model;
            string name = xElem.Get<string>("name");
            Console.WriteLine($@"< {name}");

            bool isOverlapping = xElem != null && xElem.Name == "overlapping";
            int width = form.getOutputWidth();
            int height = form.getOutputHeight();

            bool periodic = xElem.Get("periodic", false);
            string heuristicString = xElem.Get<string>("heuristic");
            Model.Heuristic heuristic = heuristicString == "Scanline"
                ? Model.Heuristic.SCANLINE
                : heuristicString == "MRV"
                    ? Model.Heuristic.MRV
                    : Model.Heuristic.ENTROPY;

            if (isOverlapping) {
                int overlapTileDimension = form.getSelectedOverlapTileDimension();
                bool periodicInput = xElem.Get("periodicInput", true);
                int symmetry = xElem.Get("symmetry", 8);
                int ground = xElem.Get("ground", 0);

                model = new OverlappingModel(name, overlapTileDimension, width, height, periodicInput, periodic,
                    symmetry, ground, heuristic, form);
            } else {
                string subset = xElem.Get<string>("subset");
                bool blackBackground = xElem.Get("blackBackground", false);

                model = new SimpleTiledModel(name, subset, width, height, periodic, blackBackground, heuristic, form);
            }

            for (int i = 0; i < xElem.Get("screenshots", 2); i++) {
                for (int k = 0; k < 10; k++) {
                    Console.Write(@"> ");
                    int seed = random.Next();
                    bool success = model.Run(seed, xElem.Get("limit", -1));

                    if (success) {
                        Console.WriteLine(@"DONE");

                        Console.WriteLine($@"time = {sw.ElapsedMilliseconds}");
                        return model.Graphics();
                    }

                    Console.WriteLine(@"CONTRADICTION");
                }
            }

            Console.WriteLine($@"time = {sw.ElapsedMilliseconds}");
            return null;
        }

        public static List<string> availableImages() {
            return xDoc.Root.Elements("overlapping").Select(xElement => xElement.Get<string>("name"))
                .ToList();
        }

        public static string[] getImages() {
            return availableImages().ToArray();
        }

        public static Bitmap getImage(String name) {
            return new Bitmap($"samples/{name}.png");
        }

        public static object[] getImagePatternDimensions(string imageName) {
            IEnumerable<XElement> xElements = xDoc.Root.Elements("overlapping", "simpletiled");
            IEnumerable<int> matchingElements = xElements.Where(x => x.Get<string>("name") == imageName).Select(t => t.Get("N", 3));

            List<object> patternDimensionsList = new List<object>();
            for (int i = 2; i < 6; i++) {
                String append = matchingElements.Contains(i) ? " (recommended)" : "";
                patternDimensionsList.Add("  " + i + append);
            }

            return patternDimensionsList.ToArray();
        }

        public static Bitmap resizePixels(PictureBox pictureBox, Bitmap bitmap, int w1, int h1, int w2, int h2) {
            int marginX = (int) Math.Floor((pictureBox.Width - w2) / 2d);
            int marginY = (int) Math.Floor((pictureBox.Height - h2) / 2d);

            Bitmap temp = new Bitmap(pictureBox.Width, pictureBox.Height);
            double xRatio = w1 / (double) w2;
            double yRatio = h1 / (double) h2;

            for (int x = 0; x < pictureBox.Width; x++) {
                for (int y = 0; y < pictureBox.Height; y++) {
                    if (y <= marginY || x <= marginX || y >= pictureBox.Height - marginY ||
                        x >= pictureBox.Width - marginX) {
                        temp.SetPixel(x, y, Color.DarkGray);
                    } else {
                        // Skip ahead horizontally
                        y = pictureBox.Height - marginY - 1;
                    }
                }
            }

            for (int i = 0; i < h2; i++) {
                for (int j = 0; j < w2; j++) {
                    int px = (int) Math.Floor(j * xRatio);
                    int py = (int) Math.Floor(i * yRatio);
                    temp.SetPixel(j + marginX, i + marginY, bitmap.GetPixel(px, py));
                }
            }

            return temp;
        }
    }
}