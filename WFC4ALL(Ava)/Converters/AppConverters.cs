using Avalonia;
using Avalonia.Data.Converters;
using WFC4ALL.ContentControls;
// ReSharper disable InconsistentNaming

namespace WFC4ALL.Converters; 

public static class AppConverters {
    public static readonly IValueConverter GridlineToStartPoint =
        new FuncValueConverter<GridlineViewModel, Point>(line => new Point(line!.X1, line.Y1));

    public static readonly IValueConverter GridlineToEndPoint =
        new FuncValueConverter<GridlineViewModel, Point>(line => new Point(line!.X2, line.Y2));
}