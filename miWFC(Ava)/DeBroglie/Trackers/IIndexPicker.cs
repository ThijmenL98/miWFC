using System;
using System.Collections.Generic;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Trackers;

/// <summary>
///     Class implementing the heuristic choice of index
/// </summary>
public interface IIndexPicker {
    void Init(WavePropagator wavePropagator);
    int GetRandomIndex(Func<double> randomDouble);
}

internal interface IFilteredIndexPicker {
    void Init(WavePropagator wavePropagator);
    int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices);
}