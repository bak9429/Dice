// Path: Assets/Script/Rule/NodeSystem/InvestigationGenerator.cs
using System.Collections.Generic;
using GameData.Nodes;
using UnityEngine;

namespace Rule.NodeSystem
{
    public static class InvestigationGenerator
    {
        public static InvestigationRuntimeState Generate(InvestigationDefSO def)
        {
            var state = new InvestigationRuntimeState();

            if (def == null)
            {
                Debug.LogError("[InvestigationGenerator] InvestigationDefSO is null.");
                return state;
            }

            state.bossId = def.bossId;
            state.bossDisplayName = def.displayName;
            state.spawnPlaceId = PickSpawnPlace(def);
            state.currentNodeIndex = 0;
            state.currentStepIndex = 0;
            state.currentReviewStepIndex = 0;
            state.deductionPhaseStarted = false;

            Debug.Log($"[InvestigationGenerator] boss={def.bossId}, spawnPlace={state.spawnPlaceId}");
            return state;
        }

        private static string PickSpawnPlace(InvestigationDefSO def)
        {
            var candidates = new List<string>();

            if (def.spawnablePlaceIds != null)
            {
                for (int i = 0; i < def.spawnablePlaceIds.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(def.spawnablePlaceIds[i]))
                        candidates.Add(def.spawnablePlaceIds[i]);
                }
            }

            if (candidates.Count == 0 && def.places != null)
            {
                for (int i = 0; i < def.places.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(def.places[i].placeId))
                        candidates.Add(def.places[i].placeId);
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[InvestigationGenerator] No spawn place candidates found.");
                return "";
            }

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}