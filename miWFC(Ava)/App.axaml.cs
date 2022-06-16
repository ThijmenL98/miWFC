using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using miWFC.Managers;
using miWFC.ViewModels;
using miWFC.Views;

namespace miWFC;

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

            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
        }

        base.OnFrameworkInitializationCompleted();
    }
}