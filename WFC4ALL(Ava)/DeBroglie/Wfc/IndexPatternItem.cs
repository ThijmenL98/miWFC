﻿using System;

namespace WFC4ALL.DeBroglie.Wfc; 

internal struct IndexPatternItem : IEquatable<IndexPatternItem> {
    public int Index { get; set; }
    public int Pattern { get; set; }

    public bool Equals(IndexPatternItem other) {
        return other.Index == Index && other.Pattern == Pattern;
    }

    public override bool Equals(object obj) {
        if (obj is IndexPatternItem other) {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode() {
        unchecked {
            return Index.GetHashCode() * 17 + Pattern.GetHashCode();
        }
    }

    public static bool operator ==(IndexPatternItem a, IndexPatternItem b) {
        return a.Equals(b);
    }

    public static bool operator !=(IndexPatternItem a, IndexPatternItem b) {
        return !a.Equals(b);
    }
}