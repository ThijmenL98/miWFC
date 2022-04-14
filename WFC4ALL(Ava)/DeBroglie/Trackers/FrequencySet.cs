using System;
using System.Collections.Generic;
using System.Linq;

namespace WFC4ALL.DeBroglie.Trackers; 

internal class FrequencySet {
    public double[] Frequencies;
    public double[] Plogp;

    public int[] PriorityIndices;

    public FrequencySet(double[] weights, int[] priorities = null) {
        if (priorities == null) {
            priorities = Enumerable.Repeat(0, weights.Length).ToArray();
        }

        Dictionary<int, Group> groupsByPriority = new();
        Dictionary<int, List<double>> frequenciesByPriority = new();
        Dictionary<int, List<int>> patternsByPriority = new();
        // Group the patterns by prioirty
        for (int i = 0; i < weights.Length; i++) {
            int priority = priorities[i];
            double weight = weights[i];
            if (!groupsByPriority.TryGetValue(priority, out Group group)) {
                group = new Group {
                    Priority = priority,
                    Plogp = new List<double>()
                };
                frequenciesByPriority[priority] = new List<double>();
                patternsByPriority[priority] = new List<int>();
            }

            group.PatternCount += 1;
            group.WeightSum += weight;
            patternsByPriority[priority].Add(i);
            groupsByPriority[priority] = group;
        }

        Frequencies = new double[weights.Length];
        Plogp = new double[weights.Length];
        // Compute normalized frequencies
        for (int i = 0; i < weights.Length; i++) {
            int priority = priorities[i];
            Group group = groupsByPriority[priority];
            double f = weights[i] / group.WeightSum;
            Frequencies[i] = f;
            Plogp[i] = ToPLogP(f);
            frequenciesByPriority[priority].Add(f);
            group.Plogp.Add(ToPLogP(f));
        }

        // Convert from list to array
        foreach (int priority in groupsByPriority.Keys.ToList()) {
            Group g = groupsByPriority[priority];
            groupsByPriority[priority] = new Group {
                Priority = g.Priority,
                PatternCount = g.PatternCount,
                WeightSum = g.WeightSum,
                Patterns = patternsByPriority[priority].ToArray(),
                Frequencies = frequenciesByPriority[priority].ToArray(),
                Plogp = g.Plogp
            };
        }

        // Order groups by priority
        groups = groupsByPriority.OrderByDescending(x => x.Key).Select(x => x.Value).ToArray();
        Dictionary<int, int> priorityToPriorityIndex
            = groups.Select((g, i) => new {g, i}).ToDictionary(t => t.g.Priority, t => t.i);
        PriorityIndices = priorities.Select(p => priorityToPriorityIndex[p]).ToArray();
    }

    public Group[] groups { get; }

    private double ToPLogP(double frequency) {
        return frequency > 0.0 ? frequency * Math.Log(frequency) : 0.0;
    }

    public struct Group {
        public int Priority;
        public int PatternCount;
        public double WeightSum;
        public int[] Patterns;
        public double[] Frequencies;
        public List<double> Plogp;
    }
}