using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using WFC4All.DeBroglie.Models;
using WFC4All.enums;
using WFC4All.Properties;

namespace WFC4All {
    public partial class Form1 : Form {
        private readonly InputManager inputManager;
        public readonly BitMaps bitMaps;
        public readonly PictureBox[] pbs;
        private int defaultSymmetry, savePoint;
        private readonly int initOutWidth, initInWidth, initOutHeight, initInHeight;
        private bool defaultPeriodicity;
        public bool isChangingModels;

        private Bitmap result;

        private readonly XDocument xDoc;

        private Size oldSize;

        private readonly string[] patternHeuristicDataSource = {"Weighted", "Random", "Least Used"},
            selectionHeuristicDataSource = {"Least Entropy", "Simple", "Lexical", "Random", "Spiral", "Hilbert Curve"};

        public Form1() {
            Icon = Resources.icon;

            xDoc = XDocument.Parse(Resources.samples);
            defaultSymmetry = 8;
            oldSize = new Size(1616, 939);
            result = new Bitmap(1, 1);
            savePoint = 0;
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

            string[] inputImageDataSource = inputManager.getImages("overlapping", "Textures"); // or "simpletiled"
            inputImageCB.DataSource = inputImageDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(inputImageDataSource[0]);
            patternSize.SelectedText = inputImageDataSource[0];

            string[] categoriesDataSource = inputManager.getCategories("overlapping");
            categoryCB.DataSource = categoriesDataSource;
            categoryCB.SelectedIndex = 0;

            selectionHeuristicCB.DataSource = selectionHeuristicDataSource;
            InputManager.setSelectionHeuristic(SelectionHeuristic.ENTROPY);

            patternHeuristicCB.DataSource = patternHeuristicDataSource;
            InputManager.setPatternHeuristic(PatternHeuristic.WEIGHTED);

            (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(inputImageDataSource[0]);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

            initializeRotations();
        }

        protected override void OnResize(EventArgs e) {
            Size minSize = new Size(1616, 939);
            if (Size.Width < minSize.Width) {
                Size = new Size(minSize.Width, Size.Height);
            }

            if (Size.Height < minSize.Height) {
                Size = new Size(Size.Width, minSize.Height);
            }

            base.OnResize(e);

            Control.ControlCollection[] allControls = {
                Controls, inputTab.Controls, patternPanel.Controls, inputPanel.Controls, pattHeurPanel.Controls,
                selHeurPanel.Controls
            };

            foreach (var cList in allControls) {
                foreach (Control cnt in cList) {
                    resizeAll(cnt, Size);
                }
            }

            oldSize = Size;
        }

        private void resizeAll(Control control, Size newSize) {
            int width = newSize.Width - oldSize.Width;
            control.Left += control.Left * width / oldSize.Width;
            control.Width += control.Width * width / oldSize.Width;

            int height = newSize.Height - oldSize.Height;
            control.Top += control.Top * height / oldSize.Height;
            control.Height += control.Height * height / oldSize.Height;
        }

        private int lastRanForced;

        private void executeButton_Click(object sender, EventArgs e) {
            if (changingIndex) {
                return;
            }

            try {
                (Bitmap result2, bool _) = inputManager.initAndRunWfcDB(true, getStepAmount());
                while (result2 == null) {
                    (result2, _) = inputManager.initAndRunWfcDB(true, getStepAmount());
                }

                try {
                    resultPB.Image = InputManager.resizeBitmap(result2,
                        Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result.Width));
                } catch (Exception exception) {
                    Console.WriteLine(exception);
                }

                result = new Bitmap(result2);
                result2.Dispose();
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
                (Bitmap result2, bool finished) = inputManager.initAndRunWfcDB(false, getStepAmount());
                if (finished) {
                    return;
                }

                resultPB.Image = InputManager.resizeBitmap(result2,
                    Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result2.Width));

                result = new Bitmap(result2);
                result2.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void backButton_Click(object sender, EventArgs e) {
            try {
                Bitmap result2 = inputManager.stepBackWfc(getStepAmount());
                resultPB.Image = InputManager.resizeBitmap(result2,
                    Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result2.Width));

                int curStep = inputManager.getCurrentStep();
                if (curStep < savePoint) {
                    savePoint = curStep;
                }

                result = new Bitmap(result2);
                result2.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private Timer myTimer = new();

        private void myTimer_Tick(object sender, EventArgs e) {
            if (InvokeRequired) {
                /* Not on UI thread, reenter there... */
                BeginInvoke(new EventHandler(myTimer_Tick), sender, e);
            } else {
                lock (myTimer) {
                    /* only work when this is no reentry while we are already working */
                    if (myTimer.Enabled) {
                        myTimer.Stop();
                        myTimer.Interval = getAnimationSpeed();
                        try {
                            (Bitmap result2, bool finished) = inputManager.initAndRunWfcDB(false, getStepAmount());
                            resultPB.Image = InputManager.resizeBitmap(result2,
                                Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result2.Width));

                            result = new Bitmap(result2);
                            result2.Dispose();
                            resultPB.Refresh();
                            if (finished) {
                                animateButton.Text = @"Animate";
                                return;
                            }
                        } catch (Exception exception) {
                            Console.WriteLine(exception);
                            resultPB.Image = InputManager.getImage("NoResultFound");
                        }

                        myTimer.Start(); /* optionally restart for periodic work */
                    }
                }
            }
        }

        private void animateButton_Click(object sender, EventArgs e) {
            int now = DateTime.Now.Millisecond;

            if (sender == null && now - lastRanForced <= 50) {
                lastRanForced = now;
                return;
            }

            if (animateButton.Text.Equals("Animate")) {
                lock (myTimer) {
                    myTimer.Interval = getAnimationSpeed();
                    myTimer.Tick += myTimer_Tick;
                    myTimer.Start();
                }
            } else {
                lock (myTimer) {
                    myTimer.Stop();
                    myTimer = new Timer();
                }
            }

            animateButton.Text = animateButton.Text.Equals("Animate") ? "Pause" : "Animate";
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
                = inputManager.getImages(btn.Text.Equals("Overlapping Model") ? "overlapping" : "simpletiled",
                    "Textures");

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
            // TODO: Redo RF1
            // for (int i = 0; i < 3; i++) {
            //     pbs[i].Visible = hide;
            // }
            //
            // originalRotPB.Visible = hide;
            // patternRotationLabel.Visible = hide;
            // periodicInput.Visible = hide;
            patternSize.Visible = hide;
            patternSizeLabel.Visible = hide;
        }

        private void initializeRotations() {
            Bitmap referenceImg = new(Resources.rotationRef);
            const int padding = 3;
            originalRotPB.BackColor = Color.Black;
            originalRotPB.Padding = new Padding(padding);
            originalRotPB.Image = referenceImg;

            Bitmap[] rfs = {
                Resources.rotationRef1, Resources.rotationRef2, Resources.rotationRef3
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
            markerButton.Visible = value is not (0 or -1);
            revertMarkerButton.Visible = value is not (0 or -1);
        }

        private void markerButton_Click(object sender, EventArgs e) {
            savePoint = inputManager.getCurrentStep();
        }

        private void revertMarkerButton_Click(object sender, EventArgs e) {
            int stepsToRevert = inputManager.getCurrentStep() - savePoint;
            try {
                result = inputManager.stepBackWfc(stepsToRevert);
                resultPB.Image = InputManager.resizeBitmap(result,
                    Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));

                markerButton_Click(null, null);

                result.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void resultPB_Click(object sender, EventArgs e) {
            Point clickPos = ((PictureBox) sender).PointToClient(Cursor.Position);
            int clickX = clickPos.X, clickY = clickPos.Y, width = getOutputWidth(), height = getOutputHeight();
            int a = (int) Math.Floor((double) clickX * width / 600d),
                b = (int) Math.Floor((double) clickY * height / 600d);
            Console.WriteLine($@"(x:{clickX}, y:{clickY}) -> (a:{a}, b:{b})");

            //TODO CF2
            // result.SetPixel(a, b, Color.Red);
            //
            // resultPB.Image = InputManager.resizeBitmap(result,
            //     Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));
        }

        private void selectionHeuristicCB_SelectedIndexChanged(object sender, EventArgs e) {
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            switch (Array.FindIndex(selectionHeuristicDataSource, x => x.Contains(newValue))) {
                case 0: // Least Entropy
                    selectionHeuristicPB.Image = Resources.Entropy;
                    selectionHeuristicDesc.Text
                        = @"Select the most logical choice based on available options, solve ties randomly";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.ENTROPY);
                    break;
                case 1: // Simple
                    selectionHeuristicPB.Image = Resources.Simple;
                    selectionHeuristicDesc.Text = @"Similar to least entropy, but solve ties lexically";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.SIMPLE);
                    break;
                case 2: // Lexical
                    selectionHeuristicPB.Image = Resources.Lexical;
                    selectionHeuristicDesc.Text = @"In reading order, starting left to right, top to bottom";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.LEXICAL);
                    break;
                case 3: // Random
                    selectionHeuristicPB.Image = Resources.Random;
                    // ReSharper disable once LocalizableElement
                    selectionHeuristicDesc.Text = "Select randomly\nWarning, Slow!";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.RANDOM);
                    break;
                case 4: // Spiral
                    selectionHeuristicPB.Image = Resources.Spiral;
                    selectionHeuristicDesc.Text = @"Select in an outwards spiral fashion";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.SPIRAL);
                    break;
                case 5: // Hilbert Curve
                    selectionHeuristicPB.Image = Resources.Hilbert;
                    selectionHeuristicDesc.Text = @"Select following a space-filling path";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.HILBERT);
                    break;
                default:
                    return;
            }

            selectionHeuristicDesc.Refresh();
            selectionHeuristicPB.Refresh();
            ((ComboBox) sender).Refresh();

            inputManager.setSizeChanged();
            executeButton_Click(null, null);
        }

        private void patternHeuristicCB_SelectedIndexChanged(object sender, EventArgs e) {
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            switch (Array.FindIndex(patternHeuristicDataSource, x => x.Contains(newValue))) {
                case 0: // Weighted Choice
                    patternHeuristicPB.Image = Resources.Weighted;
                    patternHeuristicDesc.Text = @"Select the next pattern though prominence in the input image";
                    InputManager.setPatternHeuristic(PatternHeuristic.WEIGHTED);
                    break;
                case 1: // Random Choice
                    patternHeuristicPB.Image = Resources.Random;
                    patternHeuristicDesc.Text = @"Randomly select the pattern to use next";
                    InputManager.setPatternHeuristic(PatternHeuristic.RANDOM);
                    break;
                case 2: // Least Used
                    patternHeuristicPB.Image = Resources.LeastUsed;
                    patternHeuristicDesc.Text = @"Select the pattern that has been used the least so far";
                    InputManager.setPatternHeuristic(PatternHeuristic.LEAST_USED);
                    break;
                default:
                    return;
            }

            patternHeuristicDesc.Refresh();
            selectionHeuristicPB.Refresh();
            ((ComboBox) sender).Refresh();

            inputManager.setSizeChanged();
            executeButton_Click(null, null);
        }

        private void categoryCB_SelectedIndexChanged(object sender, EventArgs e) {
            //TODO
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            string[] inputImageDataSource
                = inputManager.getImages(modelChoice.Text.Equals("Overlapping Model") ? "overlapping" : "simpletiled",
                    newValue); // or "simpletiled"
            inputImageCB.DataSource = inputImageDataSource;

           // throw new System.NotImplementedException();
        }
    }

    public class BitMaps {
        private readonly Form1 myForm;

        private HashSet<ImageR> curBitmaps;
        private Dictionary<int, List<Bitmap>> similarityMap;
        private List<PictureBox> pictureBoxes;
        private readonly Dictionary<Tuple<string, int>, Tuple<List<PictureBox>, int>> cache;

        public BitMaps(Form1 form) {
            myForm = form;
            patternCount = 0;
            curBitmaps = new HashSet<ImageR>();
            similarityMap = new Dictionary<int, List<Bitmap>>();
            cache = new Dictionary<Tuple<string, int>, Tuple<List<PictureBox>, int>>();
            pictureBoxes = new List<PictureBox>();
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
            Tuple<string, int> key
                = new(myForm.getSelectedInput(), myForm.getSelectedOverlapTileDimension());
            cache[key] = new Tuple<List<PictureBox>, int>(pictureBoxes.ToList(), curFloorIndex);
        }

        public int getFloorIndex() {
            return curFloorIndex == 0 ? 0 : curFloorIndex;
        }

        public bool addPattern(PatternArray colors, List<Color> distinctColors, MouseEventHandler pictureBoxMouseDown) {
            Tuple<string, int> key
                = new(myForm.getSelectedInput(), myForm.getSelectedOverlapTileDimension());
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
            newPB.Image = InputManager.resizePixels(newPB, pattern, pattern.Width, pattern.Height, size, size, padding);
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
            Tuple<string, int> key
                = new(myForm.getSelectedInput(), myForm.getSelectedOverlapTileDimension());
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
            newPB.Image = InputManager.resizePixels(newPB, pattern, pattern.Width, pattern.Height, size, size, padding);
            pattern.Dispose();
            newPB.BackColor = Color.LawnGreen;
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