using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System.Linq;
using WFC4All;

namespace WFC4ALL.ContentControls
{
    public partial class InputControl : UserControl
    {

        private InputManager? inputManager;
        private ComboBox _categoryCB, _inputCB, _patternSizeCB;

        public InputControl()
        {
            InitializeComponent();

            _categoryCB = this.Find<ComboBox>("categoryCB");
            _inputCB = this.Find<ComboBox>("inputCB");
            _patternSizeCB = this.Find<ComboBox>("patternSizeCB");
        }

        public void setInputManager(InputManager im)
        {
            inputManager = im;
            inImgCBChangeHandler(null, null);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /*
         * Handlers
         */

        private void catCBChangeHandler(object sender, SelectionChangedEventArgs e)
        {
            if (inputManager == null)
            {
                return;
            }

            string newValue = getCategory();

            this.Find<Button>("borderPaddingToggle").IsVisible = newValue.Equals("Textures");
            string[] inputImageDataSource
                = inputManager.getImages(
                    ((string)this.Find<Button>("modeToggle").Content).Contains("Tile") ? "overlapping" : "simpletiled",
                    newValue);
            Trace.WriteLine(inputImageDataSource.Count());
            setInputImages(inputImageDataSource);
            e.Handled = true;
        }

        private void inImgCBChangeHandler(object? sender, SelectionChangedEventArgs e)
        {
            if (inputManager == null)
            {
                return;
            }

            Trace.WriteLine("CHANGE Img");
            string newValue = getInputImage();
            inputManager.updateInputImage(newValue);

            if (((string)this.Find<Button>("modeToggle").Content).Contains("Tile"))
            {
                (int[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(newValue);
                setPatternSizes(patternSizeDataSource, i);
            }

            //updateInputPadding();

            inputManager.setInputChanged("Changing image");
            //executeButton_Click(null, null);
            if (e != null)
            {
                e.Handled = true;
            }
        }

        private void pattSizeCBChangeHandler(object? sender, SelectionChangedEventArgs e)
        {
            Trace.WriteLine("CHANGE PattSize");
            e.Handled = true;
        }

        /*
         * Setters
         */

        public void setInputImages(string[] values)
        {
            _inputCB.Items = values;
            _inputCB.SelectedIndex = 0;
        }
        public void setPatternSizes(int[] values, int idx)
        {
            _patternSizeCB.Items = values;
            _patternSizeCB.SelectedIndex = idx;
        }

        public void setCategories(string[] values)
        {
            _categoryCB.Items = values;
            _categoryCB.SelectedIndex = 0;
        }

        /*
         * Getters
         */

        public string getInputImage()
        {
            return _inputCB.SelectedItem as string ?? "3Bricks";
        }

        public string getCategory()
        {
            return _categoryCB.SelectedItem as string ?? "Textures";
        }

        public int getPatternSize()
        {
            return int.Parse(_patternSizeCB.SelectedItem as string ?? "3");
        }
    }
}
