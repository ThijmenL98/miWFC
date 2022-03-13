using System.ComponentModel;
using System.Windows.Forms;

namespace WFC4All
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.task1Tab = new System.Windows.Forms.TabPage();
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
            this.loadingPB = new System.Windows.Forms.PictureBox();
            this.IconPB = new System.Windows.Forms.PictureBox();
            this.imUsefulPB = new System.Windows.Forms.PictureBox();
            this.closeButton = new System.Windows.Forms.Button();
            this.infoGraphicPB = new System.Windows.Forms.PictureBox();
            this.infoButton = new System.Windows.Forms.Button();
            this.tabSelection = new System.Windows.Forms.TabControl();
            this.task2Tab = new System.Windows.Forms.TabPage();
            this.sandBoxTab = new System.Windows.Forms.TabPage();
            this.exportButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.slowSpeedPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fastSpeedPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.animSpeedTrackbar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stepSizeTrackbar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputWidthValue)).BeginInit();
            this.pattHeurPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.patternHeuristicPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputHeightValue)).BeginInit();
            this.selHeurPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.selectionHeuristicPB)).BeginInit();
            this.patternPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.inputImagePB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.resultPB)).BeginInit();
            this.inputPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.inputPaddingPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.loadingPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.IconPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imUsefulPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.infoGraphicPB)).BeginInit();
            this.tabSelection.SuspendLayout();
            this.SuspendLayout();
            // 
            // task1Tab
            // 
            this.task1Tab.AutoScroll = true;
            this.task1Tab.BackColor = System.Drawing.Color.DarkGray;
            this.task1Tab.Location = new System.Drawing.Point(4, 22);
            this.task1Tab.Name = "task1Tab";
            this.task1Tab.Padding = new System.Windows.Forms.Padding(3);
            this.task1Tab.Size = new System.Drawing.Size(1592, 874);
            this.task1Tab.TabIndex = 0;
            this.task1Tab.Text = "Task 1";
            // 
            // slowSpeedPB
            // 
            this.slowSpeedPB.BackgroundImage = global::WFC4All.Properties.Resources.slowSpeed;
            this.slowSpeedPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.slowSpeedPB.Location = new System.Drawing.Point(1283, 828);
            this.slowSpeedPB.Name = "slowSpeedPB";
            this.slowSpeedPB.Size = new System.Drawing.Size(35, 35);
            this.slowSpeedPB.TabIndex = 41;
            this.slowSpeedPB.TabStop = false;
            // 
            // fastSpeedPB
            // 
            this.fastSpeedPB.BackgroundImage = global::WFC4All.Properties.Resources.fastSpeed;
            this.fastSpeedPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.fastSpeedPB.Location = new System.Drawing.Point(1529, 828);
            this.fastSpeedPB.Name = "fastSpeedPB";
            this.fastSpeedPB.Size = new System.Drawing.Size(35, 35);
            this.fastSpeedPB.TabIndex = 40;
            this.fastSpeedPB.TabStop = false;
            // 
            // animSpeedTrackbar
            // 
            this.animSpeedTrackbar.LargeChange = 100;
            this.animSpeedTrackbar.Location = new System.Drawing.Point(1316, 836);
            this.animSpeedTrackbar.Maximum = 2000;
            this.animSpeedTrackbar.Minimum = 1;
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
            this.stepSizeTrackbar.Location = new System.Drawing.Point(1316, 761);
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
            this.categoryCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.categoryCB.FormattingEnabled = true;
            this.categoryCB.Location = new System.Drawing.Point(136, 43);
            this.categoryCB.Name = "categoryCB";
            this.categoryCB.Size = new System.Drawing.Size(214, 26);
            this.categoryCB.TabIndex = 37;
            this.categoryCB.SelectedIndexChanged += new System.EventHandler(this.categoryCB_SelectedIndexChanged);
            // 
            // category
            // 
            this.category.BackColor = System.Drawing.Color.Transparent;
            this.category.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.category.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.category.Location = new System.Drawing.Point(21, 43);
            this.category.Name = "category";
            this.category.Size = new System.Drawing.Size(180, 21);
            this.category.TabIndex = 36;
            this.category.Text = "Category";
            // 
            // outputWidthValue
            // 
            this.outputWidthValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.outputWidthValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputWidthValue.Location = new System.Drawing.Point(1135, 58);
            this.outputWidthValue.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.outputWidthValue.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.outputWidthValue.Name = "outputWidthValue";
            this.outputWidthValue.Size = new System.Drawing.Size(60, 24);
            this.outputWidthValue.TabIndex = 4;
            this.outputWidthValue.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.outputWidthValue.ValueChanged += new System.EventHandler(this.executeButton_Click);
            // 
            // pattHeurPanel
            // 
            this.pattHeurPanel.BackColor = System.Drawing.Color.Silver;
            this.pattHeurPanel.Controls.Add(this.patternHeuristicDesc);
            this.pattHeurPanel.Controls.Add(this.patternSelectionLabel);
            this.pattHeurPanel.Controls.Add(this.patternHeuristicCB);
            this.pattHeurPanel.Controls.Add(this.patternHeuristicPB);
            this.pattHeurPanel.Location = new System.Drawing.Point(9, 642);
            this.pattHeurPanel.Name = "pattHeurPanel";
            this.pattHeurPanel.Size = new System.Drawing.Size(346, 112);
            this.pattHeurPanel.TabIndex = 35;
            this.pattHeurPanel.Visible = false;
            // 
            // patternHeuristicDesc
            // 
            this.patternHeuristicDesc.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.patternHeuristicDesc.BackColor = System.Drawing.Color.Transparent;
            this.patternHeuristicDesc.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.patternHeuristicDesc.Location = new System.Drawing.Point(90, 42);
            this.patternHeuristicDesc.Name = "patternHeuristicDesc";
            this.patternHeuristicDesc.Size = new System.Drawing.Size(139, 58);
            this.patternHeuristicDesc.TabIndex = 33;
            this.patternHeuristicDesc.Text = "Select the most logical choice based on the amount of available options, and solv" +
    "e ties randomly";
            this.patternHeuristicDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // patternSelectionLabel
            // 
            this.patternSelectionLabel.AutoSize = true;
            this.patternSelectionLabel.BackColor = System.Drawing.Color.Transparent;
            this.patternSelectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.patternSelectionLabel.Location = new System.Drawing.Point(11, 39);
            this.patternSelectionLabel.Name = "patternSelectionLabel";
            this.patternSelectionLabel.Size = new System.Drawing.Size(64, 30);
            this.patternSelectionLabel.TabIndex = 32;
            this.patternSelectionLabel.Text = "Pattern\r\nHeuristic";
            // 
            // patternHeuristicCB
            // 
            this.patternHeuristicCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.patternHeuristicCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
            this.outputHeightValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.outputHeightValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputHeightValue.Location = new System.Drawing.Point(1225, 58);
            this.outputHeightValue.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.outputHeightValue.Name = "outputHeightValue";
            this.outputHeightValue.Size = new System.Drawing.Size(60, 24);
            this.outputHeightValue.TabIndex = 3;
            this.outputHeightValue.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.outputHeightValue.ValueChanged += new System.EventHandler(this.outputHeightValue_ValueChanged);
            // 
            // selHeurPanel
            // 
            this.selHeurPanel.BackColor = System.Drawing.Color.Silver;
            this.selHeurPanel.Controls.Add(this.selectionHeuristicDesc);
            this.selHeurPanel.Controls.Add(this.selectionHeuristicLabel);
            this.selHeurPanel.Controls.Add(this.selectionHeuristicCB);
            this.selHeurPanel.Controls.Add(this.selectionHeuristicPB);
            this.selHeurPanel.Location = new System.Drawing.Point(9, 524);
            this.selHeurPanel.Name = "selHeurPanel";
            this.selHeurPanel.Size = new System.Drawing.Size(346, 112);
            this.selHeurPanel.TabIndex = 34;
            this.selHeurPanel.Visible = false;
            // 
            // selectionHeuristicDesc
            // 
            this.selectionHeuristicDesc.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.selectionHeuristicDesc.BackColor = System.Drawing.Color.Transparent;
            this.selectionHeuristicDesc.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectionHeuristicDesc.Location = new System.Drawing.Point(90, 42);
            this.selectionHeuristicDesc.Name = "selectionHeuristicDesc";
            this.selectionHeuristicDesc.Size = new System.Drawing.Size(139, 58);
            this.selectionHeuristicDesc.TabIndex = 33;
            this.selectionHeuristicDesc.Text = "Select the most logical choice based on the amount of available options, and solv" +
    "e ties randomly";
            this.selectionHeuristicDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // selectionHeuristicLabel
            // 
            this.selectionHeuristicLabel.AutoSize = true;
            this.selectionHeuristicLabel.BackColor = System.Drawing.Color.Transparent;
            this.selectionHeuristicLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectionHeuristicLabel.Location = new System.Drawing.Point(11, 39);
            this.selectionHeuristicLabel.Name = "selectionHeuristicLabel";
            this.selectionHeuristicLabel.Size = new System.Drawing.Size(67, 30);
            this.selectionHeuristicLabel.TabIndex = 32;
            this.selectionHeuristicLabel.Text = "Selection\r\nHeuristic";
            // 
            // selectionHeuristicCB
            // 
            this.selectionHeuristicCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.selectionHeuristicCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
            this.outputImageWidthLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputImageWidthLabel.Location = new System.Drawing.Point(1108, 60);
            this.outputImageWidthLabel.Name = "outputImageWidthLabel";
            this.outputImageWidthLabel.Size = new System.Drawing.Size(29, 18);
            this.outputImageWidthLabel.TabIndex = 6;
            this.outputImageWidthLabel.Text = "W:";
            // 
            // markerButton
            // 
            this.markerButton.BackColor = System.Drawing.Color.Transparent;
            this.markerButton.BackgroundImage = global::WFC4All.Properties.Resources.Save;
            this.markerButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.markerButton.FlatAppearance.BorderSize = 0;
            this.markerButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.markerButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.markerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.markerButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.markerButton.ForeColor = System.Drawing.Color.Transparent;
            this.markerButton.Location = new System.Drawing.Point(1095, 811);
            this.markerButton.Name = "markerButton";
            this.markerButton.Size = new System.Drawing.Size(70, 70);
            this.markerButton.TabIndex = 29;
            this.markerButton.UseVisualStyleBackColor = false;
            this.markerButton.Click += new System.EventHandler(this.markerButton_Click);
            // 
            // outputImageHeightLabel
            // 
            this.outputImageHeightLabel.AutoSize = true;
            this.outputImageHeightLabel.BackColor = System.Drawing.Color.Transparent;
            this.outputImageHeightLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputImageHeightLabel.Location = new System.Drawing.Point(1199, 60);
            this.outputImageHeightLabel.Name = "outputImageHeightLabel";
            this.outputImageHeightLabel.Size = new System.Drawing.Size(25, 18);
            this.outputImageHeightLabel.TabIndex = 7;
            this.outputImageHeightLabel.Text = "H:";
            // 
            // revertMarkerButton
            // 
            this.revertMarkerButton.BackColor = System.Drawing.Color.Transparent;
            this.revertMarkerButton.BackgroundImage = global::WFC4All.Properties.Resources.Load;
            this.revertMarkerButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.revertMarkerButton.FlatAppearance.BorderSize = 0;
            this.revertMarkerButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.revertMarkerButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.revertMarkerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.revertMarkerButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.revertMarkerButton.ForeColor = System.Drawing.Color.Transparent;
            this.revertMarkerButton.Location = new System.Drawing.Point(1186, 811);
            this.revertMarkerButton.Name = "revertMarkerButton";
            this.revertMarkerButton.Size = new System.Drawing.Size(70, 70);
            this.revertMarkerButton.TabIndex = 28;
            this.revertMarkerButton.UseVisualStyleBackColor = false;
            this.revertMarkerButton.Click += new System.EventHandler(this.revertMarkerButton_Click);
            // 
            // outputSizeLabel
            // 
            this.outputSizeLabel.AutoSize = true;
            this.outputSizeLabel.BackColor = System.Drawing.Color.Transparent;
            this.outputSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputSizeLabel.Location = new System.Drawing.Point(951, 60);
            this.outputSizeLabel.Name = "outputSizeLabel";
            this.outputSizeLabel.Size = new System.Drawing.Size(146, 18);
            this.outputSizeLabel.TabIndex = 5;
            this.outputSizeLabel.Text = "Output Image Size";
            // 
            // backButton
            // 
            this.backButton.BackColor = System.Drawing.Color.Transparent;
            this.backButton.BackgroundImage = global::WFC4All.Properties.Resources.Revert;
            this.backButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.backButton.FlatAppearance.BorderSize = 0;
            this.backButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.backButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.backButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.backButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.backButton.ForeColor = System.Drawing.Color.Transparent;
            this.backButton.Location = new System.Drawing.Point(1095, 731);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(70, 70);
            this.backButton.TabIndex = 27;
            this.backButton.UseVisualStyleBackColor = false;
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            // 
            // animationSpeedLabel
            // 
            this.animationSpeedLabel.BackColor = System.Drawing.Color.Transparent;
            this.animationSpeedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.animationSpeedLabel.Location = new System.Drawing.Point(1319, 808);
            this.animationSpeedLabel.Name = "animationSpeedLabel";
            this.animationSpeedLabel.Size = new System.Drawing.Size(211, 25);
            this.animationSpeedLabel.TabIndex = 26;
            this.animationSpeedLabel.Text = "Animation speed";
            this.animationSpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // animateButton
            // 
            this.animateButton.BackColor = System.Drawing.Color.Transparent;
            this.animateButton.BackgroundImage = global::WFC4All.Properties.Resources.Animate;
            this.animateButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.animateButton.FlatAppearance.BorderSize = 0;
            this.animateButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.animateButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.animateButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.animateButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.animateButton.ForeColor = System.Drawing.Color.Transparent;
            this.animateButton.Location = new System.Drawing.Point(954, 741);
            this.animateButton.Margin = new System.Windows.Forms.Padding(0);
            this.animateButton.Name = "animateButton";
            this.animateButton.Size = new System.Drawing.Size(125, 125);
            this.animateButton.TabIndex = 24;
            this.animateButton.UseVisualStyleBackColor = false;
            this.animateButton.Click += new System.EventHandler(this.animateButton_Click);
            // 
            // stepSizeLabel
            // 
            this.stepSizeLabel.BackColor = System.Drawing.Color.Transparent;
            this.stepSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.stepSizeLabel.Location = new System.Drawing.Point(1283, 730);
            this.stepSizeLabel.Name = "stepSizeLabel";
            this.stepSizeLabel.Size = new System.Drawing.Size(281, 27);
            this.stepSizeLabel.TabIndex = 23;
            this.stepSizeLabel.Text = "Amount of steps to take: 1";
            this.stepSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // advanceButton
            // 
            this.advanceButton.BackColor = System.Drawing.Color.Transparent;
            this.advanceButton.BackgroundImage = global::WFC4All.Properties.Resources.Advance;
            this.advanceButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.advanceButton.FlatAppearance.BorderSize = 0;
            this.advanceButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.advanceButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.advanceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.advanceButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.advanceButton.ForeColor = System.Drawing.Color.Transparent;
            this.advanceButton.Location = new System.Drawing.Point(1186, 731);
            this.advanceButton.Name = "advanceButton";
            this.advanceButton.Size = new System.Drawing.Size(70, 70);
            this.advanceButton.TabIndex = 21;
            this.advanceButton.UseVisualStyleBackColor = false;
            this.advanceButton.Click += new System.EventHandler(this.advanceButton_Click);
            // 
            // patternPanel
            // 
            this.patternPanel.AutoScroll = true;
            this.patternPanel.AutoScrollMargin = new System.Drawing.Size(0, 50);
            this.patternPanel.BackColor = System.Drawing.Color.Silver;
            this.patternPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.patternPanel.Controls.Add(this.patternsLabel);
            this.patternPanel.Location = new System.Drawing.Point(365, 46);
            this.patternPanel.Name = "patternPanel";
            this.patternPanel.Size = new System.Drawing.Size(551, 825);
            this.patternPanel.TabIndex = 11;
            // 
            // patternsLabel
            // 
            this.patternsLabel.AutoSize = true;
            this.patternsLabel.BackColor = System.Drawing.Color.Transparent;
            this.patternsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.patternsLabel.Location = new System.Drawing.Point(15, 14);
            this.patternsLabel.Name = "patternsLabel";
            this.patternsLabel.Size = new System.Drawing.Size(306, 18);
            this.patternsLabel.TabIndex = 0;
            this.patternsLabel.Text = "Extracted patterns with current settings!";
            // 
            // inputImageCB
            // 
            this.inputImageCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.inputImageCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.inputImageCB.FormattingEnabled = true;
            this.inputImageCB.Location = new System.Drawing.Point(136, 73);
            this.inputImageCB.Name = "inputImageCB";
            this.inputImageCB.Size = new System.Drawing.Size(214, 26);
            this.inputImageCB.TabIndex = 4;
            this.inputImageCB.SelectedIndexChanged += new System.EventHandler(this.inputImage_SelectedIndexChanged);
            // 
            // inputImagePB
            // 
            this.inputImagePB.Location = new System.Drawing.Point(26, 112);
            this.inputImagePB.Name = "inputImagePB";
            this.inputImagePB.Size = new System.Drawing.Size(300, 300);
            this.inputImagePB.TabIndex = 3;
            this.inputImagePB.TabStop = false;
            // 
            // inputImage
            // 
            this.inputImage.BackColor = System.Drawing.Color.Transparent;
            this.inputImage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.inputImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.inputImage.Location = new System.Drawing.Point(20, 76);
            this.inputImage.Name = "inputImage";
            this.inputImage.Size = new System.Drawing.Size(180, 21);
            this.inputImage.TabIndex = 2;
            this.inputImage.Text = "Input Image";
            // 
            // resultPB
            // 
            this.resultPB.BackColor = System.Drawing.Color.Silver;
            this.resultPB.Location = new System.Drawing.Point(954, 111);
            this.resultPB.Name = "resultPB";
            this.resultPB.Size = new System.Drawing.Size(600, 600);
            this.resultPB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.resultPB.TabIndex = 0;
            this.resultPB.TabStop = false;
            this.resultPB.Click += new System.EventHandler(this.resultPB_Click);
            // 
            // restartButton
            // 
            this.restartButton.BackColor = System.Drawing.Color.Transparent;
            this.restartButton.BackgroundImage = global::WFC4All.Properties.Resources.Restart;
            this.restartButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.restartButton.FlatAppearance.BorderSize = 0;
            this.restartButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.restartButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.restartButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.restartButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.restartButton.ForeColor = System.Drawing.Color.Transparent;
            this.restartButton.Location = new System.Drawing.Point(1359, 44);
            this.restartButton.Name = "restartButton";
            this.restartButton.Size = new System.Drawing.Size(125, 50);
            this.restartButton.TabIndex = 1;
            this.restartButton.UseVisualStyleBackColor = false;
            this.restartButton.Click += new System.EventHandler(this.executeButton_Click);
            // 
            // inputPanel
            // 
            this.inputPanel.BackColor = System.Drawing.Color.Silver;
            this.inputPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.inputPanel.Controls.Add(this.inputPaddingPB);
            this.inputPanel.Controls.Add(this.modelChoice);
            this.inputPanel.Controls.Add(this.patternSize);
            this.inputPanel.Controls.Add(this.patternSizeLabel);
            this.inputPanel.Location = new System.Drawing.Point(9, 418);
            this.inputPanel.Name = "inputPanel";
            this.inputPanel.Size = new System.Drawing.Size(346, 100);
            this.inputPanel.TabIndex = 35;
            // 
            // inputPaddingPB
            // 
            this.inputPaddingPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.inputPaddingPB.Location = new System.Drawing.Point(247, 1);
            this.inputPaddingPB.Name = "inputPaddingPB";
            this.inputPaddingPB.Size = new System.Drawing.Size(94, 94);
            this.inputPaddingPB.TabIndex = 38;
            this.inputPaddingPB.TabStop = false;
            this.inputPaddingPB.Click += new System.EventHandler(this.inputPaddingPB_Click);
            // 
            // modelChoice
            // 
            this.modelChoice.BackColor = System.Drawing.Color.Transparent;
            this.modelChoice.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.modelChoice.Location = new System.Drawing.Point(114, 14);
            this.modelChoice.Name = "modelChoice";
            this.modelChoice.Size = new System.Drawing.Size(129, 73);
            this.modelChoice.TabIndex = 8;
            this.modelChoice.Text = "Switch to Tile Mode";
            this.modelChoice.UseVisualStyleBackColor = false;
            this.modelChoice.Click += new System.EventHandler(this.modelChoice_Click);
            // 
            // patternSize
            // 
            this.patternSize.BackColor = System.Drawing.SystemColors.ControlLight;
            this.patternSize.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
            this.patternSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.patternSizeLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.patternSizeLabel.Location = new System.Drawing.Point(3, 14);
            this.patternSizeLabel.Name = "patternSizeLabel";
            this.patternSizeLabel.Size = new System.Drawing.Size(107, 44);
            this.patternSizeLabel.TabIndex = 1;
            this.patternSizeLabel.Text = "Pattern Size\r\n(n x n)";
            this.patternSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // loadingPB
            // 
            this.loadingPB.BackColor = System.Drawing.Color.Transparent;
            this.loadingPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.loadingPB.Image = global::WFC4All.Properties.Resources.Loading;
            this.loadingPB.Location = new System.Drawing.Point(602, 305);
            this.loadingPB.Name = "loadingPB";
            this.loadingPB.Size = new System.Drawing.Size(663, 218);
            this.loadingPB.TabIndex = 39;
            this.loadingPB.TabStop = false;
            this.loadingPB.WaitOnLoad = true;
            // 
            // IconPB
            // 
            this.IconPB.BackgroundImage = global::WFC4All.Properties.Resources.iconPNG;
            this.IconPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.IconPB.InitialImage = global::WFC4All.Properties.Resources.iconPNG;
            this.IconPB.Location = new System.Drawing.Point(56, 572);
            this.IconPB.Name = "IconPB";
            this.IconPB.Size = new System.Drawing.Size(250, 250);
            this.IconPB.TabIndex = 42;
            this.IconPB.TabStop = false;
            // 
            // imUsefulPB
            // 
            this.imUsefulPB.BackColor = System.Drawing.Color.Transparent;
            this.imUsefulPB.BackgroundImage = global::WFC4All.Properties.Resources.TryMe;
            this.imUsefulPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.imUsefulPB.Location = new System.Drawing.Point(65, 828);
            this.imUsefulPB.Name = "imUsefulPB";
            this.imUsefulPB.Size = new System.Drawing.Size(120, 50);
            this.imUsefulPB.TabIndex = 46;
            this.imUsefulPB.TabStop = false;
            // 
            // closeButton
            // 
            this.closeButton.BackColor = System.Drawing.Color.Transparent;
            this.closeButton.BackgroundImage = global::WFC4All.Properties.Resources.Close;
            this.closeButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.closeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.closeButton.ForeColor = System.Drawing.Color.Transparent;
            this.closeButton.Location = new System.Drawing.Point(1164, 35);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(50, 50);
            this.closeButton.TabIndex = 45;
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // infoGraphicPB
            // 
            this.infoGraphicPB.BackColor = System.Drawing.Color.Transparent;
            this.infoGraphicPB.BackgroundImage = global::WFC4All.Properties.Resources.InfoGraphic;
            this.infoGraphicPB.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.infoGraphicPB.Location = new System.Drawing.Point(358, 22);
            this.infoGraphicPB.Name = "infoGraphicPB";
            this.infoGraphicPB.Size = new System.Drawing.Size(871, 874);
            this.infoGraphicPB.TabIndex = 44;
            this.infoGraphicPB.TabStop = false;
            this.infoGraphicPB.WaitOnLoad = true;
            // 
            // infoButton
            // 
            this.infoButton.BackColor = System.Drawing.Color.Transparent;
            this.infoButton.BackgroundImage = global::WFC4All.Properties.Resources.Info;
            this.infoButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.infoButton.FlatAppearance.BorderSize = 0;
            this.infoButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.infoButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.infoButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.infoButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.infoButton.ForeColor = System.Drawing.Color.Transparent;
            this.infoButton.Location = new System.Drawing.Point(9, 838);
            this.infoButton.Name = "infoButton";
            this.infoButton.Size = new System.Drawing.Size(50, 50);
            this.infoButton.TabIndex = 43;
            this.infoButton.UseVisualStyleBackColor = false;
            this.infoButton.Click += new System.EventHandler(this.infoButton_Click);
            // 
            // tabSelection
            // 
            this.tabSelection.Controls.Add(this.task1Tab);
            this.tabSelection.Controls.Add(this.task2Tab);
            this.tabSelection.Controls.Add(this.sandBoxTab);
            this.tabSelection.Location = new System.Drawing.Point(0, 0);
            this.tabSelection.Name = "tabSelection";
            this.tabSelection.SelectedIndex = 0;
            this.tabSelection.Size = new System.Drawing.Size(1600, 900);
            this.tabSelection.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabSelection.TabIndex = 2;
            this.tabSelection.SelectedIndexChanged += new System.EventHandler(this.tabSelection_SelectedIndexChanged);
            // 
            // task2Tab
            // 
            this.task2Tab.AutoScroll = true;
            this.task2Tab.BackColor = System.Drawing.Color.DarkGray;
            this.task2Tab.Location = new System.Drawing.Point(4, 22);
            this.task2Tab.Name = "task2Tab";
            this.task2Tab.Padding = new System.Windows.Forms.Padding(3);
            this.task2Tab.Size = new System.Drawing.Size(1592, 874);
            this.task2Tab.TabIndex = 1;
            this.task2Tab.Text = "Task 2";
            // 
            // sandBoxTab
            // 
            this.sandBoxTab.BackColor = System.Drawing.Color.DarkGray;
            this.sandBoxTab.Location = new System.Drawing.Point(4, 22);
            this.sandBoxTab.Name = "sandBoxTab";
            this.sandBoxTab.Size = new System.Drawing.Size(1592, 874);
            this.sandBoxTab.TabIndex = 2;
            this.sandBoxTab.Text = "SandBox";
            // 
            // exportButton
            // 
            this.exportButton.BackColor = System.Drawing.Color.Transparent;
            this.exportButton.BackgroundImage = global::WFC4All.Properties.Resources.Export;
            this.exportButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.exportButton.FlatAppearance.BorderSize = 0;
            this.exportButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.exportButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.exportButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.exportButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.exportButton.ForeColor = System.Drawing.Color.Transparent;
            this.exportButton.Location = new System.Drawing.Point(1502, 44);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(50, 50);
            this.exportButton.TabIndex = 47;
            this.exportButton.UseVisualStyleBackColor = false;
            this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.exportButton);
            this.Controls.Add(this.imUsefulPB);
            this.Controls.Add(this.infoButton);
            this.Controls.Add(this.IconPB);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.loadingPB);
            this.Controls.Add(this.infoGraphicPB);
            this.Controls.Add(this.slowSpeedPB);
            this.Controls.Add(this.fastSpeedPB);
            this.Controls.Add(this.animSpeedTrackbar);
            this.Controls.Add(this.stepSizeTrackbar);
            this.Controls.Add(this.category);
            this.Controls.Add(this.categoryCB);
            this.Controls.Add(this.outputWidthValue);
            this.Controls.Add(this.pattHeurPanel);
            this.Controls.Add(this.outputHeightValue);
            this.Controls.Add(this.selHeurPanel);
            this.Controls.Add(this.outputImageWidthLabel);
            this.Controls.Add(this.markerButton);
            this.Controls.Add(this.outputImageHeightLabel);
            this.Controls.Add(this.revertMarkerButton);
            this.Controls.Add(this.outputSizeLabel);
            this.Controls.Add(this.backButton);
            this.Controls.Add(this.animationSpeedLabel);
            this.Controls.Add(this.animateButton);
            this.Controls.Add(this.stepSizeLabel);
            this.Controls.Add(this.advanceButton);
            this.Controls.Add(this.patternPanel);
            this.Controls.Add(this.inputImageCB);
            this.Controls.Add(this.inputImagePB);
            this.Controls.Add(this.inputImage);
            this.Controls.Add(this.resultPB);
            this.Controls.Add(this.restartButton);
            this.Controls.Add(this.inputPanel);
            this.Controls.Add(this.tabSelection);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1616, 939);
            this.Name = "Form1";
            this.Text = "WFC4All";
            ((System.ComponentModel.ISupportInitialize)(this.slowSpeedPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fastSpeedPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.animSpeedTrackbar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.stepSizeTrackbar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputWidthValue)).EndInit();
            this.pattHeurPanel.ResumeLayout(false);
            this.pattHeurPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.patternHeuristicPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputHeightValue)).EndInit();
            this.selHeurPanel.ResumeLayout(false);
            this.selHeurPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.selectionHeuristicPB)).EndInit();
            this.patternPanel.ResumeLayout(false);
            this.patternPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.inputImagePB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.resultPB)).EndInit();
            this.inputPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.inputPaddingPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.loadingPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.IconPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imUsefulPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.infoGraphicPB)).EndInit();
            this.tabSelection.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.PictureBox imUsefulPB;

        private System.Windows.Forms.Button closeButton;

        private System.Windows.Forms.PictureBox infoGraphicPB;

        private System.Windows.Forms.Button infoButton;

        private System.Windows.Forms.PictureBox loadingPB;

        private System.Windows.Forms.PictureBox IconPB;

        #endregion
        private System.Windows.Forms.TabControl tabSelection;
        private TabPage task1Tab;
        private PictureBox slowSpeedPB;
        private PictureBox fastSpeedPB;
        private TrackBar animSpeedTrackbar;
        private TrackBar stepSizeTrackbar;
        private System.Windows.Forms.ComboBox categoryCB;
        private Label category;
        private NumericUpDown outputWidthValue;
        private Panel pattHeurPanel;
        private Label patternHeuristicDesc;
        private Label patternSelectionLabel;
        private ComboBox patternHeuristicCB;
        private PictureBox patternHeuristicPB;
        private NumericUpDown outputHeightValue;
        private Panel selHeurPanel;
        private Label selectionHeuristicDesc;
        private Label selectionHeuristicLabel;
        private ComboBox selectionHeuristicCB;
        private PictureBox selectionHeuristicPB;
        private Label outputImageWidthLabel;
        private Button markerButton;
        private Label outputImageHeightLabel;
        private Button revertMarkerButton;
        private Label outputSizeLabel;
        private Button backButton;
        private Label animationSpeedLabel;
        private Button animateButton;
        private Label stepSizeLabel;
        private Button advanceButton;
        public Panel patternPanel;
        private Label patternsLabel;
        private ComboBox inputImageCB;
        private PictureBox inputImagePB;
        private Label inputImage;
        public PictureBox resultPB;
        private Button restartButton;
        private Panel inputPanel;
        private PictureBox inputPaddingPB;
        private Button modelChoice;
        private ComboBox patternSize;
        private Label patternSizeLabel;
        private TabPage task2Tab;
        private System.Windows.Forms.TabPage sandBoxTab;
        private System.Windows.Forms.Button exportButton;
    }
}