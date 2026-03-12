// Path: Assets/Script/GameData/Combat/Registry/BulletRegistry.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Combat.Registry
{
    public static class BulletRegistry
    {
        private static bool _warmed;
        private static readonly Dictionary<string, BulletDefSO> _byId = new();
        public static IReadOnlyDictionary<string, BulletDefSO> ById => _byId;

        // Resources root: Resources/GameData/Combat/Bullets/
        private const string RES_ROOT = "GameData/Combat/Bullets";

        public static void Warmup()
        {
            if (_warmed) return;
            _warmed = true;

            _byId.Clear();
            var assets = Resources.LoadAll<BulletDefSO>(RES_ROOT);
            foreach (var a in assets)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.bulletId)) continue;
                _byId[a.bulletId] = a;
            }

            // Runtime defaults (6 kinds) if none in Resources.
            if (_byId.Count == 0)
            {
                // ✅ NOTE
                // - Bullet은 별도 액션(근접 옵션 아님)
                // - AP 소모는 UI 레이어에서 제거됨(현재 프로젝트 정책)
                // - Ammo는 bulletId 단위로 관리됨

                // --- 권장 런타임 디폴트 6종 ---
                AddRuntimeDefault(new BulletSpec
                {
                    bulletId = "Pierce",
                    displayName = "관통",
                    kind = BulletKind.Pierce,
                    baseDamage = 6,
                    shieldDamage = 4,
                    guardModifier = 0.35f,
                    aoeRadius = 0,
                    heat = 1,
                    moveDistance = 0,
                    maxAmmoBase = 2,
                    ammoGrowthPerGate = 1,
                });

                AddRuntimeDefault(new BulletSpec
                {
                    bulletId = "Impact",
                    displayName = "충격",
                    kind = BulletKind.Impact,
                    baseDamage = 7,
                    shieldDamage = 3,
                    guardModifier = 0.10f,
                    aoeRadius = 0,
                    heat = 0,
                    moveDistance = 0,
                    maxAmmoBase = 2,
                    ammoGrowthPerGate = 1,
                });

                AddRuntimeDefault(new BulletSpec
                {
                    bulletId = "Break",
                    displayName = "파쇄",
                    kind = BulletKind.Break,
                    baseDamage = 3,
                    shieldDamage = 12,
                    guardModifier = 0.15f,
                    aoeRadius = 0,
                    heat = 1,
                    moveDistance = 0,
                    maxAmmoBase = 2,
                    ammoGrowthPerGate = 1,
                });

                AddRuntimeDefault(new BulletSpec
                {
                    bulletId = "Splash",
                    displayName = "스플래시",
                    kind = BulletKind.Splash,
                    baseDamage = 4,
                    shieldDamage = 6,
                    guardModifier = 0.05f,
                    aoeRadius = 1,
                    heat = 1,
                    moveDistance = 0,
                    maxAmmoBase = 2,
                    ammoGrowthPerGate = 1,
                });

                AddRuntimeDefault(new BulletSpec
                {
                    bulletId = "Heat",
                    displayName = "과부하",
                    kind = BulletKind.Heat,
                    baseDamage = 9,
                    shieldDamage = 4,
                    guardModifier = 0.25f,
                    aoeRadius = 0,
                    heat = 3,
                    moveDistance = 0,
                    maxAmmoBase = 2,
                    ammoGrowthPerGate = 1,
                });

                AddRuntimeDefault(new BulletSpec
                {
                    bulletId = "Move",
                    displayName = "이동",
                    kind = BulletKind.Move,
                    baseDamage = 0,
                    shieldDamage = 0,
                    guardModifier = 0f,
                    aoeRadius = 0,
                    heat = 0,
                    moveDistance = 1,
                    maxAmmoBase = 2,
                    ammoGrowthPerGate = 1,
                });

                Debug.Log("[BulletRegistry] No assets found. Using runtime default bullets (6).");
            }
            else
            {
                Debug.Log($"[BulletRegistry] Loaded BulletDefSO count={_byId.Count}");
            }
        }

        private struct BulletSpec
        {
            public string bulletId;
            public string displayName;
            public BulletKind kind;

            public int baseDamage;
            public int shieldDamage;
            public float guardModifier;
            public int aoeRadius;
            public int heat;

            public int moveDistance;

            public int maxAmmoBase;
            public int ammoGrowthPerGate;
        }

        private static void AddRuntimeDefault(BulletSpec s)
        {
            var b = ScriptableObject.CreateInstance<BulletDefSO>();
            b.bulletId = s.bulletId;
            b.displayName = s.displayName;
            b.kind = s.kind;

            b.baseDamage = s.baseDamage;
            b.shieldDamage = s.shieldDamage;
            b.guardModifier = Mathf.Clamp01(s.guardModifier);
            b.aoeRadius = Mathf.Max(0, s.aoeRadius);
            b.heat = Mathf.Max(0, s.heat);

            b.moveDistance = Mathf.Max(0, s.moveDistance);
            b.maxAmmoBase = Mathf.Max(0, s.maxAmmoBase);
            b.ammoGrowthPerGate = Mathf.Max(0, s.ammoGrowthPerGate);

            _byId[s.bulletId] = b;
        }

        public static BulletDefSO Get(string bulletId)
        {
            Warmup();
            if (string.IsNullOrWhiteSpace(bulletId)) return null;
            return _byId.TryGetValue(bulletId, out var b) ? b : null;
        }

        public static List<BulletDefSO> GetAll()
        {
            Warmup();
            return new List<BulletDefSO>(_byId.Values);
        }
    }
}
