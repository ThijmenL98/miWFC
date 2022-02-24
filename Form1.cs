﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using WFC4All.DeBroglie.Models;

namespace WFC4All {
    public partial class Form1 : Form {
        private readonly InputManager inputManager;
        public readonly BitMaps bitMaps;
        public readonly PictureBox[] pbs;
        private int defaultSymmetry;
        private readonly int initOutWidth;
        private readonly int initInWidth;
        private readonly int initOutHeight;
        private readonly int initInHeight;
        private bool defaultPeriodicity;
        public bool isChangingModels;

        private readonly XDocument xDoc;

        public Form1() {
            xDoc = XDocument.Load("samples.xml");
            defaultSymmetry = 8;
            isChangingModels = false;
            defaultPeriodicity = true;
            inputManager = new InputManager(this);
            bitMaps = new BitMaps(this);
            InitializeComponent();

            initInWidth = inputImagePB.Width;
            initInHeight = inputImagePB.Height;
            initOutWidth = resultPB.Width;
            initOutHeight = resultPB.Height;

            pbs = new[] {p1RotPB, p2RotPB, p3RotPB};

            string[] inputImageDataSource = inputManager.getImages("overlapping"); // or "simpletiled"
            inputImageCB.DataSource = inputImageDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(inputImageDataSource[0]);
            patternSize.SelectedText = inputImageDataSource[0];

            (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(inputImageDataSource[0]);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

            initializeRotations();

            advanceButton.Visible = false;
            backButton.Visible = false;
            animateButton.Visible = false;
            animationSpeedLabel.Visible = false;
            animationSpeedValue.Visible = false;
        }

        private int lastRanForced;

        private void executeButton_Click(object sender, EventArgs e) {
            if (changingIndex) {
                return;
            }

            int now = DateTime.Now.Millisecond;

            if (sender == null && now - lastRanForced <= 50) {
                lastRanForced = now;
                return;
            }

            try {
                (Bitmap result, bool _) = inputManager.initAndRunWfcDB(true, getStepAmount());
                while (result == null) {
                    (result, _) = inputManager.initAndRunWfcDB(true, getStepAmount());
                }

                try {
                    resultPB.Image = InputManager.resizeBitmap(result,
                        Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));
                } catch (Exception e1) {
                    Console.WriteLine(e1);
                }

                result.Dispose();
            } catch (Exception exception) {
                Console.WriteLine(exception);
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void advanceButton_Click(object sender, EventArgs e) {
            int now = DateTime.Now.Millisecond;

            if (sender == null && now - lastRanForced <= 50) {
                lastRanForced = now;
                return;
            }

            try {
                (Bitmap result, bool finished) = inputManager.initAndRunWfcDB(false, getStepAmount());
                if (finished) {
                    return;
                }

                resultPB.Image = InputManager.resizeBitmap(result,
                    Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));

                result.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void backButton_Click(object sender, EventArgs e) {
            try {
                Bitmap result = inputManager.stepBackWfc(getStepAmount());
                resultPB.Image = InputManager.resizeBitmap(result,
                    Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));

                result.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void animateButton_Click(object sender, EventArgs e) {
            int now = DateTime.Now.Millisecond;

            if (sender == null && now - lastRanForced <= 50) {
                lastRanForced = now;
                return;
            }

            try {
                bool finished = false;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                while (!finished) {
                    Bitmap result;
                    (result, finished) = inputManager.initAndRunWfcDB(false, getStepAmount());

                    resultPB.Image = InputManager.resizeBitmap(result,
                        Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));

                    result.Dispose();
                    resultPB.Refresh();
                    Thread.Sleep(getAnimationSpeed());
                    if (finished) {
                        return;
                    }
                }
            } catch (Exception exception) {
                Console.WriteLine(exception);
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        public int getSelectedOverlapTileDimension() {
            return patternSize.SelectedIndex + 2;
        }

        public bool getPeriodicEnabled() {
            return periodicInput.Checked;
        }

        private bool changingIndex;

        private void inputImage_SelectedIndexChanged(object sender, EventArgs e) {
            changingIndex = true;
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            updateInputImage(newValue);

            if (modelChoice.Text.Equals("Overlapping Model")) {
                XElement curSelection = xDoc.Root.elements("overlapping").Where(x =>
                    x.get<string>("name") == getSelectedInput()).ElementAtOrDefault(0);
                (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(newValue);
                patternSize.DataSource = patternSizeDataSource;
                patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

                defaultSymmetry = curSelection.get("symmetry", 8);
                defaultPeriodicity = curSelection.get("periodicInput", true);
            }

            Refresh();

            updateRotations();
            updatePeriodicity();

            changingIndex = false;
            inputManager.setInputChanged("Changing image");
            executeButton_Click(null, null);
        }

        private void outputSizeChanged(object sender, EventArgs e) {
            inputManager.setSizeChanged();
        }

        public int getOutputWidth() {
            return (int) outputWidthValue.Value;
        }

        private int getStepAmount() {
            return (int) stepValue.Value;
        }

        private int getAnimationSpeed() {
            return (int) animationSpeedValue.Value;
        }

        public int getOutputHeight() {
            return (int) outputHeightValue.Value;
        }

        private void updateInputImage(string imageName) {
            Bitmap newImage = InputManager.getImage(imageName);

            inputImagePB.Image = InputManager.resizeBitmap(newImage,
                Math.Min(initInHeight / (float) newImage.Height, initInWidth / (float) newImage.Width));

            inputImagePB.Refresh();
        }

        public string getSelectedInput() {
            return inputImageCB.SelectedItem.ToString();
        }

        private void modelChoice_Click(object sender, EventArgs e) {
            isChangingModels = true;
            Button btn = (Button) sender;

            outputHeightValue.Value = 24;
            outputWidthValue.Value = 24;

            btn.Text = btn.Text.Equals("Overlapping Model") ? "Simple Model" : "Overlapping Model";
            showRotationalOptions(btn.Text.Equals("Overlapping Model"));

            string[] images
                = inputManager.getImages(btn.Text.Equals("Overlapping Model") ? "overlapping" : "simpletiled");

            inputImageCB.DataSource = images;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(images[0]);
            patternSize.SelectedText = images[0];

            (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(images[0]);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

            isChangingModels = false;
            inputManager.setInputChanged("Model change");
            executeButton_Click(null, null);
        }

        public bool addPattern(Bitmap bitmap) {
            return bitMaps.addPattern(bitmap, pictureBoxMouseDown);
        }

        public bool addPattern(PatternArray pixels, List<Color> distinctColors) {
            return bitMaps.addPattern(pixels, distinctColors, pictureBoxMouseDown);
        }

        public bool isOverlappingModel() {
            return modelChoice.Text.Equals("Overlapping Model");
        }

        private void inputChanged(object sender, EventArgs e) {
            if (!Visible) {
                return;
            }

            inputManager.setInputChanged(sender.GetType().ToString());
            executeButton_Click(null, null);
        }

        private static void pictureBoxMouseDown(object sender, MouseEventArgs e) {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left) {
                PictureBox pb = (PictureBox) sender;
                pb.BackColor = pb.BackColor.Equals(Color.LawnGreen) ? Color.Red : Color.LawnGreen;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void addHover(object sender, EventArgs e, string message) {
            ToolTip toolTip = new();

            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 0;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;

            toolTip.SetToolTip((PictureBox) sender, message);
        }

        private void updatePeriodicity() {
            periodicInput.Checked = defaultPeriodicity;
        }

        private void updateRotations() {
            for (int i = 0; i < 3; i++) {
                pbs[i].BackColor = Math.Ceiling(defaultSymmetry / 2f) - 1 <= i ? Color.Red : Color.LawnGreen;
            }
        }

        private void showRotationalOptions(bool hide) {
            for (int i = 0; i < 3; i++) {
                pbs[i].Visible = hide;
            }

            originalRotPB.Visible = hide;
            patternRotationLabel.Visible = hide;
            periodicInput.Visible = hide;
            patternSize.Visible = hide;
            patternSizeLabel.Visible = hide;
        }

        private void initializeRotations() {
            Bitmap referenceImg = new("rotationRef.png");
            const int padding = 3;
            originalRotPB.BackColor = Color.Black;
            originalRotPB.Padding = new Padding(padding);
            originalRotPB.Image = referenceImg;

            string[] rfs = {
                "rotationRef1.png", "rotationRef2.png", "rotationRef3.png"
            };
            string[] rfsString = {
                "Rotate Clockwise 90°\nand Horizontally Flipped", "Rotate 180°\nand Horizontally Flipped",
                "Rotate Clockwise 270°\nand Horizontally Flipped"
            };
            for (int i = 0; i < 3; i++) {
                pbs[i].BackColor = Math.Ceiling(defaultSymmetry / 2f) - 1 <= i ? Color.Red : Color.LawnGreen;
                pbs[i].Padding = new Padding(padding);
                pbs[i].Image = new Bitmap(rfs[i]);
                pbs[i].MouseDown += pictureBoxMouseDown;
                int nonLocalI = i;
                pbs[i].MouseHover += (sender, eventArgs) => { addHover(sender, eventArgs, rfsString[nonLocalI]); };
            }
        }

        private void stepCountValueChanged(object sender, EventArgs e) {
            int value = (int) stepValue.Value;
            if (value is 0 or -1) {
                stepValue.Value = -1;
            }

            advanceButton.Visible = value is not (0 or -1);
            backButton.Visible = value is not (0 or -1);
            animateButton.Visible = value is not (0 or -1);
            animationSpeedLabel.Visible = value is not (0 or -1);
            animationSpeedValue.Visible = value is not (0 or -1);

            advanceButton.Text = $@"Advance {stepValue.Value}";
            backButton.Text = $@"Step {stepValue.Value} Back";
        }
    }

    public class BitMaps {
        private readonly Form1 myForm;

        private HashSet<ImageR> curBitmaps;
        private Dictionary<int, List<Bitmap>> similarityMap;
        private List<PictureBox> pictureBoxes;
        private Dictionary<string, Tuple<List<PictureBox>, int>> cache;

        public BitMaps(Form1 form) {
            myForm = form;
            patternCount = 0;
            curBitmaps = new HashSet<ImageR>();
            similarityMap = new Dictionary<int, List<Bitmap>>();
            cache = new Dictionary<string, Tuple<List<PictureBox>, int>>();
            pictureBoxes = new List<PictureBox>();
        }

        private int patternCount;
        private int curFloorIndex;

        public int getPatternCount() {
            return patternCount;
        }

        public void reset() {
            patternCount = 0;
            curFloorIndex = 0;
            curBitmaps = new HashSet<ImageR>();
            similarityMap = new Dictionary<int, List<Bitmap>>();
            pictureBoxes = new List<PictureBox>();
        }

        public void saveCache() {
            cache[myForm.getSelectedInput()] = new Tuple<List<PictureBox>, int>(pictureBoxes.ToList(), curFloorIndex);
        }

        public int getFloorIndex(int patternSize) {
            return curFloorIndex == 0 ? 0 : curFloorIndex - patternSize;
        }

        public bool addPattern(PatternArray colors, List<Color> distinctColors, MouseEventHandler pictureBoxMouseDown) {
            if (cache.ContainsKey(myForm.getSelectedInput())) {
                foreach (PictureBox pb in cache[myForm.getSelectedInput()].Item1) {
                    myForm.patternPanel.Controls.Add(pb);
                }

                patternCount = cache[myForm.getSelectedInput()].Item1.Count;
                curFloorIndex = cache[myForm.getSelectedInput()].Item2;
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

                    if (y == 0 && colorIdx != distinctColors.Count - 1) {
                        isGroundPattern = false;
                    }

                    if (y != 0 && colorIdx != 0) {
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
                curFloorIndex = patternCount;
            }

            const int padding = 3;
            newPB.Image = InputManager.resizePixels(newPB, pattern, pattern.Width, pattern.Height, size, size, padding);
            newPB.BackColor = Color.DimGray; //TODO Re-Enable for CF Color.LawnGreen;
            newPB.Padding = new Padding(padding);
            //newPB.MouseDown += pictureBoxMouseDown;

            pictureBoxes.Add(newPB);
            myForm.patternPanel.Controls.Add(newPB);

            patternCount++;
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
            if (cache.ContainsKey(myForm.getSelectedInput())) {
                foreach (PictureBox pb in cache[myForm.getSelectedInput()].Item1) {
                    myForm.patternPanel.Controls.Add(pb);
                }

                patternCount = cache[myForm.getSelectedInput()].Item1.Count;
                curFloorIndex = cache[myForm.getSelectedInput()].Item2;
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
            newPB.Image = InputManager.resizePixels(newPB, pattern, pattern.Width, pattern.Height, size, size, padding);
            newPB.BackColor = Color.LawnGreen;
            newPB.Padding = new Padding(padding);
            newPB.MouseDown += pictureBoxMouseDown;

            pictureBoxes.Add(newPB);
            myForm.patternPanel.Controls.Add(newPB);
            patternCount++;
            return false;
        }

        private record ImageR(Size Size, Dictionary<Point, Color> Data) {
            public Size Size { get; } = Size;
            public Dictionary<Point, Color> Data { get; } = Data;
        }
    }
}