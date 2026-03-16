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

            InvestigationDefSO best = null;
            int bestScore = -1;
            int matchCount = 0;

            for (int i = 0; i < _cache.Length; i++)
            {
                var def = _cache[i];
                if (def == null || def.bossId != bossId)
                    continue;

                matchCount++;
                int score = Score(def);

                Debug.Log($"[InvestigationDatabase] candidate bossId={bossId}, asset={def.name}, score={score}");

                if (score > bestScore)
                {
                    best = def;
                    bestScore = score;
                }
            }

            if (best != null)
            {
                if (matchCount > 1)
                {
                    Debug.LogWarning(
                        $"[InvestigationDatabase] Duplicate bossId detected: {bossId}. " +
                        $"Selected asset={best.name}, score={bestScore}, matches={matchCount}");
                }
                else
                {
                    Debug.Log($"[InvestigationDatabase] Selected asset={best.name}, score={bestScore}");
                }

                return best;
            }

            Debug.LogWarning($"[InvestigationDatabase] bossId not found: {bossId}");
            return null;
        }

        private static int Score(InvestigationDefSO def)
        {
            if (def == null) return -1;

            int score = 0;

            if (def.investigationNodes != null)
            {
                score += def.investigationNodes.Count * 100;

                for (int i = 0; i < def.investigationNodes.Count; i++)
                {
                    var node = def.investigationNodes[i];
                    if (node == null || node.variantsBySpawnPlace == null)
                        continue;

                    score += node.variantsBySpawnPlace.Count * 20;

                    for (int j = 0; j < node.variantsBySpawnPlace.Count; j++)
                    {
                        var variant = node.variantsBySpawnPlace[j];
                        if (variant == null || variant.steps == null)
                            continue;

                        score += variant.steps.Count;
                    }
                }
            }

            if (def.finalDeductionNode != null)
            {
                if (def.finalDeductionNode.reviewVariantsBySpawnPlace != null)
                    score += def.finalDeductionNode.reviewVariantsBySpawnPlace.Count * 10;

                if (def.finalDeductionNode.weaknessOptions != null)
                    score += def.finalDeductionNode.weaknessOptions.Count;

                if (def.finalDeductionNode.placeOptions != null)
                    score += def.finalDeductionNode.placeOptions.Count;
            }

            return score;
        }
    }
}