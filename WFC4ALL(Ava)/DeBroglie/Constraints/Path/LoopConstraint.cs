using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Constraints.Path; 

/// <summary>
///     Enforces that the entire path is made out of loops,
///     i.e. there are at least two routes between any two connected points.
/// </summary>
public class LoopConstraint : ITileConstraint {
    private IPathView pathView;

    public IPathSpec PathSpec { get; set; }

    public void Init(TilePropagator propagator) {
        if (PathSpec is PathSpec pathSpec) {
            // Convert PathSpec to EdgedPathSpec
            // As we have a bug with PathSpec ignoring paths of length 2.
            // (probably should use bridge edges instead of articulation points)
            ISet<Direction> allDirections
                = new HashSet<Direction>(Enumerable.Range(0, propagator.Topology.DirectionsCount).Cast<Direction>());
            EdgedPathSpec edgedPathSpec = new() {
                Exits = pathSpec.Tiles.ToDictionary(x => x, _ => allDirections),
                RelevantCells = pathSpec.RelevantCells,
                RelevantTiles = pathSpec.RelevantTiles,
                TileRotation = pathSpec.TileRotation
            };
            pathView = edgedPathSpec.MakeView(propagator);
        } else {
            pathView = PathSpec.MakeView(propagator);
        }

        pathView.Init();
    }

    public void Check(TilePropagator propagator) {
        pathView.Update();

        PathConstraintUtils.AtrticulationPointsInfo info
            = PathConstraintUtils.GetArticulationPoints(pathView.Graph, pathView.CouldBePath, pathView.MustBeRelevant);
        bool[] isArticulation = info.IsArticulation;

        for (int i = 0; i < pathView.Graph.NodeCount; i++) {
            if (isArticulation[i]) {
                propagator.SetContradiction("Loop constraint found articulation point.", this);
                return;
            }
        }
    }
}