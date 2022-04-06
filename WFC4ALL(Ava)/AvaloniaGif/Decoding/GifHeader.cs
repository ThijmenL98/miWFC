// Licensed under the MIT License.
// Copyright (C) 2018 Jumar A. Macato, All Rights Reserved.

namespace WFC4ALL.AvaloniaGif.Decoding
{
    public class GifHeader
    {
        public bool HasGlobalColorTable;
        public int GlobalColorTableSize;
        public ulong GlobalColorTableCacheID;
        public int BackgroundColorIndex;
        public long HeaderSize;
        internal int Iterations = -1;
        public GifRepeatBehavior IterationCount;
        public GifRect Dimensions;
    }
}