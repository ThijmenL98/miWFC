using System;
using System.Collections.Generic;
using System.Linq;

namespace miWFC.DeBroglie;

internal interface IHeapNode<TKey> where TKey : IComparable<TKey> {
    int HeapIndex { get; set; }

    TKey Key { get; }
}

/// <summary>
///     Implements a basic min-key heap.
/// </summary>
internal class Heap<T, TKey> where T : IHeapNode<TKey> where TKey : IComparable<TKey> {
    private T[] data;

    public Heap() {
        data = new T[0];
        Count = 0;
    }

    public Heap(int capacity) {
        data = new T[capacity];
        Count = 0;
    }

    public Heap(T[] items) {
        data = new T[items.Length];
        Count = data.Length;
        Array.Copy(items, data, data.Length);
        for (int i = 0; i < Count; i++) {
            data[i].HeapIndex = i;
        }

        Heapify();
    }

    public Heap(IEnumerable<T> items) {
        data = items.ToArray();
        Count = data.Length;
        for (int i = 0; i < Count; i++) {
            data[i].HeapIndex = i;
        }

        Heapify();
    }

    public int Count { get; private set; }

    private static int Parent(int i) {
        return (i - 1) >> 1;
    }

    private static int Left(int i) {
        return (i << 1) + 1;
    }

    private static int Right(int i) {
        return (i << 1) + 2;
    }

    public T Peek() {
        if (Count == 0) {
            throw new Exception("Heap is empty");
        }

        return data[0];
    }

    public void Heapify() {
        for (int i = Parent(Count); i >= 0; i--) {
            Heapify(i);
        }
    }

    private void Heapify(int i) {
        TKey ip = data[i].Key;
        int smallest = i;
        TKey smallestP = ip;
        int l = Left(i);
        if (l < Count) {
            TKey lp = data[l].Key;
            if (lp.CompareTo(smallestP) < 0) {
                smallest = l;
                smallestP = lp;
            }
        }

        int r = Right(i);
        if (r < Count) {
            TKey rp = data[r].Key;
            if (rp.CompareTo(smallestP) < 0) {
                smallest = r;
                smallestP = rp;
            }
        }

        if (i == smallest) {
            data[i].HeapIndex = i;
        } else {
            (data[i], data[smallest]) = (data[smallest], data[i]);
            data[i].HeapIndex = i;
            Heapify(smallest);
        }
    }

    public void DecreasedKey(T item) {
        int i = item.HeapIndex;
        TKey priority = item.Key;
        while (true) {
            if (i == 0) {
                item.HeapIndex = i;
                return;
            }

            int p = Parent(i);
            T parent = data[p];
            TKey parentP = parent.Key;

            if (parentP.CompareTo(priority) > 0) {
                (data[p], data[i]) = (data[i], data[p]);
                parent.HeapIndex = i;
                i = p;
                continue;
            }

            item.HeapIndex = i;
            return;
        }
    }

    public void IncreasedKey(T item) {
        Heapify(item.HeapIndex);
    }

    public void ChangedKey(T item) {
        DecreasedKey(item);
        IncreasedKey(item);
    }

    public void Insert(T item) {
        if (data.Length == Count) {
            T[] data2 = new T[Count * 2];
            Array.Copy(data, data2, Count);
            data = data2;
        }

        data[Count] = item;
        item.HeapIndex = Count;
        Count++;
        DecreasedKey(item);
    }

    public void Delete(T item) {
        int i = item.HeapIndex;
        if (i == Count - 1) {
            Count--;
        } else {
            item = data[i] = data[Count - 1];
            item.HeapIndex = i;
            Count--;
            IncreasedKey(item);
            DecreasedKey(item);
        }
    }

    public void Clear() {
        Count = 0;
    }
}