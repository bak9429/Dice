// Path: Assets/Script/GameData/Field/FieldRuleRegistry.cs
using System.Collections.Generic;
using UnityEngine;
using Rule.Field;

namespace GameData.Field
{
    public static class FieldRuleRegistry
    {
        private static Dictionary<string, FieldRuleSO> _cache;

        public static void Warmup()
        {
            if (_cache != null) return;
            _cache = new Dictionary<string, FieldRuleSO>();

            // Resources/GameData/FieldRules/ 아래 전부 로드
            var all = Resources.LoadAll<FieldRuleSO>("GameData/FieldRules");
            foreach (var so in all)
            {
                if (so == null) continue;
                if (string.IsNullOrWhiteSpace(so.fieldId)) continue;
                _cache[so.fieldId] = so;
            }

            Debug.Log($"[FieldRuleRegistry] Loaded FieldRuleSO count={_cache.Count}");
        }

        public static FieldRuleSO Get(string fieldId)
        {
            Warmup();
            if (string.IsNullOrWhiteSpace(fieldId)) return null;
            _cache.TryGetValue(fieldId, out var so);
            return so;
        }
    }
}
