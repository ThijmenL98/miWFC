using System;
using System.Windows.Forms;

namespace WFC4All {
    internal static class Program {
        private static Form1 form;

        [STAThread]
        // ReSharper disable once InconsistentNaming
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();
            Application.Run(form);
        }
    }
}