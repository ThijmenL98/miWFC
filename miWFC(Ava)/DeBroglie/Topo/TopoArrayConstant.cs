namespace miWFC.DeBroglie.Topo;

internal class TopoArrayConstant<T> : ITopoArray<T> {
    private readonly T value;

    public TopoArrayConstant(T value, ITopology topology) {
        Topology = topology;
        this.value = value;
    }

    public ITopology Topology { get; }

    public T get(int x, int y, int z) {
        return value;
    }

    public T get(int index) {
        return value;
    }
}