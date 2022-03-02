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
            // Choose a random cell
            // TODO RF7
            // Lexical: entropyTracker.getLexicalIndex()
            // Entropy: entropyTracker.getRandomMinEntropyIndex(randomDouble, false)
            // Simple: entropyTracker.getRandomMinEntropyIndex(randomDouble, true)
            // Random: entropyTracker.getRandomIndex()
            // Spiral: entropyTracker.getSpiralIndex()
            // Hilbert: TODO
            index = entropyTracker.getRandomMinEntropyIndex(randomDouble, false);
            if (index == -1)
            {
                pattern = -1;
                return;
            }
            
            // Choose a random pattern
            //TODO RF8
            pattern = entropyTracker.getRandomPossiblePatternAt(index, randomDouble);
        }
    }
}
