namespace WFC4ALL.DeBroglie.Wfc
{
    /// <summary>
    /// A variant of ITileConstraint that works on patterns instead of tiles.
    /// </summary>
    internal interface IWaveConstraint
    {
        void init(WavePropagator wavePropagator);

        void check(WavePropagator wavePropagator);
    }
}
