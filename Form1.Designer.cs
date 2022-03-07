using System.Windows.Forms;
using System.Drawing;

namespace WFC4All
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.inputTab = new System.Windows.Forms.TabPage();
            this.IconPB = new System.Windows.Forms.PictureBox();
            this.slowSpeedPB = new System.Windows.Forms.PictureBox();
            this.fastSpeedPB = new System.Windows.Forms.PictureBox();
            this.animSpeedTrackbar = new System.Windows.Forms.TrackBar();
            this.stepSizeTrackbar = new System.Windows.Forms.TrackBar();
            this.categoryCB = new System.Windows.Forms.ComboBox();
            this.category = new System.Windows.Forms.Label();
            this.outputWidthValue = new System.Windows.Forms.NumericUpDown();
            this.pattHeurPanel = new System.Windows.Forms.Panel();
            this.patternHeuristicDesc = new System.Windows.Forms.Label();
            this.patternSelectionLabel = new System.Windows.Forms.Label();
            this.patternHeuristicCB = new System.Windows.Forms.ComboBox();
            this.patternHeuristicPB = new System.Windows.Forms.PictureBox();
            this.outputHeightValue = new System.Windows.Forms.NumericUpDown();
            this.selHeurPanel = new System.Windows.Forms.Panel();
            this.selectionHeuristicDesc = new System.Windows.Forms.Label();
            this.selectionHeuristicLabel = new System.Windows.Forms.Label();
            this.selectionHeuristicCB = new System.Windows.Forms.ComboBox();
            this.selectionHeuristicPB = new System.Windows.Forms.PictureBox();
            this.outputImageWidthLabel = new System.Windows.Forms.Label();
            this.markerButton = new System.Windows.Forms.Button();
            this.outputImageHeightLabel = new System.Windows.Forms.Label();
            this.revertMarkerButton = new System.Windows.Forms.Button();
            this.outputSizeLabel = new System.Windows.Forms.Label();
            this.backButton = new System.Windows.Forms.Button();
            this.animationSpeedLabel = new System.Windows.Forms.Label();
            this.animateButton = new System.Windows.Forms.Button();
            this.stepSizeLabel = new System.Windows.Forms.Label();
            this.advanceButton = new System.Windows.Forms.Button();
            this.patternRotationLabel = new System.Windows.Forms.Label();
            this.p3RotPB = new System.Windows.Forms.PictureBox();
            this.p2RotPB = new System.Windows.Forms.PictureBox();
            this.p1RotPB = new System.Windows.Forms.PictureBox();
            this.originalRotPB = new System.Windows.Forms.PictureBox();
            this.patternPanel = new System.Windows.Forms.Panel();
            this.patternsLabel = new System.Windows.Forms.Label();
            this.inputImageCB = new System.Windows.Forms.ComboBox();
            this.inputImagePB = new System.Windows.Forms.PictureBox();
            this.inputImage = new System.Windows.Forms.Label();
            this.resultPB = new System.Windows.Forms.PictureBox();
            this.restartButton = new System.Windows.Forms.Button();
            this.inputPanel = new System.Windows.Forms.Panel();
            this.inputPaddingPB = new System.Windows.Forms.PictureBox();
            this.modelChoice = new System.Windows.Forms.Button();
            this.patternSize = new System.Windows.Forms.ComboBox();
            this.patternSizeLabel = new System.Windows.Forms.Label();
            this.tabSelection = new System.Windows.Forms.TabControl();
            this.inputTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.IconPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.slowSpeedPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.fastSpeedPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.animSpeedTrackbar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.stepSizeTrackbar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.outputWidthValue)).BeginInit();
            this.pattHeurPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.patternHeuristicPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.outputHeightValue)).BeginInit();
            this.selHeurPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.selectionHeuristicPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.p3RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.p2RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.p1RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.originalRotPB)).BeginInit();
            this.patternPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.inputImagePB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.resultPB)).BeginInit();
            this.inputPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.inputPaddingPB)).BeginInit();
            this.tabSelection.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputTab
            // 
            this.inputTab.AutoScroll = true;
            this.inputTab.BackColor = System.Drawing.Color.DarkGray;
            this.inputTab.Controls.Add(this.IconPB);
            this.inputTab.Controls.Add(this.slowSpeedPB);
            this.inputTab.Controls.Add(this.fastSpeedPB);
            this.inputTab.Controls.Add(this.animSpeedTrackbar);
            this.inputTab.Controls.Add(this.stepSizeTrackbar);
            this.inputTab.Controls.Add(this.categoryCB);
            this.inputTab.Controls.Add(this.category);
            this.inputTab.Controls.Add(this.outputWidthValue);
            this.inputTab.Controls.Add(this.pattHeurPanel);
            this.inputTab.Controls.Add(this.outputHeightValue);
            this.inputTab.Controls.Add(this.selHeurPanel);
            this.inputTab.Controls.Add(this.outputImageWidthLabel);
            this.inputTab.Controls.Add(this.markerButton);
            this.inputTab.Controls.Add(this.outputImageHeightLabel);
            this.inputTab.Controls.Add(this.revertMarkerButton);
            this.inputTab.Controls.Add(this.outputSizeLabel);
            this.inputTab.Controls.Add(this.backButton);
            this.inputTab.Controls.Add(this.animationSpeedLabel);
            this.inputTab.Controls.Add(this.animateButton);
            this.inputTab.Controls.Add(this.stepSizeLabel);
            this.inputTab.Controls.Add(this.advanceButton);
            this.inputTab.Controls.Add(this.patternRotationLabel);
            this.inputTab.Controls.Add(this.p3RotPB);
            this.inputTab.Controls.Add(this.p2RotPB);
            this.inputTab.Controls.Add(this.p1RotPB);
            this.inputTab.Controls.Add(this.originalRotPB);
            this.inputTab.Controls.Add(this.patternPanel);
            this.inputTab.Controls.Add(this.inputImageCB);
            this.inputTab.Controls.Add(this.inputImagePB);
            this.inputTab.Controls.Add(this.inputImage);
            this.inputTab.Controls.Add(this.resultPB);
            this.inputTab.Controls.Add(this.restartButton);
            this.inputTab.Controls.Add(this.inputPanel);
            this.inputTab.Location = new System.Drawing.Point(4, 22);
            this.inputTab.Name = "inputTab";
            this.inputTab.Padding = new System.Windows.Forms.Padding(3);
            this.inputTab.Size = new System.Drawing.Size(1592, 874);
            this.inputTab.TabIndex = 0;
            this.inputTab.Text = "Input Manipulation";
            // 
            // IconPB
            // 
            this.IconPB.BackgroundImage = global::WFC4All.Properties.Resources.iconPNG;
            this.IconPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.IconPB.InitialImage = global::WFC4All.Properties.Resources.iconPNG;
            this.IconPB.Location = new System.Drawing.Point(56, 550);
            this.IconPB.Name = "IconPB";
            this.IconPB.Size = new System.Drawing.Size(250, 250);
            this.IconPB.TabIndex = 42;
            this.IconPB.TabStop = false;
            // 
            // slowSpeedPB
            // 
            this.slowSpeedPB.BackgroundImage = global::WFC4All.Properties.Resources.slowSpeed;
            this.slowSpeedPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.slowSpeedPB.Location = new System.Drawing.Point(1283, 806);
            this.slowSpeedPB.Name = "slowSpeedPB";
            this.slowSpeedPB.Size = new System.Drawing.Size(35, 35);
            this.slowSpeedPB.TabIndex = 41;
            this.slowSpeedPB.TabStop = false;
            // 
            // fastSpeedPB
            // 
            this.fastSpeedPB.BackgroundImage = global::WFC4All.Properties.Resources.fastSpeed;
            this.fastSpeedPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.fastSpeedPB.Location = new System.Drawing.Point(1529, 806);
            this.fastSpeedPB.Name = "fastSpeedPB";
            this.fastSpeedPB.Size = new System.Drawing.Size(35, 35);
            this.fastSpeedPB.TabIndex = 40;
            this.fastSpeedPB.TabStop = false;
            // 
            // animSpeedTrackbar
            // 
            this.animSpeedTrackbar.LargeChange = 100;
            this.animSpeedTrackbar.Location = new System.Drawing.Point(1316, 814);
            this.animSpeedTrackbar.Maximum = 2000;
            this.animSpeedTrackbar.Minimum = 25;
            this.animSpeedTrackbar.Name = "animSpeedTrackbar";
            this.animSpeedTrackbar.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.animSpeedTrackbar.Size = new System.Drawing.Size(214, 45);
            this.animSpeedTrackbar.SmallChange = 25;
            this.animSpeedTrackbar.TabIndex = 39;
            this.animSpeedTrackbar.TickFrequency = 100;
            this.animSpeedTrackbar.Value = 25;
            // 
            // stepSizeTrackbar
            // 
            this.stepSizeTrackbar.LargeChange = 10;
            this.stepSizeTrackbar.Location = new System.Drawing.Point(1316, 730);
            this.stepSizeTrackbar.Maximum = 100;
            this.stepSizeTrackbar.Minimum = 1;
            this.stepSizeTrackbar.Name = "stepSizeTrackbar";
            this.stepSizeTrackbar.Size = new System.Drawing.Size(214, 45);
            this.stepSizeTrackbar.SmallChange = 5;
            this.stepSizeTrackbar.TabIndex = 38;
            this.stepSizeTrackbar.TickFrequency = 10;
            this.stepSizeTrackbar.Value = 1;
            this.stepSizeTrackbar.ValueChanged += new System.EventHandler(this.stepSizeTrackbar_Scroll);
            // 
            // categoryCB
            // 
            this.categoryCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.categoryCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.categoryCB.FormattingEnabled = true;
            this.categoryCB.Location = new System.Drawing.Point(136, 21);
            this.categoryCB.Name = "categoryCB";
            this.categoryCB.Size = new System.Drawing.Size(214, 26);
            this.categoryCB.TabIndex = 37;
            this.categoryCB.SelectedIndexChanged += new System.EventHandler(this.categoryCB_SelectedIndexChanged);
            // 
            // category
            // 
            this.category.BackColor = System.Drawing.Color.Transparent;
            this.category.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.category.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.category.Location = new System.Drawing.Point(20, 24);
            this.category.Name = "category";
            this.category.Size = new System.Drawing.Size(180, 21);
            this.category.TabIndex = 36;
            this.category.Text = "Category";
            // 
            // outputWidthValue
            // 
            this.outputWidthValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputWidthValue.Location = new System.Drawing.Point(1122, 30);
            this.outputWidthValue.Maximum = new decimal(new int[] {500, 0, 0, 0});
            this.outputWidthValue.Minimum = new decimal(new int[] {10, 0, 0, 0});
            this.outputWidthValue.Name = "outputWidthValue";
            this.outputWidthValue.Size = new System.Drawing.Size(60, 24);
            this.outputWidthValue.TabIndex = 4;
            this.outputWidthValue.Value = new decimal(new int[] {24, 0, 0, 0});
            // 
            // pattHeurPanel
            // 
            this.pattHeurPanel.BackColor = System.Drawing.Color.Silver;
            this.pattHeurPanel.Controls.Add(this.patternHeuristicDesc);
            this.pattHeurPanel.Controls.Add(this.patternSelectionLabel);
            this.pattHeurPanel.Controls.Add(this.patternHeuristicCB);
            this.pattHeurPanel.Controls.Add(this.patternHeuristicPB);
            this.pattHeurPanel.Location = new System.Drawing.Point(9, 620);
            this.pattHeurPanel.Name = "pattHeurPanel";
            this.pattHeurPanel.Size = new System.Drawing.Size(346, 112);
            this.pattHeurPanel.TabIndex = 35;
            this.pattHeurPanel.Visible = false;
            // 
            // patternHeuristicDesc
            // 
            this.patternHeuristicDesc.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.patternHeuristicDesc.BackColor = System.Drawing.Color.Transparent;
            this.patternHeuristicDesc.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternHeuristicDesc.Location = new System.Drawing.Point(90, 42);
            this.patternHeuristicDesc.Name = "patternHeuristicDesc";
            this.patternHeuristicDesc.Size = new System.Drawing.Size(139, 58);
            this.patternHeuristicDesc.TabIndex = 33;
            this.patternHeuristicDesc.Text = "Select the most logical choice based on the amount of available options, and solv" + "e ties randomly";
            this.patternHeuristicDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // patternSelectionLabel
            // 
            this.patternSelectionLabel.AutoSize = true;
            this.patternSelectionLabel.BackColor = System.Drawing.Color.Transparent;
            this.patternSelectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternSelectionLabel.Location = new System.Drawing.Point(11, 39);
            this.patternSelectionLabel.Name = "patternSelectionLabel";
            this.patternSelectionLabel.Size = new System.Drawing.Size(64, 30);
            this.patternSelectionLabel.TabIndex = 32;
            this.patternSelectionLabel.Text = "Pattern\r\nHeuristic";
            // 
            // patternHeuristicCB
            // 
            this.patternHeuristicCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.patternHeuristicCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternHeuristicCB.FormattingEnabled = true;
            this.patternHeuristicCB.Location = new System.Drawing.Point(90, 14);
            this.patternHeuristicCB.Name = "patternHeuristicCB";
            this.patternHeuristicCB.Size = new System.Drawing.Size(139, 21);
            this.patternHeuristicCB.TabIndex = 31;
            this.patternHeuristicCB.SelectedIndexChanged += new System.EventHandler(this.patternHeuristicCB_SelectedIndexChanged);
            // 
            // patternHeuristicPB
            // 
            this.patternHeuristicPB.Image = global::WFC4All.Properties.Resources.Weighted;
            this.patternHeuristicPB.InitialImage = global::WFC4All.Properties.Resources.Weighted;
            this.patternHeuristicPB.Location = new System.Drawing.Point(241, 6);
            this.patternHeuristicPB.Name = "patternHeuristicPB";
            this.patternHeuristicPB.Size = new System.Drawing.Size(100, 100);
            this.patternHeuristicPB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.patternHeuristicPB.TabIndex = 30;
            this.patternHeuristicPB.TabStop = false;
            // 
            // outputHeightValue
            // 
            this.outputHeightValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputHeightValue.Location = new System.Drawing.Point(1212, 30);
            this.outputHeightValue.Minimum = new decimal(new int[] {10, 0, 0, 0});
            this.outputHeightValue.Name = "outputHeightValue";
            this.outputHeightValue.Size = new System.Drawing.Size(60, 24);
            this.outputHeightValue.TabIndex = 3;
            this.outputHeightValue.Value = new decimal(new int[] {24, 0, 0, 0});
            this.outputHeightValue.ValueChanged += new System.EventHandler(this.outputHeightValue_ValueChanged);
            // 
            // selHeurPanel
            // 
            this.selHeurPanel.BackColor = System.Drawing.Color.Silver;
            this.selHeurPanel.Controls.Add(this.selectionHeuristicDesc);
            this.selHeurPanel.Controls.Add(this.selectionHeuristicLabel);
            this.selHeurPanel.Controls.Add(this.selectionHeuristicCB);
            this.selHeurPanel.Controls.Add(this.selectionHeuristicPB);
            this.selHeurPanel.Location = new System.Drawing.Point(9, 502);
            this.selHeurPanel.Name = "selHeurPanel";
            this.selHeurPanel.Size = new System.Drawing.Size(346, 112);
            this.selHeurPanel.TabIndex = 34;
            this.selHeurPanel.Visible = false;
            // 
            // selectionHeuristicDesc
            // 
            this.selectionHeuristicDesc.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.selectionHeuristicDesc.BackColor = System.Drawing.Color.Transparent;
            this.selectionHeuristicDesc.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.selectionHeuristicDesc.Location = new System.Drawing.Point(90, 42);
            this.selectionHeuristicDesc.Name = "selectionHeuristicDesc";
            this.selectionHeuristicDesc.Size = new System.Drawing.Size(139, 58);
            this.selectionHeuristicDesc.TabIndex = 33;
            this.selectionHeuristicDesc.Text = "Select the most logical choice based on the amount of available options, and solv" + "e ties randomly";
            this.selectionHeuristicDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // selectionHeuristicLabel
            // 
            this.selectionHeuristicLabel.AutoSize = true;
            this.selectionHeuristicLabel.BackColor = System.Drawing.Color.Transparent;
            this.selectionHeuristicLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.selectionHeuristicLabel.Location = new System.Drawing.Point(11, 39);
            this.selectionHeuristicLabel.Name = "selectionHeuristicLabel";
            this.selectionHeuristicLabel.Size = new System.Drawing.Size(67, 30);
            this.selectionHeuristicLabel.TabIndex = 32;
            this.selectionHeuristicLabel.Text = "Selection\r\nHeuristic";
            // 
            // selectionHeuristicCB
            // 
            this.selectionHeuristicCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.selectionHeuristicCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.selectionHeuristicCB.FormattingEnabled = true;
            this.selectionHeuristicCB.Location = new System.Drawing.Point(90, 14);
            this.selectionHeuristicCB.Name = "selectionHeuristicCB";
            this.selectionHeuristicCB.Size = new System.Drawing.Size(139, 21);
            this.selectionHeuristicCB.TabIndex = 31;
            this.selectionHeuristicCB.SelectedIndexChanged += new System.EventHandler(this.selectionHeuristicCB_SelectedIndexChanged);
            // 
            // selectionHeuristicPB
            // 
            this.selectionHeuristicPB.Image = global::WFC4All.Properties.Resources.Entropy;
            this.selectionHeuristicPB.InitialImage = global::WFC4All.Properties.Resources.Entropy;
            this.selectionHeuristicPB.Location = new System.Drawing.Point(241, 6);
            this.selectionHeuristicPB.Name = "selectionHeuristicPB";
            this.selectionHeuristicPB.Size = new System.Drawing.Size(100, 100);
            this.selectionHeuristicPB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.selectionHeuristicPB.TabIndex = 30;
            this.selectionHeuristicPB.TabStop = false;
            // 
            // outputImageWidthLabel
            // 
            this.outputImageWidthLabel.AutoSize = true;
            this.outputImageWidthLabel.BackColor = System.Drawing.Color.Transparent;
            this.outputImageWidthLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputImageWidthLabel.Location = new System.Drawing.Point(1095, 32);
            this.outputImageWidthLabel.Name = "outputImageWidthLabel";
            this.outputImageWidthLabel.Size = new System.Drawing.Size(29, 18);
            this.outputImageWidthLabel.TabIndex = 6;
            this.outputImageWidthLabel.Text = "W:";
            // 
            // markerButton
            // 
            this.markerButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.markerButton.Location = new System.Drawing.Point(947, 814);
            this.markerButton.Name = "markerButton";
            this.markerButton.Size = new System.Drawing.Size(150, 40);
            this.markerButton.TabIndex = 29;
            this.markerButton.Text = "Save";
            this.markerButton.UseVisualStyleBackColor = true;
            this.markerButton.Click += new System.EventHandler(this.markerButton_Click);
            // 
            // outputImageHeightLabel
            // 
            this.outputImageHeightLabel.AutoSize = true;
            this.outputImageHeightLabel.BackColor = System.Drawing.Color.Transparent;
            this.outputImageHeightLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputImageHeightLabel.Location = new System.Drawing.Point(1186, 32);
            this.outputImageHeightLabel.Name = "outputImageHeightLabel";
            this.outputImageHeightLabel.Size = new System.Drawing.Size(25, 18);
            this.outputImageHeightLabel.TabIndex = 7;
            this.outputImageHeightLabel.Text = "H:";
            // 
            // revertMarkerButton
            // 
            this.revertMarkerButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.revertMarkerButton.Location = new System.Drawing.Point(1104, 814);
            this.revertMarkerButton.Name = "revertMarkerButton";
            this.revertMarkerButton.Size = new System.Drawing.Size(150, 40);
            this.revertMarkerButton.TabIndex = 28;
            this.revertMarkerButton.Text = "Load";
            this.revertMarkerButton.UseVisualStyleBackColor = true;
            this.revertMarkerButton.Click += new System.EventHandler(this.revertMarkerButton_Click);
            // 
            // outputSizeLabel
            // 
            this.outputSizeLabel.AutoSize = true;
            this.outputSizeLabel.BackColor = System.Drawing.Color.Transparent;
            this.outputSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputSizeLabel.Location = new System.Drawing.Point(938, 32);
            this.outputSizeLabel.Name = "outputSizeLabel";
            this.outputSizeLabel.Size = new System.Drawing.Size(146, 18);
            this.outputSizeLabel.TabIndex = 5;
            this.outputSizeLabel.Text = "Output Image Size";
            // 
            // backButton
            // 
            this.backButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.backButton.Location = new System.Drawing.Point(1129, 757);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(104, 40);
            this.backButton.TabIndex = 27;
            this.backButton.Text = "Step Back";
            this.backButton.UseVisualStyleBackColor = true;
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            // 
            // animationSpeedLabel
            // 
            this.animationSpeedLabel.BackColor = System.Drawing.Color.Transparent;
            this.animationSpeedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.animationSpeedLabel.Location = new System.Drawing.Point(1319, 786);
            this.animationSpeedLabel.Name = "animationSpeedLabel";
            this.animationSpeedLabel.Size = new System.Drawing.Size(211, 25);
            this.animationSpeedLabel.TabIndex = 26;
            this.animationSpeedLabel.Text = "Animation speed";
            this.animationSpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // animateButton
            // 
            this.animateButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.animateButton.Location = new System.Drawing.Point(975, 711);
            this.animateButton.Name = "animateButton";
            this.animateButton.Size = new System.Drawing.Size(104, 86);
            this.animateButton.TabIndex = 24;
            this.animateButton.Text = "Animate";
            this.animateButton.UseVisualStyleBackColor = true;
            this.animateButton.Click += new System.EventHandler(this.animateButton_Click);
            // 
            // stepSizeLabel
            // 
            this.stepSizeLabel.BackColor = System.Drawing.Color.Transparent;
            this.stepSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.stepSizeLabel.Location = new System.Drawing.Point(1316, 699);
            this.stepSizeLabel.Name = "stepSizeLabel";
            this.stepSizeLabel.Size = new System.Drawing.Size(214, 27);
            this.stepSizeLabel.TabIndex = 23;
            this.stepSizeLabel.Text = "Amount of steps to take: 1";
            this.stepSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // advanceButton
            // 
            this.advanceButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.advanceButton.Location = new System.Drawing.Point(1129, 711);
            this.advanceButton.Name = "advanceButton";
            this.advanceButton.Size = new System.Drawing.Size(104, 40);
            this.advanceButton.TabIndex = 21;
            this.advanceButton.Text = "Advance";
            this.advanceButton.UseVisualStyleBackColor = true;
            this.advanceButton.Click += new System.EventHandler(this.advanceButton_Click);
            // 
            // patternRotationLabel
            // 
            this.patternRotationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternRotationLabel.Location = new System.Drawing.Point(20, 788);
            this.patternRotationLabel.Name = "patternRotationLabel";
            this.patternRotationLabel.Size = new System.Drawing.Size(180, 60);
            this.patternRotationLabel.TabIndex = 20;
            this.patternRotationLabel.Text = "Select allowed pattern transformations. On the right is a non-transformed referen" + "ce (black border)";
            this.patternRotationLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.patternRotationLabel.Visible = false;
            // 
            // p3RotPB
            // 
            this.p3RotPB.Location = new System.Drawing.Point(141, 806);
            this.p3RotPB.Name = "p3RotPB";
            this.p3RotPB.Size = new System.Drawing.Size(60, 60);
            this.p3RotPB.TabIndex = 15;
            this.p3RotPB.TabStop = false;
            this.p3RotPB.Visible = false;
            this.p3RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p2RotPB
            // 
            this.p2RotPB.Location = new System.Drawing.Point(75, 806);
            this.p2RotPB.Name = "p2RotPB";
            this.p2RotPB.Size = new System.Drawing.Size(60, 60);
            this.p2RotPB.TabIndex = 14;
            this.p2RotPB.TabStop = false;
            this.p2RotPB.Visible = false;
            this.p2RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p1RotPB
            // 
            this.p1RotPB.Location = new System.Drawing.Point(9, 806);
            this.p1RotPB.Name = "p1RotPB";
            this.p1RotPB.Size = new System.Drawing.Size(60, 60);
            this.p1RotPB.TabIndex = 13;
            this.p1RotPB.TabStop = false;
            this.p1RotPB.Visible = false;
            this.p1RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // originalRotPB
            // 
            this.originalRotPB.Location = new System.Drawing.Point(207, 792);
            this.originalRotPB.Name = "originalRotPB";
            this.originalRotPB.Size = new System.Drawing.Size(60, 60);
            this.originalRotPB.TabIndex = 12;
            this.originalRotPB.TabStop = false;
            this.originalRotPB.Visible = false;
            // 
            // patternPanel
            // 
            this.patternPanel.AutoScroll = true;
            this.patternPanel.AutoScrollMargin = new System.Drawing.Size(0, 50);
            this.patternPanel.BackColor = System.Drawing.Color.Silver;
            this.patternPanel.Controls.Add(this.patternsLabel);
            this.patternPanel.Location = new System.Drawing.Point(365, 24);
            this.patternPanel.Name = "patternPanel";
            this.patternPanel.Size = new System.Drawing.Size(500, 825);
            this.patternPanel.TabIndex = 11;
            // 
            // patternsLabel
            // 
            this.patternsLabel.BackColor = System.Drawing.Color.Transparent;
            this.patternsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternsLabel.Location = new System.Drawing.Point(14, 14);
            this.patternsLabel.Name = "patternsLabel";
            this.patternsLabel.Size = new System.Drawing.Size(459, 56);
            this.patternsLabel.TabIndex = 0;
            this.patternsLabel.Text = "Extracted patterns with current settings!";
            // 
            // inputImageCB
            // 
            this.inputImageCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.inputImageCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.inputImageCB.FormattingEnabled = true;
            this.inputImageCB.Location = new System.Drawing.Point(136, 51);
            this.inputImageCB.Name = "inputImageCB";
            this.inputImageCB.Size = new System.Drawing.Size(214, 26);
            this.inputImageCB.TabIndex = 4;
            this.inputImageCB.SelectedIndexChanged += new System.EventHandler(this.inputImage_SelectedIndexChanged);
            // 
            // inputImagePB
            // 
            this.inputImagePB.Location = new System.Drawing.Point(26, 90);
            this.inputImagePB.Name = "inputImagePB";
            this.inputImagePB.Size = new System.Drawing.Size(300, 300);
            this.inputImagePB.TabIndex = 3;
            this.inputImagePB.TabStop = false;
            // 
            // inputImage
            // 
            this.inputImage.BackColor = System.Drawing.Color.Transparent;
            this.inputImage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.inputImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.inputImage.Location = new System.Drawing.Point(20, 54);
            this.inputImage.Name = "inputImage";
            this.inputImage.Size = new System.Drawing.Size(180, 21);
            this.inputImage.TabIndex = 2;
            this.inputImage.Text = "Input Image";
            // 
            // resultPB
            // 
            this.resultPB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.resultPB.Location = new System.Drawing.Point(938, 80);
            this.resultPB.Name = "resultPB";
            this.resultPB.Size = new System.Drawing.Size(600, 600);
            this.resultPB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.resultPB.TabIndex = 0;
            this.resultPB.TabStop = false;
            this.resultPB.Click += new System.EventHandler(this.resultPB_Click);
            // 
            // restartButton
            // 
            this.restartButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.restartButton.Location = new System.Drawing.Point(1434, 23);
            this.restartButton.Name = "restartButton";
            this.restartButton.Size = new System.Drawing.Size(104, 40);
            this.restartButton.TabIndex = 1;
            this.restartButton.Text = "Restart";
            this.restartButton.UseVisualStyleBackColor = true;
            this.restartButton.Click += new System.EventHandler(this.executeButton_Click);
            // 
            // inputPanel
            // 
            this.inputPanel.BackColor = System.Drawing.Color.Silver;
            this.inputPanel.Controls.Add(this.inputPaddingPB);
            this.inputPanel.Controls.Add(this.modelChoice);
            this.inputPanel.Controls.Add(this.patternSize);
            this.inputPanel.Controls.Add(this.patternSizeLabel);
            this.inputPanel.Location = new System.Drawing.Point(9, 396);
            this.inputPanel.Name = "inputPanel";
            this.inputPanel.Size = new System.Drawing.Size(346, 100);
            this.inputPanel.TabIndex = 35;
            // 
            // inputPaddingPB
            // 
            this.inputPaddingPB.Location = new System.Drawing.Point(249, 3);
            this.inputPaddingPB.Name = "inputPaddingPB";
            this.inputPaddingPB.Size = new System.Drawing.Size(94, 94);
            this.inputPaddingPB.TabIndex = 38;
            this.inputPaddingPB.TabStop = false;
            this.inputPaddingPB.Click += new System.EventHandler(this.inputPaddingPB_Click);
            // 
            // modelChoice
            // 
            this.modelChoice.BackColor = System.Drawing.Color.Transparent;
            this.modelChoice.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.modelChoice.Location = new System.Drawing.Point(110, 3);
            this.modelChoice.Name = "modelChoice";
            this.modelChoice.Size = new System.Drawing.Size(129, 94);
            this.modelChoice.TabIndex = 8;
            this.modelChoice.Text = "Switch to Simple Model";
            this.modelChoice.UseVisualStyleBackColor = false;
            this.modelChoice.Click += new System.EventHandler(this.modelChoice_Click);
            // 
            // patternSize
            // 
            this.patternSize.BackColor = System.Drawing.SystemColors.ControlLight;
            this.patternSize.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternSize.FormattingEnabled = true;
            this.patternSize.Location = new System.Drawing.Point(12, 61);
            this.patternSize.Name = "patternSize";
            this.patternSize.Size = new System.Drawing.Size(87, 26);
            this.patternSize.TabIndex = 0;
            this.patternSize.SelectedIndexChanged += new System.EventHandler(this.inputChanged);
            // 
            // patternSizeLabel
            // 
            this.patternSizeLabel.BackColor = System.Drawing.Color.Transparent;
            this.patternSizeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.patternSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternSizeLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.patternSizeLabel.Location = new System.Drawing.Point(3, 14);
            this.patternSizeLabel.Name = "patternSizeLabel";
            this.patternSizeLabel.Size = new System.Drawing.Size(107, 44);
            this.patternSizeLabel.TabIndex = 1;
            this.patternSizeLabel.Text = "Pattern Size\r\n(n x n)";
            this.patternSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabSelection
            // 
            this.tabSelection.Controls.Add(this.inputTab);
            this.tabSelection.Location = new System.Drawing.Point(0, 0);
            this.tabSelection.Name = "tabSelection";
            this.tabSelection.SelectedIndex = 0;
            this.tabSelection.Size = new System.Drawing.Size(1600, 900);
            this.tabSelection.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabSelection.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.tabSelection);
            this.Name = "Form1";
            this.Text = "WFC4All";
            this.inputTab.ResumeLayout(false);
            this.inputTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.IconPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.slowSpeedPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.fastSpeedPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.animSpeedTrackbar)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.stepSizeTrackbar)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.outputWidthValue)).EndInit();
            this.pattHeurPanel.ResumeLayout(false);
            this.pattHeurPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.patternHeuristicPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.outputHeightValue)).EndInit();
            this.selHeurPanel.ResumeLayout(false);
            this.selHeurPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.selectionHeuristicPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.p3RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.p2RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.p1RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.originalRotPB)).EndInit();
            this.patternPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.inputImagePB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.resultPB)).EndInit();
            this.inputPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.inputPaddingPB)).EndInit();
            this.tabSelection.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.PictureBox IconPB;

        private System.Windows.Forms.Label category;
        private System.Windows.Forms.ComboBox categoryCB;

        private System.Windows.Forms.Label selectionHeuristicDesc;

        private System.Windows.Forms.PictureBox selectionHeuristicPB;
        private System.Windows.Forms.ComboBox selectionHeuristicCB;
        private System.Windows.Forms.Label selectionHeuristicLabel;

        private System.Windows.Forms.Button revertMarkerButton;
        private System.Windows.Forms.Button markerButton;

        #endregion

        private System.Windows.Forms.TabPage inputTab;
        private System.Windows.Forms.Label patternRotationLabel;
        private System.Windows.Forms.PictureBox p3RotPB;
        private System.Windows.Forms.PictureBox p2RotPB;
        private System.Windows.Forms.PictureBox p1RotPB;
        private System.Windows.Forms.PictureBox originalRotPB;
        public System.Windows.Forms.Panel patternPanel;
        private System.Windows.Forms.Label patternsLabel;
        private System.Windows.Forms.Button modelChoice;
        private System.Windows.Forms.Label outputImageHeightLabel;
        private System.Windows.Forms.Label outputImageWidthLabel;
        private System.Windows.Forms.Label outputSizeLabel;
        private System.Windows.Forms.NumericUpDown outputHeightValue;
        private System.Windows.Forms.NumericUpDown outputWidthValue;
        private System.Windows.Forms.ComboBox inputImageCB;
        private PictureBox inputImagePB;
        private System.Windows.Forms.Label inputImage;
        private System.Windows.Forms.Label patternSizeLabel;
        private System.Windows.Forms.ComboBox patternSize;
        public System.Windows.Forms.PictureBox resultPB;
        private System.Windows.Forms.Button restartButton;
        private TabControl tabSelection;
        private System.Windows.Forms.Label stepSizeLabel;
        private System.Windows.Forms.Button advanceButton;
        private System.Windows.Forms.Button animateButton;
        private System.Windows.Forms.Label animationSpeedLabel;
        private System.Windows.Forms.Button backButton;
        private System.Windows.Forms.Panel inputPanel;
        private System.Windows.Forms.Panel selHeurPanel;
        private System.Windows.Forms.Panel pattHeurPanel;
        private Label patternHeuristicDesc;
        private System.Windows.Forms.Label patternSelectionLabel;
        private System.Windows.Forms.ComboBox patternHeuristicCB;
        private System.Windows.Forms.PictureBox patternHeuristicPB;
        private PictureBox inputPaddingPB;
        private TrackBar animSpeedTrackbar;
        private System.Windows.Forms.TrackBar stepSizeTrackbar;
        private System.Windows.Forms.PictureBox slowSpeedPB;
        private System.Windows.Forms.PictureBox fastSpeedPB;
    }
}