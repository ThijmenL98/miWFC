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
using System.Xml.Linq;

namespace WFC4All {
    internal class SimpleTiledModel : Model {
        private readonly List<Color[]> tiles;
        private readonly int tilesize;

        public SimpleTiledModel(int outputWidth, int outputHeight, bool periodic,
            Heuristic heuristic, Form1 form, InputManager inputManager) 
            : base(outputWidth, outputHeight, 1, periodic, heuristic, form) {

            tilesize = inputManager.getTileSize();
            XElement xRoot = inputManager.getSimpleXRoot();
            tiles = inputManager.getSimpleColors();
            Dictionary<string, int> firstOccurrence = inputManager.getFirstOccurrences();
            List<int[]> action = inputManager.getActions();
            actionCount = action.Count;
            weights = inputManager.getSimpleWeights().ToArray();

            propagator = new int[4][][];
            bool[][][] densePropagator = new bool[4][][];
            for (int d = 0; d < 4; d++) {
                densePropagator[d] = new bool[actionCount][];
                propagator[d] = new int[actionCount][];
                for (int t = 0; t < actionCount; t++) {
                    densePropagator[d][t] = new bool[actionCount];
                }
            }

            foreach (XElement xNeighbor in xRoot.Element("neighbors").Elements("neighbor")) {
                string[] left = xNeighbor.Get<string>("left")
                    .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                string[] right = xNeighbor.Get<string>("right")
                    .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                int l = action[firstOccurrence[left[0]]][left.Length == 1 ? 0 : int.Parse(left[1])];
                int d = action[l][1];
                int r = action[firstOccurrence[right[0]]][right.Length == 1 ? 0 : int.Parse(right[1])];
                int u = action[r][1];

                densePropagator[0][r][l] = true;
                densePropagator[0][action[r][6]][action[l][6]] = true;
                densePropagator[0][action[l][4]][action[r][4]] = true;
                densePropagator[0][action[l][2]][action[r][2]] = true;

                densePropagator[1][u][d] = true;
                densePropagator[1][action[d][6]][action[u][6]] = true;
                densePropagator[1][action[u][4]][action[d][4]] = true;
                densePropagator[1][action[d][2]][action[u][2]] = true;
            }

            for (int t2 = 0; t2 < actionCount; t2++) {
                for (int t1 = 0; t1 < actionCount; t1++) {
                    densePropagator[2][t2][t1] = densePropagator[0][t1][t2];
                    densePropagator[3][t2][t1] = densePropagator[1][t1][t2];
                }
            }

            List<int>[][] sparsePropagator = new List<int>[4][];
            for (int d = 0; d < 4; d++) {
                sparsePropagator[d] = new List<int>[actionCount];
                for (int t = 0; t < actionCount; t++) {
                    sparsePropagator[d][t] = new List<int>();
                }
            }

            for (int d = 0; d < 4; d++) {
                for (int t1 = 0; t1 < actionCount; t1++) {
                    List<int> sp = sparsePropagator[d][t1];
                    bool[] tp = densePropagator[d][t1];

                    for (int t2 = 0; t2 < actionCount; t2++) {
                        if (tp[t2]) {
                            sp.Add(t2);
                        }
                    }

                    int spCount = sp.Count;
                    if (spCount == 0) {
                        Console.WriteLine($@"ERROR: tile {inputManager.getSimpleTileName(t1)} has no neighbors in direction {d}");
                    }

                    propagator[d][t1] = new int[spCount];
                    for (int st = 0; st < spCount; st++) {
                        propagator[d][t1][st] = sp[st];
                    }
                }
            }
        }

        public override Bitmap Graphics() {
            Bitmap result = new Bitmap(imageOutputWidth * tilesize, imageOutputHeight * tilesize);
            int[] bitmapData = new int[result.Height * result.Width];

            if (observed[0] >= 0) {
                for (int x = 0; x < imageOutputWidth; x++) {
                    for (int y = 0; y < imageOutputHeight; y++) {
                        Color[] tile = tiles[observed[x + y * imageOutputWidth]];
                        for (int yt = 0; yt < tilesize; yt++) {
                            for (int xt = 0; xt < tilesize; xt++) {
                                Color c = tile[xt + yt * tilesize];
                                bitmapData[x * tilesize + xt + (y * tilesize + yt) * imageOutputWidth * tilesize] =
                                    unchecked((int) 0xff000000 | (c.R << 16) | (c.G << 8) | c.B);
                            }
                        }
                    }
                }
            } else {
                for (int x = 0; x < imageOutputWidth; x++) {
                    for (int y = 0; y < imageOutputHeight; y++) {
                        bool[] a = wave[x + y * imageOutputWidth];
                        double lambda = 1.0 / (from t in Enumerable.Range(0, actionCount) where a[t] select weights[t])
                            .Sum();

                        for (int yt = 0; yt < tilesize; yt++) {
                            for (int xt = 0; xt < tilesize; xt++) {
                                double r = 0, g = 0, b = 0;
                                int count = 0;
                                for (int t = 0; t < actionCount; t++) {
                                    if (a[t]) {
                                        Color c = tiles[t][xt + yt * tilesize];
                                        r += c.R * weights[t] * lambda;
                                        g += c.G * weights[t] * lambda;
                                        b += c.B * weights[t] * lambda;
                                        count++;
                                    }
                                }

                                if (count == 1) {
                                    bitmapData[x * tilesize + xt + (y * tilesize + yt) * imageOutputWidth * tilesize] =
                                        unchecked((int) 0xff000000 | ((int) r << 16) | ((int) g << 8) | (int) b);
                                } else {
                                    bitmapData[x * tilesize + xt + (y * tilesize + yt) * imageOutputWidth * tilesize] =
                                        unchecked((int) 0xff000000);
                                }
                            }
                        }
                    }
                }
            }

            BitmapData bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
            result.UnlockBits(bits);
            return result;
        }
    }
}