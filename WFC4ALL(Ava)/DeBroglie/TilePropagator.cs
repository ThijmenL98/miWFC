using System;
using System.Collections.Generic;
using System.Linq;
using WFC4All.DeBroglie.Constraints;
using WFC4All.DeBroglie.Models;
using WFC4All.DeBroglie.Topo;
using WFC4All.DeBroglie.Trackers;
using WFC4All.DeBroglie.Wfc;

namespace WFC4All.DeBroglie
{
    public class PriorityAndWeight
    {
        public int Priority { get; set; }
        public double Weight { get; set; }
    }

    public class TilePropagatorOptions
    {
        /// <summary>
        /// Maximum number of steps to backtrack.
        /// Set to 0 to disable backtracking, and -1 for indefinite amounts of backtracking.
        /// </summary>
        public int BackTrackDepth { get; set; }

        /// <summary>
        /// Extra constraints to control the generation process.
        /// </summary>
        public ITileConstraint[] Constraints { get; set; }

        /// <summary>
        /// Overrides the weights set from the model, on a per-position basis.
        /// </summary>
        public ITopoArray<IDictionary<Tile, PriorityAndWeight>> Weights { get; set; }

        /// <summary>
        /// Source of randomness used by generation
        /// </summary>
        public Func<double> RandomDouble { get; set; }

        [Obsolete("Use RandomDouble")]
        public Random Random { get; set; }
    }

    // Implemenation wise, this wraps a WavePropagator to do the majority of the work.
    // The only thing this class handles is conversion of tile objects into sets of patterns
    // And co-ordinate conversion.
    /// <summary>
    /// TilePropagator is the main entrypoint to the DeBroglie library. 
    /// It takes a <see cref="TileModel"/> and an output <see cref="Topology"/> and generates
    /// an output array using those parameters.
    /// </summary>
    public class TilePropagator
    {
        private readonly WavePropagator wavePropagator;

        private readonly ITopology topology;

        private readonly TileModel tileModel;

        private readonly TileModelMapping tileModelMapping;

        /// <summary>
        /// Constructs a TilePropagator.
        /// </summary>
        /// <param name="tileModel">The model to guide the generation.</param>
        /// <param name="topology">The dimensions of the output to generate</param>
        /// <param name="backtrack">If true, store additional information to allow rolling back choices that lead to a contradiction.</param>
        /// <param name="constraints">Extra constraints to control the generation process.</param>
        /// <param name="selectionHeuristic">Currently selected selection heuristic</param>
        /// <param name="patternHeuristic">Currently selected pattern heuristic</param>
        public TilePropagator(TileModel tileModel, ITopology topology,
            bool backtrack = false, ITileConstraint[] constraints = null)
            : this(tileModel, topology, new TilePropagatorOptions
            {
                BackTrackDepth = backtrack ? -1 : 0,
                Constraints = constraints,
            })
        {

        }

        /// <summary>
        /// Constructs a TilePropagator.
        /// </summary>
        /// <param name="tileModel">The model to guide the generation.</param>
        /// <param name="topology">The dimensions of the output to generate</param>
        /// <param name="backtrack">If true, store additional information to allow rolling back choices that lead to a contradiction.</param>
        /// <param name="constraints">Extra constraints to control the generation process.</param>
        /// <param name="random">Source of randomness</param>
        [Obsolete("Use TilePropagatorOptions")]
        public TilePropagator(TileModel tileModel, ITopology topology, bool backtrack,
            ITileConstraint[] constraints, Random random)
            :this(tileModel, topology, new TilePropagatorOptions
            {
                BackTrackDepth = backtrack ? -1 : 0,
                Constraints = constraints,
                Random = random,
            })
        {

        }

        public TilePropagator(TileModel tileModel, ITopology topology, TilePropagatorOptions options)
        {
            this.tileModel = tileModel;
            this.topology = topology;

            OverlappingModel overlapping = tileModel as OverlappingModel;

            tileModelMapping = tileModel.getTileModelMapping(topology);
            ITopology patternTopology = tileModelMapping.PatternTopology;
            PatternModel patternModel = tileModelMapping.PatternModel;

            IWaveConstraint[] waveConstraints =
                (options.Constraints?.Select(x => new TileConstraintAdaptor(x, this)).ToArray() ?? Enumerable.Empty<IWaveConstraint>())
                .ToArray();

            FrequencySet[] waveFrequencySets = options.Weights == null ? null : getFrequencySets(options.Weights, tileModelMapping);

#pragma warning disable CS0618 // Type or member is obsolete
            wavePropagator = new WavePropagator(
                patternModel, 
                patternTopology,
                topology.Width,
                topology.Height, 
                options.BackTrackDepth, 
                waveConstraints, 
                options.RandomDouble ?? (options.Random == null ? null : options.Random.NextDouble),
                waveFrequencySets,
                clear: false);
#pragma warning restore CS0618 // Type or member is obsolete
            wavePropagator.clear();

        }

        private void tileCoordToPatternCoord(int x, int y, int z, out int px, out int py, out int pz, out int offset)
        {
            tileModelMapping.getTileCoordToPatternCoord(x, y, z, out px, out py, out pz, out offset);
        }

        private static FrequencySet[] getFrequencySets(ITopoArray<IDictionary<Tile, PriorityAndWeight>> weights, TileModelMapping tileModelMapping)
        {
            FrequencySet[] frequencies = new FrequencySet[tileModelMapping.PatternTopology.IndexCount];
            foreach(int patternIndex in tileModelMapping.PatternTopology.getIndices())
            {
                // TOODO
                if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset != null) {
                    throw new NotImplementedException();
                }

                int tileIndex = patternIndex;
                int offset = 0;
                IDictionary<Tile, PriorityAndWeight> weightDict = weights.get(tileIndex);
                double[] newWeights = new double[tileModelMapping.PatternModel.PatternCount];
                int[] newPriorities = new int[tileModelMapping.PatternModel.PatternCount];
                foreach(KeyValuePair<Tile, PriorityAndWeight> kv in weightDict)
                {
                    int pattern = tileModelMapping.TilesToPatternsByOffset[offset][kv.Key].Single();
                    newWeights[pattern] = kv.Value.Weight;
                    newPriorities[pattern] = kv.Value.Priority;
                }
                frequencies[patternIndex] = new FrequencySet(newWeights, newPriorities);
            }
            return frequencies;
        }

        /// <summary>
        /// The topology of the output.
        /// </summary>
        public ITopology Topology => topology;

        /// <summary>
        /// The source of randomness
        /// </summary>
        public Func<double> RandomDouble => wavePropagator.RandomDouble;


        /// <summary>
        /// The model to use when generating.
        /// </summary>
        public TileModel TileModel => tileModel;

        /// <summary>
        /// The overall resolution of the generated array.
        /// This will be <see cref="Resolution.CONTRADICTION"/> if at least one location is in contradiction (has no possible tiles)
        /// otherwilse will be <see cref="Resolution.UNDECIDED"/> if at least one location is undecided (has multiple possible tiles)
        /// and will be <see cref="Resolution.DECIDED"/> otherwise (exactly one tile valid for each location).
        /// </summary>
        public Resolution Status => wavePropagator.Status;

        /// <summary>
        /// This is incremented each time it is necessary to backtrack
        /// a tile while generating results.
        /// It is reset when <see cref="clear"/> is called.
        /// </summary>
        public int BacktrackCount => wavePropagator.BacktrackCount;

        /// <summary>
        /// Returns a number between 0 and 1 indicating how much of the generation is complete.
        /// This number may decrease at times due to backtracking.
        /// </summary>
        /// <returns></returns>
        public double getProgress()
        {
            return wavePropagator.Wave.getProgress();
        }

        /// <summary>
        /// Resets the TilePropagator to the state it was in at construction.
        /// </summary>
        /// <returns>The current <see cref="Status"/> (usually <see cref="Resolution.UNDECIDED"/> unless there are very specific initial conditions)</returns>
        public Resolution clear()
        {
            return wavePropagator.clear();
        }

        /// <summary>
        /// Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
        /// </summary>
        public void setContradiction()
        {
            wavePropagator.setContradiction();
        }

        /// <summary>
        /// Marks the given tile as not being a valid choice at a given location.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution ban(int x, int y, int z, Tile tile)
        {
            tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
            ISet<int> patterns = tileModelMapping.getPatterns(tile, o);
            foreach(int p in patterns)
            {
                Resolution status = wavePropagator.ban(px, py, pz, p);
                if (status != Resolution.UNDECIDED) {
                    return status;
                }
            }
            return Resolution.UNDECIDED;
        }

        /// <summary>
        /// Marks the given tiles as not being a valid choice at a given location.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution ban(int x, int y, int z, IEnumerable<Tile> tiles)
        {
            return ban(x, y, z, createTileSet(tiles));
        }

        /// <summary>
        /// Marks the given tiles as not being a valid choice at a given location.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution ban(int x, int y, int z, TilePropagatorTileSet tiles)
        {
            tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
            ISet<int> patterns = tileModelMapping.getPatterns(tiles, o);
            foreach (int p in patterns)
            {
                Resolution status = wavePropagator.ban(px, py, pz, p);
                if (status != Resolution.UNDECIDED) {
                    return status;
                }
            }
            return Resolution.UNDECIDED;
        }

        /// <summary>
        /// Marks the given tile as the only valid choice at a given location.
        /// This is equivalent to banning all other tiles.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution select(int x, int y, int z, Tile tile)
        {
            tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
            ISet<int> patterns = tileModelMapping.getPatterns(tile, o);
            for (int p = 0; p < wavePropagator.PatternCount; p++)
            {
                if (patterns.Contains(p)) {
                    continue;
                }

                Resolution status = wavePropagator.ban(px, py, pz, p);
                if (status != Resolution.UNDECIDED) {
                    return status;
                }
            }
            return Resolution.UNDECIDED;
        }

        /// <summary>
        /// Marks the given tiles as the only valid choice at a given location.
        /// This is equivalent to banning all other tiles.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution select(int x, int y, int z, IEnumerable<Tile> tiles)
        {
            return select(x, y, z, createTileSet(tiles));
        }

        /// <summary>
        /// Marks the given tiles as the only valid choice at a given location.
        /// This is equivalent to banning all other tiles.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution select(int x, int y, int z, TilePropagatorTileSet tiles)
        {
            tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
            ISet<int> patterns = tileModelMapping.getPatterns(tiles, o);
            for (int p = 0; p < wavePropagator.PatternCount; p++)
            {
                if (patterns.Contains(p)) {
                    continue;
                }

                Resolution status = wavePropagator.ban(px, py, pz, p);
                if (status != Resolution.UNDECIDED) {
                    return status;
                }
            }
            return Resolution.UNDECIDED;
        }

        /// <summary>
        /// Makes a single tile selection.
        /// Then it propagates that information to other nearby tiles.
        /// If backtracking is enabled a single step can include several backtracks,.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution step()
        {
            return wavePropagator.step();
        }

        /// <summary>
        /// Repeatedly Steps until the status is Decided or Contradiction.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution run()
        {
            return wavePropagator.run();
        }

        public Resolution observe(int index, int pattern) {
            return wavePropagator.stepWith(index, pattern);
        }

        public Resolution singleProp() {
            return wavePropagator.propagateSingle();
        }

        // public Resolution ban(int x, int y, int pattern) {
        //     
        // }
        
        public Resolution doBacktrack() {
            return wavePropagator.stepBack();
        }

        internal SelectedTracker createSelectedTracker(TilePropagatorTileSet tileSet)
        {
            SelectedTracker tracker = new(this, wavePropagator, tileModelMapping, tileSet);
            tracker.reset();
            wavePropagator.addTracker(tracker);
            return tracker;
        }

        internal SelectedChangeTracker createSelectedChangeTracker(TilePropagatorTileSet tileSet, ITristateChanged onChange)
        {
            SelectedChangeTracker tracker = new(this, wavePropagator, tileModelMapping, tileSet, onChange);
            tracker.reset();
            wavePropagator.addTracker(tracker);
            return tracker;
        }

        /// <summary>
        /// Returns a tracker that indicates all recently changed tiles.
        /// This is mostly useful as a performance optimization.
        /// Trackers are valid until <see cref="clear"/> is called.
        /// </summary>
        internal ChangeTracker createChangeTracker()
        {
            ChangeTracker tracker = new(tileModelMapping);
            tracker.reset();
            wavePropagator.addTracker(tracker);
            return tracker;
        }

        /// <summary>
        /// Creates a set of tiles. This set can be used with some operations, and is marginally
        /// faster than passing in a fresh list of tiles ever time.
        /// </summary>
        public TilePropagatorTileSet createTileSet(IEnumerable<Tile> tiles)
        {
            return tileModelMapping.createTileSet(tiles);
        }

        /// <summary>
        /// Returns true if this tile is the only valid selection for a given location.
        /// </summary>
        public bool isSelected(int x, int y, int z, Tile tile)
        {
            getBannedSelected(x, y, z, tile, out bool isBanned, out bool isSelected);
            return isSelected;
        }

        /// <summary>
        /// Returns true if this tile is the not a valid selection for a given location.
        /// </summary>
        public bool isBanned(int x, int y, int z, Tile tile)
        {
            getBannedSelected(x, y, z, tile, out bool isBanned, out bool isSelected);
            return isBanned;
        }

        /// <summary>
        /// Gets the results of both IsBanned and IsSelected
        /// </summary>
        public void getBannedSelected(int x, int y, int z, Tile tile, out bool isBanned, out bool isSelected)
        {
            tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
            ISet<int> patterns = tileModelMapping.TilesToPatternsByOffset[o][tile];
            getBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
        }

        /// <summary>
        /// isBanned is set to true if all the tiles are not valid in the location.
        /// isSelected is set to true if no other the tiles are valid in the location.
        /// </summary>
        public void getBannedSelected(int x, int y, int z, IEnumerable<Tile> tiles, out bool isBanned, out bool isSelected)
        {
            getBannedSelected(x, y, z, createTileSet(tiles), out isBanned, out isSelected);
        }

        /// <summary>
        /// isBanned is set to true if all the tiles are not valid in the location.
        /// isSelected is set to true if no other the tiles are valid in the location.
        /// </summary>
        public void getBannedSelected(int x, int y, int z, TilePropagatorTileSet tiles, out bool isBanned, out bool isSelected)
        {
            tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
            ISet<int> patterns = tileModelMapping.getPatterns(tiles, o);
            getBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
        }

        internal Tristate getSelectedTristate(int x, int y, int z, TilePropagatorTileSet tiles)
        {
            getBannedSelected(x, y, z, tiles, out bool isBanned, out bool isSelected);
            return isSelected ? Tristate.YES : isBanned ? Tristate.NO : Tristate.MAYBE;
        }


        private void getBannedSelectedInternal(int px, int py, int pz, ISet<int> patterns, out bool isBanned, out bool isSelected)
        {
            int index = wavePropagator.Topology.getIndex(px, py, pz);
            Wave wave = wavePropagator.Wave;
            int patternCount = wavePropagator.PatternCount;
            isBanned = true;
            isSelected = true;
            for (int p = 0; p < patternCount; p++)
            {
                if (wave.get(index, p))
                {
                    if (patterns.Contains(p))
                    {
                        isBanned = false;
                    }
                    else
                    {
                        isSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Converts the generated results to an <see cref="ITopoArray{Tile}"/>,
        /// using specific tiles for locations that have not been decided or are in contradiction.
        /// The arguments are not relevant if the <see cref="Status"/> is <see cref="Resolution.DECIDED"/>.
        /// </summary>
        public ITopoArray<Tile> toArray(Tile undecided = default(Tile), Tile contradiction = default(Tile))
        {
            int width = topology.Width;
            int height = topology.Height;
            int depth = topology.Depth;

            ITopoArray<int> patternArray = wavePropagator.toTopoArray();

            return TopoArray.createByIndex(index =>
            {
                topology.getCoord(index, out int x, out int y, out int z);
                tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
                int pattern = patternArray.get(index);
                Tile tile;
                if (pattern == (int)Resolution.UNDECIDED)
                {
                    tile = undecided;
                }
                else if (pattern == (int)Resolution.CONTRADICTION)
                {
                    tile = contradiction;
                }
                else
                {
                    tile = tileModelMapping.PatternsToTilesByOffset[o][pattern];
                }
                return tile;
            }, topology);
        }

        /// <summary>
        /// Converts the generated results to an <see cref="ITopoArray{T}"/>,
        /// by extracting the value of each decided tile and
        /// using specific values for locations that have not been decided or are in contradiction.
        /// This is simply a convenience over 
        /// The arguments are not relevant if the <see cref="Status"/> is <see cref="Resolution.DECIDED"/>.
        /// </summary>
        public ITopoArray<T> toValueArray<T>(T undecided = default(T), T contradiction = default(T))
        {
            // TOODO: Just call ToArray() ?
            int width = topology.Width;
            int height = topology.Height;
            int depth = topology.Depth;

            ITopoArray<int> patternArray = wavePropagator.toTopoArray();

            return TopoArray.createByIndex(index =>
            {
                topology.getCoord(index, out int x, out int y, out int z);

                tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
                int pattern = patternArray.get(px, py, pz);
                T value;
                if (pattern == (int)Resolution.UNDECIDED)
                {
                    value = undecided;
                }
                else if (pattern == (int)Resolution.CONTRADICTION)
                {
                    value = contradiction;
                }
                else
                {
                    value = (T)tileModelMapping.PatternsToTilesByOffset[o][pattern].Value;
                }
                return value;
            }, topology);
        }

        /// <summary>
        /// Convert the generated result to an array of sets, where each set
        /// indicates the tiles that are still valid at the location.
        /// The size of the set indicates the resolution of that location:
        /// * Greater than 1: <see cref="Resolution.UNDECIDED"/>
        /// * Exactly 1: <see cref="Resolution.DECIDED"/>
        /// * Exactly 0: <see cref="Resolution.CONTRADICTION"/>
        /// </summary>
        public ITopoArray<ISet<Tile>> toArraySets()
        {
            int width = topology.Width;
            int height = topology.Height;
            int depth = topology.Depth;

            ITopoArray<ISet<int>> patternArray = wavePropagator.toTopoArraySets();

            return TopoArray.createByIndex(index =>
            {
                topology.getCoord(index, out int x, out int y, out int z);

                tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
                ISet<int> patterns = patternArray.get(px, py, pz);
                HashSet<Tile> hs = new();
                IReadOnlyDictionary<int, Tile> patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
                foreach (int pattern in patterns)
                {
                    hs.Add(patternToTiles[pattern]);
                }
                return(ISet<Tile>)hs;
            }, topology);
        }

        /// <summary>
        /// Convert the generated result to an array of sets, where each set
        /// indicates the values of tiles that are still valid at the location.
        /// The size of the set indicates the resolution of that location:
        /// * Greater than 1: <see cref="Resolution.UNDECIDED"/>
        /// * Exactly 1: <see cref="Resolution.DECIDED"/>
        /// * Exactly 0: <see cref="Resolution.CONTRADICTION"/>
        /// </summary>
        public ITopoArray<ISet<T>> toValueSets<T>()
        {
            int width = topology.Width;
            int height = topology.Height;
            int depth = topology.Depth;

            ITopoArray<ISet<int>> patternArray = wavePropagator.toTopoArraySets();

            return TopoArray.createByIndex(index =>
            {
                topology.getCoord(index, out int x, out int y, out int z);

                tileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
                ISet<int> patterns = patternArray.get(px, py, pz);
                HashSet<T> hs = new();
                IReadOnlyDictionary<int, Tile> patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
                foreach (int pattern in patterns)
                {
                    hs.Add((T)patternToTiles[pattern].Value);
                }
                return (ISet<T>)hs;
            }, topology);
        }
    }
}
