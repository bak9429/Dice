// Path: Assets/Script/GameData/Nodes/InvestigationDatabase.cs
using GameData.Nodes;
using UnityEngine;

namespace GameData.Nodes
{
    public static class InvestigationDatabase
    {
        private const string RESOURCE_PATH = "GameData/Nodes/InvestigationDB";

        private static InvestigationDefSO[] _cache;

        private static void EnsureLoaded()
        {
            if (_cache != null) return;

            _cache = Resources.LoadAll<InvestigationDefSO>(RESOURCE_PATH);

            Debug.Log($"[InvestigationDatabase] Loaded {_cache.Length} InvestigationDef assets.");
        }

        public static InvestigationDefSO FindByBossId(string bossId)
        {
            if (string.IsNullOrEmpty(bossId))
                return null;

            EnsureLoaded();

            for (int i = 0; i < _cache.Length; i++)
            {
                if (_cache[i] != null && _cache[i].bossId == bossId)
                    return _cache[i];
            }

            Debug.LogWarning($"[InvestigationDatabase] bossId not found: {bossId}");
            return null;
        }
    }
}