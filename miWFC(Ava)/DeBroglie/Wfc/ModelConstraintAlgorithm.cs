namespace miWFC.DeBroglie.Wfc; 

public enum ModelConstraintAlgorithm {
    /// <summary>
    ///     Equivalent to Ac4 currently.
    /// </summary>
    DEFAULT,

    /// <summary>
    ///     Use the Arc Consistency 4 algorithm.
    /// </summary>
    AC4,

    /// <summary>
    ///     Use the Arc Consistency 3 algorithm.
    /// </summary>
    AC3,

    /// <summary>
    ///     Only update tiles immediately adjacent to updated tiles.
    /// </summary>
    ONE_STEP
}