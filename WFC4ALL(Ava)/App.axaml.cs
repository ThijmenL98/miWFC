using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WFC4ALL.Managers;
using WFC4ALL.ViewModels;
using WFC4ALL.Views;

namespace WFC4ALL; 

public class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
        ConsoleTraceListener myWriter = new();
        Trace.Listeners.Add(myWriter);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            MainWindowViewModel mWVM = new();
            MainWindow mW = new() {
                DataContext = mWVM
            };
            PaintingWindow pW = new() {
                DataContext = mWVM
            };
            ItemWindow iW = new() {
                DataContext = mWVM
            };
            desktop.MainWindow = mW;

            CentralManager cm = new(mWVM, mW, pW, iW);

            mW.setCentralManager(cm);
            pW.setCentralManager(cm);
            iW.setCentralManager(cm);
            mWVM.setCentralManager(cm);
        }

        base.OnFrameworkInitializationCompleted();
    }
}