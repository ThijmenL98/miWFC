using System.Linq;

namespace WFC4ALL.DeBroglie.Topo; 

internal class RaggedTopoArray2D<T> : ITopoArray<T> {
    private readonly T[][] values;

    public RaggedTopoArray2D(T[][] values, bool periodic) {
        int height = values.Length;
        int width = values.Max(a => a.Length);
        Topology = new GridTopology(
            width,
            height,
            periodic);
        this.values = values;
    }

    public RaggedTopoArray2D(T[][] values, ITopology topology) {
        Topology = topology;
        this.values = values;
    }

    public ITopology Topology { get; }

    public T get(int x, int y, int z) {
        return values[y][x];
    }

    public T get(int index) {
        int x, y, z;
        Topology.GetCoord(index, out x, out y, out z);
        return get(x, y, z);
    }
}