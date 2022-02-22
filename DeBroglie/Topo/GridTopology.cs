namespace WFC4All.DeBroglie.Topo
{
    /// <summary>
    /// A grid topology is a topology with a regular repeating pattern.
    /// It supports more operations than a generic topology.
    /// </summary>
    public class GridTopology : ITopology
    {
        /// <summary>
        /// Constructs a 2d square grid topology of given dimensions and periodicity.
        /// </summary>
        public GridTopology(int width, int height, bool periodic)
            : this(DirectionSet.cartesian2d, width, height, 1, periodic, periodic, periodic)
        {
        }

        /// <summary>
        /// Constructs a 3d cube grid topology of given dimensions and periodicity.
        /// </summary>
        public GridTopology(int width, int height, int depth, bool periodic)
            : this(DirectionSet.cartesian3d, width, height, depth, periodic, periodic, periodic)
        {
        }

        /// <summary>
        /// Constructs a 2d topology.
        /// </summary>
        public GridTopology(DirectionSet directions, int width, int height, bool periodicX, bool periodicY, bool[] mask = null)
            : this(directions, width, height, 1, periodicX, periodicY, false, mask)
        {
        }

        /// <summary>
        /// Constructs a topology.
        /// </summary>
        public GridTopology(DirectionSet directions, int width, int height, int depth, bool periodicX, bool periodicY, bool periodicZ, bool[] mask = null)
        {
            Directions = directions;
            Width = width;
            Height = height;
            Depth = depth;
            PeriodicX = periodicX;
            PeriodicY = periodicY;
            PeriodicZ = periodicZ;
            Mask = mask;
        }

        /// <summary>
        /// Returns a <see cref="GridTopology"/> with the same parameters, but with the specified mask
        /// </summary>
        public GridTopology withMask(bool[] mask)
        {
            if (Width * Height * Depth != mask.Length) {
                throw new System.Exception("Mask size doesn't fit the topology");
            }

            return new GridTopology(Directions, Width, Height, Depth, PeriodicX, PeriodicY, PeriodicZ, mask);
        }

        /// <summary>
        /// Returns a <see cref="GridTopology"/> with the same parameters, but with the specified mask
        /// </summary>
        public GridTopology withMask(ITopoArray<bool> mask)
        {
            if (!isSameSize(mask.Topology.asGridTopology())) {
                throw new System.Exception("Mask size doesn't fit the topology");
            }

            bool[] boolMask = new bool[Width * Height * Depth];
            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        boolMask[x + y * Width + z * Width * Height] = mask.get(x, y, z);
                    }
                }
            }
            return withMask(boolMask);
        }

        /// <summary>
        /// Returns a <see cref="GridTopology"/> with the same parameters, with the dimensions overridden. Any mask is reset.
        /// </summary>
        public GridTopology withSize(int width, int height, int depth = 1)
        {
            return new GridTopology(Directions, width, height, depth, PeriodicX, PeriodicY, PeriodicZ);
        }

        /// <summary>
        /// Returns a <see cref="GridTopology"/> with the same parameters, with the dimensions overridden.
        /// </summary>
        public GridTopology withPeriodic(bool periodicX, bool periodicY, bool periodicZ = false)
        {
            return new GridTopology(Directions, Width, Height, Depth, periodicX, periodicY, periodicZ, Mask);
        }

        /// <summary>
        /// Characterizes the adjacency relationship between locations.
        /// </summary>
        public DirectionSet Directions { get; set; }

        /// <summary>
        /// Number of unique directions
        /// </summary>
        public int DirectionsCount => Directions.Count;

        /// <summary>
        /// The extent along the x-axis.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The extent along the y-axis.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The extent along the z-axis.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Does the topology wrap on the x-axis.
        /// </summary>
        public bool PeriodicX { get; set; }

        /// <summary>
        /// Does the topology wrap on the y-axis.
        /// </summary>
        public bool PeriodicY { get; set; }

        /// <summary>
        /// Does the topology wrap on the z-axis.
        /// </summary>
        public bool PeriodicZ { get; set; }

        /// <summary>
        /// A array with one value per index indcating if the value is missing. 
        /// Not all uses of Topology support masks.
        /// </summary>
        public bool[] Mask { get; set; }

        /// <summary>
        /// Number of unique indices (distinct locations) in the topology
        /// </summary>
        public int IndexCount => Width * Height * Depth;

        /// <summary>
        /// Checks if two grids are the same size without regard for masks or periodicity.
        /// </summary>
        public bool isSameSize(GridTopology other)
        {
            return Width == other.Width && Height == other.Height && Depth == other.Depth;
        }

        /// <summary>
        /// Reduces a three dimensional co-ordinate to a single integer. This is mostly used internally.
        /// </summary>
        public int getIndex(int x, int y, int z)
        {
            return x + y * Width + z * Width * Height;
        }

        /// <summary>
        /// Inverts <see cref="getIndex"/>
        /// </summary>
        public void getCoord(int index, out int x, out int y, out int z)
        {
            x = index % Width;
            int i = index / Width;
            y = i % Height;
            z = i / Height;
        }

        public bool tryMove(int index, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
        {
            inverseDirection = Directions.inverse(direction);
            edgeLabel = (EdgeLabel)(Direction)direction;
            return tryMove(index, direction, out dest);
        }

        public bool tryMove(int index, Direction direction, out int dest)
        {
            int x, y, z;
            getCoord(index, out x, out y, out z);
            return tryMove(x, y, z, direction, out dest);
        }

        public bool tryMove(int x, int y, int z, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
        {
            inverseDirection = Directions.inverse(direction);
            edgeLabel = (EdgeLabel)(Direction)direction;
            return tryMove(x, y, z, direction, out dest);
        }

        /// <summary>
        /// Given a co-ordinate and a direction, gives the index that is one step in that direction,
        /// if it exists and is not masked out. Otherwise, it returns false.
        /// </summary>
        public bool tryMove(int x, int y, int z, Direction direction, out int dest)
        {
            if (tryMove(x, y, z, direction, out x, out y, out z))
            {
                dest = getIndex(x, y, z);
                return true;
            }
            else
            {
                dest = -1;
                return false;
            }
        }

        /// <summary>
        /// Given a co-ordinate and a direction, gives the co-ordinate that is one step in that direction,
        /// if it exists and is not masked out. Otherwise, it returns false.
        /// </summary>
        public bool tryMove(int x, int y, int z, Direction direction, out int destx, out int desty, out int destz)
        {
            int d = (int)direction;
            x += Directions.Dx[d];
            y += Directions.Dy[d];
            z += Directions.Dz[d];
            if (PeriodicX)
            {
                if (x < 0) {
                    x += Width;
                }

                if (x >= Width) {
                    x -= Width;
                }
            }
            else if (x < 0 || x >= Width)
            {
                destx = -1;
                desty = -1;
                destz = -1;
                return false;
            }
            if (PeriodicY)
            {
                if (y < 0) {
                    y += Height;
                }

                if (y >= Height) {
                    y -= Height;
                }
            }
            else if (y < 0 || y >= Height)
            {
                destx = -1;
                desty = -1;
                destz = -1;
                return false;
            }
            if (PeriodicZ)
            {
                if (z < 0) {
                    z += Depth;
                }

                if (z >= Depth) {
                    z -= Depth;
                }
            }
            else if (z < 0 || z >= Depth)
            {
                destx = -1;
                desty = -1;
                destz = -1;
                return false;
            }
            destx = x;
            desty = y;
            destz = z;
            if (Mask != null)
            {
                int index2 = getIndex(x, y, z);
                return Mask[index2];
            }
            else
            {
                return true;
            }
        }
    }
}
