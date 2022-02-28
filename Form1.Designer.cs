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
            this.markerButton = new System.Windows.Forms.Button();
            this.revertMarkerButton = new System.Windows.Forms.Button();
            this.backButton = new System.Windows.Forms.Button();
            this.animationSpeedLabel = new System.Windows.Forms.Label();
            this.animationSpeedValue = new System.Windows.Forms.NumericUpDown();
            this.animateButton = new System.Windows.Forms.Button();
            this.stepValue = new System.Windows.Forms.NumericUpDown();
            this.stepSizeLabel = new System.Windows.Forms.Label();
            this.advanceButton = new System.Windows.Forms.Button();
            this.patternRotationLabel = new System.Windows.Forms.Label();
            this.p3RotPB = new System.Windows.Forms.PictureBox();
            this.p2RotPB = new System.Windows.Forms.PictureBox();
            this.p1RotPB = new System.Windows.Forms.PictureBox();
            this.originalRotPB = new System.Windows.Forms.PictureBox();
            this.patternPanel = new System.Windows.Forms.Panel();
            this.patternsLabel = new System.Windows.Forms.Label();
            this.periodicInput = new System.Windows.Forms.CheckBox();
            this.modelChoice = new System.Windows.Forms.Button();
            this.outputImageHeightLabel = new System.Windows.Forms.Label();
            this.outputImageWidthLabel = new System.Windows.Forms.Label();
            this.outputSizeLabel = new System.Windows.Forms.Label();
            this.outputHeightValue = new System.Windows.Forms.NumericUpDown();
            this.outputWidthValue = new System.Windows.Forms.NumericUpDown();
            this.inputImageCB = new System.Windows.Forms.ComboBox();
            this.inputImagePB = new System.Windows.Forms.PictureBox();
            this.inputImage = new System.Windows.Forms.Label();
            this.patternSizeLabel = new System.Windows.Forms.Label();
            this.patternSize = new System.Windows.Forms.ComboBox();
            this.resultPB = new System.Windows.Forms.PictureBox();
            this.restartButton = new System.Windows.Forms.Button();
            this.tabSelection = new System.Windows.Forms.TabControl();
            this.inputTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.animationSpeedValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.stepValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.p3RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.p2RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.p1RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.originalRotPB)).BeginInit();
            this.patternPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.outputHeightValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.outputWidthValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.inputImagePB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.resultPB)).BeginInit();
            this.tabSelection.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputTab
            // 
            this.inputTab.AutoScroll = true;
            this.inputTab.BackColor = System.Drawing.Color.DarkGray;
            this.inputTab.Controls.Add(this.markerButton);
            this.inputTab.Controls.Add(this.revertMarkerButton);
            this.inputTab.Controls.Add(this.backButton);
            this.inputTab.Controls.Add(this.animationSpeedLabel);
            this.inputTab.Controls.Add(this.animationSpeedValue);
            this.inputTab.Controls.Add(this.animateButton);
            this.inputTab.Controls.Add(this.stepValue);
            this.inputTab.Controls.Add(this.stepSizeLabel);
            this.inputTab.Controls.Add(this.advanceButton);
            this.inputTab.Controls.Add(this.patternRotationLabel);
            this.inputTab.Controls.Add(this.p3RotPB);
            this.inputTab.Controls.Add(this.p2RotPB);
            this.inputTab.Controls.Add(this.p1RotPB);
            this.inputTab.Controls.Add(this.originalRotPB);
            this.inputTab.Controls.Add(this.patternPanel);
            this.inputTab.Controls.Add(this.periodicInput);
            this.inputTab.Controls.Add(this.modelChoice);
            this.inputTab.Controls.Add(this.outputImageHeightLabel);
            this.inputTab.Controls.Add(this.outputImageWidthLabel);
            this.inputTab.Controls.Add(this.outputSizeLabel);
            this.inputTab.Controls.Add(this.outputHeightValue);
            this.inputTab.Controls.Add(this.outputWidthValue);
            this.inputTab.Controls.Add(this.inputImageCB);
            this.inputTab.Controls.Add(this.inputImagePB);
            this.inputTab.Controls.Add(this.inputImage);
            this.inputTab.Controls.Add(this.patternSizeLabel);
            this.inputTab.Controls.Add(this.patternSize);
            this.inputTab.Controls.Add(this.resultPB);
            this.inputTab.Controls.Add(this.restartButton);
            this.inputTab.Location = new System.Drawing.Point(4, 22);
            this.inputTab.Name = "inputTab";
            this.inputTab.Padding = new System.Windows.Forms.Padding(3);
            this.inputTab.Size = new System.Drawing.Size(1592, 874);
            this.inputTab.TabIndex = 0;
            this.inputTab.Text = "Input Manipulation";
            // 
            // markerButton
            // 
            this.markerButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.markerButton.Location = new System.Drawing.Point(938, 781);
            this.markerButton.Name = "markerButton";
            this.markerButton.Size = new System.Drawing.Size(110, 40);
            this.markerButton.TabIndex = 29;
            this.markerButton.Text = "Save";
            this.markerButton.UseVisualStyleBackColor = true;
            this.markerButton.Click += new System.EventHandler(this.markerButton_Click);
            // 
            // revertMarkerButton
            // 
            this.revertMarkerButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.revertMarkerButton.Location = new System.Drawing.Point(1054, 781);
            this.revertMarkerButton.Name = "revertMarkerButton";
            this.revertMarkerButton.Size = new System.Drawing.Size(110, 40);
            this.revertMarkerButton.TabIndex = 28;
            this.revertMarkerButton.Text = "Load";
            this.revertMarkerButton.UseVisualStyleBackColor = true;
            this.revertMarkerButton.Click += new System.EventHandler(this.revertMarkerButton_Click);
            // 
            // backButton
            // 
            this.backButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.backButton.Location = new System.Drawing.Point(938, 736);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(110, 40);
            this.backButton.TabIndex = 27;
            this.backButton.Text = "Step Back";
            this.backButton.UseVisualStyleBackColor = true;
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            // 
            // animationSpeedLabel
            // 
            this.animationSpeedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.animationSpeedLabel.Location = new System.Drawing.Point(1293, 788);
            this.animationSpeedLabel.Name = "animationSpeedLabel";
            this.animationSpeedLabel.Size = new System.Drawing.Size(173, 64);
            this.animationSpeedLabel.TabIndex = 26;
            this.animationSpeedLabel.Text = "Animation speed in ms (time between frames)";
            this.animationSpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // animationSpeedValue
            // 
            this.animationSpeedValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.animationSpeedValue.Location = new System.Drawing.Point(1471, 809);
            this.animationSpeedValue.Maximum = new decimal(new int[] {2000, 0, 0, 0});
            this.animationSpeedValue.Minimum = new decimal(new int[] {1, 0, 0, 0});
            this.animationSpeedValue.Name = "animationSpeedValue";
            this.animationSpeedValue.Size = new System.Drawing.Size(67, 24);
            this.animationSpeedValue.TabIndex = 25;
            this.animationSpeedValue.Value = new decimal(new int[] {10, 0, 0, 0});
            // 
            // animateButton
            // 
            this.animateButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.animateButton.Location = new System.Drawing.Point(1053, 736);
            this.animateButton.Name = "animateButton";
            this.animateButton.Size = new System.Drawing.Size(110, 40);
            this.animateButton.TabIndex = 24;
            this.animateButton.Text = "Animate";
            this.animateButton.UseVisualStyleBackColor = true;
            this.animateButton.Click += new System.EventHandler(this.animateButton_Click);
            // 
            // stepValue
            // 
            this.stepValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.stepValue.Location = new System.Drawing.Point(1471, 736);
            this.stepValue.Maximum = new decimal(new int[] {999, 0, 0, 0});
            this.stepValue.Minimum = new decimal(new int[] {1, 0, 0, -2147483648});
            this.stepValue.Name = "stepValue";
            this.stepValue.Size = new System.Drawing.Size(67, 24);
            this.stepValue.TabIndex = 22;
            this.stepValue.Value = new decimal(new int[] {10, 0, 0, 0});
            this.stepValue.ValueChanged += new System.EventHandler(this.stepCountValueChanged);
            // 
            // stepSizeLabel
            // 
            this.stepSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.stepSizeLabel.Location = new System.Drawing.Point(1297, 716);
            this.stepSizeLabel.Name = "stepSizeLabel";
            this.stepSizeLabel.Size = new System.Drawing.Size(161, 64);
            this.stepSizeLabel.TabIndex = 23;
            this.stepSizeLabel.Text = "Amount of steps to execute (-1 means to continue until finished)";
            this.stepSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // advanceButton
            // 
            this.advanceButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.advanceButton.Location = new System.Drawing.Point(1168, 736);
            this.advanceButton.Name = "advanceButton";
            this.advanceButton.Size = new System.Drawing.Size(110, 40);
            this.advanceButton.TabIndex = 21;
            this.advanceButton.Text = "Advance";
            this.advanceButton.UseVisualStyleBackColor = true;
            this.advanceButton.Click += new System.EventHandler(this.advanceButton_Click);
            // 
            // patternRotationLabel
            // 
            this.patternRotationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternRotationLabel.Location = new System.Drawing.Point(23, 658);
            this.patternRotationLabel.Name = "patternRotationLabel";
            this.patternRotationLabel.Size = new System.Drawing.Size(180, 60);
            this.patternRotationLabel.TabIndex = 20;
            this.patternRotationLabel.Text = "Select allowed pattern transformations. On the right is a non-transformed referen" + "ce (black border)";
            this.patternRotationLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.patternRotationLabel.Visible = false;
            // 
            // p3RotPB
            // 
            this.p3RotPB.Location = new System.Drawing.Point(246, 761);
            this.p3RotPB.Name = "p3RotPB";
            this.p3RotPB.Size = new System.Drawing.Size(60, 60);
            this.p3RotPB.TabIndex = 15;
            this.p3RotPB.TabStop = false;
            this.p3RotPB.Visible = false;
            this.p3RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p2RotPB
            // 
            this.p2RotPB.Location = new System.Drawing.Point(146, 764);
            this.p2RotPB.Name = "p2RotPB";
            this.p2RotPB.Size = new System.Drawing.Size(60, 60);
            this.p2RotPB.TabIndex = 14;
            this.p2RotPB.TabStop = false;
            this.p2RotPB.Visible = false;
            this.p2RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p1RotPB
            // 
            this.p1RotPB.Location = new System.Drawing.Point(46, 764);
            this.p1RotPB.Name = "p1RotPB";
            this.p1RotPB.Size = new System.Drawing.Size(60, 60);
            this.p1RotPB.TabIndex = 13;
            this.p1RotPB.TabStop = false;
            this.p1RotPB.Visible = false;
            this.p1RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // originalRotPB
            // 
            this.originalRotPB.Location = new System.Drawing.Point(226, 660);
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
            this.patternsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternsLabel.Location = new System.Drawing.Point(14, 14);
            this.patternsLabel.Name = "patternsLabel";
            this.patternsLabel.Size = new System.Drawing.Size(459, 56);
            this.patternsLabel.TabIndex = 0;
            this.patternsLabel.Text = "Extracted patterns, you can deselect patterns you don\'t want to show up in the fi" + "nal generated image (red = excluded). Duplicate patterns (such as rotated or fli" + "pped) are excluded!";
            // 
            // periodicInput
            // 
            this.periodicInput.AutoSize = true;
            this.periodicInput.Checked = true;
            this.periodicInput.CheckState = System.Windows.Forms.CheckState.Checked;
            this.periodicInput.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.periodicInput.Location = new System.Drawing.Point(20, 516);
            this.periodicInput.Name = "periodicInput";
            this.periodicInput.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.periodicInput.Size = new System.Drawing.Size(115, 19);
            this.periodicInput.TabIndex = 9;
            this.periodicInput.Text = "Periodic Input";
            this.periodicInput.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.periodicInput.UseVisualStyleBackColor = true;
            this.periodicInput.Visible = false;
            this.periodicInput.CheckedChanged += new System.EventHandler(this.inputChanged);
            // 
            // modelChoice
            // 
            this.modelChoice.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.modelChoice.Location = new System.Drawing.Point(177, 507);
            this.modelChoice.Name = "modelChoice";
            this.modelChoice.Size = new System.Drawing.Size(149, 35);
            this.modelChoice.TabIndex = 8;
            this.modelChoice.Text = "Overlapping Model";
            this.modelChoice.UseVisualStyleBackColor = true;
            this.modelChoice.Click += new System.EventHandler(this.modelChoice_Click);
            // 
            // outputImageHeightLabel
            // 
            this.outputImageHeightLabel.AutoSize = true;
            this.outputImageHeightLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputImageHeightLabel.Location = new System.Drawing.Point(177, 472);
            this.outputImageHeightLabel.Name = "outputImageHeightLabel";
            this.outputImageHeightLabel.Size = new System.Drawing.Size(53, 15);
            this.outputImageHeightLabel.TabIndex = 7;
            this.outputImageHeightLabel.Text = "Height:";
            // 
            // outputImageWidthLabel
            // 
            this.outputImageWidthLabel.AutoSize = true;
            this.outputImageWidthLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputImageWidthLabel.Location = new System.Drawing.Point(177, 447);
            this.outputImageWidthLabel.Name = "outputImageWidthLabel";
            this.outputImageWidthLabel.Size = new System.Drawing.Size(47, 15);
            this.outputImageWidthLabel.TabIndex = 6;
            this.outputImageWidthLabel.Text = "Width:";
            // 
            // outputSizeLabel
            // 
            this.outputSizeLabel.AutoSize = true;
            this.outputSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputSizeLabel.Location = new System.Drawing.Point(20, 455);
            this.outputSizeLabel.Name = "outputSizeLabel";
            this.outputSizeLabel.Size = new System.Drawing.Size(125, 15);
            this.outputSizeLabel.TabIndex = 5;
            this.outputSizeLabel.Text = "Output Image Size";
            // 
            // outputHeightValue
            // 
            this.outputHeightValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputHeightValue.Location = new System.Drawing.Point(246, 468);
            this.outputHeightValue.Minimum = new decimal(new int[] {10, 0, 0, 0});
            this.outputHeightValue.Name = "outputHeightValue";
            this.outputHeightValue.Size = new System.Drawing.Size(80, 24);
            this.outputHeightValue.TabIndex = 3;
            this.outputHeightValue.Value = new decimal(new int[] {24, 0, 0, 0});
            this.outputHeightValue.ValueChanged += new System.EventHandler(this.outputSizeChanged);
            // 
            // outputWidthValue
            // 
            this.outputWidthValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.outputWidthValue.Location = new System.Drawing.Point(246, 443);
            this.outputWidthValue.Maximum = new decimal(new int[] {500, 0, 0, 0});
            this.outputWidthValue.Minimum = new decimal(new int[] {10, 0, 0, 0});
            this.outputWidthValue.Name = "outputWidthValue";
            this.outputWidthValue.Size = new System.Drawing.Size(80, 24);
            this.outputWidthValue.TabIndex = 4;
            this.outputWidthValue.Value = new decimal(new int[] {24, 0, 0, 0});
            this.outputWidthValue.ValueChanged += new System.EventHandler(this.outputSizeChanged);
            // 
            // inputImageCB
            // 
            this.inputImageCB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.inputImageCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.inputImageCB.FormattingEnabled = true;
            this.inputImageCB.Location = new System.Drawing.Point(112, 53);
            this.inputImageCB.Name = "inputImageCB";
            this.inputImageCB.Size = new System.Drawing.Size(214, 21);
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
            this.inputImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.inputImage.Location = new System.Drawing.Point(20, 54);
            this.inputImage.Name = "inputImage";
            this.inputImage.Size = new System.Drawing.Size(180, 21);
            this.inputImage.TabIndex = 2;
            this.inputImage.Text = "Input Image";
            // 
            // patternSizeLabel
            // 
            this.patternSizeLabel.BackColor = System.Drawing.Color.Transparent;
            this.patternSizeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.patternSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternSizeLabel.Location = new System.Drawing.Point(20, 406);
            this.patternSizeLabel.Name = "patternSizeLabel";
            this.patternSizeLabel.Size = new System.Drawing.Size(150, 20);
            this.patternSizeLabel.TabIndex = 1;
            this.patternSizeLabel.Text = "Pattern Size  (n x n)";
            // 
            // patternSize
            // 
            this.patternSize.BackColor = System.Drawing.SystemColors.ControlLight;
            this.patternSize.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.patternSize.FormattingEnabled = true;
            this.patternSize.Location = new System.Drawing.Point(177, 405);
            this.patternSize.Name = "patternSize";
            this.patternSize.Size = new System.Drawing.Size(149, 21);
            this.patternSize.TabIndex = 0;
            this.patternSize.SelectedIndexChanged += new System.EventHandler(this.inputChanged);
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
            // 
            // restartButton
            // 
            this.restartButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.restartButton.Location = new System.Drawing.Point(1168, 781);
            this.restartButton.Name = "restartButton";
            this.restartButton.Size = new System.Drawing.Size(110, 40);
            this.restartButton.TabIndex = 1;
            this.restartButton.Text = "Restart";
            this.restartButton.UseVisualStyleBackColor = true;
            this.restartButton.Click += new System.EventHandler(this.executeButton_Click);
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
            ((System.ComponentModel.ISupportInitialize) (this.animationSpeedValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.stepValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.p3RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.p2RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.p1RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.originalRotPB)).EndInit();
            this.patternPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.outputHeightValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.outputWidthValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.inputImagePB)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.resultPB)).EndInit();
            this.tabSelection.ResumeLayout(false);
            this.ResumeLayout(false);
        }

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
        private System.Windows.Forms.CheckBox periodicInput;
        private System.Windows.Forms.Button modelChoice;
        private System.Windows.Forms.Label outputImageHeightLabel;
        private System.Windows.Forms.Label outputImageWidthLabel;
        private System.Windows.Forms.Label outputSizeLabel;
        private System.Windows.Forms.NumericUpDown outputHeightValue;
        private System.Windows.Forms.NumericUpDown outputWidthValue;
        private ComboBox inputImageCB;
        private PictureBox inputImagePB;
        private Label inputImage;
        private System.Windows.Forms.Label patternSizeLabel;
        private System.Windows.Forms.ComboBox patternSize;
        public PictureBox resultPB;
        private System.Windows.Forms.Button restartButton;
        private TabControl tabSelection;
        private System.Windows.Forms.NumericUpDown stepValue;
        private System.Windows.Forms.Label stepSizeLabel;
        private System.Windows.Forms.Button advanceButton;
        private System.Windows.Forms.Button animateButton;
        private System.Windows.Forms.Label animationSpeedLabel;
        private System.Windows.Forms.NumericUpDown animationSpeedValue;
        private System.Windows.Forms.Button backButton;
    }
}