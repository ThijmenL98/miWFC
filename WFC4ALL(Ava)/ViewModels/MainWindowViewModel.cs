using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using WFC4ALL.ContentControls;
using WFC4ALL.Managers;
using WFC4ALL.Models;
using WFC4ALL.Utils;

// ReSharper disable UnusedMember.Global

namespace WFC4ALL.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        private string _modelSelectionText = "Switch to\nTile Mode",
            _selectedCategory = "",
            _selectedInputImage = "",
            _stepAmountString = "Steps to take: 1",
            _seamlessText = "Toggle seamless\noutput (Disabled)";

        private bool _isPlaying, _paddingEnabled, _instantCollapse, _popupVisible, _isLoading;
        private int _stepAmount = 1, _animSpeed = 100, _imgOutWidth, _imgOutHeight, _patternSize = 3;

        private Bitmap _inputImage
                = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul),
            _outputImage
                = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul),
            _outputImageMask
                = new WriteableBitmap(new PixelSize(1, 1), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul);

        private ObservableCollection<TileViewModel> _patternTiles = new(), _paintTiles = new();
        private ObservableCollection<MarkerViewModel> _markers = new();
        private double _timeStampOffset, _timelineWidth = 600d;

        private CentralManager? centralManager;
        private Tuple<string, string>? lastOverlapSelection, lastSimpleSelection;

        private bool _pencilModeEnabled, _eraseModeEnabled,_paintKeepModeEnabled, _paintEraseModeEnabled;

        public CentralManager getCentralManager() {
            return centralManager!;
        }

        public string ModelSelectionText {
            get => _modelSelectionText;
            private set => this.RaiseAndSetIfChanged(ref _modelSelectionText, value);
        }

        public string SeamlessText {
            get => _seamlessText;
            private set => this.RaiseAndSetIfChanged(ref _seamlessText, value);
        }

        private string CategorySelection {
            get => _selectedCategory;
            // ReSharper disable once UnusedMember.Local
            set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        }

        private string InputImageSelection {
            get => _selectedInputImage;
            // ReSharper disable once UnusedMember.Local
            set => this.RaiseAndSetIfChanged(ref _selectedInputImage, value);
        }

        public string StepAmountString {
            get => _stepAmountString;
            set => this.RaiseAndSetIfChanged(ref _stepAmountString, value);
        }

        public bool IsPlaying {
            get => _isPlaying;
            set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
        }

        public bool PaddingEnabled {
            get => _paddingEnabled;
            private set => this.RaiseAndSetIfChanged(ref _paddingEnabled, value);
        }

        public bool InstantCollapse {
            get => _instantCollapse;
            set => this.RaiseAndSetIfChanged(ref _instantCollapse, value);
        }

        public int StepAmount {
            get => _stepAmount;
            set => this.RaiseAndSetIfChanged(ref _stepAmount, value);
        }

        public int AnimSpeed {
            get => _animSpeed;
            set => this.RaiseAndSetIfChanged(ref _animSpeed, value);
        }

        public int ImageOutWidth {
            get => _imgOutWidth;
            private set => this.RaiseAndSetIfChanged(ref _imgOutWidth, value);
        }

        public int ImageOutHeight {
            get => _imgOutHeight;
            private set => this.RaiseAndSetIfChanged(ref _imgOutHeight, value);
        }

        public int PatternSize {
            get => _patternSize;
            set => this.RaiseAndSetIfChanged(ref _patternSize, value);
        }

        public double TimeStampOffset {
            get => _timeStampOffset;
            set => this.RaiseAndSetIfChanged(ref _timeStampOffset, value);
        }

        public double TimelineWidth {
            get => _timelineWidth;
            set => this.RaiseAndSetIfChanged(ref _timelineWidth, value);
        }

        public Bitmap InputImage {
            get => _inputImage;
            set => this.RaiseAndSetIfChanged(ref _inputImage, value);
        }

        public Bitmap OutputImage {
            get => _outputImage;
            set => this.RaiseAndSetIfChanged(ref _outputImage, value);
        }

        public Bitmap OutputImageMask {
            get => _outputImageMask;
            set => this.RaiseAndSetIfChanged(ref _outputImageMask, value);
        }

        public ObservableCollection<TileViewModel> PatternTiles {
            get => _patternTiles;
            set => this.RaiseAndSetIfChanged(ref _patternTiles, value);
        }

        public ObservableCollection<TileViewModel> PaintTiles {
            get => _paintTiles;
            set => this.RaiseAndSetIfChanged(ref _paintTiles, value);
        }

        public ObservableCollection<MarkerViewModel> Markers {
            get => _markers;
            set => this.RaiseAndSetIfChanged(ref _markers, value);
        }

        public MainWindowViewModel VM => this;

        public bool PopupVisible {
            get => _popupVisible;
            set => this.RaiseAndSetIfChanged(ref _popupVisible, value);
        }

        public bool PencilModeEnabled {
            get => _pencilModeEnabled;
            set => this.RaiseAndSetIfChanged(ref _pencilModeEnabled, value);
        }

        public bool EraseModeEnabled {
            get => _eraseModeEnabled;
            set => this.RaiseAndSetIfChanged(ref _eraseModeEnabled, value);
        }

        public bool PaintKeepModeEnabled {
            get => _paintKeepModeEnabled;
            set => this.RaiseAndSetIfChanged(ref _paintKeepModeEnabled, value);
        }

        public bool PaintEraseModeEnabled {
            get => _paintEraseModeEnabled;
            set => this.RaiseAndSetIfChanged(ref _paintEraseModeEnabled, value);
        }

        public bool IsLoading {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        /*
         * Logic
         */

        public void setCentralManager(CentralManager cm) {
            lastOverlapSelection = new Tuple<string, string>("Textures", "3Bricks");
            lastSimpleSelection = new Tuple<string, string>("Worlds Top-Down", "Castle");
            centralManager = cm;

            ImageOutWidth = 24;
            ImageOutHeight = 24;
        }

        public void OnModelClick() {
            centralManager!.getWFCHandler().setModelChanging(true);

            ModelSelectionText
                = ModelSelectionText.Contains("Smart") ? "Switch to\nTile Mode" : "Switch to\nSmart Mode";
            bool changingToSmart = ModelSelectionText.Contains("Tile");

            string lastCat = CategorySelection;

            string[] catDataSource = Util.getCategories(changingToSmart ? "overlapping" : "simpletiled");

            string lastImg = InputImageSelection;
            int catIndex = changingToSmart
                ? Array.IndexOf(catDataSource, lastOverlapSelection!.Item1)
                : Array.IndexOf(catDataSource, lastSimpleSelection!.Item1);
            centralManager.getUIManager().updateCategories(catDataSource, catIndex);

            string[] images = Util.getModelImages(
                changingToSmart ? "overlapping" : "simpletiled",
                changingToSmart ? lastOverlapSelection!.Item1 : lastSimpleSelection!.Item1);

            int index = changingToSmart
                ? Array.IndexOf(images, lastOverlapSelection!.Item2)
                : Array.IndexOf(images, lastSimpleSelection!.Item2);
            centralManager.getUIManager().updateInputImages(images, index);
            (int[] patternSizeDataSource, int i) = Util.getImagePatternDimensions(images[index]);
            centralManager.getUIManager().updatePatternSizes(patternSizeDataSource, i);

            if (changingToSmart) {
                lastSimpleSelection = new Tuple<string, string>(lastCat, lastImg);
            } else {
                lastOverlapSelection = new Tuple<string, string>(lastCat, lastImg);
            }

            centralManager.getWFCHandler().setModelChanging(false);

            centralManager.getWFCHandler().setInputChanged("Model change");
            centralManager.getMainWindow().getInputControl().inImgCBChangeHandler(null, null);
        }

        public void OnPaddingClick() {

            centralManager!.getWFCHandler().setInputChanged("Padding Button");
            SeamlessText
                = SeamlessText.Contains("Disabled") ? "Toggle seamless\noutput (Enabled)" : "Toggle seamless\noutput (Disabled)";
            PaddingEnabled = !PaddingEnabled;
            centralManager.getInputManager().restartSolution();
        }

        public void OnAnimate() {

            IsPlaying = !IsPlaying;
            centralManager!.getInputManager().animate();
        }

        public void OnRestart() {
            centralManager!.getInputManager().restartSolution();
        }

        public void OnRevert() {
            centralManager!.getInputManager().revertStep();
        }

        public void OnAdvance() {
            centralManager!.getInputManager().advanceStep();
        }

        public void OnSave() {
            centralManager!.getInputManager().placeMarker();
        }

        public void OnLoad() {
            centralManager!.getInputManager().loadMarker();
        }

        public void OnExport() {
            centralManager!.getInputManager().exportSolution();
        }

        public void OnInfoClick() {
            centralManager!.getUIManager().showPopUp();
        }

        public void OnCloseClick() {
            centralManager!.getUIManager().hidePopUp();
        }

        public void OnPencilModeClick() {
            PencilModeEnabled = !PencilModeEnabled;
            EraseModeEnabled = false;
            PaintKeepModeEnabled = false;
            PaintEraseModeEnabled = false;
        }

        public void OnEraseModeClick() {
            EraseModeEnabled = !EraseModeEnabled;
            PencilModeEnabled = false;
            PaintKeepModeEnabled = false;
            PaintEraseModeEnabled = false;
        }

        public void OnPaintKeepModeClick() {
            PaintKeepModeEnabled = !PaintKeepModeEnabled;
            EraseModeEnabled = false;
            PencilModeEnabled = false;
            PaintEraseModeEnabled = false;
        }

        public void OnPaintEraseModeClick() {
            PaintEraseModeEnabled = !PaintEraseModeEnabled;
            EraseModeEnabled = false;
            PencilModeEnabled = false;
            PaintKeepModeEnabled = false;
        }

        public void OnCustomizeWindowSwitch(string param) {
            switch (param) {
                case "P":
                    centralManager!.getUIManager().switchWindow(Windows.PAINTING);
                    break;
                case "M":
                    centralManager!.getUIManager().switchWindow(Windows.MAIN);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void setLoading(bool value) {
            IsLoading = value;
            centralManager?.getMainWindow().InvalidateVisual();
        }
    }
}