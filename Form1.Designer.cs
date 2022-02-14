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
            this.resultPB = new System.Windows.Forms.PictureBox();
            this.refreshButton = new System.Windows.Forms.Button();
            this.tabSelection = new System.Windows.Forms.TabControl();
            this.inputTab = new System.Windows.Forms.TabPage();
            this.patternRotationLabel = new System.Windows.Forms.Label();
            this.p7RotPB = new System.Windows.Forms.PictureBox();
            this.p6RotPB = new System.Windows.Forms.PictureBox();
            this.p5RotPB = new System.Windows.Forms.PictureBox();
            this.p4RotPB = new System.Windows.Forms.PictureBox();
            this.p3RotPB = new System.Windows.Forms.PictureBox();
            this.p2RotPB = new System.Windows.Forms.PictureBox();
            this.p1RotPB = new System.Windows.Forms.PictureBox();
            this.originalRotPB = new System.Windows.Forms.PictureBox();
            this.patternPanel = new System.Windows.Forms.Panel();
            this.patternsLabel = new System.Windows.Forms.Label();
            this.extractPatternsButton = new System.Windows.Forms.Button();
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
            this.execTab = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.resultPB)).BeginInit();
            this.tabSelection.SuspendLayout();
            this.inputTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.p7RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.p6RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.p5RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.p4RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.p3RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.p2RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.p1RotPB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.originalRotPB)).BeginInit();
            this.patternPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.outputHeightValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputWidthValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputImagePB)).BeginInit();
            this.SuspendLayout();
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
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(1171, 746);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(150, 75);
            this.refreshButton.TabIndex = 1;
            this.refreshButton.Text = "Refresh";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.executeButton_Click);
            // 
            // tabSelection
            // 
            this.tabSelection.Controls.Add(this.inputTab);
            this.tabSelection.Controls.Add(this.execTab);
            this.tabSelection.Location = new System.Drawing.Point(0, 0);
            this.tabSelection.Name = "tabSelection";
            this.tabSelection.SelectedIndex = 0;
            this.tabSelection.Size = new System.Drawing.Size(1600, 900);
            this.tabSelection.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabSelection.TabIndex = 2;
            this.tabSelection.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.changeTab);
            // 
            // inputTab
            // 
            this.inputTab.AutoScroll = true;
            this.inputTab.BackColor = System.Drawing.Color.DarkGray;
            this.inputTab.Controls.Add(this.patternRotationLabel);
            this.inputTab.Controls.Add(this.p7RotPB);
            this.inputTab.Controls.Add(this.p6RotPB);
            this.inputTab.Controls.Add(this.p5RotPB);
            this.inputTab.Controls.Add(this.p4RotPB);
            this.inputTab.Controls.Add(this.p3RotPB);
            this.inputTab.Controls.Add(this.p2RotPB);
            this.inputTab.Controls.Add(this.p1RotPB);
            this.inputTab.Controls.Add(this.originalRotPB);
            this.inputTab.Controls.Add(this.patternPanel);
            this.inputTab.Controls.Add(this.extractPatternsButton);
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
            this.inputTab.Controls.Add(this.refreshButton);
            this.inputTab.Location = new System.Drawing.Point(4, 22);
            this.inputTab.Name = "inputTab";
            this.inputTab.Padding = new System.Windows.Forms.Padding(3);
            this.inputTab.Size = new System.Drawing.Size(1592, 874);
            this.inputTab.TabIndex = 0;
            this.inputTab.Text = "Input Manipulation";
            // 
            // patternRotationLabel
            // 
            this.patternRotationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.patternRotationLabel.Location = new System.Drawing.Point(23, 627);
            this.patternRotationLabel.Name = "patternRotationLabel";
            this.patternRotationLabel.Size = new System.Drawing.Size(180, 60);
            this.patternRotationLabel.TabIndex = 20;
            this.patternRotationLabel.Text = "Select allowed pattern transformations. On the right is a non-transformed referen" +
    "ce (black border)";
            this.patternRotationLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // p7RotPB
            // 
            this.p7RotPB.Location = new System.Drawing.Point(266, 789);
            this.p7RotPB.Name = "p7RotPB";
            this.p7RotPB.Size = new System.Drawing.Size(60, 60);
            this.p7RotPB.TabIndex = 19;
            this.p7RotPB.TabStop = false;
            this.p7RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p6RotPB
            // 
            this.p6RotPB.Location = new System.Drawing.Point(186, 789);
            this.p6RotPB.Name = "p6RotPB";
            this.p6RotPB.Size = new System.Drawing.Size(60, 60);
            this.p6RotPB.TabIndex = 18;
            this.p6RotPB.TabStop = false;
            this.p6RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p5RotPB
            // 
            this.p5RotPB.Location = new System.Drawing.Point(106, 789);
            this.p5RotPB.Name = "p5RotPB";
            this.p5RotPB.Size = new System.Drawing.Size(60, 60);
            this.p5RotPB.TabIndex = 17;
            this.p5RotPB.TabStop = false;
            this.p5RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p4RotPB
            // 
            this.p4RotPB.Location = new System.Drawing.Point(26, 789);
            this.p4RotPB.Name = "p4RotPB";
            this.p4RotPB.Size = new System.Drawing.Size(60, 60);
            this.p4RotPB.TabIndex = 16;
            this.p4RotPB.TabStop = false;
            this.p4RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p3RotPB
            // 
            this.p3RotPB.Location = new System.Drawing.Point(226, 709);
            this.p3RotPB.Name = "p3RotPB";
            this.p3RotPB.Size = new System.Drawing.Size(60, 60);
            this.p3RotPB.TabIndex = 15;
            this.p3RotPB.TabStop = false;
            this.p3RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p2RotPB
            // 
            this.p2RotPB.Location = new System.Drawing.Point(146, 709);
            this.p2RotPB.Name = "p2RotPB";
            this.p2RotPB.Size = new System.Drawing.Size(60, 60);
            this.p2RotPB.TabIndex = 14;
            this.p2RotPB.TabStop = false;
            this.p2RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // p1RotPB
            // 
            this.p1RotPB.Location = new System.Drawing.Point(66, 709);
            this.p1RotPB.Name = "p1RotPB";
            this.p1RotPB.Size = new System.Drawing.Size(60, 60);
            this.p1RotPB.TabIndex = 13;
            this.p1RotPB.TabStop = false;
            this.p1RotPB.Click += new System.EventHandler(this.inputChanged);
            // 
            // originalRotPB
            // 
            this.originalRotPB.Location = new System.Drawing.Point(226, 629);
            this.originalRotPB.Name = "originalRotPB";
            this.originalRotPB.Size = new System.Drawing.Size(60, 60);
            this.originalRotPB.TabIndex = 12;
            this.originalRotPB.TabStop = false;
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
            this.patternsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.patternsLabel.Location = new System.Drawing.Point(14, 14);
            this.patternsLabel.Name = "patternsLabel";
            this.patternsLabel.Size = new System.Drawing.Size(459, 56);
            this.patternsLabel.TabIndex = 0;
            this.patternsLabel.Text = "Extracted patterns, you can deselect patterns you don\'t want to show up in the fi" +
    "nal generated image (red = excluded). Duplicate patterns (such as rotated or fli" +
    "pped) are excluded!";
            // 
            // extractPatternsButton
            // 
            this.extractPatternsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.extractPatternsButton.Location = new System.Drawing.Point(23, 558);
            this.extractPatternsButton.Name = "extractPatternsButton";
            this.extractPatternsButton.Size = new System.Drawing.Size(303, 35);
            this.extractPatternsButton.TabIndex = 10;
            this.extractPatternsButton.Text = "Extract Patterns";
            this.extractPatternsButton.UseVisualStyleBackColor = true;
            this.extractPatternsButton.Click += new System.EventHandler(this.extractPatternsButton_Click);
            // 
            // periodicInput
            // 
            this.periodicInput.AutoSize = true;
            this.periodicInput.Checked = true;
            this.periodicInput.CheckState = System.Windows.Forms.CheckState.Checked;
            this.periodicInput.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.periodicInput.Location = new System.Drawing.Point(20, 516);
            this.periodicInput.Name = "periodicInput";
            this.periodicInput.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.periodicInput.Size = new System.Drawing.Size(115, 19);
            this.periodicInput.TabIndex = 9;
            this.periodicInput.Text = "Periodic Input";
            this.periodicInput.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.periodicInput.UseVisualStyleBackColor = true;
            this.periodicInput.CheckedChanged += new System.EventHandler(this.inputChanged);
            // 
            // modelChoice
            // 
            this.modelChoice.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
            this.outputImageHeightLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputImageHeightLabel.Location = new System.Drawing.Point(150, 469);
            this.outputImageHeightLabel.Name = "outputImageHeightLabel";
            this.outputImageHeightLabel.Size = new System.Drawing.Size(53, 15);
            this.outputImageHeightLabel.TabIndex = 7;
            this.outputImageHeightLabel.Text = "Height:";
            // 
            // outputImageWidthLabel
            // 
            this.outputImageWidthLabel.AutoSize = true;
            this.outputImageWidthLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputImageWidthLabel.Location = new System.Drawing.Point(150, 444);
            this.outputImageWidthLabel.Name = "outputImageWidthLabel";
            this.outputImageWidthLabel.Size = new System.Drawing.Size(47, 15);
            this.outputImageWidthLabel.TabIndex = 6;
            this.outputImageWidthLabel.Text = "Width:";
            // 
            // outputSizeLabel
            // 
            this.outputSizeLabel.AutoSize = true;
            this.outputSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputSizeLabel.Location = new System.Drawing.Point(20, 455);
            this.outputSizeLabel.Name = "outputSizeLabel";
            this.outputSizeLabel.Size = new System.Drawing.Size(125, 15);
            this.outputSizeLabel.TabIndex = 5;
            this.outputSizeLabel.Text = "Output Image Size";
            // 
            // outputHeightValue
            // 
            this.outputHeightValue.Location = new System.Drawing.Point(206, 469);
            this.outputHeightValue.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.outputHeightValue.Minimum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.outputHeightValue.Name = "outputHeightValue";
            this.outputHeightValue.Size = new System.Drawing.Size(67, 20);
            this.outputHeightValue.TabIndex = 3;
            this.outputHeightValue.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // outputWidthValue
            // 
            this.outputWidthValue.Location = new System.Drawing.Point(206, 443);
            this.outputWidthValue.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.outputWidthValue.Minimum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.outputWidthValue.Name = "outputWidthValue";
            this.outputWidthValue.Size = new System.Drawing.Size(67, 20);
            this.outputWidthValue.TabIndex = 4;
            this.outputWidthValue.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // inputImageCB
            // 
            this.inputImageCB.BackColor = System.Drawing.SystemColors.ControlLight;
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
            this.inputImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
            this.patternSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.patternSizeLabel.Location = new System.Drawing.Point(20, 406);
            this.patternSizeLabel.Name = "patternSizeLabel";
            this.patternSizeLabel.Size = new System.Drawing.Size(180, 20);
            this.patternSizeLabel.TabIndex = 1;
            this.patternSizeLabel.Text = "Pattern Dimension (n x n)";
            // 
            // patternSize
            // 
            this.patternSize.BackColor = System.Drawing.SystemColors.ControlLight;
            this.patternSize.FormattingEnabled = true;
            this.patternSize.Location = new System.Drawing.Point(206, 405);
            this.patternSize.Name = "patternSize";
            this.patternSize.Size = new System.Drawing.Size(120, 21);
            this.patternSize.TabIndex = 0;
            this.patternSize.SelectedIndexChanged += new System.EventHandler(this.inputChanged);
            // 
            // execTab
            // 
            this.execTab.BackColor = System.Drawing.Color.DarkGray;
            this.execTab.Location = new System.Drawing.Point(4, 22);
            this.execTab.Name = "execTab";
            this.execTab.Padding = new System.Windows.Forms.Padding(3);
            this.execTab.Size = new System.Drawing.Size(1592, 874);
            this.execTab.TabIndex = 1;
            this.execTab.Text = "Algorithm";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.tabSelection);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "WFC4All";
            ((System.ComponentModel.ISupportInitialize)(this.resultPB)).EndInit();
            this.tabSelection.ResumeLayout(false);
            this.inputTab.ResumeLayout(false);
            this.inputTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.p7RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.p6RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.p5RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.p4RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.p3RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.p2RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.p1RotPB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.originalRotPB)).EndInit();
            this.patternPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.outputHeightValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputWidthValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputImagePB)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Label inputImage;
        private System.Windows.Forms.PictureBox inputImagePB;
        private System.Windows.Forms.ComboBox inputImageCB;

        private System.Windows.Forms.ComboBox patternSize;
        private System.Windows.Forms.Label patternSizeLabel;

        private System.Windows.Forms.TabControl tabSelection;
        private System.Windows.Forms.TabPage inputTab;
        private System.Windows.Forms.TabPage execTab;

        private System.Windows.Forms.Button refreshButton;

        private System.Windows.Forms.PictureBox resultPB;

        #endregion

        private Label outputImageHeightLabel;
        private Label outputImageWidthLabel;
        private Label outputSizeLabel;
        private NumericUpDown outputHeightValue;
        private NumericUpDown outputWidthValue;
        private CheckBox periodicInput;
        private Button modelChoice;
        private Button extractPatternsButton;
        public Panel patternPanel;
        private PictureBox p3RotPB;
        private PictureBox p2RotPB;
        private PictureBox p1RotPB;
        private PictureBox p7RotPB;
        private PictureBox p6RotPB;
        private PictureBox p5RotPB;
        private PictureBox p4RotPB;
        private PictureBox originalRotPB;
        private Label patternRotationLabel;
        private Label patternsLabel;
    }
}