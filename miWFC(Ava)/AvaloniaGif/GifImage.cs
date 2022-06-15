using System;
using System.IO;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Visuals.Media.Imaging;

namespace miWFC.AvaloniaGif;

public class GifImage : Control {
    public static readonly StyledProperty<string> sourceUriRawProperty
        = AvaloniaProperty.Register<GifImage, string>("SourceUriRaw");

    public static readonly StyledProperty<Uri>
        sourceUriProperty = AvaloniaProperty.Register<GifImage, Uri>("SourceUri");

    public static readonly StyledProperty<Stream> sourceStreamProperty
        = AvaloniaProperty.Register<GifImage, Stream>("SourceStream");

    public static readonly StyledProperty<IterationCount> iterationCountProperty
        = AvaloniaProperty.Register<GifImage, IterationCount>("IterationCount");

    public static readonly StyledProperty<bool> autoStartProperty
        = AvaloniaProperty.Register<GifImage, bool>("AutoStart");

    public static readonly StyledProperty<StretchDirection> stretchDirectionProperty
        = AvaloniaProperty.Register<GifImage, StretchDirection>("StretchDirection");

    public static readonly StyledProperty<Stretch> stretchProperty
        = AvaloniaProperty.Register<GifImage, Stretch>("Stretch");

    private RenderTargetBitmap backingRTB;
    private GifInstance gifInstance;

    static GifImage() {
        sourceUriRawProperty.Changed.Subscribe(SourceChanged);
        sourceUriProperty.Changed.Subscribe(SourceChanged);
        sourceStreamProperty.Changed.Subscribe(SourceChanged);
        iterationCountProperty.Changed.Subscribe(IterationCountChanged);
        autoStartProperty.Changed.Subscribe(AutoStartChanged);
        AffectsRender<GifImage>(sourceStreamProperty, sourceUriProperty, sourceUriRawProperty);
        AffectsArrange<GifImage>(sourceStreamProperty, sourceUriProperty, sourceUriRawProperty);
        AffectsMeasure<GifImage>(sourceStreamProperty, sourceUriProperty, sourceUriRawProperty);
    }

    public string SourceUriRaw {
        get => GetValue(sourceUriRawProperty);
        set => SetValue(sourceUriRawProperty, value);
    }

    public Uri SourceUri {
        get => GetValue(sourceUriProperty);
        set => SetValue(sourceUriProperty, value);
    }

    public Stream SourceStream {
        get => GetValue(sourceStreamProperty);
        set => SetValue(sourceStreamProperty, value);
    }

    public IterationCount IterationCount {
        get => GetValue(iterationCountProperty);
        set => SetValue(iterationCountProperty, value);
    }

    public bool AutoStart {
        get => GetValue(autoStartProperty);
        set => SetValue(autoStartProperty, value);
    }

    public StretchDirection StretchDirection {
        get => GetValue(stretchDirectionProperty);
        set => SetValue(stretchDirectionProperty, value);
    }

    public Stretch Stretch {
        get => GetValue(stretchProperty);
        set => SetValue(stretchProperty, value);
    }

    private static void AutoStartChanged(AvaloniaPropertyChangedEventArgs e) {
        GifImage? image = e.Sender as GifImage;
        if (image == null) { }
    }

    private static void IterationCountChanged(AvaloniaPropertyChangedEventArgs e) {
        GifImage? image = e.Sender as GifImage;
        if (image == null) { }
    }

    public override void Render(DrawingContext context) {
        if (gifInstance == null) {
            return;
        }

        if (gifInstance.GetBitmap() is WriteableBitmap source && backingRTB is not null) {
            using (IDrawingContextImpl? ctx = backingRTB.CreateDrawingContext(null)) {
                Rect ts = new(source.Size);
                ctx.DrawBitmap(source.PlatformImpl, 1, ts, ts);
            }
        }

        if (backingRTB is not null && Bounds.Width > 0 && Bounds.Height > 0) {
            Rect viewPort = new(Bounds.Size);
            Size sourceSize = backingRTB.Size;

            Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
            Size scaledSize = sourceSize * scale;
            Rect destRect = viewPort
                .CenterRect(new Rect(scaledSize))
                .Intersect(viewPort);

            Rect sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / scale));

            BitmapInterpolationMode interpolationMode = RenderOptions.GetBitmapInterpolationMode(this);

            context.DrawImage(backingRTB, sourceRect, destRect, interpolationMode);
        }

        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
    }

    /// <summary>
    ///     Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize) {
        RenderTargetBitmap? source = backingRTB;
        Size result = new();

        if (source != null) {
            result = Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
        }

        return result;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize) {
        RenderTargetBitmap? source = backingRTB;

        if (source != null) {
            Size sourceSize = source.Size;
            Size result = Stretch.CalculateSize(finalSize, sourceSize);
            return result;
        }

        return new Size();
    }

    private static void SourceChanged(AvaloniaPropertyChangedEventArgs e) {
        GifImage? image = e.Sender as GifImage;
        if (image == null) {
            return;
        }

        image.gifInstance?.Dispose();
        image.backingRTB?.Dispose();
        image.backingRTB = null;

        object? value = e.NewValue;
        if (value is string s) {
            value = new Uri(s);
        }

        image.gifInstance = new GifInstance();
        image.gifInstance.SetSource(value);

        image.backingRTB = new RenderTargetBitmap(image.gifInstance.GifPixelSize, new Vector(96, 96));
    }
}