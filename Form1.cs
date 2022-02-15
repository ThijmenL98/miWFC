using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WFC4All {
    public partial class Form1 : Form {
        private readonly InputManager inputManager;
        public readonly BitMaps bitMaps;
        public readonly PictureBox[] pbs;
        private readonly int[] rotFlipOrder;
        private int defaultSymmetry, initOutWidth, initInWidth, initOutHeight, initInHeight;
        private bool defaultPeriodicity;

        public Form1() {
            defaultSymmetry = 8;
            defaultPeriodicity = true;
            inputManager = new InputManager(this);
            bitMaps = new BitMaps(this);
            InitializeComponent();

            initInWidth = inputImagePB.Width;
            initInHeight = inputImagePB.Height;
            initOutWidth = resultPB.Width;
            initOutHeight = resultPB.Height;

            lastValue = "";

            pbs = new[] {p4RotPB, p1RotPB, p7RotPB, p2RotPB, p6RotPB, p3RotPB, p5RotPB};
            rotFlipOrder = new[] {4, 1, 7, 2, 6, 3, 5};

            string[] inputImageDataSource = inputManager.getImages("overlapping"); // or "simpletiled"
            inputImageCB.DataSource = inputImageDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(inputImageDataSource[0]);
            patternSize.SelectedText = inputImageDataSource[0];

            (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(inputImageDataSource[0]);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

            initializeRotations();

            patternsLabel.Visible = false;
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
                Bitmap result = inputManager.runWfc();
                Stopwatch sw = Stopwatch.StartNew();
                resultPB.Image = InputManager.resizeBitmap(result,
                    Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));

                result.Dispose();
                Console.WriteLine("Displaying bitmap: " + sw.ElapsedMilliseconds + "ms.\n");
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

        private string lastValue;

        private void inputImage_SelectedIndexChanged(object sender, EventArgs e) {
            changingIndex = true;
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            lastValue = newValue;
            updateInputImage(newValue);
            XDocument xDoc = XDocument.Load("samples.xml");
            XElement curSelection = xDoc.Root.Elements("overlapping", "simpletiled").Where(x =>
                x.Get<string>("name") == getSelectedInput()).ElementAtOrDefault(0);

            if (modelChoice.Text.Equals("Overlapping Model")) {
                (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(newValue);
                patternSize.DataSource = patternSizeDataSource;
                patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);
                
                defaultSymmetry = curSelection.Get("symmetry", 8);
                defaultPeriodicity = curSelection.Get("periodicInput", true);
            } else {
                inputManager.extractPatterns(true, false);
            }
            
            Refresh();

            updateRotations();
            updatePeriodicity();

            changingIndex = false;
            inputManager.setInputChanged();
            executeButton_Click(null, null);
        }

        public int getOutputWidth() {
            return (int) outputWidthValue.Value;
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

            inputManager.setInputChanged();
        }

        public void addPattern(Bitmap bitmap, int patternIdx) {
            bitMaps.addPattern(bitmap, patternIdx, pictureBoxMouseDown);
        }

        public void addPattern(byte[] bytes, List<Color> colors, int overlapTileDimension, int patternIdx, bool add) {
            bitMaps.addPattern(bytes, colors, overlapTileDimension, patternIdx, pictureBoxMouseDown, add);
        }

        private void extractPatternsButton_Click(object sender, EventArgs e) {
            patternsLabel.Visible = true;
            inputManager.extractPatterns(false, modelChoice.Text.Equals("Overlapping Model"));
        }

        private void changeTab(object sender, TabControlCancelEventArgs e) {
            TabControl tc = (TabControl) sender;
            int curTab = tc.SelectedIndex;
            if (curTab == 1 && modelChoice.Text.Equals("Overlapping Model")) {
                inputManager.extractPatterns(false, true);
            }
        }

        private void inputChanged(object sender, EventArgs e) {
            inputManager.setInputChanged();
            if (Visible && modelChoice.Text.Equals("Overlapping Model")) {
                executeButton_Click(null, null);
            }
        }

        private static void pictureBoxMouseDown(object sender, MouseEventArgs e) {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left) {
                PictureBox pb = (PictureBox) sender;
                pb.BackColor = pb.BackColor.Equals(Color.LawnGreen) ? Color.Red : Color.LawnGreen;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void addHover(object sender, EventArgs e, string message) {
            ToolTip toolTip = new ToolTip();

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
            foreach (int j in rotFlipOrder) {
                int i = j - 1;
                pbs[i].BackColor = defaultSymmetry < j + 1 ? Color.Red : Color.LawnGreen;
            }
        }

        public void showRotationalOptions(bool hide) {
            foreach (int j in rotFlipOrder) {
                int i = j - 1;
                pbs[i].Visible = hide;
            }

            originalRotPB.Visible = hide;
            patternRotationLabel.Visible = hide;
            periodicInput.Visible = hide;
            patternSize.Visible = hide;
            patternSizeLabel.Visible = hide;
            extractPatternsButton.Visible = hide;
        }

        private void initializeRotations() {
            Bitmap referenceImg = new Bitmap($"rotationRef.png");
            const int padding = 3;
            originalRotPB.BackColor = Color.Black;
            originalRotPB.Padding = new Padding(padding);
            originalRotPB.Image = referenceImg;

            RotateFlipType[] rfs = {
                RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate90FlipX,
                RotateFlipType.Rotate180FlipNone, RotateFlipType.Rotate180FlipX, RotateFlipType.Rotate270FlipNone,
                RotateFlipType.Rotate270FlipX
            };
            string[] rfsString = {
                "Flip Horizontally", "Rotate Clockwise 90°", "Rotate Clockwise 90° & Flip Horizontally",
                "Rotate 180°", "Rotate 180° & Flip Horizontally", "Rotate Clockwise 270°",
                "Rotate Clockwise 270° & Flip Horizontally"
            };
            for (int i = 0; i < 7; i++) {
                pbs[i].BackColor = defaultSymmetry < i + 1 ? Color.Red : Color.LawnGreen;
                pbs[i].Padding = new Padding(padding);
                Bitmap cloneBM = (Bitmap) referenceImg.Clone();
                cloneBM.RotateFlip(rfs[i]);
                pbs[i].Image = cloneBM;
                pbs[i].MouseDown += pictureBoxMouseDown;
                int i1 = i;
                pbs[i].MouseHover += (sender, eventArgs) => { addHover(sender, eventArgs, rfsString[i1]); };
            }
        }

        public void setPatternLabelVisible() {
            patternsLabel.Visible = true;
        }
    }

    public class BitMaps {
        private readonly Form1 myForm;

        public BitMaps(Form1 form) {
            myForm = form;
            patternCount = 0;
        }

        private int patternCount;
        private int curFloorIndex;

        public int getPatternCount() {
            int temp = patternCount;
            patternCount = 0;
            curFloorIndex = 0;
            return temp;
        }

        public int getFloorIndex(int patternSize) {
            return curFloorIndex == 0 ? 0 : (curFloorIndex - patternSize);
        }

        public void addPattern(byte[] pattern, List<Color> colors, int n, int patternIdx,
            MouseEventHandler pictureBoxMouseDown, bool add) {
            const int size = 50;
            const int patternsPerRow = 6;
            PictureBox newPB = new PictureBox();

            if (add) {
                int idxX = patternCount % patternsPerRow;
                int idxY = (int) Math.Floor(patternCount / (double) patternsPerRow);
                const int distance = 20;

                newPB.Location = new Point(size + (size + distance) * idxX, size + 30 + (size + distance) * idxY);
                newPB.Size = new Size(size, size);
                newPB.Name = "patternPB_" + patternCount;
                patternCount++;
            }

            Bitmap patternBM = new Bitmap(size, size);

            bool isGroundPattern = true;

            for (int x = 0; x < n; x++) {
                for (int y = 0; y < n; y++) {
                    int colorIdx = pattern[x + n * y];
                    patternBM.SetPixel(x, y, colors[pattern[x + n * y]]);
                    if (y == 0 && colorIdx != colors.Count - 1) {
                        isGroundPattern = false;
                    }

                    if (y != 0 && colorIdx != 0) {
                        isGroundPattern = false;
                    }
                }
            }

            if (isGroundPattern) {
                curFloorIndex = patternIdx;
            }

            if (add) {
                const int padding = 3;
                newPB.Image = InputManager.resizePixels(newPB, patternBM, n, n, size, size, padding);
                newPB.BackColor = Color.LawnGreen;
                newPB.Padding = new Padding(padding);
                newPB.MouseDown += pictureBoxMouseDown;

                myForm.patternPanel.Controls.Add(newPB);
                if (patternIdx % (patternsPerRow * 3) == patternsPerRow - 1) {
                    myForm.patternPanel.Refresh();
                }
            }
        }

        public void addPattern(Bitmap pattern, int patternIdx, MouseEventHandler pictureBoxMouseDown) {
            const int size = 50;
            const int patternsPerRow = 6;
            PictureBox newPB = new PictureBox();

            int idxX = patternCount % patternsPerRow;
            int idxY = (int) Math.Floor(patternCount / (double) patternsPerRow);
            const int distance = 20;

            newPB.Location = new Point(size + (size + distance) * idxX, size + 30 + (size + distance) * idxY);
            newPB.Size = new Size(size, size);
            newPB.Name = "patternPB_" + patternCount;
            patternCount++;
            
            const int padding = 3;
            newPB.Image = InputManager.resizePixels(newPB, pattern, pattern.Width, pattern.Height, size, size, padding);
            newPB.BackColor = Color.LawnGreen;
            newPB.Padding = new Padding(padding);
            newPB.MouseDown += pictureBoxMouseDown;

            myForm.patternPanel.Controls.Add(newPB);
            if (patternIdx % (patternsPerRow * 3) == patternsPerRow - 1) {
                myForm.patternPanel.Refresh();
            }
        }
    }
}