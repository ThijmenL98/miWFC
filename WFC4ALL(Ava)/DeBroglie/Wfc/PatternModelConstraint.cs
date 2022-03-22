using System.Collections.Generic;
using WFC4All.DeBroglie.Topo;

namespace WFC4All.DeBroglie.Wfc
{
    /// <summary>
    /// This works similarly to IWaveConstraint, in that it listens to changes in the Wave, and  
    /// makes appropriate changes to the propagator for the constraint.
    /// The constraint being enforced that adjacent patterns must filt PatternModel.Propatagor.
    /// 
    /// It's not implemented as a IWaveConstraint for historical reasons
    /// </summary>
    internal class PatternModelConstraint
    {
        // From model
        private readonly int[][][] propagatorArray;
        private readonly int patternCount;

        // Useful values
        private readonly WavePropagator propagator;
        private readonly int directionsCount;
        private readonly ITopology topology;
        private readonly int indexCount;

        // List of locations that still need to be checked against for fulfilling the model's conditions
        private readonly Stack<IndexPatternItem> toPropagate;

        /**
          * compatible[index, pattern, direction] contains the number of patterns present in the wave
          * that can be placed in the cell next to index in direction without being
          * in contradiction with pattern placed in index.
          * If possibilites[index][pattern] is set to false, then compatible[index, pattern, direction] has every direction negative or null
          */
        private int[,,] compatible;

        public PatternModelConstraint(WavePropagator propagator, PatternModel model)
        {
            toPropagate = new Stack<IndexPatternItem>();
            this.propagator = propagator;

            propagatorArray = model.Propagator;
            patternCount = model.PatternCount;

            topology = propagator.Topology;
            indexCount = topology.IndexCount;
            directionsCount = topology.DirectionsCount;
        }

        public void clear()
        {
            toPropagate.Clear();

            compatible = new int[indexCount, patternCount, directionsCount];
            for (int index = 0; index < indexCount; index++)
            {
                if (!topology.containsIndex(index)) {
                    continue;
                }

                for (int pattern = 0; pattern < patternCount; pattern++)
                {
                    for (int d = 0; d < directionsCount; d++)
                    {
                        if (topology.tryMove(index, (Direction)d, out int dest, out Direction _, out EdgeLabel el))
                        {
                            int compatiblePatterns = propagatorArray[pattern][(int)el].Length;
                            compatible[index, pattern, d] = compatiblePatterns;
                            if (compatiblePatterns == 0 && propagator.Wave.get(index, pattern))
                            {
                                if (propagator.internalBan(index, pattern))
                                {
                                    propagator.setContradiction();
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void doBan(int index, int pattern)
        {
            // Update compatible (so that we never ban twice)
            for (int d = 0; d < directionsCount; d++)
            {
                compatible[index, pattern, d] -= patternCount;
            }
            // Queue any possible consequences of this changing.
            toPropagate.Push(new IndexPatternItem
            {
                Index = index,
                Pattern = pattern,
            });
        }

        public void undoBan(IndexPatternItem item)
        {
            // Undo what was done in DoBan

            // First restore compatible for this cell
            // As it is set to zero in InteralBan
            for (int d = 0; d < directionsCount; d++)
            {
                compatible[item.Index, item.Pattern, d] += patternCount;
            }

            // As we always Undo in reverse order, if item is in toPropagate, it'll
            // be at the top of the stack.
            // If item is in toPropagate, then we haven't got round to processing yet, so there's nothing to undo.
            if (toPropagate.Count > 0)
            {
                IndexPatternItem top = toPropagate.Peek();
                if(top.Equals(item))
                {
                    toPropagate.Pop();
                    return;
                }
            }

            // Not in toPropagate, therefore undo what was done in Propagate
            for (int d = 0; d < directionsCount; d++)
            {
                if (!topology.tryMove(item.Index, (Direction)d, out int i2, out Direction id, out EdgeLabel el))
                {
                    continue;
                }
                int[] patterns = propagatorArray[item.Pattern][(int)el];
                foreach (int p in patterns)
                {
                    ++compatible[i2, p, (int)id];
                }
            }
        }

        private void propagateCore(int[] patterns, int i2, int d)
        {
            // Hot loop
            foreach (int p in patterns)
            {
                int c = --compatible[i2, p, d];
                // We've just now ruled out this possible pattern
                if (c == 0)
                {
                    if (propagator.internalBan(i2, p))
                    {
                        propagator.setContradiction();
                    }
                }
            }
        }

        public void propagate()
        {
            while (toPropagate.Count > 0)
            {
                IndexPatternItem item = toPropagate.Pop();
                topology.getCoord(item.Index, out int x, out int y, out int z);
                for (int d = 0; d < directionsCount; d++)
                {
                    if (!topology.tryMove(x, y, z, (Direction)d, out int i2, out Direction id, out EdgeLabel el))
                    {
                        continue;
                    }
                    int[] patterns = propagatorArray[item.Pattern][(int)el];
                    propagateCore(patterns, i2, (int)id);
                }
                // It's important we fully process the item before returning
                // so that we're in a consistent state for backtracking
                if (propagator.Status == Resolution.CONTRADICTION)
                {
                    return;
                }
            }
        }

    }
}
