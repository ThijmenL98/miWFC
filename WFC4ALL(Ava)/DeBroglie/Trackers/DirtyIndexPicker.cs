using System;
using System.Collections.Generic;
using WFC4ALL.DeBroglie.Topo;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class DirtyIndexPicker : IIndexPicker, ITracker {
    private readonly ITopoArray<int> cleanPatterns;
    private readonly HashSet<int> dirtyIndices;
    private readonly IFilteredIndexPicker filteredIndexPicker;

    public DirtyIndexPicker(IFilteredIndexPicker filteredIndexPicker, ITopoArray<int> cleanPatterns) {
        dirtyIndices = new HashSet<int>();
        this.filteredIndexPicker = filteredIndexPicker;
        this.cleanPatterns = cleanPatterns;
    }

    public void Init(WavePropagator wavePropagator) {
        filteredIndexPicker.Init(wavePropagator);
        wavePropagator.AddTracker(this);
    }

    public int GetRandomIndex(Func<double> randomDouble) {
        return filteredIndexPicker.GetRandomIndex(randomDouble, dirtyIndices);
    }

    public void DoBan(int index, int pattern) {
        int clean = cleanPatterns.get(index);
        if (clean == pattern) {
            dirtyIndices.Add(index);
        }
    }

    public void Reset() {
        dirtyIndices.Clear();
    }

    public void UndoBan(int index, int pattern) {
        // Doesn't undo dirty, it's too annoying to track
    }
}