namespace WFC4All.DeBroglie.Wfc
{
    internal interface IPickHeuristic
    {
        // Returns -1/-1 if all cells are decided
        void pickObservation(out int index, out int pattern);
    }
}
