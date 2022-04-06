using System.Linq;

namespace WFC4ALL.DeBroglie.Models {
    public struct PatternArray {
        public Tile[,,] values;

        public int Width => values.GetLength(0);

        public int Height => values.GetLength(1);

        public int Depth => values.GetLength(2);

        public override string ToString() {
            int i = 0;
            Tile[,,] values1 = values;
            string s = values1.Cast<Tile>().Aggregate("{",
                (current, item) => current + ((i++ > 0 ? i % values1.GetLength(1) == 1 ? "}\n {" : "," : "") + item));
            s += '}';

            return s;
        }

        private Tile getTileAt(int x, int y, int z) {
            return values[x, y, z];
        }

        public Tile getTileAt(int x, int y) {
            return getTileAt(x, y, 0);
        }
    }
}