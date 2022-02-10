using System;
using System.Windows.Forms;
using System.Drawing;

namespace WFC4All
{
    partial class Form2
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
        
        private System.Windows.Forms.Button closeButton;

        private void closeButton_Click(object sender, EventArgs e) {
            Program.form.Focus();
            Close();
        }

        private Label errorMessage;
    }
}