using System;
using System.Collections.Generic;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;

namespace WFC4All.DeBroglie.Constraints
{
    /// <summary>
    /// The MaxConsecutiveConstraint checks that no more than the specified amount of tiles can be placed
    /// in a row along the given axes.
    /// </summary>
    public class MaxConsecutiveConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileSet;

        private SelectedTracker selectedTracker;

        public ISet<Tile> Tiles { get; set; }

        public int MaxCount { get; set; }

        public ISet<Axis> Axes { get; set; }

        public void init(TilePropagator propagator)
        {
            GridTopology topology = propagator.Topology as GridTopology;
            if(topology == null ||
                topology.Directions.Type != DirectionSetType.CARTESIAN2D &&
                topology.Directions.Type != DirectionSetType.CARTESIAN3D)
            {
                // This wouldn't be that hard to fix
                throw new Exception("MaxConsecutiveConstraint only supports cartesian topologies.");
            }
            tileSet = propagator.createTileSet(Tiles);
            selectedTracker = propagator.createSelectedTracker(tileSet);
        }

        public void check(TilePropagator propagator)
        {
            GridTopology topology = propagator.Topology.asGridTopology();
            int width = topology.Width;
            int height = topology.Height;
            int depth = topology.Depth;

            if (Axes == null || Axes.Contains(Axis.X))
            {
                int y = 0, z = 0;
                StateMachine sm = new(x => propagator.ban(x, y, z, tileSet), topology.PeriodicX, width, MaxCount);

                for (z = 0; z < depth; z++)
                {
                    for (y = 0; y < height; y++)
                    {
                        sm.reset();
                        for (int x = 0; x < width; x++)
                        {
                            int index = topology.getIndex(x, y, z);
                            if (sm.next(x, selectedTracker.getTristate(index)))
                            {
                                propagator.setContradiction();
                                return;
                            }
                        }
                        if (topology.PeriodicX)
                        {
                            for (int x = 0; x < MaxCount && x < width; x++)
                            {
                                int index = topology.getIndex(x, y, z);
                                if (sm.next(x, selectedTracker.getTristate(index)))
                                {
                                    propagator.setContradiction();
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // Same thing as XAxis, just swizzled
            if (Axes == null || Axes.Contains(Axis.Y))
            {
                int x = 0, z = 0;
                StateMachine sm = new(y => propagator.ban(x, y, z, tileSet), topology.PeriodicY, height, MaxCount);

                for (z = 0; z < depth; z++)
                {
                    for (x = 0; x < width; x++)
                    {
                        sm.reset();
                        for (int y = 0; y < height; y++)
                        {
                            int index = topology.getIndex(x, y, z);
                            if (sm.next(y, selectedTracker.getTristate(index)))
                            {
                                propagator.setContradiction();
                                return;
                            }
                        }
                        if (topology.PeriodicY)
                        {
                            for (int y = 0; y < MaxCount && y < height; y++)
                            {
                                int index = topology.getIndex(x, y, z);
                                if (sm.next(y, selectedTracker.getTristate(index)))
                                {
                                    propagator.setContradiction();
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // Same thing as XAxis, just swizzled
            if (Axes == null || Axes.Contains(Axis.Z))
            {
                int x = 0, y = 0;
                StateMachine sm = new(z => propagator.ban(x, y, z, tileSet), topology.PeriodicZ, depth, MaxCount);

                for (y = 0; y < height; y++)
                {
                    for (x = 0; x < width; x++)
                    {
                        sm.reset();
                        for (int z = 0; z < depth; z++)
                        {
                            int index = topology.getIndex(x, y, z);
                            if (sm.next(z, selectedTracker.getTristate(index)))
                            {
                                propagator.setContradiction();
                                return;
                            }
                        }
                        if (topology.PeriodicZ)
                        {
                            for (int z = 0; z < MaxCount && z < depth; z++)
                            {
                                int index = topology.getIndex(x, y, z);
                                if (sm.next(z, selectedTracker.getTristate(index)))
                                {
                                    propagator.setContradiction();
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Internal for testing
        // This class is a bit fiddly, but esentially it looks at at every tile
        // along an axis on-line, and tracks enough information to emit bans stopping the constraint
        // from being violated. It also returns false if the constraint is already violated.
        // There's two cases to consider:
        // 1) A run of contiguous selected tiles of length max. 
        //    Then we want to ban the tiles at either end.
        // 2) Two runs of selected with a total length of at least max-1, separated by a single tile. 
        //    Then we want to ban the center tile.
        // For periodic topologies after running over an axis, the first max tiles need a second iteration
        // to cover all looping cases.
        internal struct StateMachine
        {
            private readonly Action<int> banAt;
            private readonly bool periodic;
            private readonly int indexCount;
            private readonly int max;
            private State state;
            private int runCount;
            private int runStartIndex;
            private int prevRunCount;

            public StateMachine(Action<int> banAt, bool periodic, int indexCount, int max)
            {
                this.banAt = banAt;
                this.periodic = periodic;
                this.indexCount = indexCount;
                this.max = max;
                state = State.INITIAL;
                runCount = 0;
                runStartIndex = 0;
                prevRunCount = 0;
            }

            public void reset()
            {
                state = State.INITIAL;
                runCount = 0;
                runStartIndex = 0;
                prevRunCount = 0;
            }

            public bool next(int index, Tristate selected)
            {
                switch (state)
                {
                    case State.INITIAL:
                        if (selected.isYes())
                        {
                            state = State.IN_RUN;
                            runCount = 1;
                            runStartIndex = index;
                        }
                        return false;
                    case State.JUST_AFTER_RUN:
                        if (selected.isYes())
                        {
                            state = State.IN_RUN;
                            runCount = 1;
                            runStartIndex = index;
                            goto checkCases;
                        }
                        else
                        {
                            state = State.INITIAL;
                            prevRunCount = 0;
                            runCount = 0;
                        }
                        return false;
                    case State.IN_RUN:
                        if(selected.isYes())
                        {
                            state = State.IN_RUN;
                            runCount += 1;
                            if(runCount > max)
                            {
                                // Immediate contradiction
                                return true;
                            }
                            goto checkCases;
                        }
                        else
                        {
                            // Also case 1.
                            if (runCount == max)
                            {
                                if (selected.possible())
                                {
                                    banAt(index);
                                }
                            }
                            state = State.JUST_AFTER_RUN;
                            prevRunCount = runCount;
                            runCount = 0;
                        }
                        return false;
                }
                // Unreachable
                throw new Exception("Unreachable");
                checkCases:
                    // Have we entered case 1 or 2?
                    if (prevRunCount + runCount == max)
                    {
                        // Ban on the previous end of the run
                        if (runStartIndex == 0)
                        {
                            if (periodic)
                            {
                                banAt(indexCount - 1);
                            }
                        }
                        else
                        {
                            banAt(runStartIndex - 1);
                        }
                    }
                return false;
            }

            enum State
            {
                INITIAL,
                IN_RUN,
                JUST_AFTER_RUN,
            }
        }
    }
}
