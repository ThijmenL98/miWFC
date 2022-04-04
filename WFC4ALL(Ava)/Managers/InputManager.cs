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
using WFC4ALL.ViewModels;
using WFC4ALL.Views;
using static WFC4ALL.Utils.Util;

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

        private Dictionary<int, Color> colourMapping;

        public InputManager(CentralManager parent) {
            parentCM = parent;
            mainWindowVM = parentCM.getMainWindowVM();
            mainWindow = parentCM.getMainWindow();
            overwriteColorCache = new Dictionary<Tuple<int, int>, Tuple<Color, int>>();
            colourMapping = new Dictionary<int, Color>();

            noResultFoundBM = new Bitmap(AppContext.BaseDirectory + "/Assets/NoResultFound.png");
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
            colourMapping = new Dictionary<int, Color>();

            if (!getModelImages(parentCM.getWFCHandler().isOverlappingModel() ? "overlapping" : "simpletiled",
                        mainWindow.getInputControl().getCategory())
                    .Contains(mainWindow.getInputControl().getInputImage())) {
                return;
            }

            try {
                int stepAmount = mainWindowVM.StepAmount;
                mainWindowVM.OutputImage = parentCM.getWFCHandler()
                    .initAndRunWfcDB(true, stepAmount == 100 ? -1 : 0, force).Item1;
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
            colourMapping = new Dictionary<int, Color>();
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
                foreach (MarkerViewModel marker in mainWindowVM.Markers.ToArray()) {
                    if (marker.MarkerIndex.Equals(savePoints.Count)) {
                        mainWindowVM.Markers.Remove(marker);
                    }
                }

                prevTimePoint = savePoints.Count == 0 ? 0 : savePoints.Peek();
            }

            int stepsToRevert = parentCM.getWFCHandler().getCurrentStep() - prevTimePoint;
            parentCM.getWFCHandler().setCurrentStep(prevTimePoint);
            try {
                mainWindowVM.OutputImage = parentCM.getWFCHandler().stepBackWfc(stepsToRevert);
                if (stepsToRevert < 0) {
                    loadMarker();
                }
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

        public bool processClick(int clickX, int clickY, int imgWidth, int imgHeight, int tileIdx) {
            int a = (int) Math.Floor(clickX * mainWindowVM.ImageOutWidth / (double) imgWidth),
                b = (int) Math.Floor(clickY * mainWindowVM.ImageOutHeight / (double) imgHeight);

#if (DEBUG)
            Trace.WriteLine($@"(x:{clickX}, y:{clickY}) -> (a:{a}, b:{b})");
#endif

            Tuple<int, int> key = new(a, b);
            if (overwriteColorCache.ContainsKey(key)) {
                return false;
            }

            WriteableBitmap bitmap;
            bool showPixel;

            if (parentCM.getWFCHandler().isOverlappingModel()) {
                Color? c;

                if (colourMapping.ContainsKey(tileIdx)) {
                    c = colourMapping[tileIdx];
                } else {
                    c = parentCM.getWFCHandler().getPaintableTiles()
                        .Where(tileViewModel => tileViewModel.PatternIndex.Equals(tileIdx)).ElementAtOrDefault(0)
                        ?.PatternColour ?? null;

                    if (c == null) {
                        return false;
                    }

                    colourMapping[tileIdx] = (Color) c;
                }

                overwriteColorCache.Add(key,
                    new Tuple<Color, int>((Color) c, parentCM.getWFCHandler().getCurrentTimeStamp()));
                (bitmap, showPixel) = parentCM.getWFCHandler().setTile(a, b, tileIdx);
            } else {
                // Tuple<Color[], Tile> cTup = parentCM.getWFCHandler().getTileCache()[tileIdx];
                (bitmap, showPixel) = parentCM.getWFCHandler().setTile(a, b, tileIdx);
            }

            if (showPixel) {
                mainWindowVM.OutputImage = bitmap;
                parentCM.getWFCHandler().setCurrentStep(parentCM.getWFCHandler().getCurrentStep() + 1);
            } else if (parentCM.getWFCHandler().isOverlappingModel()) {
                overwriteColorCache.Remove(key);
            }

            return showPixel;
        }

        public Dictionary<Tuple<int, int>, Tuple<Color, int>> getOverwriteColorCache() {
            return overwriteColorCache;
        }
    }
}