using System;
using System.Drawing;
using System.Windows.Forms;

namespace WFC4All {
    public partial class Form2 : Form {

        public Form2() {
            InitializeComponent();
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            closeButton = new Button();
            errorMessage = new Label();
            SuspendLayout();
            // 
            // closeButton
            // 
            closeButton.Font = new Font("Microsoft Sans Serif", 14.25F, FontStyle.Bold, GraphicsUnit.Point, (byte)0);
            closeButton.Location = new Point(131, 92);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(110, 47);
            closeButton.TabIndex = 0;
            closeButton.Text = "Close";
            closeButton.UseVisualStyleBackColor = true;
            closeButton.Click += new EventHandler(closeButton_Click);
            // 
            // errorMessage
            // 
            errorMessage.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, (byte)0);
            errorMessage.ForeColor = Color.Red;
            errorMessage.Location = new Point(12, 9);
            errorMessage.Name = "errorMessage";
            errorMessage.Size = new Size(349, 80);
            errorMessage.TabIndex = 1;
            errorMessage.Text = "The current input options are not able to generate a valid output! Please change " +
    "them or select the recommended values!";
            errorMessage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Form2
            // 
            BackColor = SystemColors.ButtonFace;
            ClientSize = new Size(373, 152);
            Controls.Add(errorMessage);
            Controls.Add(closeButton);
            FormBorderStyle = FormBorderStyle.None;
            Name = "Form2";
            StartPosition = FormStartPosition.CenterScreen;
            Deactivate += new EventHandler(closeButton_Click);
            ResumeLayout(false);

        }
    }
}