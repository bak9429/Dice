// Path: Assets/Script/GameData/Combat/Registry/GunRegistry.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Combat.Registry
{
    public static class GunRegistry
    {
        private static bool _warmed;
        private static readonly Dictionary<string, GunDefSO> _byId = new();
        private const string RES_ROOT = "GameData/Combat/Guns";

        public static void Warmup()
        {
            if (_warmed) return;
            _warmed = true;

            _byId.Clear();
            var assets = Resources.LoadAll<GunDefSO>(RES_ROOT);
            foreach (var a in assets)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.gunId)) continue;
                _byId[a.gunId] = a;
            }

            if (_byId.Count == 0)
            {
                // Runtime defaults (5 guns)
                BulletRegistry.Warmup();

                AddGunRuntime("Gun_001", "사이드암", 1, 3,
                    allowAll: true, allowedIds: null,
                    powerMul: 1.00f, pierce: 1.00f, impact: 1.00f, brk: 1.00f, element: 1.00f, move: 1.00f,
                    desc: "기본 총");

                AddGunRuntime("Gun_002", "카빈", 2, 5,
                    allowAll: false, allowedIds: new List<string> { "Pierce", "Impact", "Splash", "Heat" },
                    powerMul: 1.05f, pierce: 1.10f, impact: 1.00f, brk: 0.90f, element: 1.00f, move: 1.00f,
                    desc: "원거리 안정");

                AddGunRuntime("Gun_003", "브레이커", 1, 3,
                    allowAll: false, allowedIds: new List<string> { "Break", "Impact", "Splash" },
                    powerMul: 1.00f, pierce: 0.90f, impact: 1.00f, brk: 1.25f, element: 1.05f, move: 1.00f,
                    desc: "실드 압박 특화");

                AddGunRuntime("Gun_004", "레일", 2, 6,
                    allowAll: false, allowedIds: new List<string> { "Pierce", "Heat" },
                    powerMul: 1.10f, pierce: 1.35f, impact: 0.85f, brk: 0.80f, element: 0.95f, move: 1.00f,
                    desc: "관통 극대화");

                AddGunRuntime("Gun_005", "택티컬", 1, 4,
                    allowAll: false, allowedIds: new List<string> { "Impact", "Splash", "Heat", "Move" },
                    powerMul: 0.95f, pierce: 0.90f, impact: 1.10f, brk: 0.85f, element: 1.10f, move: 1.00f,
                    desc: "유틸/상태 중심");

                Debug.Log("[GunRegistry] No assets found. Using runtime default guns (5).");
            }
            else
            {
                Debug.Log($"[GunRegistry] Loaded GunDefSO count={_byId.Count}");
            }
        }

        private static void AddGunRuntime(
            string id, string name, int rmin, int rmax,
            bool allowAll, List<string> allowedIds,
            float powerMul, float pierce, float impact, float brk, float element, float move,
            string desc)
        {
            var g = ScriptableObject.CreateInstance<GunDefSO>();
            g.gunId = id;
            g.displayName = name;
            g.rangeMin = rmin;
            g.rangeMax = rmax;
            g.allowAllBullets = allowAll;
            g.allowedBulletIds = allowedIds ?? new List<string>();
            g.bulletPowerMul = powerMul;
            g.SetKindMuls(pierce, impact, brk, element, move);
            g.description = desc;
            _byId[id] = g;
        }

        public static GunDefSO Get(string gunId)
        {
            Warmup();
            if (string.IsNullOrWhiteSpace(gunId)) return null;
            return _byId.TryGetValue(gunId, out var g) ? g : null;
        }

        public static GunDefSO GetAny()
        {
            Warmup();
            foreach (var kv in _byId) return kv.Value;
            return null;
        }
    }
}
