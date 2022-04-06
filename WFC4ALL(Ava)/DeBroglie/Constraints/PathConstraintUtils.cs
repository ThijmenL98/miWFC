using System;
using System.Collections.Generic;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Constraints
{
    /// <summary>
    /// Contains utilities relating to <see cref="PathConstraint"/>
    /// </summary>
    public static class PathConstraintUtils
    {
        private static readonly int[] emtpy = { };

        public static SimpleGraph createGraph(ITopology topology)
        {
            int nodeCount = topology.IndexCount;
            int[][] neighbours = new int[nodeCount][];
            for (int i = 0; i < nodeCount; i++)
            {
                if(!topology.containsIndex(i))
                {
                    neighbours[i] = emtpy;
                }

                List<int> n = new();
                for (int d=0; d < topology.DirectionsCount; d++)
                {
                    if (topology.tryMove(i, (Direction)d, out int dest))
                    {
                        n.Add(dest);
                    }
                }
                neighbours[i] = n.ToArray();
            }

            return new SimpleGraph
            {
                NodeCount = nodeCount,
                Neighbours = neighbours,
            };
        }

        private struct CutVertexFrame
        {
            public int u;
            public int state;
            public int neighbourIndex;
            public bool isRelevantSubtree;
        }

        /// <summary>
        /// First, find the subgraph of graph given by just the walkable vertices.
        /// Then find any point, that if removed, mean there's no path between two
        /// given relevant points.
        /// If it's already not possible to path, then return null.
        /// Note: relevant points themselves are always returned as true.
        /// 
        /// Also optionally returns the extent of the connecteted component containing relevant.
        /// 
        /// If relevant is null, instead returns the points, that if removed, increase the number of
        /// connected components.
        /// 
        /// For an explanation, see:
        /// https://www.boristhebrave.com/2018/04/28/random-paths-via-chiseling/
        /// </summary>
        public static bool[] getArticulationPoints(SimpleGraph graph, bool[] walkable, bool[]? relevant = null, bool[]? component = null)
        {
            int indices = walkable.Length;

            if (indices != graph.NodeCount) {
                throw new Exception("Length of walkable doesn't match count of nodes");
            }

            int[] low = new int[indices];
            int num = 1;
            int[] dfsNum = new int[indices];
            bool[] isArticulation = new bool[indices];

            // This hideous function is a iterative version
            // of the much more elegant recursive version below.
            // Unfortunately, the recursive version tends to blow the stack for large graphs
            int cutVertex(int initialU)
            {
                List<CutVertexFrame> stack = new();

                stack.Add(new CutVertexFrame { u = initialU });

                // This is the "returned" value from recursing
                bool childRelevantSubtree = false;

                int childCount = 0;

                while(true)
                {
                    int frameIndex = stack.Count - 1;
                    CutVertexFrame frame = stack[frameIndex];
                    int u = frame.u;
                    switch(frame.state)
                    {
                        // Initialization
                        case 0:
                            {
                                bool isRelevant = relevant != null && relevant[u];
                                if (isRelevant)
                                {
                                    isArticulation[u] = true;
                                }
                                if (component != null)
                                {
                                    component[u] = true;
                                }
                                frame.isRelevantSubtree = isRelevant;
                                low[u] = dfsNum[u] = num++;
                                // Enter loop
                                goto case 1;
                            }
                        // Loop over neighbours
                        case 1:
                            {
                                // Check loop condition
                                int[] neighbours = graph.Neighbours[u];
                                int neighbourIndex = frame.neighbourIndex;
                                if(neighbourIndex >= neighbours.Length)
                                {
                                    // Exit loop
                                    goto case 3;
                                }
                                int v = neighbours[neighbourIndex];
                                if (!walkable[v])
                                {
                                    // continue to next iteration of loop
                                    frame.neighbourIndex = neighbourIndex + 1;
                                    goto case 1;
                                }
                                
                                // v is a neighbour of u
                                bool unvisited = dfsNum[v] == 0;
                                if (unvisited)
                                {
                                    // Recurse into v
                                    stack.Add(new CutVertexFrame { u = v });
                                    frame.state = 2;
                                    stack[frameIndex] = frame;
                                    break;
                                }

                                low[u] = Math.Min(low[u], dfsNum[v]);

                                // continue to next iteration of loop
                                frame.neighbourIndex = neighbourIndex + 1;
                                goto case 1;
                            }
                        // Return from recursion (still in loop)
                        case 2:
                            {
                                // At this point, childRelevantSubtree
                                // has been set to the by the recursed call we've just returned from
                                int[] neighbours = graph.Neighbours[u];
                                int neighbourIndex = frame.neighbourIndex;
                                int v = neighbours[neighbourIndex];

                                if (frameIndex == 0)
                                {
                                    // Root frame
                                    childCount++;
                                }

                                if (childRelevantSubtree)
                                {
                                    frame.isRelevantSubtree = true;
                                }
                                if (low[v] >= dfsNum[u])
                                {
                                    if (relevant == null || childRelevantSubtree)
                                    {
                                        isArticulation[u] = true;
                                    }
                                }
                                low[u] = Math.Min(low[u], low[v]);

                                // continue to next iteration of loop
                                frame.neighbourIndex = neighbourIndex + 1;
                                goto case 1;
                            }
                        // Cleanup
                        case 3:
                            if(frameIndex == 0)
                            {
                                // Root frame
                                return childCount;
                            }
                            else
                            {
                                // Set childRelevantSubtree with the return value from this recursed call
                                childRelevantSubtree = frame.isRelevantSubtree;
                                // Pop the frame
                                stack.RemoveAt(frameIndex);
                                // Resume the caller (which will be in state 2)
                                break;
                            }
                    }
                }
            }


            /*
            Tuple<int, bool> cutvertex(int u)
            {
                var childCount = 0;
                var isRelevant = relevant != null && relevant[u];
                if (isRelevant)
                {
                    isArticulation[u] = true;
                }
                if (component != null) 
                {
                    component[u] = true;
                }
                var isRelevantSubtree = isRelevant;
                low[u] = dfsNum[u] = num++;

                foreach (var v in graph.Neighbours[u])
                {
                    if (!walkable[v])
                    {
                        continue;
                    }
                    // v is a neighbour of u
                    var unvisited = dfsNum[v] == 0;
                    if (unvisited)
                    {
                        var childRelevantSubtree = cutvertex(v).Item2;
                        childCount++;
                        if (childRelevantSubtree)
                        {
                            isRelevantSubtree = true;
                        }
                        if (low[v] >= dfsNum[u])
                        {
                            if (relevant == null || childRelevantSubtree)
                            {
                                isArticulation[u] = true;
                            }
                        }
                        low[u] = Math.Min(low[u], low[v]);
                    }
                    else
                    {
                        low[u] = Math.Min(low[u], dfsNum[v]);
                    }
                }
                return Tuple.Create(childCount, isRelevantSubtree);
            }
            */

            // Find starting point
            for (int i = 0; i < indices; i++)
            {
                if (!walkable[i]) {
                    continue;
                }

                if (relevant != null && !relevant[i]) {
                    continue;
                }

                // Already visited
                if (dfsNum[i] != 0) {
                    continue;
                }

                int childCount = cutVertex(i);
                if(relevant != null)
                {
                    // Relevant points are always articulation points
                    isArticulation[i] = true;
                    // There can only be a single relevant component, so can stop
                    break;
                }

                // The root of the tree is an exception to CutVertex's calculations
                // It's an articulation point if it has multiple children
                // as removing it would give multiple subtrees.
                isArticulation[i] = childCount > 1;
            }

            // Check we've visited every relevant point.
            // If not, there's no way to satisfy the constraint.
            if (relevant != null)
            {
                for (int i = 0; i < indices; i++)
                {
                    if (relevant[i] && dfsNum[i] == 0)
                    {
                        return null;
                    }
                }
            }

            return isArticulation;
        }


        public class SimpleGraph
        {
            public int NodeCount { get; set; }

            public int[][] Neighbours { get; set; }
        }
    }
}
