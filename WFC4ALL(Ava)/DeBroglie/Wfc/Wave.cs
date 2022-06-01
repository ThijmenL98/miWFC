using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace WFC4ALL.DeBroglie.Wfc; 

/**
 * Wave is a fancy array that tracks various per-cell information.
 * Most importantly, it tracks possibilities - which patterns are possible to put
 * into which cells.
 * It has no notion of cell adjacency, cells are just referred to by integer index.
 */
public class Wave {
    private readonly int patternCount;

    private readonly int[] patternCounts;

    // possibilities[index*patternCount + pattern] is true if we haven't eliminated putting
    // that pattern at that index.
    private readonly BitArray possibilities;

    public Wave(int patternCount, int indices) {
        this.patternCount = patternCount;

        this.Indicies = indices;

        possibilities = new BitArray(indices * patternCount, true);

        patternCounts = new int[indices];
        for (int index = 0; index < indices; index++) {
            patternCounts[index] = patternCount;
        }
    }

    public int Indicies { get; }

    public bool Get(int index, int pattern) {
        return possibilities[index * patternCount + pattern];
    }

    public int GetPatternCount(int index) {
        return patternCounts[index];
    }

    // Returns true if there is a contradiction
    public bool RemovePossibility(int index, int pattern) {
        if (!possibilities[index * patternCount + pattern]) {
            throw new TargetException();
        }
        possibilities[index * patternCount + pattern] = false;
        int c = --patternCounts[index];
        return c == 0;
    }

    public void AddPossibility(int index, int pattern) {
        if (possibilities[index * patternCount + pattern]) {
            throw new TargetException();
        }
        possibilities[index * patternCount + pattern] = true;
        patternCounts[index]++;
    }

    // BORIS_TODO: This should respect mask. Maybe move out of Wave
    public double GetProgress() {
        // BORIS_TODO: Use patternCount info?
        int c = 0;
        foreach (bool b in possibilities) {
            if (!b) {
                c += 1;
            }
        }

        // We're basically done when we've banned all but one pattern for each index
        return (double) c / (patternCount - 1) / Indicies;
    }
}