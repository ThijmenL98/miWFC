using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Models;

namespace WFC4ALL.DeBroglie.Trackers
{
    internal class ChangeTracker : ITracker
    {
        private readonly TileModelMapping tileModelMapping;

        // Using pattern topology
        private List<int> changedIndices;

        // Double buffering
        private List<int> changedIndices2;

        private int generation;

        private int[] lastChangedGeneration;

        internal ChangeTracker(TileModelMapping tileModelMapping)
        {
            this.tileModelMapping = tileModelMapping;
        }

        /// <summary>
        /// Returns the set of indices that have been changed since the last call.
        /// </summary>
        public IEnumerable<int> getChangedIndices()
        {
            List<int> currentChangedIndices = changedIndices;

            // Switch over double buffering
            (changedIndices, changedIndices2) = (changedIndices2, changedIndices);
            changedIndices.Clear();
            generation++;

            if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null)
            {
                return currentChangedIndices;
            }

            return currentChangedIndices.SelectMany(i =>
                tileModelMapping.PatternCoordToTileCoordIndexAndOffset.get(i).Select(x => x.Item2));
        }

        public void doBan(int index, int pattern)
        {
            int g = lastChangedGeneration[index];
            if(g != generation)
            {
                lastChangedGeneration[index] = generation;
                changedIndices.Add(index);
            }
        }

        public void reset()
        {
            changedIndices = new List<int>();
            changedIndices2 = new List<int>();
            lastChangedGeneration = new int[tileModelMapping.PatternTopology.IndexCount];
            generation = 1;
        }

        public void undoBan(int index, int pattern)
        {
            doBan(index, pattern);
        }
    }
}
