using System;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Wfc
{

    /// <summary>
    /// Chooses the next tile based of minimum "entropy", i.e. 
    /// the tiles which are already most constrained.
    /// </summary>
    internal class EntropyHeuristic : IPickHeuristic
    {
        private readonly EntropyTracker entropyTracker;

        private readonly Func<double> randomDouble;
        
        public EntropyHeuristic(EntropyTracker entropyTracker, Func<double> randomDouble)
        {
            this.entropyTracker = entropyTracker;
            this.randomDouble = randomDouble;
        }

        public void pickObservation(out int index, out int pattern)
        {
            // index = selectionHeuristic switch {
            //     // Choose a random cell
            //     // RF7
            //     0 => entropyTracker.getRandomMinEntropyIndex(randomDouble, false),    // Least Entropy Heuristic
            //     1 => entropyTracker.getLexicalIndex(),                                      // Lexical Heuristic
            //     2 => entropyTracker.getSpiralIndex(),                                       // Spiral Heuristic
            //     3 => entropyTracker.getHilbertIndex(),                                      // Hilbert Heuristic
            //     4 => entropyTracker.getRandomMinEntropyIndex(randomDouble, true),    // Simple Heuristic
            //     5 => entropyTracker.getRandomIndex(randomDouble),                           // Random heuristic
            //     _ => throw new ArgumentOutOfRangeException()
            // };

            index = entropyTracker.getRandomMinEntropyIndex(randomDouble, false);

            if (index == -1) {
                pattern = -1;
                return;
            }
            
            // Choose a random pattern
            // RF8
            // pattern = patternHeuristic switch {
            //     0 => entropyTracker.getWeightedPatternAt(index, randomDouble), // Weighted Choice
            //     1 => entropyTracker.getRandomPatternAt(index, randomDouble),   // Random Choice
            //     2 => entropyTracker.getLeastPatternAt(index),                  // Least Used
            //     _ => throw new ArgumentOutOfRangeException()
            // };
            entropyTracker.updateZeroWeights(index);
            pattern = entropyTracker.getWeightedPatternAt(index, randomDouble);
        }
    }
}
