using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WFC4ALL.ContentControls;
using WFC4All.DeBroglie.Models;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;

namespace WFC4ALL.Managers; 

public class UIManager {
    private readonly CentralManager parentCM;

    private readonly MainWindowViewModel mainWindowVM;
    private readonly MainWindow mainWindow;

    private HashSet<ImageR> curBitmaps = new();
    private Dictionary<int, List<Bitmap>> similarityMap = new();
    private int patternCount;
    
    public UIManager(CentralManager parent) {
        parentCM = parent;
        mainWindowVM = parent.getMainWindowVM();
        mainWindow = parent.getMainWindow();
    }
    
    /*
     * UI Elements Content Updating
     */
    
    public void updateCategories(string[]? values, int idx = 0) {
        mainWindow.getInputControl().setCategories(values, idx);
    }

    public void updateInputImages(string[]? values, int idx = 0) {
        mainWindow.getInputControl().setInputImages(values, idx);
    }

    public void updatePatternSizes(int[]? values, int idx = 0) {
        mainWindow.getInputControl().setPatternSizes(values, idx);
    }

    public void updateInputImage(string newImage) {
        // ReSharper disable once InconsistentNaming
        string URI = $"samples/{newImage}.png";
        mainWindowVM.InputImage = new Avalonia.Media.Imaging.Bitmap(URI);
    }

    public void updateInstantCollapse(int newValue) {
        bool ic = newValue == 100;
        mainWindowVM.StepAmountString = ic ? "Instantly generate output" : "Steps to take: " + newValue;
        mainWindowVM.InstantCollapse = !ic;
    }

    public void updateTimeStampPosition(double amountCollapsed) {
        double tWidth = mainWindow.getOutputControl().getTimelineWidth();
        mainWindowVM.TimeStampOffset = tWidth * amountCollapsed - 9;
    }
    
    /*
     * UI Window Manipulation
     */

    public void showPopUp() {
        mainWindowVM.PopupVisible = true;
    }

    public void hidePopUp() {
        mainWindowVM.PopupVisible = false;
    }

    public bool popUpOpened() {
        return mainWindowVM.PopupVisible;
    }
    
    public void resetPatterns() {
        similarityMap = new Dictionary<int, List<Bitmap>>();
        curBitmaps = new HashSet<ImageR>();
        patternCount = 0;
    }

    public TileViewModel? addPattern(PatternArray colors, double weight, Dictionary<int, int[]>? tileSymmetries) {
        int n = colors.Height;
        Bitmap pattern = new(n, n);

        Dictionary<Point, Color> data = new();

        for (int x = 0; x < n; x++) {
            for (int y = 0; y < n; y++) {
                Color tileColor = (Color) colors.getTileAt(x, y).Value;
                pattern.SetPixel(x, y, tileColor);
                data[new Point(x, y)] = tileColor;
            }
        }

        ImageR cur = new(pattern.Size, data);

        foreach ((ImageR reference, int i) in curBitmaps.Select((reference, i) => (reference, i))) {
            if (transforms.Select(transform => cur.Data.All(x =>
                    x.Value == reference.Data[
                        transform.Invoke(cur.Size.Width - 1, cur.Size.Height - 1, x.Key.X, x.Key.Y)]))
                .Any(match => match)) {
                similarityMap[i].Add(pattern);
                List<int> symmetries = tileSymmetries!.ContainsKey(i) ? tileSymmetries[i].ToList() : new List<int>();
                symmetries.Add(patternCount);
                tileSymmetries[i] = symmetries.ToArray();
                return null;
            }
        }

        curBitmaps.Add(cur);
        similarityMap[patternCount] = new List<Bitmap> {pattern};

        Avalonia.Media.Imaging.Bitmap avaloniaBitmap = parentCM.getInputManager().ConvertToAvaloniaBitmap(pattern);
        TileViewModel tvm = new(avaloniaBitmap, weight, patternCount);

        patternCount++;

        return tvm;
    }
    
    /*
     * Helper functions
     */

    private record ImageR(Size Size, Dictionary<Point, Color> Data) {
        public Size Size { get; } = Size;
        public Dictionary<Point, Color> Data { get; } = Data;
    }

    private readonly List<Func<int, int, int, int, Point>> transforms = new() {
        (_, _, x, y) => new Point(x, y), // rotated 0
        (w, _, x, y) => new Point(w - y, x), // rotated 90
        (w, h, x, y) => new Point(w - x, h - y), // rotated 180
        (_, h, x, y) => new Point(y, h - x), // rotated 270
        (w, _, x, y) => new Point(w - x, y), // rotated 0 and mirrored
        (w, _, x, y) => new Point(w - (w - y), x), // rotated 90 and mirrored
        (w, h, x, y) => new Point(w - (w - x), h - y), // rotated 180 and mirrored
        (w, h, x, y) => new Point(w - y, h - x), // rotated 270 and mirrored
    };
}