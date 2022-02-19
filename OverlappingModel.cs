/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

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

        public OverlappingModel(int overlapTileDimension, int outputWidth, int outputHeight, bool periodic, int ground,
            Heuristic heuristic, InputManager inputManager)
            : base(outputWidth, outputHeight, overlapTileDimension, periodic, heuristic) {
            colors = inputManager.getOverlapColors();

            int colorsCount = colors.Count;
            long colorCountSquared = colorsCount.ToPower(overlapTileDimension * overlapTileDimension);

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

            Dictionary<long, int> weightsDictionary = inputManager.getOverlappingWeights();
            List<long> ordering = inputManager.getOrdering();

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

            bool acceptsPattern(IReadOnlyList<byte> pattern1, IReadOnlyList<byte> pattern2, int xChangePattern,
                int yChangePattern) {
                int xMin = xChangePattern < 0 ? 0 : xChangePattern,
                    xMax = xChangePattern < 0 ? xChangePattern + overlapTileDimension : overlapTileDimension,
                    yMin = yChangePattern < 0 ? 0 : yChangePattern,
                    yMax = yChangePattern < 0 ? yChangePattern + overlapTileDimension : overlapTileDimension;
                for (int y = yMin; y < yMax; y++) {
                    for (int x = xMin; x < xMax; x++) {
                        if (pattern1[x + overlapTileDimension * y]
                            != pattern2[x - xChangePattern + overlapTileDimension * (y - yChangePattern)]) {
                            return false;
                        }
                    }
                }

                return true;
            }

            propagator = new int[4][][];
            for (int direction = 0; direction < 4; direction++) {
                propagator[direction] = new int[actionCount][];
                for (int patternIdx = 0; patternIdx < actionCount; patternIdx++) {
                    List<int> list = new List<int>();
                    for (int a2 = 0; a2 < actionCount; a2++) {
                        if (acceptsPattern(patterns[patternIdx], patterns[a2], xChange[direction],
                            yChange[direction])) {
                            list.Add(a2);
                        }
                    }

                    propagator[direction][patternIdx] = new int[list.Count];
                    for (int a2 = 0; a2 < list.Count; a2++) {
                        propagator[direction][patternIdx][a2] = list[a2];
                    }
                }
            }
        }

        public override Bitmap graphics() {
            Bitmap result = new Bitmap(imageOutputWidth, imageOutputHeight);
            int[] bitmapData = new int[result.Height * result.Width];

            if (observed[0] >= 0) {
                for (int y = 0; y < imageOutputHeight; y++) {
                    int dy = y < imageOutputHeight - overlapTileDimension + 1 ? 0 : overlapTileDimension - 1;
                    for (int x = 0; x < imageOutputWidth; x++) {
                        int dx = x < imageOutputWidth - overlapTileDimension + 1 ? 0 : overlapTileDimension - 1;
                        int pixelLocation = x - dx + (y - dy) * imageOutputWidth;

                        int patternAtLoc = observed[pixelLocation];
                        byte[] patternPixels = patterns[patternAtLoc];
                        byte pixelAtCurrentPos = patternPixels[dx + dy * overlapTileDimension];

                        Color c = colors[pixelAtCurrentPos];
                        bitmapData[x + y * imageOutputWidth]
                            = unchecked((int) 0xff000000 | (c.R << 16) | (c.G << 8) | c.B);
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
                            if (!periodic && (sx + overlapTileDimension > imageOutputWidth
                                || sy + overlapTileDimension > imageOutputHeight || sx < 0 || sy < 0)) {
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

                    if (!colors.Select(c2 => c2.ToArgb()).Contains(bitmapData[i]) &&
                        contributors > overlapTileDimension * overlapTileDimension) {
                        bitmapData[i] = Color.DarkGray.GetHashCode();
                        
                    }
                }
            }

            BitmapData bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
            result.UnlockBits(bits);

            return result;
        }

        protected override void clear() {
            base.clear();

            if (ground != 0) {
                for (int x = 0; x < imageOutputWidth; x++) {
                    for (int t = 0; t < actionCount; t++) {
                        if (t != ground) {
                            ban(x + (imageOutputHeight - 1) * imageOutputWidth, t, false);
                        }
                    }

                    for (int y = 0; y < imageOutputHeight - 1; y++) {
                        ban(x + y * imageOutputWidth, ground, false);
                    }
                }

                propagate();
            }
        }
    }
}