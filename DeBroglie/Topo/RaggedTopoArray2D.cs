using System.Linq;

namespace WFC4All.DeBroglie.Topo
{
    internal class RaggedTopoArray2D<T> : ITopoArray<T>
    {
        private readonly T[][] values;

        public RaggedTopoArray2D(T[][] values, bool periodic)
        {
            int height = values.Length;
            int width = values.Max(a => a.Length);
            Topology = new GridTopology(
                width,
                height,
                periodic);
            this.values = values;
        }

        public RaggedTopoArray2D(T[][] values, GridTopology topology)
        {
            Topology = topology;
            this.values = values;
        }

        public GridTopology Topology { get; }

        ITopology ITopoArray<T>.Topology => Topology;

        public T get(int x, int y, int z)
        {
            return values[y][x];
        }

        public T get(int index)
        {
            int x, y, z;
            Topology.getCoord(index, out x, out y, out z);
            return get(x, y, z);
        }
    }
}
