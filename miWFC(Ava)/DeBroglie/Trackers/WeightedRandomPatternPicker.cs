using System;
using System.Diagnostics;
using miWFC.DeBroglie.Wfc;
using miWFC.Managers;

namespace miWFC.DeBroglie.Trackers;

public class WeightedRandomPatternPicker {
    private double[] frequencies;
    private Wave wave;
    private CentralManager cm;

    public void Init(WavePropagator wavePropagator, CentralManager _cm) {
        wave = wavePropagator.Wave;
        frequencies = wavePropagator.Frequencies;
        cm = _cm;
    }

    public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble) {
        return RandomPickerUtils.GetRandomPossiblePattern(wave, randomDouble, index, 
            cm.getWFCHandler().isOverlappingModel() ? frequencies : cm.getWFCHandler().getWeightsAt(index));
    }
}