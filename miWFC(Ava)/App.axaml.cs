using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using miWFC.Managers;
using miWFC.ViewModels;
using miWFC.Views;

namespace miWFC;

/// <summary>
/// Entry point of the Avalonia Application
/// </summary>
public class App : Application {
    
    /*
     * Initializing Functions & Constructor
     */

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
            WeightMapWindow wMW = new() {
                DataContext = mWVM
            };
            desktop.MainWindow = mW;

            CentralManager cm = new(mWVM, mW, pW, iW, wMW);

            mW.setCentralManager(cm);
            pW.setCentralManager(cm);
            iW.setCentralManager(cm);
            wMW.setCentralManager(cm);
            mWVM.setCentralManager(cm);

            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        base.OnFrameworkInitializationCompleted();
    }
}