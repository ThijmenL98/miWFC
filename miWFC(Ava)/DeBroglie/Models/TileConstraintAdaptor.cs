using miWFC.DeBroglie.Constraints;
using miWFC.DeBroglie.Wfc;

namespace miWFC.DeBroglie.Models;

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