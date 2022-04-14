using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Wfc; 

internal class Ac3PatternModelConstraint : IPatternModelConstraint {
    private readonly int directionsCount;
    private readonly int[][][] incoming;

    // Useful values
    private readonly WavePropagator propagator;
    private readonly ITopology topology;
    private int indexCount;

    private readonly int patternCount;

    // From model
    private readonly int[][][] propagatorArray;

    // Re-organized propagatorArray
    private BitArray[][] propagatorArrayDense;

    // List of locations that still need to be checked against for fulfilling the model's conditions
    private readonly HashSet<(int, Direction)> toPropagate;

    public Ac3PatternModelConstraint(WavePropagator propagator, PatternModel model) {
        toPropagate = new HashSet<(int, Direction)>();
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

        IEnumerable<(int p, int el, int p2)> flatPropagator = Enumerable.Range(0, propagator.PatternCount)
            .SelectMany(p => Enumerable.Range(0, propagatorArray[p].Length)
                .SelectMany(el => propagatorArray[p][el].Select(p2 => (p, el, p2))));

        int elCount = propagatorArray.Select(x => x.Length).Max() + 1;

        incoming = DenseRegroup(flatPropagator, patternCount, x => x.p2,
            g => DenseRegroup(g, elCount, x => x.el, g2 => g2.Select(x => x.p).ToArray()));
    }

    public void DoBan(int index, int pattern) {
        for (int d = 0; d < directionsCount; d++) {
            toPropagate.Add((index, (Direction) d));
        }
    }

    public void UndoBan(int index, int pattern) { }

    public void DoSelect(int index, int pattern) {
        // We just record which cells are dirty, so
        // there's no difference between a ban and a select.
        DoBan(index, pattern);
    }

    public void Propagate() {
        Wave wave = propagator.Wave;
        while (toPropagate.Count > 0) {
            (int, Direction) item = toPropagate.First();
            toPropagate.Remove(item);
            (int index, Direction d) = item;
            topology.GetCoord(index, out int x, out int y, out int z);
            if (!topology.TryMove(x, y, z, d, out int i2, out Direction id, out EdgeLabel el)) {
                continue;
            }

            for (int pattern = 0; pattern < patternCount; pattern++) {
                if (!wave.Get(i2, pattern)) {
                    continue;
                }

                int[] incomingPatterns = incoming[pattern][(int) el];
                bool found = false;
                foreach (int p in incomingPatterns) {
                    if (wave.Get(index, p)) {
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    if (propagator.InternalBan(i2, pattern)) {
                        propagator.SetContradiction();
                    }
                }
            }
        }
    }

    public void Clear() { }

    private static TV[] DenseRegroup<T, TV>(IEnumerable<T> items, int count, Func<T, int> keyFunc,
        Func<List<T>, TV> valueFunc) {
        List<T>[] vList = Enumerable.Range(0, count).Select(_ => new List<T>()).ToArray();
        foreach (T item in items) {
            int k = keyFunc(item);
            vList[k].Add(item);
        }

        return vList.Select(valueFunc).ToArray();
    }
}