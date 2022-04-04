using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace WFC4ALL.Models {
    public class TileViewModel : ReactiveObject {
        private readonly WriteableBitmap _patternImage = null!;
        private readonly Color _patternColour;
        private readonly int _patternWeight, _patternIndex, _patternRotation, _patternFlipping;

        public WriteableBitmap PatternImage {
            get => _patternImage;
            private init => this.RaiseAndSetIfChanged(ref _patternImage, value);
        }

        private int PatternWeight {
            init => this.RaiseAndSetIfChanged(ref _patternWeight, value);
        }

        public int PatternIndex {
            get => _patternIndex;
            private init => this.RaiseAndSetIfChanged(ref _patternIndex, value);
        }

        public Color PatternColour {
            get => _patternColour;
            private init => this.RaiseAndSetIfChanged(ref _patternColour, value);
        }

        public int PatternRotation {
            get => _patternRotation + 90;
            private init => this.RaiseAndSetIfChanged(ref _patternRotation, value);
        }

        public int PatternFlipping {
            get => _patternFlipping;
            private init => this.RaiseAndSetIfChanged(ref _patternFlipping, value);
        }

        /*
         * Used for input patterns
         */
        public TileViewModel(WriteableBitmap image, double weight, int index, bool isF = false) {
            PatternImage = image;
            PatternWeight = (int) weight;
            PatternIndex = index;
            PatternRotation = 0;
            PatternFlipping = isF ? -1 : 1;
        }

        /*
         * Used for Adjacent Tiles
         */
        public TileViewModel(WriteableBitmap image, int index, int patternRotation, int patternFlipping) {
            PatternImage = image;
            PatternIndex = index;
            PatternRotation = patternRotation;
            PatternFlipping = patternFlipping;
        }

        /*
         * Used for Overlapping Tiles
         */
        public TileViewModel(WriteableBitmap image, int index, Color c) {
            PatternImage = image;
            PatternIndex = index;
            PatternColour = c;
            PatternRotation = 0;
            PatternFlipping = 1;
        }
    }
}