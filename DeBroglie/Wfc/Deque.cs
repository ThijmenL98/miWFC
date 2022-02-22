using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WFC4All.DeBroglie.Wfc
{
    internal class Deque<T>
    {
        private T[] data;
        private int dataLength;

        // Data is in range lo to hi, exclusive of hi
        // hi == lo if the Deque is empty
        // You may have hi < lo if we've wrapped the end of data
        private int lo;
        private int hi;

        public Deque(int capacity = 4)
        {
            data = new T[capacity];
            dataLength = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void push(T t)
        {
            int hi = this.hi;
            int lo = this.lo;
            data[hi] = t;
            hi++;
            if (hi == dataLength) {
                hi = 0;
            }

            this.hi = hi;
            if (hi == lo)
            {
                resizeFromFull();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T pop()
        {
            int lo = this.lo;
            int hi = this.hi;
            if (lo == hi) {
                throw new Exception("Deque is empty");
            }

            if (hi == 0) {
                hi = dataLength;
            }

            hi--;
            this.hi = hi;
            return data[hi];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void shift(T t)
        {
            int lo = this.lo;
            int hi = this.hi;
            if (lo == 0) {
                lo = dataLength;
            }

            lo--;
            data[lo] = t;
            this.lo = lo;
            if (hi == lo)
            {
                resizeFromFull();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T unshift()
        {
            int lo = this.lo;
            int hi = this.hi;
            if (lo == hi) {
                throw new Exception("Deque is empty");
            }

            int oldLo = lo;
            lo++;
            if (lo == dataLength) {
                lo = 0;
            }

            this.lo = lo;
            return data[oldLo];
        }

        public void dropFirst(int n)
        {
            int hi = this.hi;
            int lo = this.lo;
            if (lo <= hi)
            {
                lo += n;
                if(lo >= hi)
                {
                    // Empty
                    this.lo = this.hi = 0;
                }
                else
                {
                    this.lo = lo;
                }
            }
            else
            {
                lo += n;
                if(lo >= dataLength)
                {
                    lo -= dataLength;
                    if(lo >= hi)
                    {
                        // Empty
                        this.lo = this.hi = 0;
                    }
                    else
                    {
                        this.lo = lo;
                    }
                }
            }
        }

        public void dropLast(int n)
        {
            int hi = this.hi;
            int lo = this.lo;
            if (lo <= hi)
            {
                hi -= n;
                if(lo >= hi)
                {
                    // Empty
                    this.lo = this.hi = 0;
                }
                else
                {
                    this.hi = hi;
                }
            }
            else
            {
                hi -= n;
                if(hi < 0)
                {
                    hi += dataLength;
                    if(lo >= hi)
                    {
                        // Empty
                        this.lo = this.hi = 0;
                    }
                    else
                    {
                        this.hi = hi;
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                int c = hi - lo;
                if (c < 0) {
                    c += dataLength;
                }

                return c;
            }
        }

        private void resizeFromFull()
        {
            int dataLength = this.dataLength;
            int newLength = dataLength * 2;
            T[] newData = new T[newLength];

            int i = lo;
            int j = 0;
            int hi = this.hi;

            do
            {
                newData[j] = data[i];

                j++;
                i++;
                if (i == dataLength) {
                    i = 0;
                }
            } while (i != hi);
            data = newData;
            this.dataLength = newLength;
            lo = 0;
            this.hi = j;
        }

        public IEnumerable<T> slice(int start, int end)
        {
            int lo = this.lo;
            int hi = this.hi;
            T[] data = this.data;
            int dataLength = this.dataLength;
            int i = lo + start;
            int e = lo + end;

            if (start < 0) {
                throw new Exception();
            }

            if (hi >= lo)
            {
                if (e > hi) {
                    throw new Exception();
                }
            }
            else
            {
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

            do
            {
                yield return data[i];
                i++;
                if (i == dataLength) {
                    i = 0;
                }
            } while (i != e);
        }

        public IEnumerable<T> reverseSlice(int start, int end)
        {
            int lo = this.lo;
            int hi = this.hi;
            T[] data = this.data;
            int dataLength = this.dataLength;
            int i = lo + start;
            int e = lo + end;

            if (start < 0) {
                throw new Exception();
            }

            if (hi >= lo)
            {
                if (e > hi) {
                    throw new Exception();
                }
            }
            else
            {
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

            do
            {
                e--;
                yield return data[e];
                if (e == 0) {
                    e = dataLength - 1;
                }
            } while (i != e);
        }
    }
}
