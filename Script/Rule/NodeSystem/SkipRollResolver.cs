// Path: Assets/Script/Rule/NodeSystem/SkipRollResolver.cs
using System;
using GameData.Nodes;
using UnityEngine;

namespace Rule.NodeSystem
{
    [Serializable]
    public sealed class SkipRollResult
    {
        public bool attempted = false;
        public bool succeeded = false;
        public float finalChance = 0f;
        public int roll = -1;
    }

    public static class SkipRollResolver
    {
        public static SkipRollResult Resolve(
            SkipRuleDef rule,
            int str,
            int dex,
            int sync,
            int investigationAdvantage = 0)
        {
            var result = new SkipRollResult();

            if (rule == null || !rule.allowSkip)
                return result;

            result.attempted = true;

            int statSum = 0;

            if ((rule.statMask & SkipStatMask.STR) != 0)
                statSum += Mathf.Max(0, str);

            if ((rule.statMask & SkipStatMask.DEX) != 0)
                statSum += Mathf.Max(0, dex);

            if ((rule.statMask & SkipStatMask.SYNC) != 0)
                statSum += Mathf.Max(0, sync);

            float baseChance = rule.baseChance + statSum * rule.perStatBonus;
            float advantageBonus = Mathf.Clamp(
                investigationAdvantage,
                0f,
                rule.maxInvestigationAdvantageBonus);

            float finalChance = Mathf.Clamp(baseChance + advantageBonus, 0f, 100f);

            int roll = UnityEngine.Random.Range(0, 100);
            bool success = roll < finalChance;

            result.finalChance = finalChance;
            result.roll = roll;
            result.succeeded = success;

            Debug.Log(
                $"[SkipRollResolver] statSum={statSum}, adv={investigationAdvantage}, chance={finalChance:0.##}, roll={roll}, success={success}"
            );

            return result;
        }
    }
}