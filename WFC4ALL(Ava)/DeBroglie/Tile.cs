﻿using System;

namespace WFC4All.DeBroglie
{
    /// <summary>
    /// A wrapper around <see cref="object"/> to give some clarity to the code.
    /// Tiles have no semantics except Equals and GetHashCode - all other behaviour is specified
    /// externally when configuring other DeBroglie classes.
    /// Arrays of <see cref="Tile"/> are the main objects that are generated by the DeBroglie library,
    /// and the Tile struct is used elsewhere to indicate the relationship to generation.
    /// </summary>
    public struct Tile : IEquatable<Tile>
    {
        public Tile(object value)
        {
            Value = value;
        }

        public object Value { get; }

        public bool Equals(Tile other)
        {
            return Equals(Value, other.Value);
        }

        public override bool Equals(object? obj) {
            if (obj is Tile other)
            {
                return Equals(Value, other.Value);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value == null ? "null" : Value.ToString();
        }

        public static bool operator ==(Tile a, Tile b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(Tile a, Tile b)
        {
            return !(a == b);
        }

    }
}