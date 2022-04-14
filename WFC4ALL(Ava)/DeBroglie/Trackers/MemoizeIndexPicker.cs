using System;
using WFC4ALL.DeBroglie.Wfc;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class MemoizeIndexPicker : IIndexPicker, IChoiceObserver {
    private readonly IIndexPicker underlying;
    private Deque<int> futureChoices;
    private Deque<int> prevChoices;

    public MemoizeIndexPicker(IIndexPicker underlying) {
        this.underlying = underlying;
    }

    public void Backtrack() {
        futureChoices.Shift(prevChoices.Pop());
    }

    public void MakeChoice(int index, int pattern) {
        prevChoices.Push(index);
    }

    public void Init(WavePropagator wavePropagator) {
        wavePropagator.AddChoiceObserver(this);
        futureChoices = new Deque<int>();
        prevChoices = new Deque<int>();
    }

    public int GetRandomIndex(Func<double> randomDouble) {
        if (futureChoices.Count > 0) {
            return futureChoices.Unshift();
        }

        return underlying.GetRandomIndex(randomDouble);
    }
}