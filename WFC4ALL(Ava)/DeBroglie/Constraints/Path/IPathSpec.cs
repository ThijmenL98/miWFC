namespace WFC4ALL.DeBroglie.Constraints.Path; 

public interface IPathSpec {
    IPathView MakeView(TilePropagator tilePropagator);
}