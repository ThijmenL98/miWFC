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
        private void InitializeComponent()
        {
            this.resultPB = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize) (this.resultPB)).BeginInit();
            this.SuspendLayout();
            // 
            // resultPB
            // 
            this.resultPB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.resultPB.Location = new System.Drawing.Point(868, 164);
            this.resultPB.Name = "resultPB";
            this.resultPB.Size = new System.Drawing.Size(400, 400);
            this.resultPB.TabIndex = 0;
            this.resultPB.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ClientSize = new System.Drawing.Size(1280, 720);
            this.Controls.Add(this.resultPB);
            this.Name = "Form1";
            this.Text = "WFC4All";
            ((System.ComponentModel.ISupportInitialize) (this.resultPB)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.PictureBox resultPB;

        #endregion
    }
}