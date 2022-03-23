using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using ReactiveUI;
using WFC4All;
using WFC4ALL.ContentControls;

// ReSharper disable UnusedMember.Global

namespace WFC4ALL.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        private string _modelSelectionText = "Switch to\nTile Mode",
            _selectedCategory,
            _selectedInputImage,
            _stepAmountString = "Steps to take: 1";

        private bool _isPlaying, _paddingEnabled, _instantCollapse, _popupVisible;
        private int _stepAmount = 1, _animSpeed = 100, _imgOutWidth = 24, _imgOutHeight = 24, _patternSize = 3;
        private Bitmap _inputImage, _outputImage;
        private ObservableCollection<TileViewModel> _tiles = new();
        private ObservableCollection<MarkerViewModel> _markers = new();
        private double _timeStampOffset = 0d, _timelineWidth = 600d;

        private InputManager inputManager;
        private Tuple<string, string> lastOverlapSelection, lastSimpleSelection;

        public string ModelSelectionText {
            get => _modelSelectionText;
            set => this.RaiseAndSetIfChanged(ref _modelSelectionText, value);
        }

        public string CategorySelection {
            get => _selectedCategory;
            set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        }

        public string InputImageSelection {
            get => _selectedInputImage;
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
            set => this.RaiseAndSetIfChanged(ref _paddingEnabled, value);
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
            set => this.RaiseAndSetIfChanged(ref _imgOutWidth, value);
        }

        public int ImageOutHeight {
            get => _imgOutHeight;
            set => this.RaiseAndSetIfChanged(ref _imgOutHeight, value);
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

        public ObservableCollection<TileViewModel> Tiles {
            get => _tiles;
            set => this.RaiseAndSetIfChanged(ref _tiles, value);
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

        public void setInputManager(InputManager im) {
            lastOverlapSelection = new Tuple<string, string>("Textures", "3Bricks");
            lastSimpleSelection = new Tuple<string, string>("Worlds Top-Down", "Castle");
            inputManager = im;
        }

        public void OnModelClick() {
            inputManager.setModelChanging(true);
#if DEBUG
            Trace.WriteLine("Model Clicked");
#endif
            ModelSelectionText
                = ModelSelectionText.Contains("Smart") ? "Switch to\nTile Mode" : "Switch to\nSmart Mode";
            bool changingToSmart = ModelSelectionText.Contains("Tile");

            string lastCat = CategorySelection;

            string[] catDataSource = InputManager.getCategories(changingToSmart ? "overlapping" : "simpletiled");

            //if (tabSelection.SelectedIndex == 2) {
            string lastImg = InputImageSelection;
            int catIndex = changingToSmart
                ? Array.IndexOf(catDataSource, lastOverlapSelection.Item1)
                : Array.IndexOf(catDataSource, lastSimpleSelection.Item1);
            inputManager.updateCategories(catDataSource, catIndex);

            string[] images = inputManager.getImages(
                changingToSmart ? "overlapping" : "simpletiled",
                changingToSmart ? lastOverlapSelection.Item1 : lastSimpleSelection.Item1);

            int index = changingToSmart
                ? Array.IndexOf(images, lastOverlapSelection.Item2)
                : Array.IndexOf(images, lastSimpleSelection.Item2);
            inputManager.updateInputImages(images, index);
            (int[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(images[index]);
            inputManager.updatePatternSizes(patternSizeDataSource, i);

            if (changingToSmart) {
                lastSimpleSelection = new Tuple<string, string>(lastCat, lastImg);
            } else {
                lastOverlapSelection = new Tuple<string, string>(lastCat, lastImg);
            }
            //}

            inputManager.setModelChanging(false);

            inputManager.setInputChanged("Model change");
            inputManager.getIC().inImgCBChangeHandler(null, null);
        }

        public void OnPaddingClick() {
#if DEBUG
            Trace.WriteLine("Padding Clicked");
#endif
            inputManager.setInputChanged("Padding Button");
            PaddingEnabled = !PaddingEnabled;
            inputManager.restartSolution();
        }

        public void OnAnimate() {
#if DEBUG
            Trace.WriteLine("Animate Clicked");
#endif
            IsPlaying = !IsPlaying;
            inputManager.animate();
        }

        public void OnRestart() {
#if DEBUG
            Trace.WriteLine("Restart Clicked");
#endif
            inputManager.restartSolution();
        }

        public void OnRevert() {
#if DEBUG
            Trace.WriteLine("Revert Clicked");
#endif
            inputManager.revertStep();
        }

        public void OnAdvance() {
#if DEBUG
            Trace.WriteLine("Advance Clicked");
#endif
            inputManager.advanceStep();
        }

        public void OnSave() {
#if DEBUG
            Trace.WriteLine("Save Clicked");
#endif
            inputManager.placeMarker();
        }

        public void OnLoad() {
#if DEBUG
            Trace.WriteLine("Load Clicked");
#endif
            inputManager.loadMarker();
        }

        public void OnExport() {
#if DEBUG
            Trace.WriteLine("Export Clicked");
#endif
            inputManager.export();
        }

        public void OnInfoClick() {
#if DEBUG
            Trace.WriteLine("Info Clicked");
#endif
            inputManager.showPopUp();
        }

        public void OnCloseClick() {
#if DEBUG
            Trace.WriteLine("Close Clicked");
#endif
            inputManager.hidePopUp();
        }
    }
}