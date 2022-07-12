namespace miWFC.DeBroglie.Trackers;

/// <summary>
///     Trackers are objects that maintain state that is a summary of the current state of the propagator.
///     By updating that state as the propagator changes, they can give a significant performance benefit
///     over calculating the value from scratch each time it is needed.
/// </summary>
public interface ITracker {
    void Reset();

    void DoBan(int index, int pattern);

    void UndoBan(int index, int pattern);
}

/// <summary>
///     Callback for when choices/backtracks occur on WavePropagator
/// </summary>
public interface IChoiceObserver {
    // Called before the wave propagator is updated for the choice
    void MakeChoice(int index, int pattern);

    // Called after the wave propagator is backtracked
    void Backtrack();
}