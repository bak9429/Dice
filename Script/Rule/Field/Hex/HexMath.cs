// Path: Assets/Script/Rule/Field/HexMath.cs
using UnityEngine;

namespace Rule.Field
{
    // Pointy-top axial (q,r) -> XY plane
    public static class HexMath
    {
        public static Vector3 AxialToWorld(HexCoord c, float size, float y = 0f)
        {
            float x = size * (Mathf.Sqrt(3f) * c.q + Mathf.Sqrt(3f) / 2f * c.r);
            float yy = size * (3f / 2f * c.r);
            return new Vector3(x, yy, 0f); // z=0 plane
        }

        public static int Distance(HexCoord a, HexCoord b)
        {
            int ax = a.q, az = a.r, ay = -ax - az;
            int bx = b.q, bz = b.r, by = -bx - bz;
            return (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) + Mathf.Abs(az - bz)) / 2;
        }
    }
}
