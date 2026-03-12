// Path: Assets/Script/Rule/Field/HexCoord.cs
using System;

namespace Rule.Field
{
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int q; // axial
        public int r;

        public HexCoord(int q, int r) { this.q = q; this.r = r; }

        public bool Equals(HexCoord other) => q == other.q && r == other.r;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => (q * 73856093) ^ (r * 19349663);
        public override string ToString() => $"({q},{r})";
    }
}
