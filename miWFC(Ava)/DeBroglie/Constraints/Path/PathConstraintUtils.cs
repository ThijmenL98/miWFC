using System;
using System.Collections.Generic;
using miWFC.DeBroglie.Topo;

namespace miWFC.DeBroglie.Constraints.Path;

/// <summary>
///     Contains utilities relating to <see cref="PathConstraint" />
/// </summary>
public static class PathConstraintUtils {
    private static readonly int[] emtpy = { };

    public static SimpleGraph CreateGraph(ITopology topology) {
        int nodeCount = topology.IndexCount;
        int[][] neighbours = new int[nodeCount][];
        for (int i = 0; i < nodeCount; i++) {
            if (!topology.ContainsIndex(i)) {
                neighbours[i] = emtpy;
            }

            List<int> n = new();
            for (int d = 0; d < topology.DirectionsCount; d++) {
                if (topology.TryMove(i, (Direction) d, out int dest)) {
                    n.Add(dest);
                }
            }

            neighbours[i] = n.ToArray();
        }

        return new SimpleGraph {
            NodeCount = nodeCount,
            Neighbours = neighbours
        };
    }

    /// <summary>
    ///     First, find the subgraph of graph given by just the walkable vertices.
    ///     <paramref name="relevant" /> defaults to walkable if null.
    ///     A cut-vertex is defined as any point that, if removed, there exist two other relevant points
    ///     that no longer have a path.
    ///     For an explanation, see:
    ///     https://www.boristhebrave.com/2018/04/28/random-paths-via-chiseling/
    /// </summary>
    public static AtrticulationPointsInfo GetArticulationPoints(SimpleGraph graph, bool[] walkable,
        bool[] relevant = null) {
        int indices = walkable.Length;

        if (indices != graph.NodeCount) {
            throw new Exception("Length of walkable doesn't match count of nodes");
        }

        int[] low = new int[indices];
        int num = 1;
        int[] dfsNum = new int[indices];
        bool[]? isArticulation = new bool[indices];
        int?[] component = new int?[indices];
        int currentComponent = 0;

        // This hideous function is a iterative version
        // of the much more elegant recursive version below.
        // Unfortunately, the recursive version tends to blow the stack for large graphs
        int cutVertex(int initialU) {
            List<CutVertexFrame> stack = new();

            stack.Add(new CutVertexFrame {U = initialU});

            // This is the "returned" value from recursing
            bool childRelevantSubtree = false;

            while (true) {
                int frameIndex = stack.Count - 1;
                CutVertexFrame frame = stack[frameIndex];
                int u = frame.U;
                switch (frame.State) {
                    // Initialization
                    case 0: {
                        component[u] = currentComponent;
                        low[u] = dfsNum[u] = num++;
                        // Enter loop
                        goto case 1;
                    }
                    // Loop over neighbours
                    case 1: {
                        // Check loop condition
                        int[] neighbours = graph.Neighbours[u];
                        int neighbourIndex = frame.NeighbourIndex;
                        if (neighbourIndex >= neighbours.Length) {
                            // Exit loop
                            goto case 3;
                        }

                        int v = neighbours[neighbourIndex];
                        if (!walkable[v]) {
                            // continue to next iteration of loop
                            frame.NeighbourIndex = neighbourIndex + 1;
                            goto case 1;
                        }

                        // v is a neighbour of u
                        bool unvisited = dfsNum[v] == 0;
                        if (unvisited) {
                            // Recurse into v
                            stack.Add(new CutVertexFrame {U = v});
                            frame.State = 2;
                            stack[frameIndex] = frame;
                            break;
                        }

                        low[u] = Math.Min(low[u], dfsNum[v]);

                        // continue to next iteration of loop
                        frame.NeighbourIndex = neighbourIndex + 1;
                        goto case 1;
                    }
                    // Return from recursion (still in loop)
                    case 2: {
                        // At this point, childRelevantSubtree
                        // has been set to the by the recursed call we've just returned from
                        int[] neighbours = graph.Neighbours[u];
                        int neighbourIndex = frame.NeighbourIndex;
                        int v = neighbours[neighbourIndex];

                        if (childRelevantSubtree) {
                            frame.RelevantChildSubtreeCount++;
                        }

                        if (low[v] >= dfsNum[u]) {
                            if (childRelevantSubtree) {
                                isArticulation[u] = true;
                            }
                        }

                        low[u] = Math.Min(low[u], low[v]);

                        // continue to next iteration of loop
                        frame.NeighbourIndex = neighbourIndex + 1;
                        goto case 1;
                    }
                    // Cleanup
                    case 3:
                        if (frameIndex == 0) {
                            // Root frame
                            return frame.RelevantChildSubtreeCount;
                        }

                        // Set childRelevantSubtree with the return value from this recursed call
                        bool isRelevant = relevant == null || relevant[u];
                        bool descendantOrSelfIsRelevant = frame.RelevantChildSubtreeCount > 0 || isRelevant;
                        childRelevantSubtree = descendantOrSelfIsRelevant;
                        // Pop the frame
                        stack.RemoveAt(frameIndex);
                        // Resume the caller (which will be in state 2)
                        break;
                }
            }
        }

        Tuple<int, bool> cutvertex(int u) {
            int relevantChildSubtreeCount = 0;
            component[u] = currentComponent;
            low[u] = dfsNum[u] = num++;

            foreach (int v in graph.Neighbours[u]) {
                if (!walkable[v]) {
                    continue;
                }

                // v is a neighbour of u
                bool unvisited = dfsNum[v] == 0;
                if (unvisited) {
                    // v is a child of u
                    bool relevantChildSubtree = cutvertex(v).Item2;
                    if (relevantChildSubtree) {
                        relevantChildSubtreeCount++;
                    }

                    if (low[v] >= dfsNum[u]) {
                        if (relevantChildSubtree) {
                            isArticulation[u] = true;
                        }
                    }

                    low[u] = Math.Min(low[u], low[v]);
                } else {
                    // v is an ancestor of u
                    low[u] = Math.Min(low[u], dfsNum[v]);
                }
            }

            bool isRelevant = relevant == null || relevant[u];
            bool descendantOrSelfIsRelevant = relevantChildSubtreeCount > 0 || isRelevant;
            return Tuple.Create(relevantChildSubtreeCount, descendantOrSelfIsRelevant);
        }

        // Find starting point
        for (int i = 0; i < indices; i++) {
            if (!walkable[i]) {
                continue;
            }

            // Only consider relevant nodes for root.
            // (this is a precondition of cutvertex)
            if (relevant != null && !relevant[i]) {
                continue;
            }

            // Already visited
            if (dfsNum[i] != 0) {
                continue;
            }

            int relevantChildSubtreeCount = cutVertex(i);
            //var relevantChildSubtreeCount = cutvertex(i).Item1;
            isArticulation[i] = relevantChildSubtreeCount > 1;
            currentComponent++;
        }

        return new AtrticulationPointsInfo {
            IsArticulation = isArticulation,
            Component = component,
            ComponentCount = currentComponent
        };
    }

    private struct CutVertexFrame {
        public int U;
        public int State;
        public int NeighbourIndex;
        public int RelevantChildSubtreeCount;
    }

    public class AtrticulationPointsInfo {
        public bool[] IsArticulation { get; set; }
        public int ComponentCount { get; set; }
        public int?[] Component { get; set; }
    }


    public class SimpleGraph {
        public int NodeCount { get; set; }

        public int[][] Neighbours { get; set; }
    }
}