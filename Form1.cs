using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using WFC4All.DeBroglie.Models;
using WFC4All.enums;
using WFC4All.Properties;

namespace WFC4All {
    public partial class Form1 : Form {
        private readonly InputManager inputManager;
        public readonly BitMaps bitMaps;
        private readonly PictureBox[] pbs;

        private int defaultSymmetry, savePoint, lastRanForced;
        private readonly int initOutWidth;
        private readonly int initInWidth;
        private readonly int initOutHeight;
        private readonly int initInHeight;

        private bool defaultInputPadding, changingIndex;
        public bool isChangingModels;
        private bool infoClicked;

        private Bitmap result;

        private readonly XDocument xDoc;

        private Size oldSize;

        private Timer animationTimer = new();
        
        private Tuple<string, string> lastOverlapSelection, lastSimpleSelection;

        private readonly string[] patternHeuristicDataSource = {"Weighted", "Random", "Least Used"},
            selectionHeuristicDataSource = {"Least Entropy", "Simple", "Lexical", "Random", "Spiral", "Hilbert Curve"};

        public Form1() {
            Icon = Resources.icon;

            xDoc = XDocument.Parse(Resources.samples);
            defaultSymmetry = 8;
            oldSize = new Size(1616, 939);
            result = new Bitmap(1, 1);
            savePoint = 0;
            isChangingModels = false;
            defaultInputPadding = true;
            inputManager = new InputManager(this);
            bitMaps = new BitMaps(this, inputManager);

            lastOverlapSelection = new Tuple<string, string>("Textures","3Bricks");
            lastSimpleSelection = new Tuple<string, string>("Worlds Top-Down","Castle");

            InitializeComponent();
            initializeAnimations();
            initializeToolTips();

            initInWidth = inputImagePB.Width;
            initInHeight = inputImagePB.Height;
            initOutWidth = resultPB.Width;
            initOutHeight = resultPB.Height;

            pbs = new[] {p1RotPB, p2RotPB, p3RotPB};

            string[] inputImageDataSource = inputManager.getImages("overlapping", "Textures"); // or "simpletiled"
            inputImageCB.DataSource = inputImageDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(inputImageDataSource[0]);
            patternSize.SelectedText = inputImageDataSource[0];

            string[] categoriesDataSource = InputManager.getCategories("overlapping");
            categoryCB.DataSource = categoriesDataSource;
            categoryCB.SelectedIndex = 0;

            selectionHeuristicCB.DataSource = selectionHeuristicDataSource;
            InputManager.setSelectionHeuristic(SelectionHeuristic.ENTROPY);

            patternHeuristicCB.DataSource = patternHeuristicDataSource;
            InputManager.setPatternHeuristic(PatternHeuristic.WEIGHTED);

            (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(inputImageDataSource[0]);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

            Bitmap disabledInit = new(Resources.borderPaddingDisabled);
            inputPaddingPB.Image = inputManager.resizePixels(inputPaddingPB, disabledInit, 3, Color.Red, false);
            inputPaddingPB.BackColor = Color.Red;
            inputPaddingPB.Padding = new Padding(3);
            inputPaddingPB.MouseHover += (sender, eventArgs) => {
                addHover(sender, eventArgs, "Toggle boundary padding");
            };

            KeyPreview = true;
            infoGraphicPB.Visible = false;
            closeButton.Visible = false;
            //TODO Re-enable initializeRotations();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (e.KeyData) {
                case Keys.Left:
                case Keys.Delete:
                case Keys.Back:
                    if (infoClicked) {
                        infoButton_Click(null, e);
                    } else {
                        backButton_Click(null, e);
                    }

                    e.Handled = true;
                    break;
                case Keys.Right:
                    advanceButton_Click(null, e);
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                case Keys.PageUp:
                case Keys.Up:
                case Keys.Down:
                    e.Handled = true;
                    break;
                case Keys.Space:
                case Keys.P:
                    animateButton_Click(null, e);
                    e.Handled = true;
                    break;
                case Keys.S:
                    markerButton_Click(null, e);
                    e.Handled = true;
                    break;
                case Keys.L:
                    revertMarkerButton_Click(null, e);
                    e.Handled = true;
                    break;
                case Keys.R:
                    executeButton_Click(null, e);
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    if (infoClicked) {
                        infoButton_Click(null, e);
                    }

                    e.Handled = true;
                    break;
                default:
                    base.OnKeyDown(e);
                    break;
            }
        }

        protected override void OnResize(EventArgs e) {
            Control.ControlCollection[] allControls = {
                Controls, inputTab.Controls, patternPanel.Controls, inputPanel.Controls, pattHeurPanel.Controls,
                selHeurPanel.Controls
            };

            foreach (Control.ControlCollection cList in allControls) {
                foreach (Control cnt in cList) {
                    resizeAll(cnt, Size);
                }
            }

            oldSize = new Size(Size.Width, Size.Height);
        }

        private void resizeAll(Control control, Size newSize) {
            int width = newSize.Width - oldSize.Width;
            control.Left += control.Left * width / oldSize.Width;
            control.Width += control.Width * width / oldSize.Width;

            int height = newSize.Height - oldSize.Height;
            control.Top += control.Top * height / oldSize.Height;
            control.Height += control.Height * height / oldSize.Height;
        }

        /* ------------------------------------------------------------------------
         * Button Click Functions
         * ------------------------------------------------------------------------ */

        private void executeButton_Click(object sender, EventArgs e) {
            if (changingIndex) {
                return;
            }

            try {
                (Bitmap result2, bool _) = inputManager.initAndRunWfcDB(true, getStepAmount());
                while (result2 == null) {
                    (result2, _) = inputManager.initAndRunWfcDB(true, getStepAmount());
                }

                try {
                    resultPB.Image = inputManager.resizeBitmap(result2,
                        Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result.Width));
                } catch (Exception exception) {
                    Console.WriteLine(exception);
                }

                result = new Bitmap(result2);
                result2.Dispose();
            } catch (Exception exception) {
                Console.WriteLine(exception);
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void advanceButton_Click(object sender, EventArgs e) {
            int now = DateTime.Now.Millisecond;

            if (sender == null && now - lastRanForced <= 50) {
                lastRanForced = now;
                return;
            }

            try {
                (Bitmap result2, bool finished) = inputManager.initAndRunWfcDB(false, getStepAmount());
                if (finished) {
                    return;
                }

                resultPB.Image = inputManager.resizeBitmap(result2,
                    Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result2.Width));

                result = new Bitmap(result2);
                result2.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void backButton_Click(object sender, EventArgs e) {
            try {
                Bitmap result2 = inputManager.stepBackWfc(getStepAmount());
                resultPB.Image = inputManager.resizeBitmap(result2,
                    Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result2.Width));

                int curStep = inputManager.getCurrentStep();
                if (curStep < savePoint) {
                    savePoint = curStep;
                }

                result = new Bitmap(result2);
                result2.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void animateButton_Click(object sender, EventArgs e) {
            int now = DateTime.Now.Millisecond;

            if (inputManager.isCollapsed()) {
                executeButton_Click(sender, e);
            }

            if (sender == null && now - lastRanForced <= 50) {
                lastRanForced = now;
                return;
            }

            if (animateButton.BackgroundImage.Tag == null || animateButton.BackgroundImage.Tag.Equals("Animate")) {
                lock (animationTimer) {
                    animationTimer.Interval = getAnimationSpeed();
                    animationTimer.Tick += animationAnimationTimerTick;
                    animationTimer.Start();
                }

                animateButton.BackgroundImage = sender == null ? Resources.Pause : Resources.PauseHover;
                animateButton.BackgroundImage.Tag = "Pause";
            } else {
                lock (animationTimer) {
                    animationTimer.Stop();
                    animationTimer = new Timer();
                }

                animateButton.BackgroundImage = sender == null ? Resources.Animate : Resources.AnimateHover;
                animateButton.BackgroundImage.Tag = "Animate";
            }
        }

        private void modelChoice_Click(object sender, EventArgs e) {
            isChangingModels = true;
            Button btn = (Button) sender;
            
            string lastCat = categoryCB.Text;
            string lastImg = inputImageCB.Text;

            outputHeightValue.Value = 24;
            outputWidthValue.Value = 24;

            string[] catDataSource = InputManager.getCategories(modelChoice.Text.Equals("Switch to Tile Mode")
                ? "simpletiled"
                : "overlapping");
            categoryCB.DataSource = catDataSource;

            btn.Text = btn.Text.Equals("Switch to Tile Mode") ? "Switch to Smart Mode" : "Switch to Tile Mode";
            bool switchingToOverlap = btn.Text.Equals("Switch to Tile Mode");
            
            showRotationalOptions(switchingToOverlap);
            
            int catIndex = switchingToOverlap ? Array.IndexOf(catDataSource, lastOverlapSelection.Item2) : Array.IndexOf(catDataSource, lastSimpleSelection.Item2);
            categoryCB.SelectedIndex = catIndex;
            categoryCB.Text = switchingToOverlap ? lastOverlapSelection.Item1 : lastSimpleSelection.Item1;

            string[] images = inputManager.getImages(
                switchingToOverlap ? "overlapping" : "simpletiled",
                switchingToOverlap ? lastOverlapSelection.Item1 : lastSimpleSelection.Item1);

            inputImageCB.DataSource = images;
            int index = switchingToOverlap ? Array.IndexOf(images, lastOverlapSelection.Item2) : Array.IndexOf(images, lastSimpleSelection.Item2);
            inputImageCB.SelectedIndex = index;
            (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(images[index]);
            patternSize.DataSource = patternSizeDataSource;
            patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

            if (switchingToOverlap) {
                lastSimpleSelection = new Tuple<string, string>(lastCat, lastImg);
            } else{
                lastOverlapSelection = new Tuple<string, string>(lastCat, lastImg);
            }

            isChangingModels = false;

            patternSize.Refresh();
            inputImageCB.Refresh();
            categoryCB.Refresh();
            inputManager.setInputChanged("Model change");
            inputImage_SelectedIndexChanged(inputImageCB, e);
        }

        private void markerButton_Click(object sender, EventArgs e) {
            savePoint = inputManager.getCurrentStep();
        }

        private void revertMarkerButton_Click(object sender, EventArgs e) {
            int stepsToRevert = inputManager.getCurrentStep() - savePoint;
            try {
                result = inputManager.stepBackWfc(stepsToRevert);
                resultPB.Image = inputManager.resizeBitmap(result,
                    Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));

                markerButton_Click(null, null);

                result.Dispose();
            } catch (Exception) {
                resultPB.Image = InputManager.getImage("NoResultFound");
            }
        }

        private void infoButton_Click(object sender, EventArgs e) {
            infoClicked = !infoClicked;
            
            closeButton.Visible = infoClicked;
            infoGraphicPB.Visible = infoClicked;
            
            foreach (Control c in inputTab.Controls) {
                if (c != loadingPB && c != patternRotationLabel && c != p1RotPB && c != p2RotPB && c != p3RotPB &&
                    c != originalRotPB && c != selHeurPanel && c != pattHeurPanel && c != closeButton && c != infoGraphicPB) {
                    c.Visible = !infoClicked;
                }
            }
        }
        private void closeButton_Click(object sender, EventArgs e) {
            infoButton_Click(sender, e);
        }

        /* ------------------------------------------------------------------------
         * Other Input Interaction Functions
         * ------------------------------------------------------------------------ */

        private static void pictureBoxMouseDown(object sender, MouseEventArgs e) {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left) {
                PictureBox pb = (PictureBox) sender;
                pb.BackColor = pb.BackColor.Equals(Color.LawnGreen) ? Color.Red : Color.LawnGreen;
            }
        }
        
        private void inputImage_SelectedIndexChanged(object sender, EventArgs e) {
            if (isChangingModels) {
                return;
            }
            changingIndex = true;
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            updateInputImage(newValue);

            if (modelChoice.Text.Equals("Switch to Tile Mode")) {
                XElement curSelection = xDoc.Root.elements("overlapping").Where(x =>
                    x.get<string>("name") == getSelectedInput()).ElementAtOrDefault(0);
                (object[] patternSizeDataSource, int i) = inputManager.getImagePatternDimensions(newValue);
                patternSize.DataSource = patternSizeDataSource;
                patternSize.SelectedIndex = patternSize.Items.IndexOf(patternSizeDataSource[i]);

                defaultSymmetry = curSelection.get("symmetry", 8);
                defaultInputPadding = curSelection.get("periodicInput", true);
                inputPaddingPB.BackColor = defaultInputPadding ? Color.LawnGreen : Color.Red;
            }

            outputHeightValue.Value = 24;
            outputWidthValue.Value = 24;

            outputHeightValue.Refresh();
            outputWidthValue.Refresh();

            Refresh();

            updateRotations();
            updateInputPadding();

            changingIndex = false;
            inputManager.setInputChanged("Changing image");
            executeButton_Click(null, null);
        }

        private void resultPB_Click(object sender, EventArgs e) {
            Point clickPos = ((PictureBox) sender).PointToClient(Cursor.Position);
            int clickX = clickPos.X, clickY = clickPos.Y, width = getOutputWidth(), height = getOutputHeight();
            int a = (int) Math.Floor((double) clickX * width / ((PictureBox) sender).Width),
                b = (int) Math.Floor((double) clickY * height / ((PictureBox) sender).Height);
            Console.WriteLine($@"(x:{clickX}, y:{clickY}) -> (a:{a}, b:{b})");

            //TODO CF2
            // result.SetPixel(a, b, Color.Red);
            //
            // resultPB.Image = InputManager.resizeBitmap(result,
            //     Math.Min(initOutHeight / (float) result.Height, initOutWidth / (float) result.Width));
        }

        private void selectionHeuristicCB_SelectedIndexChanged(object sender, EventArgs e) {
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            switch (Array.FindIndex(selectionHeuristicDataSource, x => x.Contains(newValue))) {
                case 0: // Least Entropy
                    selectionHeuristicPB.Image = Resources.Entropy;
                    selectionHeuristicDesc.Text
                        = @"Select the most logical choice based on available options, solve ties randomly";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.ENTROPY);
                    break;
                case 1: // Simple
                    selectionHeuristicPB.Image = Resources.Simple;
                    selectionHeuristicDesc.Text = @"Similar to least entropy, but solve ties lexically";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.SIMPLE);
                    break;
                case 2: // Lexical
                    selectionHeuristicPB.Image = Resources.Lexical;
                    selectionHeuristicDesc.Text = @"In reading order, starting left to right, top to bottom";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.LEXICAL);
                    break;
                case 3: // Random
                    selectionHeuristicPB.Image = Resources.Random;
                    // ReSharper disable once LocalizableElement
                    selectionHeuristicDesc.Text = "Select randomly\nWarning, Slow!";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.RANDOM);
                    break;
                case 4: // Spiral
                    selectionHeuristicPB.Image = Resources.Spiral;
                    selectionHeuristicDesc.Text = @"Select in an outwards spiral fashion";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.SPIRAL);
                    break;
                case 5: // Hilbert Curve
                    selectionHeuristicPB.Image = Resources.Hilbert;
                    selectionHeuristicDesc.Text = @"Select following a space-filling path";
                    InputManager.setSelectionHeuristic(SelectionHeuristic.HILBERT);
                    break;
                default:
                    return;
            }

            selectionHeuristicDesc.Refresh();
            selectionHeuristicPB.Refresh();
            ((ComboBox) sender).Refresh();
            executeButton_Click(null, null);
        }

        private void patternHeuristicCB_SelectedIndexChanged(object sender, EventArgs e) {
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            switch (Array.FindIndex(patternHeuristicDataSource, x => x.Contains(newValue))) {
                case 0: // Weighted Choice
                    patternHeuristicPB.Image = Resources.Weighted;
                    patternHeuristicDesc.Text = @"Select the next pattern though prominence in the input image";
                    InputManager.setPatternHeuristic(PatternHeuristic.WEIGHTED);
                    break;
                case 1: // Random Choice
                    patternHeuristicPB.Image = Resources.Random;
                    patternHeuristicDesc.Text = @"Randomly select the pattern to use next";
                    InputManager.setPatternHeuristic(PatternHeuristic.RANDOM);
                    break;
                case 2: // Least Used
                    patternHeuristicPB.Image = Resources.LeastUsed;
                    patternHeuristicDesc.Text = @"Select the pattern that has been used the least so far";
                    InputManager.setPatternHeuristic(PatternHeuristic.LEAST_USED);
                    break;
                default:
                    return;
            }

            patternHeuristicDesc.Refresh();
            selectionHeuristicPB.Refresh();
            ((ComboBox) sender).Refresh();
            executeButton_Click(null, null);
        }

        private void categoryCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (isChangingModels) {
                return;
            }
            string newValue = ((ComboBox) sender).SelectedItem.ToString();
            string[] inputImageDataSource
                = inputManager.getImages(
                    modelChoice.Text.Equals("Switch to Tile Mode") ? "overlapping" : "simpletiled",
                    newValue);
            inputImageCB.DataSource = inputImageDataSource;
        }

        private void outputHeightValue_ValueChanged(object sender, EventArgs e) {
            if (categoryCB.Text.Equals("Fonts")) {
                ((NumericUpDown) sender).Value = Math.Min(((NumericUpDown) sender).Value, 25);
            }

            executeButton_Click(sender, e);
        }

        private void inputPaddingPB_Click(object sender, EventArgs e) {
            Color c;
            Bitmap bm;
            if (inputPaddingEnabled()) {
                bm = new Bitmap(Resources.borderPaddingDisabled);
                c = Color.Red;
            } else {
                bm = new Bitmap(Resources.borderPaddingEnabled);
                c = Color.LawnGreen;
            }

            inputPaddingPB.Image = inputManager.resizePixels(inputPaddingPB, bm, 3, c, false);
            inputPaddingPB.BackColor = c;
            inputPaddingPB.Padding = new Padding(3);

            ((PictureBox) sender).Refresh();

            inputManager.setInputChanged("Input padding");
            executeButton_Click(null, null);
        }

        /* ------------------------------------------------------------------------
         * Mouse & Keyboard Interaction
         * ------------------------------------------------------------------------ */

        private void stepSizeTrackbar_Scroll(object sender, EventArgs e) {
            int value = stepSizeTrackbar.Value;
            bool instant = value == 100;

            stepSizeLabel.Text = instant ? @"Instantly generate an image" : @$"Number of steps to take: {value}";

            advanceButton.Enabled = !instant;
            backButton.Enabled = !instant;
            animateButton.Enabled = !instant;
            markerButton.Enabled = !instant;
            revertMarkerButton.Enabled = !instant;

            animateButton.BackgroundImage = instant ? Resources.AnimateDisabled : Resources.Animate;
            animateButton.BackgroundImage.Tag = "Animate";

            advanceButton.BackgroundImage = instant ? Resources.AdvanceDisabled : Resources.Advance;
            backButton.BackgroundImage = instant ? Resources.RevertDisabled : Resources.Revert;
            markerButton.BackgroundImage = instant ? Resources.SaveDisabled : Resources.Save;
            revertMarkerButton.BackgroundImage = instant ? Resources.LoadDisabled : Resources.Load;
        }

        /* ------------------------------------------------------------------------
         * Interface value retrieval
         * ------------------------------------------------------------------------ */

        public int getOutputWidth() {
            return (int) outputWidthValue.Value;
        }

        private int getStepAmount() {
            int value = stepSizeTrackbar.Value;
            bool instant = value == 100;
            return instant ? -1 : value;
        }

        private int getAnimationSpeed() {
            int rawValue = animSpeedTrackbar.Value;
            return (int) Math.Exp(3.21887d + rawValue / 25d * 0.05546d);
        }

        public int getOutputHeight() {
            return (int) outputHeightValue.Value;
        }

        public bool inputPaddingEnabled() {
            return inputPaddingPB.BackColor.Equals(Color.LawnGreen);
        }

        public string getSelectedInput() {
            return inputImageCB.SelectedItem.ToString();
        }

        public bool isOverlappingModel() {
            return modelChoice.Text.Equals("Switch to Tile Mode");
        }

        public int getSelectedOverlapTileDimension() {
            return patternSize.SelectedIndex + 2;
        }

        /* ------------------------------------------------------------------------
         * Setters
         * ------------------------------------------------------------------------ */

        private void updateInputImage(string imageName) {
            Bitmap newImage = InputManager.getImage(imageName);

            inputImagePB.Image = inputManager.resizeBitmap(newImage,
                Math.Min(initInHeight / (float) newImage.Height, initInWidth / (float) newImage.Width));

            inputImagePB.Refresh();
        }

        public void displayLoading(bool display) {
            loadingPB.Visible = display;
            loadingPB.Refresh();
        }

        /* ------------------------------------------------------------------------
         * Utils
         * ------------------------------------------------------------------------ */

        private void animationAnimationTimerTick(object sender, EventArgs e) {
            if (InvokeRequired) {
                /* Not on UI thread, reenter there... */
                BeginInvoke(new EventHandler(animationAnimationTimerTick), sender, e);
            } else {
                lock (animationTimer) {
                    /* only work when this is no reentry while we are already working */
                    if (animationTimer.Enabled) {
                        animationTimer.Stop();
                        animationTimer.Interval = getAnimationSpeed();
                        try {
                            (Bitmap result2, bool finished) = inputManager.initAndRunWfcDB(false, getStepAmount());
                            resultPB.Image = inputManager.resizeBitmap(result2,
                                Math.Min(initOutHeight / (float) result2.Height, initOutWidth / (float) result2.Width));

                            result = new Bitmap(result2);
                            result2.Dispose();
                            resultPB.Refresh();
                            if (finished) {
                                animateButton.BackgroundImage = Resources.Animate;
                                animateButton.BackgroundImage.Tag = "Animate";
                                return;
                            }
                        } catch (Exception exception) {
                            Console.WriteLine(exception);
                            resultPB.Image = InputManager.getImage("NoResultFound");
                        }

                        animationTimer.Start();
                    }
                }
            }
        }

        private static Bitmap toGrayScale(Image source) {
            Bitmap grayImage = new(source.Width, source.Height, source.PixelFormat);
            grayImage.SetResolution(source.HorizontalResolution, source.VerticalResolution);

            ColorMatrix grayMatrix = new(new[] {
                new[] {.2126f, .2126f, .2126f, 0f, 0f},
                new[] {.7152f, .7152f, .7152f, 0f, 0f},
                new[] {.0722f, .0722f, .0722f, 0f, 0f},
                new[] {0f, 0f, 0f, 1f, 0f},
                new[] {0f, 0f, 0f, 0f, 1f}
            });

            using Graphics g = Graphics.FromImage(grayImage);
            using ImageAttributes attributes = new();
            attributes.SetColorMatrix(grayMatrix);
            g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            return grayImage;
        }

        // ReSharper disable once UnusedParameter.Local
        private static void addHover(object sender, EventArgs e, string message) {
            ToolTip toolTip = new();

            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 0;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;

            if (sender.GetType() == typeof(PictureBox)) {
                toolTip.SetToolTip((PictureBox) sender, message);
            } else if (sender.GetType() == typeof(Button)) {
                toolTip.SetToolTip((Button) sender, message);
            }
        }

        public bool addPattern(Bitmap bitmap) {
            return bitMaps.addPattern(bitmap, pictureBoxMouseDown);
        }

        public bool addPattern(PatternArray pixels, List<Color> distinctColors) {
            return bitMaps.addPattern(pixels, distinctColors, pictureBoxMouseDown);
        }

        private void initializeAnimations() {
            markerButton.MouseDown += ButtonVisualEventHandler.markerButton_MouseDown;
            markerButton.MouseEnter += ButtonVisualEventHandler.markerButton_MouseEnter;
            markerButton.MouseLeave += ButtonVisualEventHandler.markerButton_MouseLeave;
            markerButton.MouseUp += ButtonVisualEventHandler.markerButton_MouseUp;

            revertMarkerButton.MouseDown += ButtonVisualEventHandler.revertMarkerButton_MouseDown;
            revertMarkerButton.MouseEnter += ButtonVisualEventHandler.revertMarkerButton_MouseEnter;
            revertMarkerButton.MouseLeave += ButtonVisualEventHandler.revertMarkerButton_MouseLeave;
            revertMarkerButton.MouseUp += ButtonVisualEventHandler.revertMarkerButton_MouseUp;

            backButton.MouseDown += ButtonVisualEventHandler.backButton_MouseDown;
            backButton.MouseEnter += ButtonVisualEventHandler.backButton_MouseEnter;
            backButton.MouseLeave += ButtonVisualEventHandler.backButton_MouseLeave;
            backButton.MouseUp += ButtonVisualEventHandler.backButton_MouseUp;

            animateButton.MouseDown += ButtonVisualEventHandler.animateButton_MouseDown;
            animateButton.MouseEnter += ButtonVisualEventHandler.animateButton_MouseEnter;
            animateButton.MouseLeave += ButtonVisualEventHandler.animateButton_MouseLeave;

            advanceButton.MouseDown += ButtonVisualEventHandler.advanceButton_MouseDown;
            advanceButton.MouseEnter += ButtonVisualEventHandler.advanceButton_MouseEnter;
            advanceButton.MouseLeave += ButtonVisualEventHandler.advanceButton_MouseLeave;
            advanceButton.MouseUp += ButtonVisualEventHandler.advanceButton_MouseUp;

            restartButton.MouseDown += ButtonVisualEventHandler.restartButton_MouseDown;
            restartButton.MouseEnter += ButtonVisualEventHandler.restartButton_MouseEnter;
            restartButton.MouseLeave += ButtonVisualEventHandler.restartButton_MouseLeave;
            restartButton.MouseUp += ButtonVisualEventHandler.restartButton_MouseUp;

            infoButton.MouseDown += ButtonVisualEventHandler.infoButton_MouseDown;
            infoButton.MouseEnter += ButtonVisualEventHandler.infoButton_MouseEnter;
            infoButton.MouseLeave += ButtonVisualEventHandler.infoButton_MouseLeave;
            infoButton.MouseUp += ButtonVisualEventHandler.infoButton_MouseUp;
            
            closeButton.MouseDown += ButtonVisualEventHandler.closeButton_MouseDown;
            closeButton.MouseEnter += ButtonVisualEventHandler.closeButton_MouseEnter;
            closeButton.MouseLeave += ButtonVisualEventHandler.closeButton_MouseLeave;
            closeButton.MouseUp += ButtonVisualEventHandler.closeButton_MouseUp;
        }

        private void initializeToolTips() {
            animateButton.MouseHover += (sender, eventArgs) => {
                addHover(sender, eventArgs, "Start/Stop generation animation\nShortcut(s): Space bar & 'P' Key");
            };

            backButton.MouseHover += (sender, eventArgs) => {
                addHover(sender, eventArgs,
                    "Take a single step back\nShortcut(s): Left Arrow Key, Backspace or Delete");
            };

            advanceButton.MouseHover += (sender, eventArgs) => {
                addHover(sender, eventArgs, "Advance a single step\nShortcut(s): Right Arrow Key");
            };

            markerButton.MouseHover += (sender, eventArgs) => {
                addHover(sender, eventArgs, "Save the current progress\nShortcut(s): 'S' Key");
            };

            revertMarkerButton.MouseHover += (sender, eventArgs) => {
                addHover(sender, eventArgs, "Revert back to save\nShortcut(s): 'L' Key");
            };

            restartButton.MouseHover += (sender, eventArgs) => {
                addHover(sender, eventArgs, "Generate a new image\nShortcut(s): 'R' Key");
            };
        }

        /* ------------------------------------------------------------------------
         * Miscellaneous
         * ------------------------------------------------------------------------ */

        private void inputChanged(object sender, EventArgs e) {
            if (!Visible) {
                return;
            }

            inputManager.setInputChanged(sender.GetType().ToString());
            executeButton_Click(null, null);
        }

        private void updateInputPadding() {
            if (!modelChoice.Text.Equals("Switch to Tile Mode")) {
                return;
            }

            Color c;
            Bitmap bm;
            if (inputPaddingEnabled()) {
                bm = new Bitmap(Resources.borderPaddingDisabled);
                c = Color.Red;
            } else {
                bm = new Bitmap(Resources.borderPaddingEnabled);
                c = Color.LawnGreen;
            }

            inputPaddingPB.Image = inputManager.resizePixels(inputPaddingPB, bm, 3, c, false);
            inputPaddingPB.BackColor = c;
            inputPaddingPB.Padding = new Padding(3);

            inputPaddingPB.Refresh();
        }

        private void updateRotations() {
            for (int i = 0; i < 3; i++) {
                pbs[i].BackColor = Math.Ceiling(defaultSymmetry / 2f) - 1 <= i ? Color.Red : Color.LawnGreen;
            }
        }

        private void showRotationalOptions(bool show) {
            // TODO: Redo RF1
            // for (int i = 0; i < 3; i++) {
            //     pbs[i].Visible = hide;
            // }
            //
            // originalRotPB.Visible = hide;
            // patternRotationLabel.Visible = hide;
            // periodicInput.Visible = hide;
            patternSize.Enabled = show;
            patternSizeLabel.Enabled = show;
            inputPaddingPB.Enabled = show;

            if (!show) {
                inputPaddingPB.Image = inputManager.resizePixels(inputPaddingPB,
                    toGrayScale(Resources.borderPaddingEnabled), 3, Color.Transparent, false);
                inputPaddingPB.BackColor = Color.Transparent;
            }
        }

        // private void initializeRotations() {
        //     // TODO maybe re-enable
        //     Bitmap referenceImg = new(Resources.rotationRef);
        //     const int padding = 3;
        //     originalRotPB.BackColor = Color.Black;
        //     originalRotPB.Padding = new Padding(padding);
        //     originalRotPB.Image = referenceImg;
        //
        //     Bitmap[] rfs = {
        //         Resources.rotationRef1, Resources.rotationRef2, Resources.rotationRef3
        //     };
        //     string[] rfsString = {
        //         "Rotate Clockwise 90°\nand Horizontally Flipped", "Rotate 180°\nand Horizontally Flipped",
        //         "Rotate Clockwise 270°\nand Horizontally Flipped"
        //     };
        //     for (int i = 0; i < 3; i++) {
        //         pbs[i].BackColor = Math.Ceiling(defaultSymmetry / 2f) - 1 <= i ? Color.Red : Color.LawnGreen;
        //         pbs[i].Padding = new Padding(padding);
        //         pbs[i].Image = new Bitmap(rfs[i]);
        //         pbs[i].MouseDown += pictureBoxMouseDown;
        //         int nonLocalI = i;
        //         pbs[i].MouseHover += (sender, eventArgs) => { addHover(sender, eventArgs, rfsString[nonLocalI]); };
        //     }
        // }

        /* ------------------------------------------------------------------------
         * Unsorted
         * ------------------------------------------------------------------------ */
    }
}