using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WFC4ALL.DeBroglie.Wfc; 

internal class Deque<T> : IEnumerable<T> {
    private T[] _data;
    private int _dataLength;
    private int _hi;

    // Data is in range lo to hi, exclusive of hi
    // hi == lo if the Deque is empty
    // You may have hi < lo if we've wrapped the end of data
    private int _lo;

    public Deque(int capacity = 4) {
        _data = new T[capacity];
        _dataLength = capacity;
    }

    public int Count {
        get {
            int c = _hi - _lo;
            if (c < 0) {
                c += _dataLength;
            }

            return c;
        }
    }

    public IEnumerator<T> GetEnumerator() {
        int lo = _lo;
        int hi = _hi;
        T[] data = _data;
        int dataLength = _dataLength;
        int i = lo;
        int e = hi;

        if (hi >= lo) {
            if (e > hi) {
                throw new Exception();
            }
        } else {
            if (e > hi + dataLength) {
                throw new Exception();
            }
        }

        if (lo == hi) {
            yield break;
        }

        if (i >= dataLength) {
            i -= dataLength;
        }

        if (e >= dataLength) {
            e -= dataLength;
        }

        do {
            yield return data[i];
            i++;
            if (i == dataLength) {
                i = 0;
            }
        } while (i != e);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T t) {
        int hi = _hi;
        int lo = _lo;
        _data[hi] = t;
        hi++;
        if (hi == _dataLength) {
            hi = 0;
        }

        _hi = hi;
        if (hi == lo) {
            ResizeFromFull();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop() {
        int lo = _lo;
        int hi = _hi;
        if (lo == hi) {
            throw new Exception("Deque is empty");
        }

        if (hi == 0) {
            hi = _dataLength;
        }

        hi--;
        _hi = hi;
        return _data[hi];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Shift(T t) {
        int lo = _lo;
        int hi = _hi;
        if (lo == 0) {
            lo = _dataLength;
        }

        lo--;
        _data[lo] = t;
        _lo = lo;
        if (hi == lo) {
            ResizeFromFull();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unshift() {
        int lo = _lo;
        int hi = _hi;
        if (lo == hi) {
            throw new Exception("Deque is empty");
        }

        int oldLo = lo;
        lo++;
        if (lo == _dataLength) {
            lo = 0;
        }

        _lo = lo;
        return _data[oldLo];
    }

    public void DropFirst(int n) {
        int hi = _hi;
        int lo = _lo;
        if (lo <= hi) {
            lo += n;
            if (lo >= hi) {
                // Empty
                _lo = _hi = 0;
            } else {
                _lo = lo;
            }
        } else {
            lo += n;
            if (lo >= _dataLength) {
                lo -= _dataLength;
                if (lo >= hi) {
                    // Empty
                    _lo = _hi = 0;
                } else {
                    _lo = lo;
                }
            }
        }
    }

    public void DropLast(int n) {
        int hi = _hi;
        int lo = _lo;
        if (lo <= hi) {
            hi -= n;
            if (lo >= hi) {
                // Empty
                _lo = _hi = 0;
            } else {
                _hi = hi;
            }
        } else {
            hi -= n;
            if (hi < 0) {
                hi += _dataLength;
                if (lo >= hi) {
                    // Empty
                    _lo = _hi = 0;
                } else {
                    _hi = hi;
                }
            }
        }
    }

    private void ResizeFromFull() {
        int dataLength = _dataLength;
        int newLength = dataLength * 2;
        T[] newData = new T[newLength];

        int i = _lo;
        int j = 0;
        int hi = _hi;

        do {
            newData[j] = _data[i];

            j++;
            i++;
            if (i == dataLength) {
                i = 0;
            }
        } while (i != hi);

        _data = newData;
        _dataLength = newLength;
        _lo = 0;
        _hi = j;
    }

    public IEnumerable<T> Slice(int start, int end) {
        int lo = _lo;
        int hi = _hi;
        T[] data = _data;
        int dataLength = _dataLength;
        int i = lo + start;
        int e = lo + end;

        if (start < 0) {
            throw new Exception();
        }

        if (hi >= lo) {
            if (e > hi) {
                throw new Exception();
            }
        } else {
            if (e > hi + dataLength) {
                throw new Exception();
            }
        }

        if (start >= end) {
            yield break;
        }

        if (i >= dataLength) {
            i -= dataLength;
        }

        if (e >= dataLength) {
            e -= dataLength;
        }

        do {
            yield return data[i];
            i++;
            if (i == dataLength) {
                i = 0;
            }
        } while (i != e);
    }

    public IEnumerable<T> ReverseSlice(int start, int end) {
        int lo = _lo;
        int hi = _hi;
        T[] data = _data;
        int dataLength = _dataLength;
        int i = lo + start;
        int e = lo + end;

        if (start < 0) {
            throw new Exception();
        }

        if (hi >= lo) {
            if (e > hi) {
                throw new Exception();
            }
        } else {
            if (e > hi + dataLength) {
                throw new Exception();
            }
        }

        if (start >= end) {
            yield break;
        }

        if (i >= dataLength) {
            i -= dataLength;
        }

        if (e >= dataLength) {
            e -= dataLength;
        }

        do {
            e--;
            yield return data[e];
            if (e == 0) {
                e = dataLength - 1;
            }
        } while (i != e);
    }
}