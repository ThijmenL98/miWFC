using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie.Trackers
{
    internal class SelectedTracker : ITracker
    {
        private readonly TilePropagator tilePropagator;

        private readonly WavePropagator wavePropagator;

        private readonly TileModelMapping tileModelMapping;

        // Indexed by tile topology
        private readonly int[] patternCounts;

        private readonly TilePropagatorTileSet tileSet;

        public SelectedTracker(TilePropagator tilePropagator, WavePropagator wavePropagator, TileModelMapping tileModelMapping, TilePropagatorTileSet tileSet)
        {
            this.tilePropagator = tilePropagator;
            this.wavePropagator = wavePropagator;
            this.tileModelMapping = tileModelMapping;
            this.tileSet = tileSet;
            patternCounts = new int[tilePropagator.Topology.IndexCount];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tristate getTristate(int index)
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
            }
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
        }
    }
}
