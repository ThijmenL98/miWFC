using System.Collections;
using System.Collections.Generic;
using System.Linq;
using miWFC.DeBroglie.Topo;

namespace miWFC.DeBroglie.Wfc;

/// <summary>
///     Implements pattern adjacency propagation using the arc consistency 4 algorithm.
///     Roughly speaking, this algorith keeps a count for each cell/pattern/direction of the "support",
///     i.e. how many possible cells could adjoin that particular pattern.
///     This count can be straightforwardly updated, and when it drops to zero, we know that that cell/pattern is not
///     possible, and can be banned.
/// </summary>
internal class Ac4PatternModelConstraint : IPatternModelConstraint {
    private readonly int directionsCount;

    // Useful values
    private readonly WavePropagator propagator;
    private readonly ITopology topology;

    /**
     * compatible[index, pattern, direction] contains the number of patterns present in the wave
     * that can be placed in the cell next to index in direction without being
     * in contradiction with pattern placed in index.
     * If possibilites[index][pattern] is set to false, then compatible[index, pattern, direction] has every direction negative or null
     */
    private int[,,] compatible;

    private readonly int indexCount;

    private readonly int patternCount;

    // From model
    private readonly int[][][] propagatorArray;

    // Re-organized propagatorArray
    private readonly BitArray[][] propagatorArrayDense;

    // List of locations that still need to be checked against for fulfilling the model's conditions
    private readonly Stack<IndexPatternItem> toPropagate;

    public Ac4PatternModelConstraint(WavePropagator propagator, PatternModel model) {
        toPropagate = new Stack<IndexPatternItem>();
        this.propagator = propagator;

        propagatorArray = model.Propagator;
        patternCount = model.PatternCount;

        propagatorArrayDense = model.Propagator.Select(a1 => a1.Select(x => {
            BitArray dense = new(patternCount);
            foreach (int p in x) {
                dense[p] = true;
            }

            return dense;
        }).ToArray()).ToArray();

        topology = propagator.Topology;
        indexCount = topology.IndexCount;
        directionsCount = topology.DirectionsCount;
    }

    public void Clear() {
        toPropagate.Clear();

        compatible = new int[indexCount, patternCount, directionsCount];

        int[] edgeLabels = new int[directionsCount];

        for (int index = 0; index < indexCount; index++) {
            if (!topology.ContainsIndex(index)) {
                continue;
            }

            // Cache edgeLabels
            for (int d = 0; d < directionsCount; d++) {
                edgeLabels[d] = topology.TryMove(index, (Direction) d, out int dest, out Direction _, out EdgeLabel el)
                    ? (int) el
                    : -1;
            }

            for (int pattern = 0; pattern < patternCount; pattern++) {
                for (int d = 0; d < directionsCount; d++) {
                    int el = edgeLabels[d];
                    if (el >= 0) {
                        int compatiblePatterns = propagatorArray[pattern][el].Length;
                        compatible[index, pattern, d] = compatiblePatterns;
                        if (compatiblePatterns == 0 && propagator.Wave.Get(index, pattern)) {
                            if (propagator.InternalBan(index, pattern)) {
                                propagator.SetContradiction();
                            }

                            break;
                        }
                    }
                }
            }
        }
    }

    // Precondition that pattern at index is possible.
    public void DoBan(int index, int pattern) {
        // Update compatible (so that we never ban twice)
        for (int d = 0; d < directionsCount; d++) {
            compatible[index, pattern, d] -= patternCount;
        }

        // Queue any possible consequences of this changing.
        toPropagate.Push(new IndexPatternItem {
            Index = index,
            Pattern = pattern
        });
    }

    // This is equivalent to calling DoBan on every possible pattern
    // except the passed in one.
    // But it is more efficient.
    // Precondition that pattern at index is possible.
    public void DoSelect(int index, int pattern) {
        // Update compatible (so that we never ban twice)
        for (int p = 0; p < patternCount; p++) {
            if (p == pattern) {
                continue;
            }

            for (int d = 0; d < directionsCount; d++) {
                if (compatible[index, p, d] > 0) {
                    compatible[index, p, d] -= patternCount;
                }
            }
        }

        // Queue any possible consequences of this changing.
        toPropagate.Push(new IndexPatternItem {
            Index = index,
            Pattern = ~pattern
        });
    }

    public void UndoBan(int index, int pattern) {
        // Undo what was done in DoBan

        // First restore compatible for this cell
        // As it is set a negative value in InteralBan
        for (int d = 0; d < directionsCount; d++) {
            compatible[index, pattern, d] += patternCount;
        }

        // As we always Undo in reverse order, if item is in toPropagate, it'll
        // be at the top of the stack.
        // If item is in toPropagate, then we haven't got round to processing yet, so there's nothing to undo.
        if (toPropagate.Count > 0) {
            IndexPatternItem top = toPropagate.Peek();
            if (top.Index == index && top.Pattern == pattern) {
                toPropagate.Pop();
                return;
            }
        }

        // Not in toPropagate, therefore undo what was done in Propagate
        for (int d = 0; d < directionsCount; d++) {
            if (!topology.TryMove(index, (Direction) d, out int i2, out Direction id, out EdgeLabel el)) {
                continue;
            }

            int[] patterns = propagatorArray[pattern][(int) el];
            foreach (int p in patterns) {
                ++compatible[i2, p, (int) id];
            }
        }
    }

    public void Propagate() {
        while (toPropagate.Count > 0) {
            IndexPatternItem item = toPropagate.Pop();
            int x, y, z;
            topology.GetCoord(item.Index, out x, out y, out z);
            if (item.Pattern >= 0) {
                // Process a ban
                for (int d = 0; d < directionsCount; d++) {
                    if (!topology.TryMove(x, y, z, (Direction) d, out int i2, out Direction id, out EdgeLabel el)) {
                        continue;
                    }

                    int[] patterns = propagatorArray[item.Pattern][(int) el];
                    PropagateBanCore(patterns, i2, (int) id);
                }
            } else {
                // Process a select.
                // Selects work similarly to bans, only instead of decrementing the compatible array
                // we set it to a known value.
                int pattern = ~item.Pattern;
                for (int d = 0; d < directionsCount; d++) {
                    if (!topology.TryMove(x, y, z, (Direction) d, out int i2, out Direction id, out EdgeLabel el)) {
                        continue;
                    }

                    BitArray patternsDense = propagatorArrayDense[pattern][(int) el];

                    // BORIS_TODO: Special case for when patterns.Length == 1?

                    PropagateSelectCore(patternsDense, i2, (int) id);
                }
            }

            // It's important we fully process the item before returning
            // so that we're in a consistent state for backtracking
            // Hence we don't check this during the loops above
            if (propagator.Status == Resolution.CONTRADICTION) {
                return;
            }
        }
    }

    private void PropagateBanCore(int[] patterns, int i2, int d) {
        // Hot loop
        foreach (int p in patterns) {
            int c = --compatible[i2, p, d];
            // Have we just now ruled out this possible pattern?
            if (c == 0) {
                if (propagator.InternalBan(i2, p)) {
                    propagator.SetContradiction();
                }
            }
        }
    }

    private void PropagateSelectCore(BitArray patternsDense, int i2, int id) {
        for (int p = 0; p < patternCount; p++) {
            bool patternsContainsP = patternsDense[p];

            // Sets the value of compatible, triggering internal bans
            int prevCompatible = compatible[i2, p, id];
            bool currentlyPossible = prevCompatible > 0;
            int newCompatible = (currentlyPossible ? 0 : -patternCount) + (patternsContainsP ? 1 : 0);
            compatible[i2, p, id] = newCompatible;

            // Have we just now ruled out this possible pattern?
            if (newCompatible == 0) {
                if (propagator.InternalBan(i2, p)) {
                    propagator.SetContradiction();
                }
            }
        }
    }
}