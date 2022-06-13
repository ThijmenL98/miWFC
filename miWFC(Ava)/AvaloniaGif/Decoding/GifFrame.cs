using System;

namespace miWFC.AvaloniaGif.Decoding;

public class GifFrame {
    public GifRect Dimensions;
    public TimeSpan FrameDelay;
    public FrameDisposal FrameDisposalMethod;
    public bool HasTransparency, IsInterlaced, IsLocalColorTableUsed;
    public ulong LocalColorTableCacheId;
    public int LzwMinCodeSize, LocalColorTableSize;
    public long LzwStreamPosition;
    public bool ShouldBackup;
    public byte TransparentColorIndex;
}