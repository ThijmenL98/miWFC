namespace miWFC.DeBroglie.Constraints.Path; 

public interface IPathSpec {
    IPathView MakeView(TilePropagator tilePropagator);
}