// Path: Assets/Script/Rule/Combat/Reward/CombatRewardResolver.cs
using System.Collections.Generic;
using UnityEngine;
using GameData.Reward;

namespace Rule.Combat.Reward
{
    public enum RewardKind
    {
        Currency = 0,
        Equipment = 1,
    }

    public struct CombatRewardResult
    {
        public int diceRoll;
        public RewardKind rewardKind;
        public int currency;
        public string equipmentId;
        public string equipmentType;
    }

    public static class CombatRewardResolver
    {
        public static CombatRewardResult Resolve(string bossId, bool isGateBoss)
        {
            var def = LoadRewardDef(bossId, isGateBoss);
            int roll = Random.Range(1, 7);

            var result = new CombatRewardResult
            {
                diceRoll = roll,
                rewardKind = RewardKind.Currency,
                currency = 0,
                equipmentId = "",
                equipmentType = ""
            };

            if (roll <= 2)
            {
                result.currency = Random.Range(def.lowMin, def.lowMax + 1);
                return result;
            }

            if (roll <= 4)
            {
                result.currency = Random.Range(def.midMin, def.midMax + 1);
                return result;
            }

            if (roll == 5)
            {
                result.currency = Random.Range(def.highMin, def.highMax + 1);
                return result;
            }

            result.rewardKind = RewardKind.Equipment;
            RollEquipment(def, out result.equipmentId, out result.equipmentType);
            return result;
        }

        private static RewardDefSO LoadRewardDef(string bossId, bool isGateBoss)
        {
            var byBoss = Resources.Load<RewardDefSO>($"GameData/Reward/{bossId}");
            if (byBoss != null) return byBoss;

            var byTier = Resources.Load<RewardDefSO>(
                isGateBoss ? "GameData/Reward/GateBoss_DefaultReward"
                           : "GameData/Reward/MidBoss_DefaultReward");

            if (byTier != null) return byTier;

            return BuildFallback(isGateBoss);
        }

        private static RewardDefSO BuildFallback(bool isGateBoss)
        {
            var def = ScriptableObject.CreateInstance<RewardDefSO>();

            if (!isGateBoss)
            {
                def.rewardId = "fallback_midboss";
                def.lowMin = 5;
                def.lowMax = 8;
                def.midMin = 10;
                def.midMax = 14;
                def.highMin = 18;
                def.highMax = 24;
            }
            else
            {
                def.rewardId = "fallback_gateboss";
                def.lowMin = 12;
                def.lowMax = 18;
                def.midMin = 22;
                def.midMax = 30;
                def.highMin = 40;
                def.highMax = 55;
            }

            def.meleePool = new List<string>
            {
                "melee_iron_blade_01",
                "melee_bone_cleaver_01"
            };

            def.gunPool = new List<string>
            {
                "gun_rust_pistol_01",
                "gun_hunter_rifle_01"
            };

            return def;
        }

        private static void RollEquipment(
            RewardDefSO def,
            out string equipmentId,
            out string equipmentType)
        {
            var pool = new List<(string id, string type)>();

            foreach (var melee in def.meleePool)
                if (!string.IsNullOrWhiteSpace(melee))
                    pool.Add((melee, "Melee"));

            foreach (var gun in def.gunPool)
                if (!string.IsNullOrWhiteSpace(gun))
                    pool.Add((gun, "Gun"));

            if (pool.Count == 0)
            {
                equipmentId = "equipment_unknown";
                equipmentType = "Equipment";
                return;
            }

            int idx = Random.Range(0, pool.Count);
            equipmentId = pool[idx].id;
            equipmentType = pool[idx].type;
        }
    }
}