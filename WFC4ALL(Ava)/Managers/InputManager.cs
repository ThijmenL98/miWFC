using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using WFC4ALL.ContentControls;
using WFC4All.DeBroglie;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using static WFC4ALL.Utils.Util;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Image = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace WFC4ALL.Managers {
    public class InputManager {
        private readonly MainWindowViewModel mainWindowVM;
        private readonly MainWindow mainWindow;

        private readonly Bitmap noResultFoundBM;
        private readonly Stack<int> savePoints;

        private DispatcherTimer timer = new();

        private CentralManager parentCM;

        private Dictionary<Tuple<int, int>, Color> overwriteColorCache;

        public InputManager(CentralManager parent) {
            parentCM = parent;
            mainWindowVM = parentCM.getMainWindowVM();
            mainWindow = parentCM.getMainWindow();
            overwriteColorCache = new Dictionary<Tuple<int, int>, Color>();

            noResultFoundBM = new Bitmap("Assets/NoResultFound.png");
            savePoints = new Stack<int>();
            savePoints.Push(0);

            mainWindow.getInputControl().setCategories(getCategories("overlapping"));

            string[] inputImageDataSource = getModelImages("overlapping", "Textures");
            mainWindow.getInputControl().setInputImages(inputImageDataSource);

            (int[] patternSizeDataSource, int i) = getImagePatternDimensions(inputImageDataSource[0]);
            mainWindow.getInputControl().setPatternSizes(patternSizeDataSource, i);

            restartSolution();
            parentCM.getUIManager().updateInstantCollapse(1);
        }

        /*
         * Functionality
         */

        public void restartSolution(bool force = false) {
            if (parentCM.getWFCHandler().isChangingModels() || parentCM.getWFCHandler().isChangingImages()) {
                return;
            }

            overwriteColorCache = new Dictionary<Tuple<int, int>, Color>();

            if (!getModelImages(parentCM.getWFCHandler().isOverlappingModel() ? "overlapping" : "simpletiled",
                        mainWindow.getInputControl().getCategory())
                    .Contains(mainWindow.getInputControl().getInputImage())) {
                return;
            }

            try {
                int stepAmount = mainWindowVM.StepAmount;
                (System.Drawing.Bitmap result2, bool _) = parentCM.getWFCHandler()
                    .initAndRunWfcDB(true, stepAmount == 100 ? -1 : stepAmount, force);
                Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void advanceStep() {
            try {
                (System.Drawing.Bitmap result2, bool finished)
                    = parentCM.getWFCHandler().initAndRunWfcDB(false, mainWindowVM.StepAmount);
                if (finished) {
                    return;
                }

                Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void revertStep() {
            try {
                System.Drawing.Bitmap result2 = parentCM.getWFCHandler().stepBackWfc(mainWindowVM.StepAmount);
                Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;

                int curStep = parentCM.getWFCHandler().getCurrentStep();
                if (curStep < savePoints.Peek()) {
                    savePoints.Pop();
                    int toRemove = savePoints.Count;
                    foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                        if (marker.MarkerIndex.Equals(toRemove)) {
                            mainWindowVM.Markers.Remove(marker);
                        }
                    }
                }
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void resetOverwriteCache() {
            overwriteColorCache = new Dictionary<Tuple<int, int>, Color>();
        }

        public void placeMarker() {
            int curStep = parentCM.getWFCHandler().getCurrentStep();
            mainWindowVM.Markers.Add(new MarkerViewModel(savePoints.Count,
                mainWindow.getOutputControl().getTimelineWidth() * parentCM.getWFCHandler().getAmountCollapsed() + 1));
            while (true) {
                if (savePoints.Count != 0 && curStep < savePoints.Peek()) {
                    savePoints.Pop();
                    int toRemove = savePoints.Count;
                    foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                        if (marker.MarkerIndex.Equals(toRemove)) {
                            mainWindowVM.Markers.Remove(marker);
                        }
                    }
                } else {
                    if (!savePoints.Contains(curStep)) {
                        savePoints.Push(curStep);
                    }

                    return;
                }
            }
        }

        public void loadMarker() {
            int prevTimePoint = savePoints.Count == 0 ? 0 : savePoints.Peek();
            if (parentCM.getWFCHandler().getCurrentStep() == prevTimePoint && savePoints.Count != 0) {
                savePoints.Pop();
                int toRemove = savePoints.Count;
                foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                    if (marker.MarkerIndex.Equals(toRemove)) {
                        mainWindowVM.Markers.Remove(marker);
                    }
                }

                prevTimePoint = savePoints.Count == 0 ? 0 : savePoints.Peek();
            }

            int stepsToRevert = parentCM.getWFCHandler().getCurrentStep() - prevTimePoint;
            parentCM.getWFCHandler().setCurrentStep(prevTimePoint);
            try {
                System.Drawing.Bitmap result2 = parentCM.getWFCHandler().stepBackWfc(stepsToRevert);
                Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                mainWindowVM.OutputImage = avaloniaBitmap;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public async void exportSolution() {
            SaveFileDialog sfd = new() {
                Title = @"Select export location & file name",
                DefaultExtension = "jpg",
                Filters = new List<FileDialogFilter> {
                    new() {
                        Extensions = new List<string> {"jpg"},
                        Name = "JPeg Image"
                    }
                }
            };

            string? settingsFileName = await sfd.ShowAsync(new Window());
            if (settingsFileName != null) {
                parentCM.getWFCHandler().getLatestOutput().Save(settingsFileName, ImageFormat.Jpeg);
            }
        }

        public void animate() {
            if (parentCM.getWFCHandler().isCollapsed()) {
                restartSolution();
            }

            bool startAnimation = mainWindowVM.IsPlaying;

            if (startAnimation) {
                lock (timer) {
                    timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                    timer.Tick += animationTimerTick!;
                    timer.Start();
                }
            } else {
                lock (timer) {
                    timer.Stop();
                    timer = new DispatcherTimer();
                }
            }
        }

        private void animationTimerTick(object sender, EventArgs e) {
            lock (timer) {
                /* only work when this is no reentry while we are already working */
                if (timer.IsEnabled) {
                    timer.Stop();
                    timer.Interval = TimeSpan.FromMilliseconds(mainWindowVM.AnimSpeed);
                    try {
                        (System.Drawing.Bitmap result2, bool finished) = parentCM.getWFCHandler()
                            .initAndRunWfcDB(false, mainWindowVM.StepAmount);
                        Bitmap avaloniaBitmap = ConvertToAvaloniaBitmap(result2);
                        mainWindowVM.OutputImage = avaloniaBitmap;
                        if (finished) {
                            mainWindowVM.IsPlaying = false;
                            return;
                        }
                    } catch (Exception exception) {
#if (DEBUG)
                        Trace.WriteLine(exception);
#endif
                        mainWindowVM.OutputImage = noResultFoundBM;
                    }

                    timer.Start();
                }
            }
        }

        public Bitmap ConvertToAvaloniaBitmap(Image? bitmap) {
            if (bitmap == null) {
                return new Bitmap("");
            }

            System.Drawing.Bitmap bitmapTmp = new(bitmap);

            foreach (KeyValuePair<Tuple<int, int>, Color> kvp in overwriteColorCache) {
                bitmapTmp.SetPixel(kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
            }

            BitmapData? bitmapData = bitmapTmp.LockBits(new Rectangle(0, 0, bitmapTmp.Width, bitmapTmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Bitmap bitmap1 = new(
                Avalonia.Platform.PixelFormat.Bgra8888, AlphaFormat.Premul,
                bitmapData.Scan0,
                new PixelSize(bitmapData.Width, bitmapData.Height),
                new Vector(96, 96),
                bitmapData.Stride);
            bitmapTmp.UnlockBits(bitmapData);
            bitmapTmp.Dispose();

            return bitmap1;
        }

        /*
         * Getters
         */


        /*
         * Setters
         */

        public void processClick(int clickX, int clickY, int imgWidth, int imgHeight) {
            int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
                b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);
#if (DEBUG)
            Trace.WriteLine($@"(x:{clickX}, y:{clickY}) -> (a:{a}, b:{b})");
#endif
            //TODO CF2

            const int tileIdx = 0;
            (System.Drawing.Bitmap result2, bool showPixel) = parentCM.getWFCHandler().setTile(a, b, tileIdx);

            if (showPixel) {
                if (parentCM.getWFCHandler().isOverlappingModel()) {
                    Color c = (Color) parentCM.getWFCHandler().getTiles().get(tileIdx).Value;
                    overwriteColorCache.Add(new Tuple<int, int>(a, b), c);
                } else {
                    Tuple<Color[], Tile> c = parentCM.getWFCHandler().getTileCache()[tileIdx];
                }
            }

            mainWindowVM.OutputImage = ConvertToAvaloniaBitmap(result2);
            parentCM.getWFCHandler().setCurrentStep(parentCM.getWFCHandler().getCurrentStep() + 1);
        }
    }
}