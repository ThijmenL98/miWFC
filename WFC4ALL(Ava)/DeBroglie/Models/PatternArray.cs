using System.Linq;

namespace WFC4ALL.DeBroglie.Models; 

public struct PatternArray {
    public Tile[,,] Values;

    public int Width => Values.GetLength(0);

    public int Height => Values.GetLength(1);

    public int Depth => Values.GetLength(2);

    public override string ToString() {
        int i = 0;
        Tile[,,] values1 = Values;
        string s = values1.Cast<Tile>().Aggregate("{",
            (current, item) => current + ((i++ > 0 ? i % values1.GetLength(1) == 1 ? "}\n {" : "," : "") + item));
        s += '}';

        return s;
    }

    private Tile getTileAt(int x, int y, int z) {
        return Values[x, y, z];
    }

    public Tile getTileAt(int x, int y) {
        return getTileAt(x, y, 0);
    }
}