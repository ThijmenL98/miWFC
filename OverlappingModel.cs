/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace WFC4All {
    internal class OverlappingModel : Model {
        private readonly List<Color> colors;
        private readonly int ground;
        private readonly byte[][] patterns;

        public OverlappingModel(string name, int overlapTileDimension, int outputWidth, int outputHeight, bool periodicInput, bool periodic,
            int symmetry, int ground, Heuristic heuristic, Form1 form)
            : base(outputWidth, outputHeight, overlapTileDimension, periodic, heuristic, form) {
            Bitmap bitmap = new Bitmap($"samples/{name}.png");
            int inputWidth = bitmap.Width, inputHeight = bitmap.Height;
            byte[,] sample = new byte[inputWidth, inputHeight];
            colors = new List<Color>();

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
            long colorCountSquared = colorsCount.ToPower(overlapTileDimension * overlapTileDimension);

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

            byte[] patternFromIndex(long idx) {
                long residue = idx, power = colorCountSquared;
                byte[] result = new byte[overlapTileDimension * overlapTileDimension];

                for (int i = 0; i < result.Length; i++) {
                    power /= colorsCount;
                    int count = 0;

                    while (residue >= power) {
                        residue -= power;
                        count++;
                    }

                    result[i] = (byte) count;
                }

                return result;
            }

            Dictionary<long, int> weightsDictionary = new Dictionary<long, int>();
            List<long> ordering = new List<long>();

            for (int y = 0; y < (periodicInput ? inputHeight : inputHeight - overlapTileDimension + 1); y++) {
                for (int x = 0; x < (periodicInput ? inputWidth : inputWidth - overlapTileDimension + 1); x++) {
                    byte[][] patternSymmetry = new byte[8][];

                    patternSymmetry[0] = patternFromSample(x, y);
                    patternSymmetry[1] = reflect(patternSymmetry[0]);   // pattern flipped over y axis once
                    patternSymmetry[2] = rotate(patternSymmetry[0]);    // pattern rotated CW once
                    patternSymmetry[3] = reflect(patternSymmetry[2]);   // pattern rotated CW once, then flipped
                    patternSymmetry[4] = rotate(patternSymmetry[2]);    // pattern rotated CW twice
                    patternSymmetry[5] = reflect(patternSymmetry[4]);   // pattern rotated CW twice, then flipped
                    patternSymmetry[6] = rotate(patternSymmetry[4]);    // pattern rotated CW thrice
                    patternSymmetry[7] = reflect(patternSymmetry[6]);   // pattern rotated CW thrice, then flipped

                    for (int i = 0; i < symmetry; i++) {
                        long idx = index(patternSymmetry[i]);
                        if (weightsDictionary.ContainsKey(idx)) {
                            weightsDictionary[idx]++;
                        } else {
                            weightsDictionary.Add(idx, 1);
                            ordering.Add(idx);
                        }
                    }
                }
            }

            actionCount = weightsDictionary.Count;
            this.ground = (ground + actionCount) % actionCount;
            patterns = new byte[actionCount][];
            weights = new double[actionCount];

            int counter = 0;
            foreach (long w in ordering) {
                patterns[counter] = patternFromIndex(w);
                weights[counter] = weightsDictionary[w];
                counter++;
            }

            bool acceptsPattern(IReadOnlyList<byte> pattern1, IReadOnlyList<byte> pattern2, int xChange, int yChange) {
                int xMin = xChange < 0 ? 0 : xChange,
                    xMax = xChange < 0 ? xChange + overlapTileDimension : overlapTileDimension,
                    yMin = yChange < 0 ? 0 : yChange,
                    yMax = yChange < 0 ? yChange + overlapTileDimension : overlapTileDimension;
                for (int y = yMin; y < yMax; y++) {
                    for (int x = xMin; x < xMax; x++) {
                        if (pattern1[x + overlapTileDimension * y] != pattern2[x - xChange + overlapTileDimension * (y - yChange)]) {
                            return false;
                        }
                    }
                }

                return true;
            }

            propagator = new int[4][][];
            for (int direction = 0; direction < 4; direction++) {
                propagator[direction] = new int[actionCount][];
                for (int a = 0; a < actionCount; a++) {
                    List<int> list = new List<int>();
                    for (int a2 = 0; a2 < actionCount; a2++) {
                        if (acceptsPattern(patterns[a], patterns[a2], xChange[direction], yChange[direction])) {
                            list.Add(a2);
                        }
                    }

                    propagator[direction][a] = new int[list.Count];
                    for (int a2 = 0; a2 < list.Count; a2++) {
                        propagator[direction][a][a2] = list[a2];
                    }
                }
            }
        }

        public override Bitmap Graphics() {
            Bitmap result = new Bitmap(imageOutputWidth, imageOutputHeight);
            int[] bitmapData = new int[result.Height * result.Width];

            if (observed[0] >= 0) {
                for (int y = 0; y < imageOutputHeight; y++) {
                    int dy = y < imageOutputHeight - overlapTileDimension + 1 ? 0 : overlapTileDimension - 1;
                    for (int x = 0; x < imageOutputWidth; x++) {
                        int dx = x < imageOutputWidth - overlapTileDimension + 1 ? 0 : overlapTileDimension - 1;
                        Color c = colors[patterns[observed[x - dx + (y - dy) * imageOutputWidth]][dx + dy * overlapTileDimension]];
                        bitmapData[x + y * imageOutputWidth] = unchecked((int) 0xff000000 | (c.R << 16) | (c.G << 8) | c.B);
                    }
                }
            } else {
                for (int i = 0; i < wave.Length; i++) {
                    int contributors = 0, r = 0, g = 0, b = 0;
                    int x = i % imageOutputWidth, y = i / imageOutputWidth;

                    for (int dy = 0; dy < overlapTileDimension; dy++) {
                        for (int dx = 0; dx < overlapTileDimension; dx++) {
                            int sx = x - dx;
                            if (sx < 0) {
                                sx += imageOutputWidth;
                            }

                            int sy = y - dy;
                            if (sy < 0) {
                                sy += imageOutputHeight;
                            }

                            int s = sx + sy * imageOutputWidth;
                            if (!periodic && (sx + overlapTileDimension > imageOutputWidth || sy + overlapTileDimension > imageOutputHeight || sx < 0 || sy < 0)) {
                                continue;
                            }

                            for (int t = 0; t < actionCount; t++) {
                                if (!wave[s][t]) {
                                    continue;
                                }

                                contributors++;
                                Color color = colors[patterns[t][dx + dy * overlapTileDimension]];
                                r += color.R;
                                g += color.G;
                                b += color.B;
                            }
                        }
                    }

                    bitmapData[i] = unchecked((int) 0xff000000 | ((r / contributors) << 16) |
                                              ((g / contributors) << 8) | (b / contributors));
                }
            }

            BitmapData bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
            result.UnlockBits(bits);

            return result;
        }

        protected override void Clear() {
            base.Clear();

            if (ground != 0) {
                for (int x = 0; x < imageOutputWidth; x++) {
                    for (int t = 0; t < actionCount; t++) {
                        if (t != ground) {
                            Ban(x + (imageOutputHeight - 1) * imageOutputWidth, t);
                        }
                    }

                    for (int y = 0; y < imageOutputHeight - 1; y++) {
                        Ban(x + y * imageOutputWidth, ground);
                    }
                }

                Propagate();
            }
        }
    }
}