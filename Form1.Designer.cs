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
            this.executeButton = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
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
            this.tabPage2 = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.resultPB)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.outputHeightValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputWidthValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputImagePB)).BeginInit();
            this.tabPage2.SuspendLayout();
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
            // executeButton
            // 
            this.executeButton.Location = new System.Drawing.Point(1171, 746);
            this.executeButton.Name = "executeButton";
            this.executeButton.Size = new System.Drawing.Size(150, 75);
            this.executeButton.TabIndex = 1;
            this.executeButton.Text = "Run";
            this.executeButton.UseVisualStyleBackColor = true;
            this.executeButton.Click += new System.EventHandler(this.executeButton_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1600, 900);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.DarkGray;
            this.tabPage1.Controls.Add(this.extractPatternsButton);
            this.tabPage1.Controls.Add(this.periodicInput);
            this.tabPage1.Controls.Add(this.modelChoice);
            this.tabPage1.Controls.Add(this.outputImageHeightLabel);
            this.tabPage1.Controls.Add(this.outputImageWidthLabel);
            this.tabPage1.Controls.Add(this.outputSizeLabel);
            this.tabPage1.Controls.Add(this.outputHeightValue);
            this.tabPage1.Controls.Add(this.outputWidthValue);
            this.tabPage1.Controls.Add(this.inputImageCB);
            this.tabPage1.Controls.Add(this.inputImagePB);
            this.tabPage1.Controls.Add(this.inputImage);
            this.tabPage1.Controls.Add(this.patternSizeLabel);
            this.tabPage1.Controls.Add(this.patternSize);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1592, 874);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Input Manipulation";
            // 
            // extractPatternsButton
            // 
            this.extractPatternsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.extractPatternsButton.Location = new System.Drawing.Point(23, 558);
            this.extractPatternsButton.Name = "extractPatternsButton";
            this.extractPatternsButton.Size = new System.Drawing.Size(302, 35);
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
            this.inputImagePB.Location = new System.Drawing.Point(20, 84);
            this.inputImagePB.Name = "inputImagePB";
            this.inputImagePB.Size = new System.Drawing.Size(306, 306);
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
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.DarkGray;
            this.tabPage2.Controls.Add(this.resultPB);
            this.tabPage2.Controls.Add(this.executeButton);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1592, 874);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Algorithm";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "WFC4All";
            ((System.ComponentModel.ISupportInitialize)(this.resultPB)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.outputHeightValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputWidthValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputImagePB)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Label inputImage;
        private System.Windows.Forms.PictureBox inputImagePB;
        private System.Windows.Forms.ComboBox inputImageCB;

        private System.Windows.Forms.ComboBox patternSize;
        private System.Windows.Forms.Label patternSizeLabel;

        private System.Windows.Forms.TabControl tabControl1;
        public System.Windows.Forms.TabPage tabPage1;
        public System.Windows.Forms.TabPage tabPage2;

        private System.Windows.Forms.Button executeButton;

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
    }
}