namespace miWFC.DeBroglie.Wfc;

/// <summary>
///     A variant of ITileConstraint that works on patterns instead of tiles.
/// </summary>
public interface IWaveConstraint {
    void Init(WavePropagator wavePropagator);

    void Check(WavePropagator wavePropagator);
}