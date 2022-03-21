using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WFC4ALL.ContentControls
{
    public partial class OutputControl : UserControl
    {
        public OutputControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
