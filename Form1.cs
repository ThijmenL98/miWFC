using System;
using System.Drawing;
using System.Windows.Forms;

namespace WFC4All {
    public partial class Form1 : Form {
        private readonly InputManager inputManager;

        public Form1() {
            inputManager = new InputManager();
            InitializeComponent();
        }

        private void executeButton_Click(object sender, EventArgs e) {
            Bitmap result = InputManager.RunWfc(this);


            if (result.Width > resultPB.Width || result.Height > resultPB.Height) {
                resultPB.Image = ResizePixels(result, result.Width, result.Height, resultPB.Width,
                    resultPB.Height);
            } else {
                int i = 1;
                while ((i + 1) * result.Width < resultPB.Width && (i + 1) * result.Height < resultPB.Height) {
                    i++;
                }

                resultPB.Image = ResizePixels(result, result.Width, result.Height, i * result.Width, i * result.Height);
            }
        }

        private Bitmap ResizePixels(Bitmap bitmap, int w1, int h1, int w2, int h2) {
            int marginX = (int) Math.Floor((resultPB.Width - w2) / 2d);
            int marginY = (int) Math.Floor((resultPB.Height - h2) / 2d);

            Bitmap temp = new Bitmap(resultPB.Width, resultPB.Height);
            double xRatio = w1 / (double) w2;
            double yRatio = h1 / (double) h2;

            for (int x = 0; x < resultPB.Width; x++) {
                for (int y = 0; y < resultPB.Height; y++) {
                    if (y <= marginY || x <= marginX || y >= resultPB.Height - marginY ||
                        x >= resultPB.Width - marginX) {
                        temp.SetPixel(x, y, SystemColors.ControlDarkDark);
                    } else {
                        // Skip ahead horizontally
                        y = resultPB.Height - marginY - 1;
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