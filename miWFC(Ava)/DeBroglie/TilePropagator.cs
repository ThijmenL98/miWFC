﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using miWFC.DeBroglie.Models;
using miWFC.DeBroglie.Topo;
using miWFC.DeBroglie.Trackers;
using miWFC.DeBroglie.Wfc;
using miWFC.Delegators;

namespace miWFC.DeBroglie;

// Implemenation wise, this wraps a WavePropagator to do the majority of the work.
// The only thing this class handles is conversion of tile objects into sets of patterns
// And co-ordinate conversion.
/// <summary>
///     TilePropagator is the main entrypoint to the DeBroglie library.
///     It takes a <see cref="TileModel" /> and an output <see cref="Topology" /> and generates
///     an output array using those parameters.
/// </summary>
public class TilePropagator {
    private readonly CentralDelegator centralDelegator;
    public readonly TileModelMapping tileModelMapping;

    private readonly WavePropagator wavePropagator;

    public TilePropagator(TileModel tileModel, ITopology topology, TilePropagatorOptions options, CentralDelegator centralDelegator) {
        TileModel = tileModel;
        Topology = topology;

        tileModelMapping = tileModel.GetTileModelMapping(topology);
        ITopology patternTopology = tileModelMapping.PatternTopology;
        PatternModel patternModel = tileModelMapping.PatternModel;

        IWaveConstraint[] waveConstraints =
            (options.Constraints?.Select(x => new TileConstraintAdaptor(x, this)).ToArray()
             ?? Enumerable.Empty<IWaveConstraint>())
            .ToArray();

        Func<double> randomDouble = options.RandomDouble;

        (HeapEntropyTracker indexPicker, WeightedRandomPatternPicker patternPicker) = MakePickers(options);

        WavePropagatorOptions wavePropagatorOptions = new() {
            BacktrackPolicy = MakeBacktrackPolicy(options),
            RandomDouble = randomDouble,
            Constraints = waveConstraints,
            IndexPicker = indexPicker,
            PatternPicker = patternPicker,
            Clear = false,
            ModelConstraintAlgorithm = options.ModelConstraintAlgorithm
        };

        centralDelegator = centralDelegator;

        wavePropagator = new WavePropagator(
            patternModel,
            patternTopology,
            topology.Width,
            topology.Height,
            wavePropagatorOptions,
            centralDelegator);
        wavePropagator.Clear();
    }

    /// <summary>
    ///     The topology of the output.
    /// </summary>
    public ITopology Topology { get; }

    /// <summary>
    ///     The source of randomness
    /// </summary>
    public Func<double> RandomDouble => wavePropagator.RandomDouble;


    /// <summary>
    ///     The model to use when generating.
    /// </summary>
    public TileModel TileModel { get; }

    /// <summary>
    ///     The overall resolution of the generated array.
    ///     This will be <see cref="Resolution.CONTRADICTION" /> if at least one location is in contradiction (has no possible
    ///     tiles)
    ///     otherwilse will be <see cref="Resolution.UNDECIDED" /> if at least one location is undecided (has multiple possible
    ///     tiles)
    ///     and will be <see cref="Resolution.DECIDED" /> otherwise (exactly one tile valid for each location).
    /// </summary>
    public Resolution Status => wavePropagator.Status;

    /// <summary>
    ///     A string indicating the reason a contradiction occured. This is sometimes set when Status is
    ///     <see cref="Resolution.CONTRADICTION" />.
    /// </summary>
    public string ContradictionReason => wavePropagator.ContradictionReason;

    /// <summary>
    ///     An object indicating what caused the contradiction. This is somtimes set to a give constraint if the constraint
    ///     caused the issue.
    /// </summary>
    public object ContradictionSource => wavePropagator.ContradictionSource;

    /// <summary>
    ///     This is incremented each time it is necessary to backtrack
    ///     a tile while generating results.
    ///     It is reset when <see cref="Clear" /> is called.
    /// </summary>
    public int BacktrackCount => wavePropagator.BacktrackCount;

    /// <summary>
    ///     This is incremented each time it is necessary to backjump
    ///     while generating results (i.e. when multiple steps are undone simultaneously).
    ///     It is reset when <see cref="Clear" /> is called.
    /// </summary>
    public int BackjumpCount => wavePropagator.BackjumpCount;

    public TileModelMapping getTMM() {
        return tileModelMapping;
    }

    public WavePropagator getWP() {
        return wavePropagator;
    }

    private static IBacktrackPolicy MakeBacktrackPolicy(TilePropagatorOptions options) {
        switch (options.BacktrackType) {
            case BacktrackType.NONE:
                return null;
            case BacktrackType.BACKTRACK:
                return new ConstantBacktrackPolicy(1);
            case BacktrackType.BACKJUMP:
                return new PatienceBackjumpPolicy();
            default:
                throw new Exception($"Unknown BacktrackType {options.BacktrackType}");
        }
    }

    private Tuple<HeapEntropyTracker, WeightedRandomPatternPicker> MakePickers(TilePropagatorOptions options) {
        // Use the appropriate random picker
        // Generally this is HeapEntropyTracker, but it doesn't support some features
        // so there's a few slower implementations for that
        return Tuple.Create(new HeapEntropyTracker(), new WeightedRandomPatternPicker());
    }

    public void TileCoordToPatternCoord(int x, int y, int z, out int px, out int py, out int pz, out int offset) {
        tileModelMapping.GetTileCoordToPatternCoord(x, y, z, out px, out py, out pz, out offset);
    }

    /// <summary>
    ///     Returns a number between 0 and 1 indicating how much of the generation is complete.
    ///     This number may decrease at times due to backtracking.
    /// </summary>
    /// <returns></returns>
    public double GetProgress() {
        return wavePropagator.Wave.GetProgress();
    }

    /// <summary>
    ///     Resets the TilePropagator to the state it was in at construction.
    /// </summary>
    /// <returns>
    ///     The current <see cref="Status" /> (usually <see cref="Resolution.UNDECIDED" /> unless there are very specific
    ///     initial conditions)
    /// </returns>
    public Resolution Clear() {
        return wavePropagator.Clear();
    }

    /// <summary>
    ///     Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
    /// </summary>
    public void SetContradiction() {
        wavePropagator.SetContradiction();
    }

    /// <summary>
    ///     Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
    /// </summary>
    public void SetContradiction(string reason, object source) {
        wavePropagator.SetContradiction(reason, source);
    }

    /// <summary>
    ///     Marks the given tile as not being a valid choice at a given location.
    ///     Then it propagates that information to other nearby tiles.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution ban(int x, int y, int z, Tile tile) {
        TileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
        ISet<int> patterns = tileModelMapping.GetPatterns(tile, o);
        foreach (int p in patterns) {
            Resolution status = wavePropagator.ban(px, py, pz, p);
            if (status != Resolution.UNDECIDED) {
                return status;
            }
        }

        return Resolution.UNDECIDED;
    }

    /// <summary>
    ///     Marks the given tiles as not being a valid choice at a given location.
    ///     Then it propagates that information to other nearby tiles.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution ban(int x, int y, int z, IEnumerable<Tile> tiles) {
        return ban(x, y, z, CreateTileSet(tiles));
    }

    /// <summary>
    ///     Marks the given tiles as not being a valid choice at a given location.
    ///     Then it propagates that information to other nearby tiles.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution ban(int x, int y, int z, TilePropagatorTileSet tiles) {
        TileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
        ISet<int> patterns = tileModelMapping.GetPatterns(tiles, o);
        foreach (int p in patterns) {
            Resolution status = wavePropagator.ban(px, py, pz, p);
            if (status != Resolution.UNDECIDED) {
                return status;
            }
        }

        return Resolution.UNDECIDED;
    }

    public void AddBackTrackPoint() {
        wavePropagator.AddBacktrackPoint();
    }

    /// <summary>
    ///     Marks the given tile as the only valid choice at a given location.
    ///     This is equivalent to banning all other tiles.
    ///     Then it propagates that information to other nearby tiles.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution select(int x, int y, int z, Tile tile) {
        TileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
        ISet<int> patterns = tileModelMapping.GetPatterns(tile, o);
        for (int p = 0; p < wavePropagator.PatternCount; p++) {
            if (patterns.Contains(p)) {
                continue;
            }

            Resolution status = wavePropagator.ban(px, py, pz, p);
            if (status != Resolution.UNDECIDED) {
                return status;
            }
        }
#if DEBUG
        Trace.WriteLine(@$"Selecting {tile.Value} @ ({x}, {y})");
#endif

        return Resolution.UNDECIDED;
    }

    /// <summary>
    ///     Marks the given tiles as the only valid choice at a given location.
    ///     This is equivalent to banning all other tiles.
    ///     Then it propagates that information to other nearby tiles.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution select(int x, int y, int z, IEnumerable<Tile> tiles) {
        return select(x, y, z, CreateTileSet(tiles));
    }

    /// <summary>
    ///     Marks the given tiles as the only valid choice at a given location.
    ///     This is equivalent to banning all other tiles.
    ///     Then it propagates that information to other nearby tiles.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution select(int x, int y, int z, TilePropagatorTileSet tiles) {
        TileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
        ISet<int> patterns = tileModelMapping.GetPatterns(tiles, o);
        for (int p = 0; p < wavePropagator.PatternCount; p++) {
            if (patterns.Contains(p)) {
                continue;
            }

            Resolution status = wavePropagator.ban(px, py, pz, p);
            if (status != Resolution.UNDECIDED) {
                return status;
            }
        }
#if DEBUG
        Trace.WriteLine(@$"Selecting {tiles.Tiles.ToList()[0].Value} @ ({x}, {y})");
#endif

        return Resolution.UNDECIDED;
    }

    public Resolution selWith(int x, int y, int pattern) {
        return wavePropagator.StepWith(Topology.GetIndex(x, y, 0), pattern);
    }

    /// <summary>
    ///     Makes a single tile selection.
    ///     Then it propagates that information to other nearby tiles.
    ///     If backtracking is enabled a single step can include several backtracks,.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution step() {
        return wavePropagator.Step();
    }

    public void StepConstraints() {
        wavePropagator.StepConstraints();
    }

    /// <summary>
    ///     Repeatedly Steps until the status is Decided or Contradiction.
    /// </summary>
    /// <returns>The current <see cref="Status" /></returns>
    public Resolution run() {
        return wavePropagator.Run();
    }

    public Resolution doBacktrack() {
        wavePropagator.DoCustomBacktrack();
        return wavePropagator.Status;
    }

    /// <summary>
    ///     Returns a tracker that tracks the banned/selected status of each tile with respect to a tileset.
    /// </summary>
    public SelectedTracker CreateSelectedTracker(TilePropagatorTileSet tileSet) {
        SelectedTracker tracker = new(this, wavePropagator, tileModelMapping, tileSet);
        ((ITracker) tracker).Reset();
        wavePropagator.AddTracker(tracker);
        return tracker;
    }

    /// <summary>
    ///     Returns a tracker that indicates all recently changed tiles.
    ///     This is mostly useful as a performance optimization.
    ///     Trackers are valid until <see cref="Clear" /> is called.
    /// </summary>
    public ChangeTracker CreateChangeTracker() {
        ChangeTracker tracker = new(tileModelMapping);
        ((ITracker) tracker).Reset();
        wavePropagator.AddTracker(tracker);
        return tracker;
    }

    /// <summary>
    ///     Creates a set of tiles. This set can be used with some operations, and is marginally
    ///     faster than passing in a fresh list of tiles ever time.
    /// </summary>
    public TilePropagatorTileSet CreateTileSet(IEnumerable<Tile> tiles) {
        return tileModelMapping.CreateTileSet(tiles);
    }

    /// <summary>
    ///     Returns true if this tile is the only valid selection for a given location.
    /// </summary>
    public bool IsSelected(int x, int y, int z, Tile tile) {
        GetBannedSelected(x, y, z, tile, out bool isBanned, out bool isSelected);
        return isSelected;
    }

    /// <summary>
    ///     Returns true if this tile is the not a valid selection for a given location.
    /// </summary>
    public bool IsBanned(int x, int y, int z, Tile tile) {
        GetBannedSelected(x, y, z, tile, out bool isBanned, out bool isSelected);
        return isBanned;
    }

    /// <summary>
    ///     Gets the results of both IsBanned and IsSelected
    /// </summary>
    public void GetBannedSelected(int x, int y, int z, Tile tile, out bool isBanned, out bool isSelected) {
        TileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
        ISet<int> patterns;
        try {
            patterns = tileModelMapping.TilesToPatternsByOffset[o][tile];
        } catch (KeyNotFoundException) {
            throw new KeyNotFoundException($"Couldn't find pattern for tile {tile} at offset {o}");
        }

        GetBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
    }

    /// <summary>
    ///     isBanned is set to true if all the tiles are not valid in the location.
    ///     isSelected is set to true if no other the tiles are valid in the location.
    /// </summary>
    public void GetBannedSelected(int x, int y, int z, IEnumerable<Tile> tiles, out bool isBanned,
        out bool isSelected) {
        GetBannedSelected(x, y, z, CreateTileSet(tiles), out isBanned, out isSelected);
    }

    /// <summary>
    ///     isBanned is set to true if all the tiles are not valid in the location.
    ///     isSelected is set to true if no other the tiles are valid in the location.
    /// </summary>
    public void GetBannedSelected(int x, int y, int z, TilePropagatorTileSet tiles, out bool isBanned,
        out bool isSelected) {
        TileCoordToPatternCoord(x, y, z, out int px, out int py, out int pz, out int o);
        ISet<int> patterns = tileModelMapping.GetPatterns(tiles, o);
        GetBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
    }

    internal Quadstate GetSelectedQuadstate(int x, int y, int z, TilePropagatorTileSet tiles) {
        GetBannedSelected(x, y, z, tiles, out bool isBanned, out bool isSelected);
        if (isSelected) {
            if (isBanned) {
                return Quadstate.CONTRADICTION;
            }

            return Quadstate.YES;
        }

        if (isBanned) {
            return Quadstate.NO;
        }

        return Quadstate.MAYBE;
    }


    private void GetBannedSelectedInternal(int px, int py, int pz, ISet<int> patterns, out bool isBanned,
        out bool isSelected) {
        int index = wavePropagator.Topology.GetIndex(px, py, pz);
        Wave wave = wavePropagator.Wave;
        int patternCount = wavePropagator.PatternCount;
        isBanned = true;
        isSelected = true;
        for (int p = 0; p < patternCount; p++) {
            if (wave.Get(index, p)) {
                if (patterns.Contains(p)) {
                    isBanned = false;
                } else {
                    isSelected = false;
                }
            }
        }
    }


    /// <summary>
    ///     Gets the tile that has been decided at a given index.
    ///     Otherwise returns undecided or contradiction as appropriate.
    /// </summary>
    public Tile GetTile(int index, Tile undecided = default, Tile contradiction = default) {
        tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int o);
        int pattern = wavePropagator.GetDecidedPattern(patternIndex);
        if (pattern == (int) Resolution.UNDECIDED) {
            return undecided;
        }

        if (pattern == (int) Resolution.CONTRADICTION) {
            return contradiction;
        }

        return tileModelMapping.PatternsToTilesByOffset[o][pattern];
    }

    /// <summary>
    ///     Gets the value of a Tile that has been decided at a given index.
    ///     Otherwise returns undecided or contradiction as appropriate.
    /// </summary>
    public T GetValue<T>(int index, T undecided = default, T contradiction = default) {
        tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int o);
        int pattern = wavePropagator.GetDecidedPattern(patternIndex);
        if (pattern == (int) Resolution.UNDECIDED) {
            return undecided;
        }

        if (pattern == (int) Resolution.CONTRADICTION) {
            return contradiction;
        }

        return (T) tileModelMapping.PatternsToTilesByOffset[o][pattern].Value;
    }

    public ISet<Tile> GetPossibleTiles(int index) {
        tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int o);
        IEnumerable<int> patterns = wavePropagator.GetPossiblePatterns(patternIndex);
        HashSet<Tile> hs = new();
        IReadOnlyDictionary<int, Tile> patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
        foreach (int pattern in patterns) {
            hs.Add(patternToTiles[pattern]);
        }

        return hs;
    }

    public IDictionary<Tile, double> GetWeightedTiles(int index) {
        tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int o);
        IEnumerable<int> patterns = wavePropagator.GetPossiblePatterns(patternIndex);
        Dictionary<Tile, double> result = new();
        IReadOnlyDictionary<int, Tile> patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
        foreach (int pattern in patterns) {
            double f = tileModelMapping.PatternModel.Frequencies[pattern];
            Tile tile = patternToTiles[pattern];
            if (!result.ContainsKey(tile)) {
                result[tile] = 0;
            }

            result[tile] += f;
        }

        return result;
    }

    public ISet<T> GetPossibleValues<T>(int index) {
        tileModelMapping.GetTileCoordToPatternCoord(index, out int patternIndex, out int o);
        IEnumerable<int> patterns = wavePropagator.GetPossiblePatterns(patternIndex);
        HashSet<T> hs = new();
        IReadOnlyDictionary<int, Tile> patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
        foreach (int pattern in patterns) {
            hs.Add((T) patternToTiles[pattern].Value);
        }

        return hs;
    }

    /// <summary>
    ///     Converts the generated results to an <see cref="ITopoArray{Tile}" />,
    ///     using specific tiles for locations that have not been decided or are in contradiction.
    ///     The arguments are not relevant if the <see cref="Status" /> is <see cref="Resolution.DECIDED" />.
    /// </summary>
    public ITopoArray<Tile> ToArray(Tile undecided = default, Tile contradiction = default) {
        return TopoArray.CreateByIndex(index => GetTile(index, undecided, contradiction), Topology);
    }

    /// <summary>
    ///     Converts the generated results to an <see cref="ITopoArray{T}" />,
    ///     by extracting the value of each decided tile and
    ///     using specific values for locations that have not been decided or are in contradiction.
    ///     This is simply a convenience over
    ///     The arguments are not relevant if the <see cref="Status" /> is <see cref="Resolution.DECIDED" />.
    /// </summary>
    public ITopoArray<T> toValueArray<T>(T undecided = default, T contradiction = default) {
        return TopoArray.CreateByIndex(index => GetValue(index, undecided, contradiction), Topology);
    }

    /// <summary>
    ///     Convert the generated result to an array of sets, where each set
    ///     indicates the tiles that are still valid at the location.
    ///     The size of the set indicates the resolution of that location:
    ///     * Greater than 1: <see cref="Resolution.UNDECIDED" />
    ///     * Exactly 1: <see cref="Resolution.DECIDED" />
    ///     * Exactly 0: <see cref="Resolution.CONTRADICTION" />
    /// </summary>
    public ITopoArray<ISet<Tile>> ToArraySets() {
        return TopoArray.CreateByIndex(GetPossibleTiles, Topology);
    }

    public ITopoArray<IDictionary<Tile, double>> ToWeightedArraySets() {
        return TopoArray.CreateByIndex(GetWeightedTiles, Topology);
    }

    /// <summary>
    ///     Convert the generated result to an array of sets, where each set
    ///     indicates the values of tiles that are still valid at the location.
    ///     The size of the set indicates the resolution of that location:
    ///     * Greater than 1: <see cref="Resolution.UNDECIDED" />
    ///     * Exactly 1: <see cref="Resolution.DECIDED" />
    ///     * Exactly 0: <see cref="Resolution.CONTRADICTION" />
    /// </summary>
    public ITopoArray<ISet<T>> ToValueSets<T>() {
        return TopoArray.CreateByIndex(GetPossibleValues<T>, Topology);
    }
}