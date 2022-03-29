using Avalonia.Media.Imaging;
using ReactiveUI;

namespace WFC4ALL.ContentControls {
    public class TileViewModel : ReactiveObject {

        private readonly WriteableBitmap _patternImage = null!;
        private int _patternWeight;
        private readonly int _patternIndex;

        public WriteableBitmap PatternImage {
            get => _patternImage;
            private init => this.RaiseAndSetIfChanged(ref _patternImage, value);
        }

        public int PatternWeight {
            get => _patternWeight;
            set => this.RaiseAndSetIfChanged(ref _patternWeight, value);
        }

        public int PatternIndex {
            get => _patternIndex;
            private init => this.RaiseAndSetIfChanged(ref _patternIndex, value);
        }

        public TileViewModel(WriteableBitmap image, double weight, int index) {
            PatternImage = image;
            PatternWeight = (int) weight;
            PatternIndex = index;
        }

    }
}