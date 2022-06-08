namespace miWFC.DeBroglie; 

/// <summary>
///     Represents an uncertain boolean.
/// </summary>
public enum Quadstate {
    CONTRADICTION = -2,
    NO = -1,
    MAYBE = 0,
    YES = 1
}

public static class QuadstateExtensions {
    public static bool IsYes(this Quadstate v) {
        return v == Quadstate.YES;
    }

    public static bool IsMaybe(this Quadstate v) {
        return v == Quadstate.MAYBE;
    }

    public static bool IsNo(this Quadstate v) {
        return v == Quadstate.NO;
    }

    public static bool IsContradiction(this Quadstate v) {
        return v == Quadstate.CONTRADICTION;
    }

    public static bool Possible(this Quadstate v) {
        return (int) v >= 0;
    }
}