// Path: Assets/Script/GameData/Combat/Registry/AttackRegistry.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Combat.Registry
{
    public static class AttackRegistry
    {
        private static bool _warmed;
        private static readonly Dictionary<string, AttackDefSO> _byId = new();
        private const string RES_ROOT = "GameData/Combat/Attacks";

        public static void Warmup()
        {
            if (_warmed) return;
            _warmed = true;

            _byId.Clear();
            var assets = Resources.LoadAll<AttackDefSO>(RES_ROOT);
            foreach (var a in assets)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.attackId)) continue;
                _byId[a.attackId] = a;
            }

            if (_byId.Count == 0)
                Debug.Log("[AttackRegistry] No assets found. Attacks will be created by weapon defaults (runtime). ");
            else
                Debug.Log($"[AttackRegistry] Loaded AttackDefSO count={_byId.Count}");
        }

        public static AttackDefSO Get(string attackId)
        {
            Warmup();
            if (string.IsNullOrWhiteSpace(attackId)) return null;
            return _byId.TryGetValue(attackId, out var a) ? a : null;
        }

        public static AttackDefSO CreateRuntime(string id, string name, int apCost, TargetShape shape, string desc = "")
        {
            var a = ScriptableObject.CreateInstance<AttackDefSO>();
            a.attackId = id;
            a.displayName = name;
            a.apCost = apCost;
            a.shape = shape;
            a.description = desc;
            return a;
        }
    }
}
