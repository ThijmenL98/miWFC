namespace WFC4ALL.DeBroglie.Topo
{

    internal class TopoArray2D<T> : ITopoArray<T>
    {
        private readonly T[,] values;

        public TopoArray2D(T[,] values, bool periodic)
        {
            Topology = new GridTopology(
                values.GetLength(0),
                values.GetLength(1),
                periodic);
            this.values = values;
        }

        public TopoArray2D(T[,] values, GridTopology topology)
        {
            Topology = topology;
            this.values = values;
        }

        public GridTopology Topology { get; }

        ITopology ITopoArray<T>.Topology => Topology;

        public T get(int x, int y, int z)
        {
            return values[x, y];
        }

        public T get(int index)
        {
            int x, y, z;
            Topology.getCoord(index, out x, out y, out z);
            return get(x, y, z);
        }
    }
}
