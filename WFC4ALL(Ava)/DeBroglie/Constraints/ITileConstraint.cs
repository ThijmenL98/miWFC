namespace WFC4ALL.DeBroglie.Constraints
{
    /// <summary>
    /// Interface for specifying non-local constraints to be respected during generation.
    /// </summary>
    public interface ITileConstraint
    {
        /// <summary>
        /// Called once when the propagator first initializes.
        /// </summary>
        /// <param name="propagator">The propagator to constrain</param>
        void init(TilePropagator propagator);

        /// <summary>
        /// Called frequently during generation to help maintain the constraint.
        /// </summary>
        /// <param name="propagator">The propagator to constrain</param>
        void check(TilePropagator propagator);
    }
}
