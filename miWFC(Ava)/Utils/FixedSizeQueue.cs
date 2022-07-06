using System.Collections.Concurrent;
using System.Collections.Generic;

namespace miWFC.Utils;

/// <summary>
/// Custom Queue that automatically pops if the size is exceeded
/// </summary>
/// 
/// <typeparam name="T">Object Type of the Queue</typeparam>
public class FixedSizeQueue<T> {
    private readonly object lockObject = new();
    private readonly ConcurrentQueue<T> q = new();

    /*
     * Initializing Functions & Constructor
     */

    public FixedSizeQueue(int length) {
        Limit = length;
    }
    
    /*
     * Getters & Setters
     */

    // Strings

    // Numeric (Integer, Double, Float, Long ...)

    /// <summary>
    /// Size limit of the Queue
    /// </summary>
    private int Limit { get; }

    // Booleans

    // Images

    // Objects

    // Lists

    // Other
    
    /*
     * Functions
     */

    /// <summary>
    /// Custom enqueue function that auto pops values if the size is exceeded
    /// </summary>
    /// 
    /// <param name="obj">Object to enqueue</param>
    public void Enqueue(T obj) {
        lock (lockObject) {
            q.Enqueue(obj);
            while (q.Count > Limit && q.TryDequeue(out T? _)) { }
        }
    }

    /// <summary>
    /// Custom list representation of the Queue
    /// </summary>
    /// 
    /// <returns>List representation of the Queue</returns>
    public List<T> ToList() {
        lock (lockObject) {
            List<T> listQueue = new(q.ToArray());
            listQueue.Reverse();
            return listQueue;
        }
    }
}