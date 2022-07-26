using System;
using miWFC.DeBroglie.Wfc;
using miWFC.Delegators;

namespace miWFC.DeBroglie.Trackers;

public class WeightedRandomPatternPicker {
    private CentralDelegator cd;
    private double[] frequencies;
    private Wave wave;

    public void Init(WavePropagator wavePropagator, CentralDelegator _cd) {
        wave = wavePropagator.Wave;
        frequencies = wavePropagator.Frequencies;
        cd = _cd;
    }

    public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble) {
        return RandomPickerUtils.GetRandomPossiblePattern(wave, randomDouble, index,
            cd.GetWFCHandler().IsOverlappingModel() ? frequencies : cd.GetWFCHandler().GetWeightsAt(index));
    }
}