namespace WFC4All.DeBroglie.Trackers
{
    /// <summary>
    /// Trackers are objects that maintain state that is a summary of the current state of the propagator.
    /// By updating that state as the propagator changes, they can give a significant performance benefit
    /// over calculating the value from scratch each time it is needed.
    /// </summary>
    internal interface ITracker
    {
        void reset();

        void doBan(int index, int pattern);

        void undoBan(int index, int pattern);
    }
}
