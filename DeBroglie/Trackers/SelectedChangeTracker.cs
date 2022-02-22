using System.Collections.Generic;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie.Trackers
{
    internal interface ITristateChanged
    {
        void reset(SelectedChangeTracker tracker);

        void notify(int index, Tristate before, Tristate after);
    }

    internal class SelectedChangeTracker : ITracker
    {
        private readonly TilePropagator tilePropagator;

        private readonly WavePropagator wavePropagator;

        private readonly TileModelMapping tileModelMapping;

        // Indexed by tile topology
        private readonly int[] patternCounts;

        private readonly Tristate[] values;

        private readonly TilePropagatorTileSet tileSet;

        private readonly ITristateChanged onChange;

        public SelectedChangeTracker(TilePropagator tilePropagator, WavePropagator wavePropagator, TileModelMapping tileModelMapping, TilePropagatorTileSet tileSet, ITristateChanged onChange)
        {
            this.tilePropagator = tilePropagator;
            this.wavePropagator = wavePropagator;
            this.tileModelMapping = tileModelMapping;
            this.tileSet = tileSet;
            this.onChange = onChange;
            patternCounts = new int[tilePropagator.Topology.IndexCount];
            values = new Tristate[tilePropagator.Topology.IndexCount];
        }

        private Tristate getTristateInner(int index)
        {
            int selectedPatternCount = patternCounts[index];
            if (selectedPatternCount == 0) {
                return Tristate.NO;
            }

            tileModelMapping.getTileCoordToPatternCoord(index, out int patternIndex, out int offset);

            int totalPatternCount = wavePropagator.Wave.getPatternCount(patternIndex);
            if (totalPatternCount == selectedPatternCount)
            {
                return Tristate.YES;
            }
            return Tristate.MAYBE;
        }

        public Tristate getTristate(int index)
        {
            return values[index];
        }

        public bool isSelected(int index)
        {
            return getTristate(index).isYes();
        }

        public void doBan(int patternIndex, int pattern)
        {
            if(tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null)
            {
                doBan(patternIndex, pattern, patternIndex, 0);
            }
            else
            {
                foreach ((Point p, int index, int offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.get(patternIndex))
                {
                    doBan(patternIndex, pattern, index, offset);
                }
            }
        }

        private void doBan(int patternIndex, int pattern, int index, int offset)
        {
            ISet<int> patterns = tileModelMapping.getPatterns(tileSet, offset);
            if (patterns.Contains(pattern))
            {
                patternCounts[index] -= 1;
            }
            doNotify(index);
        }

        public void reset()
        {
            Wave wave = wavePropagator.Wave;
            foreach(int index in tilePropagator.Topology.getIndices())
            {
                tileModelMapping.getTileCoordToPatternCoord(index, out int patternIndex, out int offset);
                ISet<int> patterns = tileModelMapping.getPatterns(tileSet, offset);
                int count = 0;
                foreach (int p in patterns)
                {
                    if(patterns.Contains(p) && wave.get(patternIndex, p))
                    {
                        count++;
                    }
                }
                patternCounts[index] = count;
                values[index] = getTristateInner(index);
            }
            onChange.reset(this);
        }


        public void undoBan(int patternIndex, int pattern)
        {
            if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null)
            {
                undoBan(patternIndex, pattern, patternIndex, 0);
            }
            else
            {
                foreach ((Point p, int index, int offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.get(patternIndex))
                {
                    undoBan(patternIndex, pattern, index, offset);
                }
            }
        }

        private void undoBan(int patternIndex, int pattern, int index, int offset)
        {
            ISet<int> patterns = tileModelMapping.getPatterns(tileSet, offset);
            if (patterns.Contains(pattern))
            {
                patternCounts[index] += 1;
            }
            doNotify(index);
        }

        private void doNotify(int index)
        {
            Tristate newValue = getTristateInner(index);
            Tristate oldValue = values[index];
            if (newValue != oldValue)
            {
                values[index] = newValue;
                onChange.notify(index, oldValue, newValue);
            }
        }
    }
}
