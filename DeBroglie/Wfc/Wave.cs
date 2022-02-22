using System.Collections;
using System.Diagnostics;

namespace WFC4All.DeBroglie.Wfc
{

    /**
     * Wave is a fancy array that tracks various per-cell information.
     * Most importantly, it tracks possibilities - which patterns are possible to put
     * into which cells.
     * It has no notion of cell adjacency, cells are just referred to by integer index.
     */
    internal class Wave
    {
        private readonly int patternCount;

        // possibilities[index*patternCount + pattern] is true if we haven't eliminated putting
        // that pattern at that index.
        private readonly BitArray possibilities;

        private readonly int[] patternCounts;

        private readonly int indices;

        public Wave(int patternCount, int indices)
        {
            this.patternCount = patternCount;

            this.indices = indices;

            possibilities = new BitArray(indices * patternCount, true);

            patternCounts = new int[indices];
            for (int index = 0; index < indices; index++)
            {
                patternCounts[index] = patternCount;
            }
        }

        public int Indicies => indices;

        public bool get(int index, int pattern)
        {
            return possibilities[index * patternCount + pattern];
        }

        public int getPatternCount(int index)
        {
            return patternCounts[index];
        }

        // Returns true if there is a contradiction
        public bool removePossibility(int index, int pattern)
        {
            Debug.Assert(possibilities[index * patternCount + pattern] == true);
            possibilities[index * patternCount + pattern] = false;
            int c = --patternCounts[index];
            return c == 0;
        }

        public void addPossibility(int index, int pattern)
        {
            Debug.Assert(possibilities[index * patternCount + pattern] == false);
            possibilities[index * patternCount + pattern] = true;
            patternCounts[index]++;
        }

        // TODO: This should respect mask. Maybe move out of Wave
        public double getProgress()
        {
            // TODO: Use patternCount info?
            int c = 0;
            foreach(bool b in possibilities)
            {
                if (!b) {
                    c += 1;
                }
            }
            // We're basically done when we've banned all but one pattern for each index
            return (double)c / (patternCount-1) / indices;
        }
    }
}
