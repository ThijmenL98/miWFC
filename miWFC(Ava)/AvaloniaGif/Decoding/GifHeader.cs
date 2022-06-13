// Licensed under the MIT License.
// Copyright (C) 2018 Jumar A. Macato, All Rights Reserved.

namespace miWFC.AvaloniaGif.Decoding;

public class GifHeader {
    public int BackgroundColorIndex;
    public GifRect Dimensions;
    public ulong GlobalColorTableCacheId;
    public int GlobalColorTableSize;
    public bool HasGlobalColorTable;
    public long HeaderSize;
    public GifRepeatBehavior IterationCount;
    internal int Iterations = -1;
}