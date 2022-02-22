namespace WFC4All.DeBroglie
{
    /// <summary>
    /// Represents a location in a topology.
    /// </summary>
    public struct Point
    {
        public int x;
        public int y;
        public int z;

        public Point(int x, int y, int z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
