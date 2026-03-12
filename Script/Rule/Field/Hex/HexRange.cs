// Path: Assets/Script/Rule/Field/HexRange.cs
using System.Collections.Generic;

namespace Rule.Field
{
    public static class HexRange
    {
        // axial directions (q,r)
        private static readonly HexCoord[] Dir =
        {
            new HexCoord(1,0), new HexCoord(1,-1), new HexCoord(0,-1),
            new HexCoord(-1,0), new HexCoord(-1,1), new HexCoord(0,1),
        };

        public static IEnumerable<HexCoord> Neighbors(HexCoord c)
        {
            for (int i = 0; i < 6; i++)
                yield return new HexCoord(c.q + Dir[i].q, c.r + Dir[i].r);
        }

        /// <summary>
        /// costPerStep=MoveCost(기본 1). AP 예산 내로 도달 가능한 타일 계산.
        /// 장애물/유닛 충돌은 아직 없음(프로토용).
        /// </summary>
        public static HashSet<HexCoord> Reachable(
            IReadOnlyDictionary<HexCoord, SpriteTile> tiles,
            HexCoord start, int apBudget, int costPerStep)
        {
            var result = new HashSet<HexCoord>();
            if (apBudget <= 0 || costPerStep <= 0) return result;
            if (tiles == null || !tiles.ContainsKey(start)) return result;

            var q = new Queue<(HexCoord c, int cost)>();
            var best = new Dictionary<HexCoord, int>();

            q.Enqueue((start, 0));
            best[start] = 0;

            while (q.Count > 0)
            {
                var (cur, cost) = q.Dequeue();
                result.Add(cur);

                foreach (var nb in Neighbors(cur))
                {
                    if (!tiles.ContainsKey(nb)) continue;

                    int next = cost + costPerStep;
                    if (next > apBudget) continue;

                    if (best.TryGetValue(nb, out var prevBest) && prevBest <= next) continue;
                    best[nb] = next;
                    q.Enqueue((nb, next));
                }
            }

            // 시작 타일은 이동 하이라이트에서 제외
            result.Remove(start);
            return result;
        }
    }
}
