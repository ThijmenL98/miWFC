// This source file's Lempel-Ziv-Welch algorithm is derived from Chromium's Android GifPlayer
// as seen here (https://github.com/chromium/chromium/blob/master/third_party/gif_player/src/jp/tomorrowkey/android/gifplayer)
// Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
// Copyright (C) 2015 The Gifplayer Authors. All Rights Reserved.

// The rest of the source file is licensed under MIT License.
// Copyright (C) 2018 Jumar A. Macato, All Rights Reserved.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WFC4ALL.AvaloniaGif.Caching;
using static WFC4ALL.AvaloniaGif.Extensions.StreamExtensions;

namespace WFC4ALL.AvaloniaGif.Decoding; 

public sealed class GifDecoder : IDisposable {
    private static readonly ReadOnlyMemory<byte> g87AMagic
        = Encoding.ASCII.GetBytes("GIF87a").AsMemory();

    private static readonly ReadOnlyMemory<byte> g89AMagic
        = Encoding.ASCII.GetBytes("GIF89a").AsMemory();

    private static readonly ReadOnlyMemory<byte> netscapeMagic
        = Encoding.ASCII.GetBytes("NETSCAPE2.0").AsMemory();

    private static readonly TimeSpan frameDelayThreshold = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan frameDelayDefault = TimeSpan.FromMilliseconds(100);
    private static readonly GifColor transparentColor = new(0, 0, 0, 0);
    private static readonly XxHash64 hasher = new();
    private static readonly int maxTempBuf = 768;
    private static readonly int maxStackSize = 4096;
    private static readonly int maxBits = 4097;

    internal static ICache<ulong, GifColor[]> GlobalColorTableCache
        = Caches.KeyValue<ulong, GifColor[]>()
            .WithBackgroundPurge(TimeSpan.FromSeconds(30))
            .WithExpiration(TimeSpan.FromSeconds(10))
            .WithSlidingExpiration()
            .Build();

    private static readonly (int Start, int Step)[] pass = {
        (0, 8),
        (4, 8),
        (2, 4),
        (1, 2)
    };

    private static readonly Action<int, Action<int>> interlaceRows = (height, rowAction) => {
        for (int i = 0; i < 4; i++) {
            (int Start, int Step) curPass = pass[i];
            int y = curPass.Start;
            while (y < height) {
                rowAction(y);
                y += curPass.Step;
            }
        }
    };

    private static readonly Action<int, Action<int>> normalRows = (height, rowAction) => {
        for (int i = 0; i < height; i++) {
            rowAction(i);
        }
    };

    private static MemoryStream memoryStream = new();
    private readonly int _backBufferBytes;

    private readonly Stream _fileStream;
    private readonly bool _hasFrameBackups;
    private readonly object _lockObj;
    private GifColor[] _bitmapBackBuffer;

    private int _gctSize, _bgIndex, _prevFrame;
    private bool _gctUsed;
    private GifRect _gifDimensions;

    private ulong _globalColorTable;
    internal volatile bool HasNewFrame;
    private byte[] _indexBuf;
    private byte[] _pixelStack;

    private short[] _prefixBuf;
    private byte[] _prevFrameIndexBuf;
    private byte[] _suffixBuf;
    public List<GifFrame> Frames = new();

    public GifDecoder(Stream fileStream) {
        _fileStream = fileStream;
        _lockObj = new object();

        ProcessHeaderData();
        ProcessFrameData();

        if (Header.Iterations == -1) {
            Header.IterationCount = new GifRepeatBehavior {Count = 1};
        } else if (Header.Iterations == 0) {
            Header.IterationCount = new GifRepeatBehavior {LoopForever = true};
        } else if (Header.Iterations > 0) {
            Header.IterationCount = new GifRepeatBehavior {Count = Header.Iterations};
        }

        int pixelCount = _gifDimensions.TotalPixels;

        _hasFrameBackups = Frames
            .Any(f => f.FrameDisposalMethod == FrameDisposal.RESTORE);

        _bitmapBackBuffer = new GifColor[pixelCount];
        _indexBuf = new byte[pixelCount];

        if (_hasFrameBackups) {
            _prevFrameIndexBuf = new byte[pixelCount];
        }

        _prefixBuf = new short[maxStackSize];
        _suffixBuf = new byte[maxStackSize];
        _pixelStack = new byte[maxStackSize + 1];

        _backBufferBytes = pixelCount * Marshal.SizeOf(typeof(GifColor));
    }

    public GifHeader Header { get; private set; }

    public void Dispose() {
        lock (_lockObj) {
            Frames.Clear();

            _bitmapBackBuffer = null;
            _prefixBuf = null;
            _suffixBuf = null;
            _pixelStack = null;
            _indexBuf = null;
            _prevFrameIndexBuf = null;

            _fileStream?.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int PixCoord(int x, int y) {
        return x + y * _gifDimensions.Width;
    }


    internal void ClearImage() {
        ClearArea(_gifDimensions);
    }

    public void RenderFrame(int fIndex) {
        lock (_lockObj) {
            if ((fIndex < 0) | (fIndex >= Frames.Count)) {
                return;
            }

            if (fIndex == 0) {
                ClearImage();
            }

            byte[] tmpB = ArrayPool<byte>.Shared.Rent(maxTempBuf);

            GifFrame curFrame = Frames[fIndex];

            DisposePreviousFrame();

            DecompressFrameToIndexBuffer(curFrame, _indexBuf, tmpB);

            if (_hasFrameBackups & curFrame.ShouldBackup) {
                Buffer.BlockCopy(_indexBuf, 0, _prevFrameIndexBuf, 0, curFrame.Dimensions.TotalPixels);
            }

            DrawFrame(curFrame, _indexBuf);

            _prevFrame = fIndex;
            HasNewFrame = true;

            ArrayPool<byte>.Shared.Return(tmpB);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawFrame(GifFrame curFrame, Memory<byte> frameIndexSpan) {
        ulong activeColorTableHash
            = curFrame.IsLocalColorTableUsed ? curFrame.LocalColorTableCacheId : _globalColorTable;
        GifColor[]? activeColorTable = GlobalColorTableCache.Get(activeColorTableHash);

        int cX = curFrame.Dimensions.X;
        int cY = curFrame.Dimensions.Y;
        int cH = curFrame.Dimensions.Height;
        int cW = curFrame.Dimensions.Width;
        byte tC = curFrame.TransparentColorIndex;
        bool hT = curFrame.HasTransparency;

        if (curFrame.IsInterlaced) {
            interlaceRows(cH, drawRow);
        } else {
            normalRows(cH, drawRow);
        }

        //for (var row = 0; row < cH; row++)
        void drawRow(int row) {
            // Get the starting point of the current row on frame's index stream.
            int indexOffset = row * cW;

            // Get the target backbuffer offset from the frames coords.
            int targetOffset = PixCoord(cX, row + cY);
            int len = _bitmapBackBuffer.Length;

            for (int i = 0; i < cW; i++) {
                byte indexColor = frameIndexSpan.Span[indexOffset + i];

                if (activeColorTable == null || targetOffset >= len || indexColor >= activeColorTable.Length) {
                    return;
                }

                if (!(hT & (indexColor == tC))) {
                    _bitmapBackBuffer[targetOffset] = activeColorTable[indexColor];
                }

                targetOffset++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DisposePreviousFrame() {
        GifFrame prevFrame = Frames[_prevFrame];

        switch (prevFrame.FrameDisposalMethod) {
            case FrameDisposal.BACKGROUND:
                ClearArea(prevFrame.Dimensions);
                break;
            case FrameDisposal.RESTORE:
                if (_hasFrameBackups) {
                    DrawFrame(prevFrame, _prevFrameIndexBuf);
                }

                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearArea(GifRect area) {
        for (int y = 0; y < area.Height; y++) {
            int targetOffset = PixCoord(area.X, y + area.Y);
            for (int x = 0; x < area.Width; x++) {
                _bitmapBackBuffer[targetOffset + x] = transparentColor;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DecompressFrameToIndexBuffer(GifFrame curFrame, Span<byte> indexSpan, byte[] tempBuf) {
        Stream str = _fileStream;

        str.Position = curFrame.LzwStreamPosition;
        int totalPixels = curFrame.Dimensions.TotalPixels;

        // Initialize GIF data stream decoder.
        int dataSize = curFrame.LzwMinCodeSize;
        int clear = 1 << dataSize;
        int endOfInformation = clear + 1;
        int available = clear + 2;
        int oldCode = -1;
        int codeSize = dataSize + 1;
        int codeMask = (1 << codeSize) - 1;

        for (int code = 0; code < clear; code++) {
            _prefixBuf[code] = 0;
            _suffixBuf[code] = (byte) code;
        }

        // Decode GIF pixel stream.
        int bits, first, top, pixelIndex, blockPos;
        int datum = bits = first = top = pixelIndex = blockPos = 0;

        while (pixelIndex < totalPixels) {
            int blockSize = str.ReadBlock(tempBuf);

            if (blockSize == 0) {
                break;
            }

            blockPos = 0;

            while (blockPos < blockSize) {
                datum += tempBuf[blockPos] << bits;
                blockPos++;

                bits += 8;

                while (bits >= codeSize) {
                    // Get the next code.
                    int code = datum & codeMask;
                    datum >>= codeSize;
                    bits -= codeSize;

                    // Interpret the code
                    if (code == clear) {
                        // Reset decoder.
                        codeSize = dataSize + 1;
                        codeMask = (1 << codeSize) - 1;
                        available = clear + 2;
                        oldCode = -1;
                        continue;
                    }

                    // Check for explicit end-of-stream
                    if (code == endOfInformation) {
                        return;
                    }

                    if (oldCode == -1) {
                        indexSpan[pixelIndex++] = _suffixBuf[code];
                        oldCode = code;
                        first = code;
                        continue;
                    }

                    int inCode = code;
                    if (code >= available) {
                        _pixelStack[top++] = (byte) first;
                        code = oldCode;

                        if (top == maxBits) {
                            throwException();
                        }
                    }

                    while (code >= clear) {
                        if (code >= maxBits || code == _prefixBuf[code]) {
                            throwException();
                        }

                        _pixelStack[top++] = _suffixBuf[code];
                        code = _prefixBuf[code];

                        if (top == maxBits) {
                            throwException();
                        }
                    }

                    first = _suffixBuf[code];
                    _pixelStack[top++] = (byte) first;

                    // Add new code to the dictionary
                    if (available < maxStackSize) {
                        _prefixBuf[available] = (short) oldCode;
                        _suffixBuf[available] = (byte) first;
                        available++;

                        if ((available & codeMask) == 0 && available < maxStackSize) {
                            codeSize++;
                            codeMask += available;
                        }
                    }

                    oldCode = inCode;

                    // Drain the pixel stack.
                    do {
                        indexSpan[pixelIndex++] = _pixelStack[--top];
                    } while (top > 0);
                }
            }
        }

        while (pixelIndex < totalPixels) {
            indexSpan[pixelIndex++] = 0; // clear missing pixels
        }

        void throwException() {
            throw new LzwDecompressionException();
        }
    }

    /// <summary>
    ///     Directly copies the <see cref="GifColor" /> struct array to a bitmap IntPtr.
    /// </summary>
    public void WriteBackBufToFb(IntPtr targetPointer) {
        if (HasNewFrame & (_bitmapBackBuffer != null)) {
            lock (_lockObj) {
                unsafe {
                    fixed (void* src = &_bitmapBackBuffer[0]) {
                        Buffer.MemoryCopy(src, targetPointer.ToPointer(), (uint) _backBufferBytes,
                            (uint) _backBufferBytes);
                    }

                    HasNewFrame = false;
                }
            }
        }
    }

    /// <summary>
    ///     Processes GIF Header.
    /// </summary>
    private void ProcessHeaderData() {
        Stream str = _fileStream;
        byte[] tmpB = ArrayPool<byte>.Shared.Rent(maxTempBuf);
        Span<byte> tempBuf = tmpB.AsSpan();


        str.Read(tmpB, 0, 6);

        if (!tempBuf.Slice(0, 3).SequenceEqual(g87AMagic.Slice(0, 3).Span)) {
            throw new InvalidGifStreamException("Not a GIF stream.");
        }

        if (!(tempBuf.Slice(0, 6).SequenceEqual(g87AMagic.Span) | tempBuf.Slice(0, 6).SequenceEqual(g89AMagic.Span))) {
            throw new InvalidGifStreamException("Unsupported GIF Version: " +
                Encoding.ASCII.GetString(tempBuf.Slice(0, 6).ToArray()));
        }

        ProcessScreenDescriptor(tmpB);

        if (_gctUsed) {
            _globalColorTable = ProcessColorTable(ref str, tmpB, _gctSize);
        }


        Header = new GifHeader {
            Dimensions = _gifDimensions,
            HasGlobalColorTable = _gctUsed,
            GlobalColorTableCacheId = _globalColorTable,
            GlobalColorTableSize = _gctSize,
            BackgroundColorIndex = _bgIndex,
            HeaderSize = _fileStream.Position
        };

        ArrayPool<byte>.Shared.Return(tmpB);
    }

    /// <summary>
    ///     Parses colors from file stream to target color table.
    /// </summary>
    private static ulong ProcessColorTable(ref Stream stream, byte[] rawBufSpan, int nColors) {
        int nBytes = 3 * nColors;
        GifColor[] targ = new GifColor[nColors];

        int n = stream.Read(rawBufSpan, 0, nBytes);

        if (n < nBytes) {
            throw new InvalidOperationException("Wrong color table bytes.");
        }

        hasher.ComputeHash(rawBufSpan, 0, nBytes);

        ulong tableHash = hasher.HashUInt64;

        int i = 0, j = 0;

        while (i < nColors) {
            byte r = rawBufSpan[j++];
            byte g = rawBufSpan[j++];
            byte b = rawBufSpan[j++];
            targ[i++] = new GifColor(r, g, b);
        }

        GlobalColorTableCache.Set(tableHash, targ);

        return tableHash;
    }

    /// <summary>
    ///     Parses screen and other GIF descriptors.
    /// </summary>
    private void ProcessScreenDescriptor(byte[] tempBuf) {
        Stream str = _fileStream;

        ushort width = str.ReadUShortS(tempBuf);
        ushort height = str.ReadUShortS(tempBuf);

        byte packed = str.ReadByteS(tempBuf);

        _gctUsed = (packed & 0x80) != 0;
        _gctSize = 2 << (packed & 7);
        _bgIndex = str.ReadByteS(tempBuf);

        _gifDimensions = new GifRect(0, 0, width, height);
        str.Skip(1);
    }

    /// <summary>
    ///     Parses all frame data.
    /// </summary>
    private void ProcessFrameData() {
        Stream str = _fileStream;
        str.Position = Header.HeaderSize;

        byte[] tempBuf = ArrayPool<byte>.Shared.Rent(maxTempBuf);

        bool terminate = false;
        int curFrame = 0;

        Frames.Add(new GifFrame());

        do {
            BlockTypes blockType = (BlockTypes) str.ReadByteS(tempBuf);

            switch (blockType) {
                case BlockTypes.EMPTY:
                    break;

                case BlockTypes.EXTENSION:
                    ProcessExtensions(ref curFrame, tempBuf);
                    break;

                case BlockTypes.IMAGE_DESCRIPTOR:
                    ProcessImageDescriptor(ref curFrame, tempBuf);
                    str.SkipBlocks(tempBuf);
                    break;

                case BlockTypes.TRAILER:
                    Frames.RemoveAt(Frames.Count - 1);
                    terminate = true;
                    break;

                default:
                    str.SkipBlocks(tempBuf);
                    break;
            }

            // Break the loop when the stream is not valid anymore.
            if ((str.Position >= str.Length) & (terminate == false)) {
                throw new InvalidProgramException("Reach the end of the filestream without trailer block.");
            }
        } while (!terminate);

        ArrayPool<byte>.Shared.Return(tempBuf);
    }

    /// <summary>
    ///     Parses GIF Image Descriptor Block.
    /// </summary>
    private void ProcessImageDescriptor(ref int curFrame, byte[] tempBuf) {
        Stream str = _fileStream;
        GifFrame currentFrame = Frames[curFrame];

        // Parse frame dimensions.
        ushort frameX = str.ReadUShortS(tempBuf);
        ushort frameY = str.ReadUShortS(tempBuf);
        ushort frameW = str.ReadUShortS(tempBuf);
        ushort frameH = str.ReadUShortS(tempBuf);

        frameW = (ushort) Math.Min(frameW, _gifDimensions.Width - frameX);
        frameH = (ushort) Math.Min(frameH, _gifDimensions.Height - frameY);

        currentFrame.Dimensions = new GifRect(frameX, frameY, frameW, frameH);

        // Unpack interlace and lct info.
        byte packed = str.ReadByteS(tempBuf);
        currentFrame.IsInterlaced = (packed & 0x40) != 0;
        currentFrame.IsLocalColorTableUsed = (packed & 0x80) != 0;
        currentFrame.LocalColorTableSize = (int) Math.Pow(2, (packed & 0x07) + 1);

        if (currentFrame.IsLocalColorTableUsed) {
            currentFrame.LocalColorTableCacheId = ProcessColorTable(ref str, tempBuf, currentFrame.LocalColorTableSize);
        }

        currentFrame.LzwMinCodeSize = str.ReadByteS(tempBuf);
        currentFrame.LzwStreamPosition = str.Position;

        curFrame += 1;
        Frames.Add(new GifFrame());
    }

    /// <summary>
    ///     Parses GIF Extension Blocks.
    /// </summary>
    private void ProcessExtensions(ref int curFrame, byte[] tempBuf) {
        Stream str = _fileStream;

        ExtensionType extType = (ExtensionType) str.ReadByteS(tempBuf);

        switch (extType) {
            case ExtensionType.GRAPHICS_CONTROL:

                str.ReadBlock(tempBuf);
                GifFrame currentFrame = Frames[curFrame];
                byte packed = tempBuf[0];

                currentFrame.FrameDisposalMethod = (FrameDisposal) ((packed & 0x1c) >> 2);

                if (currentFrame.FrameDisposalMethod != FrameDisposal.RESTORE) {
                    currentFrame.ShouldBackup = true;
                }

                currentFrame.HasTransparency = (packed & 1) != 0;

                currentFrame.FrameDelay =
                    TimeSpan.FromMilliseconds(SpanToShort(tempBuf.AsSpan(1)) * 10);

                if (currentFrame.FrameDelay <= frameDelayThreshold) {
                    currentFrame.FrameDelay = frameDelayDefault;
                }

                currentFrame.TransparentColorIndex = tempBuf[3];
                break;

            case ExtensionType.APPLICATION:
                int blockLen = str.ReadBlock(tempBuf);
                Span<byte> blockSpan = tempBuf.AsSpan(0, blockLen);
                Span<byte> blockHeader = tempBuf.AsSpan(0, netscapeMagic.Length);

                if (blockHeader.SequenceEqual(netscapeMagic.Span)) {
                    int count = 1;

                    while (count > 0) {
                        count = str.ReadBlock(tempBuf);
                    }

                    ushort iterationCount = SpanToShort(tempBuf.AsSpan(1));

                    Header.Iterations = iterationCount;
                } else {
                    str.SkipBlocks(tempBuf);
                }

                break;

            default:
                str.SkipBlocks(tempBuf);
                break;
        }
    }
}