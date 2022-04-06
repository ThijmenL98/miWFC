using System;
using WFC4ALL.DeBroglie.Trackers;

namespace WFC4ALL.DeBroglie.Wfc
{
    /// <summary>
    /// Chooses the next tile based of minimum "entropy", i.e. 
    /// the tiles which are already most constrained.
    /// </summary>
    internal class ArrayPriorityEntropyHeuristic : IPickHeuristic
    {
        private readonly ArrayPriorityEntropyTracker entropyTracker;

        private readonly Func<double> randomDouble;

        public ArrayPriorityEntropyHeuristic(ArrayPriorityEntropyTracker entropyTracker, Func<double> randomDouble)
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
