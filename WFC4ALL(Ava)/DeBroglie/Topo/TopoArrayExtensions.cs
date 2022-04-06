using System;

namespace WFC4ALL.DeBroglie.Topo
{
    public static class TopoArrayExtensions
    {
        /// <summary>
        /// Copies a <see cref="ITopoArray{T}"/> into a 2d array.
        /// </summary>
        public static T[,] toArray2d<T>(this ITopoArray<T> topoArray)
        {
            int width = topoArray.Topology.Width;
            int height = topoArray.Topology.Height;
            T[,] results = new T[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    results[x, y] = topoArray.get(x, y);
                }
            }
            return results;
        }

        /// <summary>
        /// Copies a <see cref="ITopoArray{T}"/> into a 3d array.
        /// </summary>
        public static T[,,] toArray3d<T>(this ITopoArray<T> topoArray)
        {
            int width = topoArray.Topology.Width;
            int height = topoArray.Topology.Height;
            int depth = topoArray.Topology.Depth;
            T[,,] results = new T[width, height, depth];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        results[x, y, z] = topoArray.get(x, y, z);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Calls <c>func</c> on each element of the array, returning a new <see cref="ITopoArray{T}"/>
        /// </summary>
        private static ITopoArray<TU> map<T, TU>(this ITopoArray<T> topoArray, Func<T, TU> func)
        {
            /*
            var width = topoArray.Topology.Width;
            var height = topoArray.Topology.Height;
            var depth = topoArray.Topology.Depth;
            var r = new U[width, height, depth];

            for (var z = 0; z < depth; z++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        r[x, y, z] = func(topoArray.Get(x, y, z));
                    }
                }
            }

            return new TopoArray3D<U>(r, topoArray.Topology);
            */
            TU[] r = new TU[topoArray.Topology.IndexCount];
            foreach(int i in topoArray.Topology.getIndices())
            {
                r[i] = func(topoArray.get(i));
            }
            return new TopoArray1D<TU>(r, topoArray.Topology);
        }

        /// <summary>
        /// Wraps each element of an <see cref="ITopoArray{T}"/> in a <see cref="Tile"/> struct, 
        /// so it can be consumed by other DeBroglie classes.
        /// </summary>
        public static ITopoArray<Tile> toTiles<T>(this ITopoArray<T> topoArray)
        {
            return topoArray.map(v => new Tile(v));
        }
    }
}
