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

        private readonly int selectionHeuristic;
        
        public EntropyHeuristic(EntropyTracker entropyTracker, Func<double> randomDouble, int selectionHeuristic)
        {
            this.entropyTracker = entropyTracker;
            this.randomDouble = randomDouble;
            this.selectionHeuristic = selectionHeuristic;
            Console.WriteLine(@$"EYO Entropy heuristic created {selectionHeuristic}");
        }

        public void pickObservation(out int index, out int pattern)
        {
            index = selectionHeuristic switch {
                // Choose a random cell
                // RF7
                0 => entropyTracker.getRandomMinEntropyIndex(randomDouble, false),    // Least Entropy Heuristic
                1 => entropyTracker.getLexicalIndex(),                                      // Lexical Heuristic
                2 => entropyTracker.getSpiralIndex(),                                       // Spiral Heuristic
                3 => entropyTracker.getHilbertIndex(),                                      // Hilbert Heuristic
                4 => entropyTracker.getRandomMinEntropyIndex(randomDouble, true),    // Simple Heuristic
                5 => entropyTracker.getRandomIndex(randomDouble),                           // Random heuristic
                _ => throw new ArgumentOutOfRangeException()
            };

            if (index == -1) {
                pattern = -1;
                return;
            }
            
            // Choose a random pattern
            //TODO RF8
            // Weighted - Pick weighted pattern (although randomly) = entropyTracker.getWeightedPatternAt(index, randomDouble);
            // Random - Pick random pattern                         = entropyTracker.getRandomPatternAt(index, randomDouble);
            // Least - Pick least used pattern next
            pattern = entropyTracker.getWeightedPatternAt(index, randomDouble);
        }
    }
}
