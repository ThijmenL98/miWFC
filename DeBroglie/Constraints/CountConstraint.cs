using System.Collections.Generic;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Constraints
{
    public enum CountComparison
    {
        AT_LEAST,
        AT_MOST,
        EXACTLY
    }

    /// <summary>
    /// Enforces that the global count of tiles within a given set
    /// must be at most/least/equal to a given count
    /// </summary>
    public class CountConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileSet;

        private SelectedChangeTracker selectedChangeTracker;

        private CountTracker countTracker;

        /// <summary>
        /// The set of tiles to count
        /// </summary>
        public ISet<Tile> Tiles { get; set; }

        /// <summary>
        /// How to compare the count of <see cref="Tiles"/> to <see cref="Count"/>.
        /// </summary>
        public CountComparison Comparison { get; set; }

        /// <summary>
        /// The count to be compared against.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// If set, this constraint will attempt to pick tiles as early as possible.
        /// This can give a better random distribution, but higher chance of contradictions.
        /// </summary>
        public bool Eager { get; set; }

        public void check(TilePropagator propagator)
        {
            ITopology topology = propagator.Topology;
            int width = topology.Width;
            int height = topology.Height;
            int depth = topology.Depth;

            int yesCount = countTracker.YesCount;
            int noCount = countTracker.NoCount;
            int maybeCount = countTracker.MaybeCount;

            if (Comparison == CountComparison.AT_MOST || Comparison == CountComparison.EXACTLY)
            {
                if (yesCount > Count)
                {
                    // Already got too many, just fail
                    propagator.setContradiction();
                    return;
                }
                if (yesCount == Count && maybeCount > 0)
                {
                    // We've reached the limit, ban any more
                    foreach (int index in topology.getIndices())
                    {
                        Tristate selected = selectedChangeTracker.getTristate(index);
                        if (selected.isMaybe())
                        {
                            propagator.Topology.getCoord(index, out int x, out int y, out int z);
                            propagator.ban(x, y, z, tileSet);
                        }
                    }
                }
            }
            if (Comparison == CountComparison.AT_LEAST || Comparison == CountComparison.EXACTLY)
            {
                if (yesCount + maybeCount < Count)
                {
                    // Already got too few, just fail
                    propagator.setContradiction();
                    return;
                }
                if (yesCount + maybeCount == Count && maybeCount > 0)
                {
                    // We've reached the limit, select all the rest
                    foreach (int index in topology.getIndices())
                    {
                        Tristate selected = selectedChangeTracker.getTristate(index);
                        if (selected.isMaybe())
                        {
                            propagator.Topology.getCoord(index, out int x, out int y, out int z);
                            propagator.@select(x, y, z, tileSet);
                        }
                    }
                }
            }
        }

        public void init(TilePropagator propagator)
        {
            tileSet = propagator.createTileSet(Tiles);

            countTracker = new CountTracker(propagator.Topology);

            selectedChangeTracker = propagator.createSelectedChangeTracker(tileSet, countTracker);

            if (Eager)
            {
                // Naive implementation
                /*
                // Pick Count random indices
                var topology = propagator.Topology;
                var pickedIndices = new List<int>();
                var remainingIndices = new List<int>(topology.Indicies);
                for (var c = 0; c < Count; c++)
                {
                    var pickedIndexIndex = (int)(propagator.RandomDouble() * remainingIndices.Count);
                    pickedIndices.Add(remainingIndices[pickedIndexIndex]);
                    remainingIndices[pickedIndexIndex] = remainingIndices[remainingIndices.Count - 1];
                    remainingIndices.RemoveAt(remainingIndices.Count - 1);
                }
                // Ban or select tiles to ensure an appropriate count
                if(Comparison == CountComparison.AtMost || Comparison == CountComparison.Exactly)
                {
                    foreach (var i in remainingIndices)
                    {
                        topology.GetCoord(i, out var x, out var y, out var z);
                        propagator.Ban(x, y, z, tileSet);
                    }
                }
                if (Comparison == CountComparison.AtLeast || Comparison == CountComparison.Exactly)
                {
                    foreach (var i in pickedIndices)
                    {
                        topology.GetCoord(i, out var x, out var y, out var z);
                        propagator.Select(x, y, z, tileSet);
                    }
                }
                */

                ITopology topology = propagator.Topology;
                int width = topology.Width;
                int height = topology.Height;
                int depth = topology.Depth;
                List<int> pickedIndices = new List<int>();
                List<int> remainingIndices = new List<int>(topology.getIndices());

                while (true)
                {
                    int noCount = 0;
                    int yesCount = 0;
                    List<int> maybeList = new List<int>();
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int index = topology.getIndex(x, y, z);
                                if (topology.containsIndex(index))
                                {
                                    Tristate selected = propagator.getSelectedTristate(x, y, z, tileSet);
                                    if (selected.isNo()) {
                                        noCount++;
                                    }

                                    if (selected.isMaybe()) {
                                        maybeList.Add(index);
                                    }

                                    if (selected.isYes()) {
                                        yesCount++;
                                    }
                                }
                            }
                        }
                    }
                    int maybeCount = maybeList.Count;

                    if (Comparison == CountComparison.AT_MOST)
                    {
                        if (yesCount > Count)
                        {
                            // Already got too many, just fail
                            propagator.setContradiction();
                            return;
                        }
                        if (yesCount == Count)
                        {
                            // We've reached the limit, ban any more and exit
                            check(propagator);
                            return;
                        }
                        int pickedIndex = maybeList[(int)(propagator.RandomDouble() * maybeList.Count)];
                        topology.getCoord(pickedIndex, out int x, out int y, out int z);
                        propagator.@select(x, y, z, tileSet);
                    }
                    else if (Comparison == CountComparison.AT_LEAST || Comparison == CountComparison.EXACTLY)
                    {
                        if (yesCount + maybeCount < Count)
                        {
                            // Already got too few, just fail
                            propagator.setContradiction();
                            return;
                        }
                        if (yesCount + maybeCount == Count)
                        {

                            // We've reached the limit, ban any more and exit
                            check(propagator);
                            return;
                        }
                        int pickedIndex = maybeList[(int)(propagator.RandomDouble() * maybeList.Count)];
                        topology.getCoord(pickedIndex, out int x, out int y, out int z);
                        propagator.ban(x, y, z, tileSet);
                    }
                }
            }
        }

        private class CountTracker : ITristateChanged
        {
            private readonly ITopology topology;

            public CountTracker(ITopology topology)
            {
                this.topology = topology;
            }

            public int NoCount { get; set; }
            public int YesCount { get; set; }
            public int MaybeCount { get; set; }

            public void reset(SelectedChangeTracker tracker)
            {
                NoCount = 0;
                YesCount = 0;
                MaybeCount = 0;
                foreach (int index in topology.getIndices())
                {
                    Tristate selected = tracker.getTristate(index);
                    switch (selected)
                    {
                        case Tristate.NO: NoCount++; break;
                        case Tristate.MAYBE: MaybeCount++; break;
                        case Tristate.YES: YesCount++; break;
                    }
                }
            }

            public void notify(int index, Tristate before, Tristate after)
            {
                switch(before)
                {
                    case Tristate.NO: NoCount--; break;
                    case Tristate.MAYBE: MaybeCount--; break;
                    case Tristate.YES: YesCount--; break;
                }
                switch (after)
                {
                    case Tristate.NO: NoCount++; break;
                    case Tristate.MAYBE: MaybeCount++; break;
                    case Tristate.YES: YesCount++; break;
                }
            }
        }

    }
}
