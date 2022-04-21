using System;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

/// <summary>
///     Class implementing the heuristic choice of pattern at a given index
/// </summary>
public interface IPatternPicker {
    void Init(WavePropagator wavePropagator);
    int GetRandomPossiblePatternAt(int index, Func<double> randomDouble);
}