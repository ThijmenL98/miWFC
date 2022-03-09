using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WFC4All.DeBroglie.Models;

namespace WFC4All {
    public class BitMaps {
        private readonly Form1 myForm;

        private HashSet<ImageR> curBitmaps;
        private Dictionary<int, List<Bitmap>> similarityMap;
        private List<PictureBox> pictureBoxes;
        private readonly Dictionary<Tuple<string, int, bool>, Tuple<List<PictureBox>, int>> cache;
        private readonly InputManager inputManager;

        public BitMaps(Form1 form, InputManager inputManager) {
            myForm = form;
            patternCount = 0;
            curBitmaps = new HashSet<ImageR>();
            similarityMap = new Dictionary<int, List<Bitmap>>();
            cache = new Dictionary<Tuple<string, int, bool>, Tuple<List<PictureBox>, int>>();
            pictureBoxes = new List<PictureBox>();
            this.inputManager = inputManager;
        }

        private int patternCount;
        private int totalPatternCount;
        private int curFloorIndex;

        public int getPatternCount() {
            return patternCount;
        }

        public void reset() {
            patternCount = 0;
            totalPatternCount = 0;
            curFloorIndex = 0;
            curBitmaps = new HashSet<ImageR>();
            similarityMap = new Dictionary<int, List<Bitmap>>();
            pictureBoxes = new List<PictureBox>();
        }

        public void saveCache() {
            Tuple<string, int, bool> key
                = new(myForm.getSelectedInput(), myForm.getSelectedOverlapTileDimension(),
                    myForm.inputPaddingEnabled());
            cache[key] = new Tuple<List<PictureBox>, int>(pictureBoxes.ToList(), curFloorIndex);
        }

        //TODO Maybe re-enable?
        // public int getFloorIndex() {
        //     return curFloorIndex == 0 ? 0 : curFloorIndex;
        // }

        public bool addPattern(PatternArray colors, List<Color> distinctColors, MouseEventHandler pictureBoxMouseDown) {
            Tuple<string, int, bool> key
                = new(myForm.getSelectedInput(), myForm.getSelectedOverlapTileDimension(),
                    myForm.inputPaddingEnabled());
            if (cache.ContainsKey(key)) {
                foreach (PictureBox pb in cache[key].Item1) {
                    myForm.patternPanel.Controls.Add(pb);
                }

                patternCount = cache[key].Item1.Count;
                curFloorIndex = cache[key].Item2;
                return true;
            }

            const int size = 50;
            const int patternsPerRow = 6;
            PictureBox newPB = new();
            const int distance = 20;

            int pPerRow = (int) Math.Floor((myForm.patternPanel.Width - distance) / (float) (distance + size));
            int spacing = (myForm.patternPanel.Width - size * pPerRow) / (pPerRow + 3);

            int idxX = patternCount % pPerRow;
            int idxY = (int) Math.Floor(patternCount / (double) pPerRow);

            newPB.Location = new Point(size + (size + spacing) * idxX - spacing/2, size + 30 + (size + distance) * idxY);
            newPB.Size = new Size(size, size);
            newPB.Name = "patternPB_" + patternCount;

            int n = colors.Height;
            Bitmap pattern = new(n, n);

            bool isGroundPattern = true;
            Dictionary<Point, Color> data = new();

            for (int x = 0; x < n; x++) {
                for (int y = 0; y < n; y++) {
                    Color tileColor = (Color) colors.getTileAt(x, y).Value;
                    pattern.SetPixel(x, y, tileColor);
                    int colorIdx = distinctColors.IndexOf(tileColor);
                    data[new Point(x, y)] = tileColor;

                    if (y != n - 1 && colorIdx != 0) {
                        isGroundPattern = false;
                    }

                    if (y == n - 1 && colorIdx != distinctColors.Count - 1) {
                        isGroundPattern = false;
                    }
                }
            }

            ImageR cur = new(pattern.Size, data);

            foreach ((ImageR reference, int i) in curBitmaps.Select((reference, i) => (reference, i))) {
                if (transforms.Select(transform => cur.Data.All(x =>
                        x.Value == reference.Data[
                            transform.Invoke(cur.Size.Width - 1, cur.Size.Height - 1, x.Key.X, x.Key.Y)]))
                    .Any(match => match)) {
                    similarityMap[i].Add(pattern);
                    return false;
                }
            }

            curBitmaps.Add(cur);
            similarityMap[patternCount] = new List<Bitmap> {pattern};

            if (isGroundPattern) {
                curFloorIndex = totalPatternCount;
            }

            const int padding = 3;
            newPB.Image = inputManager.resizePixels(newPB, pattern, padding, Color.DimGray, true);
            pattern.Dispose();
            newPB.BackColor = Color.DimGray; //TODO Re-Enable for CF Color.LawnGreen;
            newPB.Padding = new Padding(padding);
            //newPB.MouseDown += pictureBoxMouseDown;

            pictureBoxes.Add(newPB);
            myForm.patternPanel.Controls.Add(newPB);

            patternCount++;
            totalPatternCount++;
            return false;
        }

        private readonly List<Func<int, int, int, int, Point>> transforms = new() {
            (_, _, x, y) => new Point(x, y), // rotated 0
            (w, _, x, y) => new Point(w - y, x), // rotated 90
            (w, h, x, y) => new Point(w - x, h - y), // rotated 180
            (_, h, x, y) => new Point(y, h - x), // rotated 270
            (w, _, x, y) => new Point(w - x, y), // rotated 0 and mirrored
            (w, _, x, y) => new Point(w - (w - y), x), // rotated 90 and mirrored
            (w, h, x, y) => new Point(w - (w - x), h - y), // rotated 180 and mirrored
            (w, h, x, y) => new Point(w - y, h - x), // rotated 270 and mirrored
        };

        public bool addPattern(Bitmap pattern, MouseEventHandler pictureBoxMouseDown) {
            Tuple<string, int, bool> key
                = new(myForm.getSelectedInput(), myForm.getSelectedOverlapTileDimension(),
                    myForm.inputPaddingEnabled());
            if (cache.ContainsKey(key)) {
                foreach (PictureBox pb in cache[key].Item1) {
                    myForm.patternPanel.Controls.Add(pb);
                }

                patternCount = cache[key].Item1.Count;
                curFloorIndex = cache[key].Item2;
                return true;
            }

            const int size = 50;
            const int patternsPerRow = 6;
            PictureBox newPB = new();

            int idxX = patternCount % patternsPerRow;
            int idxY = (int) Math.Floor(patternCount / (double) patternsPerRow);
            const int distance = 20;

            newPB.Location = new Point(size + (size + distance) * idxX, size + 30 + (size + distance) * idxY);
            newPB.Size = new Size(size, size);
            newPB.Name = "patternPB_" + patternCount;

            const int padding = 3;
            newPB.Image = inputManager.resizePixels(newPB, pattern, padding, Color.DimGray, true);
            pattern.Dispose();
            newPB.BackColor = Color.DimGray;
            newPB.Padding = new Padding(padding);
            newPB.MouseDown += pictureBoxMouseDown;

            pictureBoxes.Add(newPB);
            myForm.patternPanel.Controls.Add(newPB);
            patternCount++;
            totalPatternCount++;
            return false;
        }

        private record ImageR(Size Size, Dictionary<Point, Color> Data) {
            public Size Size { get; } = Size;
            public Dictionary<Point, Color> Data { get; } = Data;
        }
    }
}