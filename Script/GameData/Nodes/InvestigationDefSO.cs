// Path: Assets/Script/GameData/Nodes/InvestigationDefSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Nodes
{
    public enum HintQuality
    {
        Good = 0,
        Vague = 1,
        False = 2
    }

    public enum HintCategory
    {
        Weakness = 0,
        Trait = 1,
        Symbol = 2,
        Place = 3
    }

    [Flags]
    public enum SkipStatMask
    {
        None = 0,
        STR = 1 << 0,
        DEX = 1 << 1,
        SYNC = 1 << 2
    }

    public enum InvestigationStepType
    {
        Text = 0,
        Choice = 1,
        Result = 2
    }

    public enum InvestigationNodeRole
    {
        FirstTrace = 0,
        PathReveal = 1,
        BehaviorReveal = 2,
        LairReveal = 3
    }

    [CreateAssetMenu(fileName = "INV_NewBoss", menuName = "GameData/Investigation/Boss Investigation")]
    public class InvestigationDefSO : ScriptableObject
    {
        [Header("Boss")]
        public string bossId;
        public string displayName;
        public string themeId;

        [Header("Places")]
        public List<InvestigationPlaceDef> places = new();
        public List<string> spawnablePlaceIds = new();

        [Header("Investigation Nodes (1~4)")]
        public List<InvestigationNodeDef> investigationNodes = new();

        [Header("Final Deduction Node (5)")]
        public FinalDeductionNodeDef finalDeductionNode = new();

        [Header("Skip Rule")]
        public SkipRuleDef skipRule = new();

        [Header("Success Debuffs")]
        public List<string> successDebuffIds = new();
    }

    [Serializable]
    public class InvestigationPlaceDef
    {
        public string placeId;
        public string displayName;
        [TextArea(2, 6)] public string shortDescription;
        public string themeTag;
    }

    [Serializable]
    public class InvestigationNodeDef
    {
        public string nodeId;
        public InvestigationNodeRole nodeRole;
        public List<NodeVariantBySpawnPlaceDef> variantsBySpawnPlace = new();
    }

    [Serializable]
    public class NodeVariantBySpawnPlaceDef
    {
        public string spawnPlaceId;
        public string displayName;
        public string locationId;
        [TextArea(2, 6)] public string reviewSummaryText;
        public List<InvestigationStepDef> steps = new();
    }

    [Serializable]
    public class InvestigationStepDef
    {
        public InvestigationStepType stepType = InvestigationStepType.Text;
        public TextStepDef textStep = new();
        public ChoiceStepDef choiceStep = new();
        public ResultStepDef resultStep = new();
    }

    [Serializable]
    public class TextStepDef
    {
        [TextArea(3, 12)] public string text;
    }

    [Serializable]
    public class ChoiceStepDef
    {
        [TextArea(2, 8)] public string promptText;
        public List<ChoiceOptionDef> choices = new();
    }

    [Serializable]
    public class ChoiceOptionDef
    {
        public string choiceId;
        public string text;
        public int nextStepIndex = -1;
        [TextArea(2, 8)] public string resultText;
        public HintDef grantedHint = new();
        public int advantageValue = 0;
    }

    [Serializable]
    public class ResultStepDef
    {
        [TextArea(2, 8)] public string text;
        public HintDef grantedHint = new();
        public int advantageValue = 0;
        public int nextStepIndex = -1;
    }

    [Serializable]
    public class HintDef
    {
        public string hintId;
        [TextArea(2, 8)] public string text;
        public HintQuality quality = HintQuality.Good;
        public HintCategory category = HintCategory.Weakness;
        public string relatedAnswerId;
    }

    [Serializable]
    public class FinalDeductionNodeDef
    {
        public List<FinalReviewVariantDef> reviewVariantsBySpawnPlace = new();
        [TextArea(2, 8)] public string questionText;
        public List<DeductionOption> weaknessOptions = new();
        public List<DeductionOption> placeOptions = new();
        public string correctWeaknessId;
        public List<SpawnPlaceAnswerMap> correctPlaceMap = new();
    }

    [Serializable]
    public class FinalReviewVariantDef
    {
        public string spawnPlaceId;
        public List<TextStepDef> reviewSteps = new();
    }

    [Serializable]
    public class SpawnPlaceAnswerMap
    {
        public string spawnPlaceId;
        public string correctPlaceId;
    }

    [Serializable]
    public class DeductionOption
    {
        public string answerId;
        public string text;
    }

    [Serializable]
    public class SkipRuleDef
    {
        public bool allowSkip = true;
        public SkipStatMask statMask = SkipStatMask.STR;
        [Min(0f)] public float baseChance = 5f;
        [Min(0f)] public float perStatBonus = 2f;
        [Min(0f)] public float maxChance = 45f;
        [Min(0f)] public float maxInvestigationAdvantageBonus = 10f;
    }
}