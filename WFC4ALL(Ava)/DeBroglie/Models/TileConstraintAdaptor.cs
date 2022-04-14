using WFC4ALL.DeBroglie.Constraints;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Models; 

internal class TileConstraintAdaptor : IWaveConstraint {
    private readonly TilePropagator propagator;
    private readonly ITileConstraint underlying;

    public TileConstraintAdaptor(ITileConstraint underlying, TilePropagator propagator) {
        this.underlying = underlying;
        this.propagator = propagator;
    }

    public void Check(WavePropagator wavePropagator) {
        underlying.Check(propagator);
    }

    public void Init(WavePropagator wavePropagator) {
        underlying.Init(propagator);
    }
}