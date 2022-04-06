using WFC4ALL.DeBroglie.Constraints;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Models
{
    internal class TileConstraintAdaptor : IWaveConstraint
    {
        private readonly ITileConstraint underlying;
        private readonly TilePropagator propagator;

        public TileConstraintAdaptor(ITileConstraint underlying, TilePropagator propagator)
        {
            this.underlying = underlying;
            this.propagator = propagator;
        }

        public void check(WavePropagator wavePropagator)
        {
            underlying.check(propagator);
        }

        public void init(WavePropagator wavePropagator)
        {
            underlying.init(propagator);
        }
    }
}
