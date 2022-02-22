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
            index = entropyTracker.getRandomMinEntropyIndex(randomDouble);
            if (index == -1)
            {
                pattern = -1;
                return;
            }
            // Choose a random pattern
            pattern = entropyTracker.getRandomPossiblePatternAt(index, randomDouble);
        }
    }
}
