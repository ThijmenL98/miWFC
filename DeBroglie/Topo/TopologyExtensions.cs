using System.Collections.Generic;

namespace WFC4All.DeBroglie.Topo
{
    public static class TopologyExtensions
    {
        public static GridTopology asGridTopology(this ITopology topology)
        {
            if(topology is GridTopology t)
            {
                return t;
            }
            else
            {
                throw new System.Exception("Expected a grid-based topology");
            }
        }

        /// <summary>
        /// Returns true if a given index has not been masked out.
        /// </summary>
        public static bool containsIndex(this ITopology topology, int index)
        {
            bool[] mask = topology.Mask;
            return mask == null || mask[index];
        }

        public static IEnumerable<int> getIndices(this ITopology topology)
        {
            int indexCount = topology.IndexCount;
            bool[] mask = topology.Mask;
            for (int i = 0; i < indexCount; i++)
            {
                if (mask == null || mask[i]) {
                    yield return i;
                }
            }
        }
    }
}
