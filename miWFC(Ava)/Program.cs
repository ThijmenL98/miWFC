using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Logging;
using Avalonia.ReactiveUI;

namespace miWFC;

internal static class Program {
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
#if DEBUG
        try {
#endif
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
#if DEBUG
        } catch (OutOfMemoryException e) {
            Trace.WriteLine(e);
        }
#endif
    }

    // Avalonia configuration, don't remove; also used by visual designer.

    private static AppBuilder BuildAvaloniaApp() {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace(LogEventLevel.Error)
            .UseReactiveUI();
    }
}