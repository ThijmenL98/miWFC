using System;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Trackers; 

internal static class RandomPickerUtils {
    public static int GetRandomPossiblePattern(Wave wave, Func<double> randomDouble, int index, double[] frequencies) {
        int patternCount = frequencies.Length;
        double s = 0.0;
        for (int pattern = 0; pattern < patternCount; pattern++) {
            if (wave.Get(index, pattern)) {
                s += frequencies[pattern];
            }
        }

        double r = randomDouble() * s;
        for (int pattern = 0; pattern < patternCount; pattern++) {
            if (wave.Get(index, pattern)) {
                r -= frequencies[pattern];
            }

            if (r <= 0) {
                return pattern;
            }
        }

        return patternCount - 1;
    }

    public static int GetRandomPossiblePattern(Wave wave, Func<double> randomDouble, int index, double[] frequencies,
        int[] patterns) {
        if (patterns == null) {
            return GetRandomPossiblePattern(wave, randomDouble, index, frequencies);
        }

        double s = 0.0;
        int patternCount = frequencies.Length;
        for (int i = 0; i < patternCount; i++) {
            int pattern = patterns[i];
            if (wave.Get(index, pattern)) {
                s += frequencies[i];
            }
        }

        double r = randomDouble() * s;
        for (int i = 0; i < patternCount; i++) {
            int pattern = patterns[i];
            if (wave.Get(index, pattern)) {
                r -= frequencies[i];
            }

            if (r <= 0) {
                return pattern;
            }
        }

        return patterns[patterns.Length - 1];
    }
}