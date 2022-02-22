using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
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
            XDocument xDoc = XDocument.Load("samples.xml");
            XElement curSelection = xDoc.Root.elements("overlapping", "simpletiled").Where(x =>
                x.get<string>("name") == getSelectedInput()).ElementAtOrDefault(0);

            if (modelChoice.Text.Equals("Overlapping Model")) {
                (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(newValue);
                patternSize.DataSource = patternSizeDataSource;
                patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

                defaultSymmetry = curSelection.get("symmetry", 8);
                defaultPeriodicity = curSelection.get("periodicInput", true);
            } else {
                //TODO inputManager.extractPatterns(true, false);
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

            inputManager.setInputChanged("Model change");
        }

        public void addPattern(PatternArray pixels, List<Color> distinctColors) {
            bitMaps.addPattern(pixels, distinctColors, pictureBoxMouseDown);
        }

        private void extractPatternsButton_Click(object sender, EventArgs e) {
            //TODO inputManager.extractPatterns(false, modelChoice.Text.Equals("Overlapping Model"));
        }

        private void changeTab(object sender, TabControlCancelEventArgs e) {
            TabControl tc = (TabControl) sender;
            int curTab = tc.SelectedIndex;
            if (curTab == 1 && modelChoice.Text.Equals("Overlapping Model")) {
                //TODO inputManager.extractPatterns(false, true);
            }
        }

        public bool isOverlappingModel() {
            return modelChoice.Text.Equals("Overlapping Model");
        }

        private void inputChanged(object sender, EventArgs e) {
            if (!Visible) {
                return;
            }

            inputManager.setInputChanged(sender.GetType().ToString());

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
            extractPatternsButton.Visible = hide;
        }

        private void initializeRotations() {
            Bitmap referenceImg = new Bitmap("rotationRef.png");
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
            if (value == 0 || value == -1) {
                stepValue.Value = -1;
            }

            advanceButton.Visible = !(value == 0 || value == -1);
            animateButton.Visible = !(value == 0 || value == -1);
            animationSpeedLabel.Visible = !(value == 0 || value == -1);
            animationSpeedValue.Visible = !(value == 0 || value == -1);

            string prefix = stepValue.Value == 1 || stepValue.Value == -1 ? "" : "s";
            advanceButton.Text = $@"Advance {stepValue.Value} step{prefix}";
        }
    }

    public class BitMaps {
        private readonly Form1 myForm;

        private HashSet<Bitmap> curBitmaps;

        public BitMaps(Form1 form) {
            myForm = form;
            patternCount = 0;
            curBitmaps = new HashSet<Bitmap>();
        }

        private int patternCount;
        private int curFloorIndex;

        public int getPatternCount() {
            return patternCount;
        }

        public void reset() {
            patternCount = 0;
            curFloorIndex = 0;
            curBitmaps = new HashSet<Bitmap>();
        }

        public int getFloorIndex(int patternSize) {
            return curFloorIndex == 0 ? 0 : curFloorIndex - patternSize;
        }

        public void addPattern(PatternArray colors, List<Color> distinctColors, MouseEventHandler pictureBoxMouseDown) {
            const int size = 50;
            const int patternsPerRow = 6;
            PictureBox newPB = new PictureBox();

            int idxX = patternCount % patternsPerRow;
            int idxY = (int) Math.Floor(patternCount / (double) patternsPerRow);
            const int distance = 20;

            newPB.Location = new Point(size + (size + distance) * idxX, size + 30 + (size + distance) * idxY);
            newPB.Size = new Size(size, size);
            newPB.Name = "patternPB_" + patternCount;

            int n = colors.Height;
            Bitmap pattern = new Bitmap(n, n);

            bool isGroundPattern = true;

            for (int x = 0; x < n; x++) {
                for (int y = 0; y < n; y++) {
                    Color tileColor = (Color) colors.getTileAt(x, y).Value;
                    pattern.SetPixel(x, y, tileColor);
                    int colorIdx = distinctColors.IndexOf(tileColor);
                    if (y == 0 && colorIdx != distinctColors.Count - 1) {
                        isGroundPattern = false;
                    }

                    if (y != 0 && colorIdx != 0) {
                        isGroundPattern = false;
                    }
                }
            }

            int idxSimilar = checkSymmetry(pattern);
            if (idxSimilar != -1) {
                return;
            }

            curBitmaps.Add(pattern);

            if (isGroundPattern) {
                curFloorIndex = patternCount;
            }

            const int padding = 3;
            newPB.Image = InputManager.resizePixels(newPB, pattern, pattern.Width, pattern.Height, size, size, padding);
            newPB.BackColor = Color.LawnGreen;
            newPB.Padding = new Padding(padding);
            newPB.MouseDown += pictureBoxMouseDown;

            myForm.patternPanel.Controls.Add(newPB);
            if (patternCount % (patternsPerRow * 3) == patternsPerRow - 1) {
                myForm.patternPanel.Refresh();
            }

            patternCount++;
        }

        private readonly RotateFlipType[] rfs = {
            RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate90FlipX,
            RotateFlipType.Rotate180FlipNone, RotateFlipType.Rotate180FlipX, RotateFlipType.Rotate270FlipNone,
            RotateFlipType.Rotate270FlipX
        };

        private int checkSymmetry(Bitmap b) {
            int isSimilar = -1;

            foreach ((Bitmap b2, int i) in curBitmaps.Select((value, i) => (value, i))) {
                foreach (RotateFlipType rft in rfs) {
                    Bitmap b3 = new Bitmap(b2);
                    b3.RotateFlip(rft);
                    if (compareMemCmp(b, b3)) {
                        isSimilar = i;
                    }
                }
            }

            return isSimilar;
        }

        [DllImport("msvcrt.dll")]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        public static bool compareMemCmp(Bitmap b1, Bitmap b2) {
            if (b1 == null != (b2 == null)) {
                return false;
            }

            if (b1.Size != b2.Size) {
                return false;
            }

            BitmapData bd1 = b1.LockBits(new Rectangle(new Point(0, 0), b1.Size), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try {
                IntPtr bd1scan0 = bd1.Scan0;
                IntPtr bd2scan0 = bd2.Scan0;

                int stride = bd1.Stride;
                int len = stride * b1.Height;

                return memcmp(bd1scan0, bd2scan0, len) == 0;
            } finally {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }
    }
}