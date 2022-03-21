using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WFC4All;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;

namespace WFC4ALL
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            
        
            var myWriter = new ConsoleTraceListener();
            Trace.Listeners.Add(myWriter);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindowViewModel mWVM = new();
                MainWindow mW = new()
                {
                    DataContext = mWVM,
                };
                desktop.MainWindow = mW;
                InputManager inputManager = new(mWVM, mW);
                mW.setInputManager(inputManager);
                mWVM.setInputManager(inputManager);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
