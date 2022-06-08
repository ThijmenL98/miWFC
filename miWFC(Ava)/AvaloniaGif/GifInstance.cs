using System;
using System.IO;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using miWFC.AvaloniaGif.Decoding;

namespace miWFC.AvaloniaGif; 

public class GifInstance : IDisposable {
    private GifBackgroundWorker _bgWorker;
    private GifDecoder _gifDecoder;
    private bool _hasNewFrame;
    private bool _isDisposed;

    private bool _streamCanDispose;
    private WriteableBitmap _targetBitmap;
    public Stream Stream { get; private set; }
    public IterationCount IterationCount { get; private set; }
    public bool AutoStart { get; private set; } = true;
    public Progress<int> Progress { get; private set; }

    public PixelSize GifPixelSize { get; private set; }

    public void Dispose() {
        _isDisposed = true;
        _bgWorker?.SendCommand(BgWorkerCommand.DISPOSE);
        _targetBitmap?.Dispose();
    }

    public void SetSource(object newValue) {
        Uri? sourceUri = newValue as Uri;
        Stream? sourceStr = newValue as Stream;

        Stream stream = null;

        if (sourceUri != null) {
            _streamCanDispose = true;
            Progress = new Progress<int>();

            if (sourceUri.OriginalString.Trim().StartsWith("resm")) {
                IAssetLoader? assetLocator = AvaloniaLocator.Current.GetService<IAssetLoader>();
                stream = assetLocator.Open(sourceUri);
            }
        } else if (sourceStr != null) {
            stream = sourceStr;
        } else {
            throw new InvalidDataException("Missing valid URI or Stream.");
        }

        Stream = stream;
        _gifDecoder = new GifDecoder(Stream);
        _bgWorker = new GifBackgroundWorker(_gifDecoder);
        PixelSize pixSize = new(_gifDecoder.Header.Dimensions.Width, _gifDecoder.Header.Dimensions.Height);

        _targetBitmap = new WriteableBitmap(pixSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        _bgWorker.CurrentFrameChanged += FrameChanged;
        GifPixelSize = pixSize;
        Run();
    }

    public WriteableBitmap GetBitmap() {
        WriteableBitmap ret = null;

        if (_hasNewFrame) {
            _hasNewFrame = false;
            ret = _targetBitmap;
        }

        return ret;
    }

    private void FrameChanged() {
        if (_isDisposed) {
            return;
        }

        _hasNewFrame = true;

        using (ILockedFramebuffer? lockedBitmap = _targetBitmap?.Lock()) {
            _gifDecoder?.WriteBackBufToFb(lockedBitmap.Address);
        }
    }

    private void Run() {
        if (!Stream.CanSeek) {
            throw new ArgumentException("The stream is not seekable");
        }

        _bgWorker?.SendCommand(BgWorkerCommand.PLAY);
    }

    public void IterationCountChanged(AvaloniaPropertyChangedEventArgs e) {
        IterationCount newVal = (IterationCount) e.NewValue;
        IterationCount = newVal;
    }

    public void AutoStartChanged(AvaloniaPropertyChangedEventArgs e) {
        bool newVal = (bool) e.NewValue;
        AutoStart = newVal;
    }
}