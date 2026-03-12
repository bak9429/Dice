// Path: Assets/Script/Rule/Combat/Boss/BossPatterns.cs
using System.Collections.Generic;
using UnityEngine;
using Rule.Field;

namespace Rule.Combat.Boss
{
    public static class BossPatterns
    {
        // ✅ 프로토 패턴 2개
        // 1) RingAOE : 보스 중심 원형(거리<=2)
        // 2) LaserLine : 보스에서 +q 방향 직선(길이 6)

        public static HashSet<HexCoord> BuildTelegraphTiles(string patternKey, HexCoord bossCoord)
        {
            if (string.IsNullOrWhiteSpace(patternKey)) patternKey = "RingAOE";

            switch (patternKey)
            {
                case "RingAOE":
                    return Disc(bossCoord, 2);

                case "LaserLine":
                    // axial dir: (+1, 0)
                    return Line(bossCoord, new HexCoord(1, 0), 6);

                default:
                    // 알 수 없는 키는 Ring으로 폴백
                    return Disc(bossCoord, 2);
            }
        }

        private static HashSet<HexCoord> Disc(HexCoord center, int radius)
        {
            var set = new HashSet<HexCoord>();
            for (int dq = -radius; dq <= radius; dq++)
            {
                for (int dr = -radius; dr <= radius; dr++)
                {
                    var c = new HexCoord(center.q + dq, center.r + dr);
                    if (AxialDistance(center, c) <= radius)
                        set.Add(c);
                }
            }
            return set;
        }

        private static HashSet<HexCoord> Line(HexCoord start, HexCoord dir, int length)
        {
            var set = new HashSet<HexCoord>();
            var c = start;
            for (int i = 0; i < length; i++)
            {
                set.Add(c);
                c = new HexCoord(c.q + dir.q, c.r + dir.r);
            }
            return set;
        }

        private static int AxialDistance(HexCoord a, HexCoord b)
        {
            int ax = a.q, az = a.r, ay = -ax - az;
            int bx = b.q, bz = b.r, by = -bx - bz;

            int dx = Mathf.Abs(ax - bx);
            int dy = Mathf.Abs(ay - by);
            int dz = Mathf.Abs(az - bz);

            return (dx + dy + dz) / 2;
        }
    }
}
