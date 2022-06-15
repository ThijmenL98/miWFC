using System;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Trackers;

/// <summary>
///     Class implementing the heuristic choice of pattern at a given index
/// </summary>
public interface IPatternPicker {
    void Init(WavePropagator wavePropagator);
    int GetRandomPossiblePatternAt(int index, Func<double> randomDouble);
}