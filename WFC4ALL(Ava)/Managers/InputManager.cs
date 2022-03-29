using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using WFC4ALL.ContentControls;
using WFC4All.DeBroglie;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using static WFC4ALL.Utils.Util;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

#pragma warning disable CS8618
#pragma warning disable CS8625

namespace WFC4ALL.Managers {
    public class InputManager {
        private readonly MainWindowViewModel mainWindowVM;
        private readonly MainWindow mainWindow;

        private readonly Bitmap noResultFoundBM;
        private readonly Stack<int> savePoints;

        private DispatcherTimer timer = new();

        private readonly CentralManager parentCM;

        private Dictionary<Tuple<int, int>, Tuple<Color, int>> overwriteColorCache;

        public InputManager(CentralManager parent) {
            parentCM = parent;
            mainWindowVM = parentCM.getMainWindowVM();
            mainWindow = parentCM.getMainWindow();
            overwriteColorCache = new Dictionary<Tuple<int, int>, Tuple<Color, int>>();

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

            mainWindowVM.Markers = new ObservableCollection<MarkerViewModel>();

            overwriteColorCache = new Dictionary<Tuple<int, int>, Tuple<Color, int>>();

            if (!getModelImages(parentCM.getWFCHandler().isOverlappingModel() ? "overlapping" : "simpletiled",
                        mainWindow.getInputControl().getCategory())
                    .Contains(mainWindow.getInputControl().getInputImage())) {
                return;
            }

            try {
                int stepAmount = mainWindowVM.StepAmount;
                mainWindowVM.OutputImage = parentCM.getWFCHandler()
                    .initAndRunWfcDB(true, stepAmount == 100 ? -1 : stepAmount, force).Item1;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void advanceStep() {
            try {
                (WriteableBitmap result2, bool finished)
                    = parentCM.getWFCHandler().initAndRunWfcDB(false, mainWindowVM.StepAmount);
                if (finished) {
                    return;
                }

                mainWindowVM.OutputImage = result2;
            } catch (Exception exception) {
#if (DEBUG)
                Trace.WriteLine(exception);
#endif
                mainWindowVM.OutputImage = noResultFoundBM;
            }
        }

        public void revertStep() {
            try {
                Bitmap avaloniaBitmap = parentCM.getWFCHandler().stepBackWfc(mainWindowVM.StepAmount);
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
            overwriteColorCache = new Dictionary<Tuple<int, int>, Tuple<Color, int>>();
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

                    Trace.WriteLine(@$"Placed a marker at {curStep}");

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

            Trace.WriteLine(
                @$"Reverting to marker at {prevTimePoint} - Cur: {parentCM.getWFCHandler().getCurrentStep()}");
            int stepsToRevert = parentCM.getWFCHandler().getCurrentStep() - prevTimePoint;
            parentCM.getWFCHandler().setCurrentStep(prevTimePoint);
            try {
                mainWindowVM.OutputImage = parentCM.getWFCHandler().stepBackWfc(stepsToRevert);
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
                parentCM.getWFCHandler().getLatestOutput().Save(settingsFileName);
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
                        (WriteableBitmap result2, bool finished) = parentCM.getWFCHandler()
                            .initAndRunWfcDB(false, mainWindowVM.StepAmount);
                        mainWindowVM.OutputImage = result2;
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

        public void processClick(int clickX, int clickY, int imgWidth, int imgHeight) {
            int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
                b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);
#if (DEBUG)
            Trace.WriteLine($@"(x:{clickX}, y:{clickY}) -> (a:{a}, b:{b})");
#endif
            //TODO CF2

            const int tileIdx = 0;
            (WriteableBitmap r2, bool showPixel) = parentCM.getWFCHandler().setTile(a, b, tileIdx);

            Tuple<int, int> key = new(a, b);
            if (overwriteColorCache.ContainsKey(key)) {
                return;
            }

            if (showPixel) {
                if (parentCM.getWFCHandler().isOverlappingModel()) {
                    Color c = (Color) parentCM.getWFCHandler().getTiles().get(tileIdx).Value;
                    overwriteColorCache.Add(key,
                        new Tuple<Color, int>(c, parentCM.getWFCHandler().getCurrentTimeStamp()));
                } else {
                    Tuple<Color[], Tile> c = parentCM.getWFCHandler().getTileCache()[tileIdx];
                }
            }

            mainWindowVM.OutputImage = parentCM.getWFCHandler().getLatestOutputBM();
            parentCM.getWFCHandler().setCurrentStep(parentCM.getWFCHandler().getCurrentStep() + 1);
        }

        public Dictionary<Tuple<int, int>, Tuple<Color, int>> getOverwriteColorCache() {
            return overwriteColorCache;
        }
    }
}