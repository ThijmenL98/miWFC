using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace miWFC.Utils;

public class FixedSizeQueue<T> {
    private readonly object lockObject = new();
    private readonly ConcurrentQueue<T> q = new();

    public FixedSizeQueue(int length) {
        Limit = length;
    }

    private int Limit { get; }

    public void Enqueue(T obj) {
        lock (lockObject) {
            q.Enqueue(obj);
            while (q.Count > Limit && q.TryDequeue(out T? _)) { }
        }
    }

    public override string ToString() {
        lock (lockObject) {
            return new List<T>(q.ToArray()).Aggregate("", (current, entry) => current + entry);
        }
    }

    public List<T> toList() {
        lock (lockObject) {
            List<T> listQueue = new(q.ToArray());
            listQueue.Reverse();
            return listQueue;
        }
    }
}