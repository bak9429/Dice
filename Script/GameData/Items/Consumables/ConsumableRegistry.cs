using System.Collections.Generic;
using UnityEngine;

namespace GameData.Items.Consumables
{
    public static class ConsumableRegistry
    {
        private const string RES_ROOT = "GameData/Items/Consumables";
        private static bool _warmed;
        private static readonly Dictionary<string, ConsumableDefSO> _byId = new();

        public static void Warmup()
        {
            if (_warmed) return;
            _warmed = true;

            _byId.Clear();

            var all = Resources.LoadAll<ConsumableDefSO>(RES_ROOT);
            foreach (var def in all)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.id)) continue;
                _byId[def.id.Trim()] = def;
            }

            // ✅ 프로토타입 편의: SO가 0개면 런타임 기본값을 만들어서라도 동작하게 한다.
            if (_byId.Count == 0)
            {
                AddRuntimeDefault_RepairKit();
                AddRuntimeDefault_ShieldPatch();
                AddRuntimeDefault_Thruster();
                AddRuntimeDefault_Dampener();
            }

            Debug.Log($"[ConsumableRegistry] Warmup loaded={_byId.Count} from Resources/{RES_ROOT}");
        }

        public static ConsumableDefSO Get(string id)
        {
            if (!_warmed) Warmup();
            if (string.IsNullOrWhiteSpace(id)) return null;

            _byId.TryGetValue(id.Trim(), out var def);
            return def;
        }

        private static void AddRuntimeDefault_RepairKit()
        {
            var so = ScriptableObject.CreateInstance<ConsumableDefSO>();
            so.id = "RepairKit";
            so.type = ConsumableType.RepairKit;
            so.displayName = "수리 키트";
            so.hpHealRatio = 0.30f; // maxHp의 30%
            _byId[so.id] = so;
        }

        private static void AddRuntimeDefault_ShieldPatch()
        {
            var so = ScriptableObject.CreateInstance<ConsumableDefSO>();
            so.id = "ShieldPatch";
            so.type = ConsumableType.ShieldPatch;
            so.displayName = "쉴드 패치";
            so.shieldRestoreRatio = 0.50f; // maxShield의 50%
            _byId[so.id] = so;
        }

        private static void AddRuntimeDefault_Thruster()
        {
            var so = ScriptableObject.CreateInstance<ConsumableDefSO>();
            so.id = "Thruster";
            so.type = ConsumableType.Thruster;
            so.displayName = "추진 장치";
            so.thrusterFreeMoveDistance = 2;
            _byId[so.id] = so;
        }

        private static void AddRuntimeDefault_Dampener()
        {
            var so = ScriptableObject.CreateInstance<ConsumableDefSO>();
            so.id = "Dampener";
            so.type = ConsumableType.Dampener;
            so.displayName = "감쇠 장치";
            so.dampenerDamageMult = 0.60f; // 피해 60%로
            so.dampenerHits = 1;
            _byId[so.id] = so;
        }
    }
}