using System.Collections.Generic;

namespace WFC4ALL.DeBroglie.Constraints.Path; 

/// <summary>
///     Enforces that there are no loops at all
/// </summary>
public class AcyclicConstraint : ITileConstraint {
    private IPathView pathView;

    public IPathSpec PathSpec { get; set; }

    public void Init(TilePropagator propagator) {
        pathView = PathSpec.MakeView(propagator);
        pathView.Init();
    }

    public void Check(TilePropagator propagator) {
        pathView.Update();

        PathConstraintUtils.SimpleGraph graph = pathView.Graph;
        bool[] mustBePath = pathView.MustBePath;
        // TODO: Support relevant?
        bool[] visited = new bool[graph.NodeCount];
        for (int i = 0; i < graph.NodeCount; i++) {
            if (!mustBePath[i]) {
                continue;
            }

            if (visited[i]) {
                continue;
            }

            // Start DFS
            Stack<(int, int)> stack = new();
            stack.Push((-1, i));
            while (stack.Count > 0) {
                (int prev, int u) = stack.Pop();
                if (visited[u]) {
                    propagator.SetContradiction("Acyclic constraint found cycle", this);
                    return;
                }

                visited[u] = true;
                foreach (int v in graph.Neighbours[u]) {
                    if (!mustBePath[v]) {
                        continue;
                    }

                    if (v == prev) {
                        continue;
                    }

                    stack.Push((u, v));
                }
            }
        }
    }
}