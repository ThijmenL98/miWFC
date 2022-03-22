using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WFC4All;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable UnusedParameter.Local

namespace WFC4ALL.ContentControls {
    public partial class InputControl : UserControl {
        private InputManager? inputManager;
        private readonly ComboBox _categoryCB, _inputCB, _patternSizeCB;

        public InputControl() {
            InitializeComponent();

            _categoryCB = this.Find<ComboBox>("categoryCB");
            _inputCB = this.Find<ComboBox>("inputCB");
            _patternSizeCB = this.Find<ComboBox>("patternSizeCB");
        }

        public void setInputManager(InputManager im) {
            inputManager = im;
            inImgCBChangeHandler(null, null);
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        /*
         * Handlers
         */

        private void catCBChangeHandler(object _, SelectionChangedEventArgs e) {
            if (inputManager == null || inputManager.isChangingModels()) {
                return;
            }

            inputManager.setInputChanged("Category CB");

            string newValue = getCategory();

            this.Find<Button>("borderPaddingToggle").IsVisible = newValue.Equals("Textures");
            string[] inputImageDataSource
                = inputManager.getImages(
                    ((string) this.Find<Button>("modeToggle").Content).Contains("Tile") ? "overlapping" : "simpletiled",
                    newValue);
            setInputImages(inputImageDataSource);
            e.Handled = true;
        }

        public void inImgCBChangeHandler(object? _, SelectionChangedEventArgs? e) {
            if (inputManager == null || inputManager.isChangingModels()) {
                return;
            }

            inputManager.setImageChanging(true);

            string newValue = getInputImage();
            inputManager.updateInputImage(newValue);

            inputManager.setInputChanged("Image CB");

            if (((string) this.Find<Button>("modeToggle").Content).Contains("Tile")) {
                (int[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(newValue);
                setPatternSizes(patternSizeDataSource, i);
            }

            //updateInputPadding();
            inputManager.restartSolution();
            if (e != null) {
                e.Handled = true;
            }
            inputManager.setImageChanging(false);
        }

        private void pattSizeCBChangeHandler(object? _, SelectionChangedEventArgs e) {
            inputManager?.setInputChanged("Pattern Size CB");
            e.Handled = true;
            inputManager?.restartSolution();
        }

        /*
         * Setters
         */

        public void setInputImages(string[]? values, int idx = 0) {
            if (values != null) {
                _inputCB.Items = values;
            }

            _inputCB.SelectedIndex = idx;
        }

        public void setPatternSizes(int[]? values, int idx = 0) {
            if (values != null) {
                _patternSizeCB.Items = values;
            }

            _patternSizeCB.SelectedIndex = idx;
        }

        public void setCategories(string[]? values, int idx = 0) {
            if (values != null) {
                _categoryCB.Items = values;
            }

            _categoryCB.SelectedIndex = idx;
        }

        /*
         * Getters
         */

        public string getInputImage() {
            return _inputCB.SelectedItem as string ?? "3Bricks";
        }

        public string getCategory() {
            return _categoryCB.SelectedItem as string ?? "Textures";
        }

        public int getPatternSize() {
            return (int) (_patternSizeCB.SelectedItem ?? 3);
        }
    }
}