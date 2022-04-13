using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using WFC4ALL.Managers;
using WFC4ALL.Utils;

namespace WFC4ALL.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        private string _selectedCategory = "",
            _selectedInputImage = "",
            _stepAmountString = "Steps to take: 1";

        private bool _isPlaying,
            _seamlessOutput,
            _inputWrapping,
            _instantCollapse,
            _popupVisible,
            _isLoading,
            _advancedEnabled,
            _simpleModel,
            _advancedOverlapping,
            _irreversiblePopupAccepted;

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

        private bool _pencilModeEnabled, _eraseModeEnabled, _paintKeepModeEnabled, _paintEraseModeEnabled, _isRunning;

        public bool SimpleModelSelected {
            get => _simpleModel;
            private set {
                this.RaiseAndSetIfChanged(ref _simpleModel, value);
                OverlappingAdvancedEnabled = AdvancedEnabled && !SimpleModelSelected;
            }
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

        public bool SeamlessOutput {
            get => _seamlessOutput;
            private set => this.RaiseAndSetIfChanged(ref _seamlessOutput, value);
        }

        public bool InputWrapping {
            get => _inputWrapping;
            private set => this.RaiseAndSetIfChanged(ref _inputWrapping, value);
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
            private set => this.RaiseAndSetIfChanged(ref _imgOutWidth, Math.Min(Math.Max(10, value), 128));
        }

        public int ImageOutHeight {
            get => _imgOutHeight;
            private set => this.RaiseAndSetIfChanged(ref _imgOutHeight, Math.Min(Math.Max(10, value), 128));
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

        private bool AdvancedEnabled {
            get => _advancedEnabled;
            set {
                this.RaiseAndSetIfChanged(ref _advancedEnabled, value);
                OverlappingAdvancedEnabled = AdvancedEnabled && !SimpleModelSelected;
            }
        }

        private bool OverlappingAdvancedEnabled {
            get => _advancedOverlapping;
            set => this.RaiseAndSetIfChanged(ref _advancedOverlapping, value);
        }

        public bool IsRunning {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        public bool IrreversiblePopupAccepted {
            get => _irreversiblePopupAccepted;
            set => this.RaiseAndSetIfChanged(ref _irreversiblePopupAccepted, value);
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
            SimpleModelSelected = !SimpleModelSelected;
            
            if (IsPlaying) {
                OnAnimate();
            }

            bool changingToSmart = !SimpleModelSelected;

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

        public void OnPaddingToggle() {
            if (IsPlaying) {
                OnAnimate();
            }
            SeamlessOutput = !SeamlessOutput;
            centralManager!.getInputManager().restartSolution();
        }

        public void OnInputWrappingChanged() {
            InputWrapping = !InputWrapping;
            centralManager!.getInputManager().restartSolution();
        }

        public void OnAnimate() {
            IsPlaying = !IsPlaying;
            centralManager!.getInputManager().animate();
        }

        public void OnRestart() {
            if (IsPlaying) {
                OnAnimate();
            }
            centralManager!.getInputManager().restartSolution();
        }

        public void OnRevert() {
            if (IsPlaying) {
                OnAnimate();
            }
            centralManager!.getInputManager().revertStep();
        }

        public void OnAdvance() {
            if (IsPlaying) {
                OnAnimate();
            }
            centralManager!.getInputManager().advanceStep();
        }

        public void OnSave() {
            centralManager!.getInputManager().placeMarker(false);
        }

        public void OnLoad() {
            if (IsPlaying) {
                OnAnimate();
            }
            centralManager!.getInputManager().loadMarker();
        }

        public void OnExport() {
            if (IsPlaying) {
                OnAnimate();
            }
            centralManager!.getInputManager().exportSolution();
        }

        public void OnInfoClick() {
            if (IsPlaying) {
                OnAnimate();
            }
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
                    if (IsPlaying) {
                        OnAnimate();
                    }
                    centralManager!.getUIManager().switchWindow(Windows.PAINTING, false);
                    break;
                case "M":
                    centralManager!.getUIManager().switchWindow(Windows.MAIN, false);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void OnPopupAcceptClick() {
            IrreversiblePopupAccepted = true;
        }

        public void setLoading(bool value) {
            IsLoading = value;
            centralManager?.getMainWindow().InvalidateVisual();
        }
    }
}