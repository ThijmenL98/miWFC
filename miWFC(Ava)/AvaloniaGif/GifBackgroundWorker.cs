using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using miWFC.AvaloniaGif.Decoding;

namespace miWFC.AvaloniaGif; 

internal sealed class GifBackgroundWorker {
    private static readonly Stopwatch timer = Stopwatch.StartNew();
    private readonly Queue<BgWorkerCommand> _cmdQueue;
    private readonly List<ulong> _colorTableIDList;
    private readonly object _lockObj;

    private Task _bgThread;
    private int _currentIndex;
    private readonly GifDecoder _gifDecoder;
    private int _iterationCount;

    private GifRepeatBehavior _repeatBehavior;
    private volatile bool _shouldStop;
    private BgWorkerState _state;

    public Action CurrentFrameChanged;

    public GifBackgroundWorker(GifDecoder gifDecode) {
        _gifDecoder = gifDecode;
        _lockObj = new object();
        _repeatBehavior = new GifRepeatBehavior {LoopForever = true};
        _cmdQueue = new Queue<BgWorkerCommand>();

        // Save the color table cache ID's to refresh them on cache while
        // the image is either stopped/paused.
        _colorTableIDList = _gifDecoder.Frames
            .Where(p => p.IsLocalColorTableUsed)
            .Select(p => p.LocalColorTableCacheId)
            .ToList();

        if (_gifDecoder.Header.HasGlobalColorTable) {
            _colorTableIDList.Add(_gifDecoder.Header.GlobalColorTableCacheId);
        }

        ResetPlayVars();

        _bgThread = Task.Factory.StartNew(MainLoop, CancellationToken.None, TaskCreationOptions.LongRunning,
            TaskScheduler.Current);
    }

    public GifRepeatBehavior IterationCount {
        get => _repeatBehavior;
        set {
            lock (_lockObj) {
                InternalSeek(0, true);
                ResetPlayVars();
                _state = BgWorkerState.PAUSED;
                _repeatBehavior = value;
            }
        }
    }

    public int CurrentFrameIndex {
        get => _currentIndex;
        set {
            if (value != _currentIndex) {
                lock (_lockObj) {
                    InternalSeek(value, true);
                }
            }
        }
    }

    private void ResetPlayVars() {
        _iterationCount = 0;
        CurrentFrameIndex = -1;
    }

    private void RefreshColorTableCache() {
        foreach (ulong cacheId in _colorTableIDList) {
            GifDecoder.GlobalColorTableCache.TryGetValue(cacheId, out GifColor[] _);
        }
    }

    private void InternalSeek(int value, bool isManual) {
        int lowerBound = 0;

        // Skip already rendered frames if the seek position is above the previous frame index.
        if (isManual & (value > _currentIndex)) {
            // Only render the new seeked frame if the delta
            // seek position is just 1 frame.
            if (value - _currentIndex == 1) {
                _gifDecoder.RenderFrame(value);
                SetIndexVal(value, isManual);
                return;
            }

            lowerBound = _currentIndex;
        }

        for (int fI = lowerBound; fI <= value; fI++) {
            GifFrame targetFrame = _gifDecoder.Frames[fI];

            // Ignore frames with restore disposal method except the current one.
            if ((fI != value) & (targetFrame.FrameDisposalMethod == FrameDisposal.RESTORE)) {
                continue;
            }

            _gifDecoder.RenderFrame(fI);
        }

        SetIndexVal(value, isManual);
    }

    private void SetIndexVal(int value, bool isManual) {
        _currentIndex = value;

        if (isManual) {
            if (_state == BgWorkerState.COMPLETE) {
                _state = BgWorkerState.PAUSED;
                _iterationCount = 0;
            }

            CurrentFrameChanged?.Invoke();
        }
    }

    public void SendCommand(BgWorkerCommand cmd) {
        lock (_lockObj) {
            _cmdQueue.Enqueue(cmd);
        }
    }

    public BgWorkerState GetState() {
        lock (_lockObj) {
            BgWorkerState ret = _state;
            return ret;
        }
    }

    private void MainLoop() {
        while (true) {
            if (_shouldStop) {
                DoDispose();
                break;
            }

            CheckCommands();
            DoStates();
        }
    }

    private void DoStates() {
        switch (_state) {
            case BgWorkerState.NULL:
                Thread.Sleep(40);
                break;
            case BgWorkerState.PAUSED:
                RefreshColorTableCache();
                Thread.Sleep(60);
                break;
            case BgWorkerState.START:
                _state = BgWorkerState.RUNNING;
                break;
            case BgWorkerState.RUNNING:
                WaitAndRenderNext();
                break;
            case BgWorkerState.COMPLETE:
                RefreshColorTableCache();
                Thread.Sleep(60);
                break;
        }
    }

    private void CheckCommands() {
        BgWorkerCommand cmd;

        lock (_lockObj) {
            if (_cmdQueue.Count <= 0) {
                return;
            }

            cmd = _cmdQueue.Dequeue();
        }

        switch (cmd) {
            case BgWorkerCommand.DISPOSE:
                DoDispose();
                break;
            case BgWorkerCommand.PLAY:
                switch (_state) {
                    case BgWorkerState.NULL:
                        _state = BgWorkerState.START;
                        break;
                    case BgWorkerState.PAUSED:
                        _state = BgWorkerState.RUNNING;
                        break;
                    case BgWorkerState.COMPLETE:
                        ResetPlayVars();
                        _state = BgWorkerState.START;
                        break;
                }

                break;
            case BgWorkerCommand.PAUSE:
                switch (_state) {
                    case BgWorkerState.RUNNING:
                        _state = BgWorkerState.PAUSED;
                        break;
                }

                break;
        }
    }

    private void DoDispose() {
        _state = BgWorkerState.DISPOSE;
        _shouldStop = true;
        _gifDecoder.Dispose();
    }

    private void ShowFirstFrame() {
        if (_shouldStop) {
            return;
        }

        _gifDecoder.RenderFrame(0);
    }

    private void WaitAndRenderNext() {
        if (!IterationCount.LoopForever & (_iterationCount > IterationCount.Count)) {
            _state = BgWorkerState.COMPLETE;
            return;
        }

        _currentIndex = (_currentIndex + 1) % _gifDecoder.Frames.Count;

        CurrentFrameChanged?.Invoke();

        TimeSpan targetDelay = _gifDecoder.Frames[_currentIndex].FrameDelay;

        TimeSpan t1 = timer.Elapsed;

        _gifDecoder.RenderFrame(_currentIndex);

        TimeSpan t2 = timer.Elapsed;
        TimeSpan delta = t2 - t1;

        if (delta > targetDelay) {
            return;
        }

        Thread.Sleep(targetDelay - delta);

        if (!IterationCount.LoopForever & (_currentIndex == 0)) {
            _iterationCount++;
        }
    }

    ~GifBackgroundWorker() {
        DoDispose();
    }
}