using System;
using System.Collections.Generic;
using System.Linq;
using WFC4All.DeBroglie.Rot;

namespace WFC4All.DeBroglie.Topo
{
    /// <summary>
    /// Builds a GraphTopology that represents a mesh, i.e. a series of faces that connect to each other along their edges.
    /// </summary>
    public class MeshTopologyBuilder
    {
        private readonly DirectionSet directions;

        private readonly int edgeLabelCount;
        
        // By index, direction
        private GraphTopology.NeighbourDetails[,] neighbours;

        private readonly Dictionary<(int, int), Direction> pendingInverses = new Dictionary<(int, int), Direction>();

        public MeshTopologyBuilder(DirectionSet directions)
        {
            if(directions.Type != DirectionSetType.CARTESIAN2D)
            {
                throw new NotImplementedException($"Direction type {directions.Type} not supported");
            }
            this.directions = directions;
            edgeLabelCount = directions.Count * directions.Count;
            neighbours = new GraphTopology.NeighbourDetails[0, directions.Count];
        }

        private int getAngle(Direction d)
        {
            switch (d)
            {
                case Direction.X_PLUS: return 0;
                case Direction.Y_PLUS: return 90;
                case Direction.X_MINUS: return 180;
                case Direction.Y_MINUS: return 270;
            }
            throw new Exception();
        }

        private Rotation getRotation(Direction direction, Direction inverseDirection)
        {
            return new Rotation((360 + getAngle(direction) - getAngle(inverseDirection) + 180) % 360);
        }

        private EdgeLabel getEdgeLabel(Direction direction, Direction inverseDirection)
        {
            return (EdgeLabel)((int)direction + directions.Count * (int)inverseDirection);
        }

        /// <summary>
        /// Registers face1 and face2 as adjacent, moving in direction from face1 to face2.
        /// If you call this, you will also need to call add with face1 and face2 swapped, to
        /// establish the direction when travelling back.
        /// </summary>
        public void add(int face1, int face2, Direction direction)
        {
            if (pendingInverses.TryGetValue((face2, face1), out Direction inverseDirection))
            {
                add(face1, face2, direction, inverseDirection);
                pendingInverses.Remove((face2, face1));
            }
            else
            {
                pendingInverses.Add((face1, face2), direction);
            }
        }

        /// <summary>
        /// Registers face1 and face2 as adjacent, moving in direction from face1 to face2 and inverseDirection from face2 to face1.
        /// </summary>
        public void add(int face1, int face2, Direction direction, Direction inverseDirection)
        {
            int maxFace = Math.Max(face1, face2);
            if (neighbours.GetLength(0) <= maxFace)
            {
                GraphTopology.NeighbourDetails[,] newNeighbours = new GraphTopology.NeighbourDetails[maxFace + 1, directions.Count];
                Array.Copy(neighbours, newNeighbours, neighbours.Length);
                for(int f = neighbours.GetLength(0);f<maxFace+1;f++)
                {
                    for(int d=0;d<directions.Count;d++)
                    {
                        newNeighbours[f, d].Index = -1;
                    }
                }
                neighbours = newNeighbours;
            }
            neighbours[face1, (int)direction] = new GraphTopology.NeighbourDetails
            {
                Index = face2,
                InverseDirection = inverseDirection,
                EdgeLabel = getEdgeLabel(direction, inverseDirection)
            };
            neighbours[face2, (int)inverseDirection] = new GraphTopology.NeighbourDetails
            {
                Index = face1,
                InverseDirection = direction,
                EdgeLabel = getEdgeLabel(inverseDirection, direction)
            };
        }

        public GraphTopology getTopology()
        {
            if(pendingInverses.Count > 0)
            {
                KeyValuePair<(int, int), Direction> kv = pendingInverses.First();
                throw new Exception($"Some face adjacencies have only been added in one direction, e.g. {kv.Key.Item1} -> {kv.Key.Item2}");
            }
            return new GraphTopology(neighbours);
        }

        public GraphInfo getInfo()
        {
            return new GraphInfo
            {
                DirectionsCount = directions.Count,
                EdgeLabelCount = edgeLabelCount,
                EdgeLabelInfo = (from el in Enumerable.Range(0, edgeLabelCount)
                                 let d = (Direction)(el % 4)
                                 let id = (Direction)(el / 4)
                                 select (d, id, getRotation(d, id))).ToArray(),
            };
        }
    }
}
