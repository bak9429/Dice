// Path: Assets/Script/GameData/Boss/BossRegistry.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Boss
{
    public static class BossRegistry
    {
        private static Dictionary<string, BossDefSO> _cache;

        public static void Warmup()
        {
            if (_cache != null) return;
            _cache = new Dictionary<string, BossDefSO>();

            var all = Resources.LoadAll<BossDefSO>("GameData/BossDefs");
            foreach (var so in all)
            {
                if (so == null) continue;
                if (string.IsNullOrWhiteSpace(so.bossId)) continue;
                _cache[so.bossId] = so;
            }

            Debug.Log($"[BossRegistry] Loaded BossDefSO count={_cache.Count}");
        }

        public static BossDefSO Get(string bossId)
        {
            Warmup();
            if (string.IsNullOrWhiteSpace(bossId)) return null;
            _cache.TryGetValue(bossId, out var so);
            return so;
        }

        public static BossDefSO GetAny()
        {
            Warmup();
            foreach (var kv in _cache) return kv.Value;
            return null;
        }
    }
}
