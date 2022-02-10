using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WFC4All {
    public partial class Form1 : Form {
        private readonly InputManager inputManager;
        private BitMaps bitMaps = null;

        public Form1() {
            inputManager = new InputManager();
            bitMaps = new BitMaps(this);
            InitializeComponent();

            string[] inputImageDataSource = InputManager.getImages();
            inputImageCB.DataSource = inputImageDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(inputImageDataSource[0]);
            patternSize.SelectedText = inputImageDataSource[0];

            object[] patternSizeDataSource = InputManager.getImagePatternDimensions(inputImageDataSource[0]);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[0]);
        }

        private void executeButton_Click(object sender, EventArgs e) {
            Bitmap result;
            try {
                result = InputManager.runWfc(this);
            } catch (Exception) {
                Form2 errorDialog = new Form2();
                errorDialog.Show();
                return;
            }

            if (result.Width > resultPB.Width || result.Height > resultPB.Height) {
                resultPB.Image = InputManager.resizePixels(resultPB, result, result.Width, result.Height,
                    resultPB.Width,
                    resultPB.Height);
            } else {
                int i = 1;
                while ((i + 1) * result.Width < resultPB.Width && (i + 1) * result.Height < resultPB.Height) {
                    i++;
                }

                resultPB.Image = InputManager.resizePixels(resultPB, result, result.Width, result.Height,
                    i * result.Width, i * result.Height);
            }
        }

        public int getSelectedOverlapTileDimension() {
            return patternSize.SelectedIndex + 2;
        }

        public bool getPeriodicEnabled() {
            return periodicInput.Checked;
        }

        private void inputImage_SelectedIndexChanged(object sender, EventArgs e) {
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            object[] patternSizeDataSource = InputManager.getImagePatternDimensions(newValue);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[0]);
            updateInputImage(newValue);
        }

        public int getOutputWidth() {
            return (int) outputWidthValue.Value;
        }

        public int getOutputHeight() {
            return (int) outputHeightValue.Value;
        }

        private void updateInputImage(String imageName) {
            Bitmap newImage = InputManager.getImage(imageName);

            if (newImage.Width > inputImagePB.Width || newImage.Height > inputImagePB.Height) {
                inputImagePB.Image = InputManager.resizePixels(inputImagePB, newImage, newImage.Width, newImage.Height,
                    inputImagePB.Width,
                    inputImagePB.Height);
            } else {
                int i = 1;
                while ((i + 1) * newImage.Width < inputImagePB.Width &&
                       (i + 1) * newImage.Height < inputImagePB.Height) {
                    i++;
                }

                inputImagePB.Image = InputManager.resizePixels(inputImagePB, newImage, newImage.Width, newImage.Height,
                    i * newImage.Width, i * newImage.Height);
            }
        }

        public string getSelectedInput() {
            return inputImageCB.SelectedItem.ToString();
        }

        private void modelChoice_Click(object sender, EventArgs e) {
            Button btn = (Button) sender;
            if (btn.Text.Equals("Overlapping Model")) {
                btn.Text = "Simple Model";
            } else {
                btn.Text = "Overlapping Model";
            }
        }

        public void addPattern(byte[] bytes, List<Color> colors, int overlapTileDimension, int patternIdx) {
            bitMaps.addPattern(bytes, colors, overlapTileDimension, patternIdx);
        }

        private void extractPatternsButton_Click(object sender, EventArgs e) {
            int total = bitMaps.getPatternCount();
            for (int i = 0; i < total; i++) {
                foreach (Control item in tabPage1.Controls) {
                    if (item.Name == "patternPB_" + i) {
                        tabPage1.Controls.Remove(item);
                        break;
                    }
                }
            }

            // TODO forward this to OverlappingModel
            bool periodicInput = getPeriodicEnabled();
            int overlapTileDimension = getSelectedOverlapTileDimension();

            Bitmap bitmap = InputManager.getImage(getSelectedInput());
            int inputWidth = bitmap.Width, inputHeight = bitmap.Height;
            byte[,] sample = new byte[inputWidth, inputHeight];

            List<Color> colors = new List<Color>();

            for (int y = 0; y < inputHeight; y++) {
                for (int x = 0; x < inputWidth; x++) {
                    Color color = bitmap.GetPixel(x, y);

                    int colorIndex = colors.TakeWhile(c => c != color).Count();

                    if (colorIndex == colors.Count) {
                        colors.Add(color);
                    }

                    sample[x, y] = (byte) colorIndex;
                }
            }

            int colorsCount = colors.Count;

            byte[] pattern(Func<int, int, byte> f) {
                byte[] result = new byte[overlapTileDimension * overlapTileDimension];
                for (int y = 0; y < overlapTileDimension; y++) {
                    for (int x = 0; x < overlapTileDimension; x++) {
                        result[x + y * overlapTileDimension] = f(x, y);
                    }
                }

                return result;
            }

            byte[] patternFromSample(int x, int y) {
                return pattern((dx, dy) => sample[(x + dx) % inputWidth, (y + dy) % inputHeight]);
            }

            byte[] rotate(IReadOnlyList<byte> inputPattern) {
                return pattern((x, y) => inputPattern[overlapTileDimension - 1 - y + x * overlapTileDimension]);
            }

            byte[] reflect(IReadOnlyList<byte> inputPattern) {
                return pattern((x, y) => inputPattern[overlapTileDimension - 1 - x + y * overlapTileDimension]);
            }

            long index(IReadOnlyList<byte> inputPattern) {
                long result = 0, power = 1;
                for (int pixelIdx = 0; pixelIdx < inputPattern.Count; pixelIdx++) {
                    result += inputPattern[inputPattern.Count - 1 - pixelIdx] * power;
                    power *= colorsCount;
                }

                return result;
            }

            Dictionary<long, int> weightsDictionary = new Dictionary<long, int>();

            for (int y = 0; y < (periodicInput ? inputHeight : inputHeight - overlapTileDimension + 1); y++) {
                for (int x = 0; x < (periodicInput ? inputWidth : inputWidth - overlapTileDimension + 1); x++) {
                    byte[][] patternSymmetry = new byte[8][];

                    patternSymmetry[0] = patternFromSample(x, y);
                    patternSymmetry[1] = reflect(patternSymmetry[0]); // pattern flipped over y axis once
                    patternSymmetry[2] = rotate(patternSymmetry[0]); // pattern rotated CW once
                    patternSymmetry[3] = reflect(patternSymmetry[2]); // pattern rotated CW once, then flipped
                    patternSymmetry[4] = rotate(patternSymmetry[2]); // pattern rotated CW twice
                    patternSymmetry[5] = reflect(patternSymmetry[4]); // pattern rotated CW twice, then flipped
                    patternSymmetry[6] = rotate(patternSymmetry[4]); // pattern rotated CW thrice
                    patternSymmetry[7] = reflect(patternSymmetry[6]); // pattern rotated CW thrice, then flipped

                    for (int i = 0; i < 2; i++) {
                        long idx = index(patternSymmetry[i]);
                        if (weightsDictionary.ContainsKey(idx)) {
                            weightsDictionary[idx]++;
                        } else {
                            weightsDictionary.Add(idx, 1);
                            addPattern(patternSymmetry[i], colors, overlapTileDimension, weightsDictionary.Count - 1);
                        }
                    }
                }
            }
        }
    }

    public class BitMaps {
        private readonly Form1 myForm = null;

        public BitMaps(Form1 form) {
            myForm = form;
            patternCount = 0;
        }

        int patternCount;

        public int getPatternCount() {
            int temp = patternCount;
            patternCount = 0;
            return temp;
        }

        public void addPattern(byte[] pattern, List<Color> colors, int n, int patternIdx) {
            // TODO add pattern to a scrollable window instead
            PictureBox newPB = new PictureBox();

            patternCount++;

            int idxX = patternIdx % 16;
            int idxY = (int) Math.Floor(patternIdx / 16d);

            int size = 40;

            newPB.Location = new Point(375 + 75 * idxX, 30 + 60 * idxY);
            newPB.Size = new Size(size, size);
            newPB.Name = "patternPB_" + patternIdx;

            Bitmap patternBM = new Bitmap(size, size);

            for (int x = 0; x < n; x++) {
                for (int y = 0; y < n; y++) {
                    patternBM.SetPixel(x, y, colors[pattern[x + n * y]]);
                }
            }

            newPB.Image = InputManager.resizePixels(newPB, patternBM, n, n, size, size);
            myForm.tabPage1.Controls.Add(newPB);
        }
    }
}