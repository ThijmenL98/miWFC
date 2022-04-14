using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Wfc; 

/// <summary>
///     When a pattern is selected, update the possible patterns in immediately adjacent cells
///     but don't recurse beyond that.
///     This constraint is much faster than <see cref="Ac4PatternModelConstraint" />, but doesn't
///     enforce full arc consistency, so leads to contradictions more frequently.
/// </summary>
internal class OneStepPatternModelConstraint : IPatternModelConstraint {
    private readonly int directionsCount;
    private readonly int patternCount;
    private readonly WavePropagator propagator;
    private int[][][] propagatorArray;
    private readonly BitArray[][] propagatorArrayDense;
    private readonly ITopology topology;
    private readonly Stack<IndexPatternItem> toPropagate;
    private Wave wave;


    public OneStepPatternModelConstraint(WavePropagator propagator, PatternModel model) {
        propagatorArray = model.Propagator;
        this.propagator = propagator;
        topology = propagator.Topology;
        directionsCount = propagator.Topology.DirectionsCount;
        patternCount = model.PatternCount;
        propagatorArrayDense = model.Propagator.Select(a1 => a1.Select(x => {
            BitArray dense = new(patternCount);
            foreach (int p in x) {
                dense[p] = true;
            }

            return dense;
        }).ToArray()).ToArray();
        toPropagate = new Stack<IndexPatternItem>();
    }


    public void Clear() {
        toPropagate.Clear();
        wave = propagator.Wave;
    }

    public void DoBan(int index, int pattern) {
        // Assumes DoBan is called before wave.RemovePossibility
        if (wave.GetPatternCount(index) == 2) {
            for (int p = 0; p < patternCount; p++) {
                if (p == pattern) {
                    continue;
                }

                if (wave.Get(index, p)) {
                    DoSelect(index, p);
                    return;
                }
            }
        }
    }

    public void UndoBan(int index, int pattern) {
        // As we always Undo in reverse order, if item is in toPropagate, it'll
        // be at the top of the stack.
        // If item is in toPropagate, then we haven't got round to processing yet, so there's nothing to undo.
        if (toPropagate.Count > 0) {
            IndexPatternItem top = toPropagate.Peek();
            if (top.Index == index && top.Pattern == pattern) {
                toPropagate.Pop();
            }
        }
    }

    public void DoSelect(int index, int pattern) {
        toPropagate.Push(new IndexPatternItem {Index = index, Pattern = pattern});
    }

    public void Propagate() {
        while (toPropagate.Count > 0) {
            IndexPatternItem item = toPropagate.Pop();
            int index = item.Index;
            int pattern = item.Pattern;


            int x, y, z;
            topology.GetCoord(index, out x, out y, out z);
            for (int d = 0; d < directionsCount; d++) {
                if (!topology.TryMove(x, y, z, (Direction) d, out int i2, out Direction id, out EdgeLabel el)) {
                    continue;
                }

                BitArray patternsDense = propagatorArrayDense[pattern][(int) el];

                for (int p = 0; p < patternCount; p++) {
                    if (patternsDense[p]) {
                        continue;
                    }

                    if (wave.Get(i2, p)) {
                        if (propagator.InternalBan(i2, p)) {
                            propagator.SetContradiction();
                        }
                    }
                }
            }
        }
    }
}