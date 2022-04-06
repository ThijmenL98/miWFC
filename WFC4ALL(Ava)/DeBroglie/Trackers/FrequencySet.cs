using System;
using System.Collections.Generic;
using System.Linq;

namespace WFC4ALL.DeBroglie.Trackers
{
    internal class FrequencySet
    {
        public struct Group
        {
            public int priority;
            public int patternCount;
            public double weightSum;
            public List<int> patterns;
            public List<double> frequencies;
            public List<double> plogp;
        }

        public FrequencySet(double[] weights, int[]? priorities = null)
        {
            if(priorities == null)
            {
                priorities = Enumerable.Repeat(0, weights.Length).ToArray();
            }

            Dictionary<int, Group> groupsByPriority = new();
            for(int i=0;i<weights.Length;i++)
            {
                int priority = priorities[i];
                double weight = weights[i];
                if (!groupsByPriority.TryGetValue(priority, out Group group))
                {
                    group = new Group {
                        priority = priority,
                        patterns = new List<int>(),
                        frequencies = new List<double>(),
                        plogp = new List<double>(),
                    };
                }
                group.patternCount += 1;
                group.weightSum += weight;
                group.patterns.Add(i);
                groupsByPriority[priority] = group;
            }
            frequencies = new double[weights.Length];
            plogp = new double[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                Group group = groupsByPriority[priorities[i]];
                double f = weights[i] / group.weightSum;
                frequencies[i] = f;
                plogp[i] = toPLogP(f);
                group.frequencies.Add(f);
                group.plogp.Add(toPLogP(f));
            }
            Groups = groupsByPriority.OrderByDescending(x => x.Key).Select(x => x.Value).ToArray();
            Dictionary<int, int> priorityToPriorityIndex = Groups.Select((g, i) => new { g, i }).ToDictionary(t => t.g.priority, t => t.i);
            priorityIndices = priorities.Select(p => priorityToPriorityIndex[p]).ToArray();
        }

        private double toPLogP(double frequency)
        {
            return frequency > 0.0 ? frequency * Math.Log(frequency) : 0.0;
        }

        public int[] priorityIndices;
        public double[] frequencies;
        public double[] plogp;

        public Group[] Groups { get; }
    }
}
