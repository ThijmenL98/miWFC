using Avalonia.Media.Imaging;
using ReactiveUI;
using System.Diagnostics;
using WFC4All;

namespace WFC4ALL.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _modelSelectionText = "Switch to\nTile Mode", _selectedCategory, _selectedInputImage;
        private bool _isPlaying = false, _paddingEnabled = false;
        private int _stepAmount = 1, _animSpeed = 25, _imgOutWidth = 24, _imgOutHeight = 24;
        private Bitmap _inputImage, _outputImage;

        private InputManager inputManager;

        public string ModelSelectionText { get => _modelSelectionText; set => this.RaiseAndSetIfChanged(ref _modelSelectionText, value); }
        public string CategorySelection { get => _selectedCategory; set => this.RaiseAndSetIfChanged(ref _selectedCategory, value); }
        public string InputImageSelection { get => _selectedInputImage; set => this.RaiseAndSetIfChanged(ref _selectedInputImage, value); }
        public bool IsPlaying { get => _isPlaying; set => this.RaiseAndSetIfChanged(ref _isPlaying, value); }
        public bool PaddingEnabled { get => _paddingEnabled; set => this.RaiseAndSetIfChanged(ref _paddingEnabled, value); }
        public int StepAmount { get => _stepAmount; set => this.RaiseAndSetIfChanged(ref _stepAmount, value); }
        public int AnimSpeed { get => _animSpeed; set => this.RaiseAndSetIfChanged(ref _animSpeed, value); }
        public int ImageOutWidth { get => _imgOutWidth; set => this.RaiseAndSetIfChanged(ref _imgOutWidth, value); }
        public int ImageOutHeight { get => _imgOutHeight; set => this.RaiseAndSetIfChanged(ref _imgOutHeight, value); }
        public Bitmap InputImage { get => _inputImage; set => this.RaiseAndSetIfChanged(ref _inputImage, value); }
        public Bitmap OutputImage { get => _outputImage; set => this.RaiseAndSetIfChanged(ref _outputImage, value); }
        public MainWindowViewModel VM { get => this; }

        public void setInputManager(InputManager im)
        {
            this.inputManager = im;
        }

        public void OnModelClick()
        {
            ModelSelectionText = ModelSelectionText.Equals("Switch to\nSmart Mode") ? "Switch to\nTile Mode" : "Switch to\nSmart Mode";
            //TODO Save old cat + input image
            Trace.WriteLine(CategorySelection);
            Trace.WriteLine(InputImageSelection);
            //TODO More Logic
        }

        public void OnPaddingClick()
        {
            PaddingEnabled = !PaddingEnabled;
            //TODO More Logic
        }

        public void OnAnimate()
        {
            IsPlaying = !IsPlaying;

            inputManager.execute();
            // TODO Logic
        }
    }
}
