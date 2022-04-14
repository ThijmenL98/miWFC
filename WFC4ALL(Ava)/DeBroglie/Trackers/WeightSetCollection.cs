using System;
using System.Collections.Generic;
using System.Linq;
using WFC4ALL.DeBroglie.Models;
using WFC4ALL.DeBroglie.Topo;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class WeightSetCollection {
    private readonly IDictionary<int, FrequencySet> frequencySets;
    private readonly TileModelMapping tileModelMapping;
    private readonly ITopoArray<int> weightSetByIndex;
    private readonly IDictionary<int, IDictionary<Tile, PriorityAndWeight>> weightSets;

    public WeightSetCollection(ITopoArray<int> weightSetByIndex,
        IDictionary<int, IDictionary<Tile, PriorityAndWeight>> weightSets, TileModelMapping tileModelMapping) {
        this.weightSetByIndex = weightSetByIndex;
        this.weightSets = weightSets;
        this.tileModelMapping = tileModelMapping;
        frequencySets = new Dictionary<int, FrequencySet>();
    }

    public FrequencySet Get(int index) {
        int id = weightSetByIndex.get(index);

        if (frequencySets.TryGetValue(id, out FrequencySet? fs)) {
            return fs;
        }

        return frequencySets[id] = GetFrequencySet(weightSets[id], tileModelMapping);
    }

    private static FrequencySet GetFrequencySet(IDictionary<Tile, PriorityAndWeight> weights,
        TileModelMapping tileModelMapping) {
        // TODO: Handle overlapped
        if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset != null) {
            throw new NotImplementedException();
        }

        int offset = 0;
        double[] newWeights = new double[tileModelMapping.PatternModel.PatternCount];
        int[] newPriorities = new int[tileModelMapping.PatternModel.PatternCount];
        foreach (KeyValuePair<Tile, PriorityAndWeight> kv in weights) {
            int pattern = tileModelMapping.TilesToPatternsByOffset[offset][kv.Key].Single();
            newWeights[pattern] = kv.Value.Weight;
            newPriorities[pattern] = kv.Value.Priority;
        }

        return new FrequencySet(newWeights, newPriorities);
    }
}