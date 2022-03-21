namespace WFC4All.DeBroglie.Topo
{
    internal class TopoArrayConstant<T> : ITopoArray<T>
    {
        private readonly T value;

        public TopoArrayConstant(T value, GridTopology topology)
        {
            Topology = topology;
            this.value = value;
        }

        private GridTopology Topology { get; }

        ITopology ITopoArray<T>.Topology => Topology;

        public T get(int x, int y, int z)
        {
            return value;
        }

        public T get(int index)
        {
            return value;
        }
    }
}
