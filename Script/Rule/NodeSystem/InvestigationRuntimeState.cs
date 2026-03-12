// Path: Assets/Script/Rule/NodeSystem/InvestigationRuntimeState.cs
using System;
using System.Collections.Generic;

namespace Rule.NodeSystem
{
    [Serializable]
    public sealed class InvestigationHintData
    {
        public string hintId = "";
        public string text = "";
        public string qualityName = "";
        public string categoryName = "";
        public string relatedAnswerId = "";
    }

    [Serializable]
    public sealed class InvestigationRuntimeState
    {
        public string bossId = "";
        public string bossDisplayName = "";
        public string spawnPlaceId = "";

        public int currentNodeIndex = 0;
        public int currentStepIndex = 0;

        public int currentReviewStepIndex = 0;
        public bool deductionPhaseStarted = false;

        public List<InvestigationHintData> collectedHints = new();
        public List<string> reviewSummaries = new();
        public List<string> resolvedNodeIds = new();

        public int accumulatedAdvantage = 0;

        public bool deductionResolved = false;
        public bool deductionCorrect = false;
        public List<string> grantedDebuffIds = new();

        public bool skipRolled = false;
        public bool skipSucceeded = false;
        public float skipChance = 0f;
        public int skipRollValue = -1;

        public HashSet<string> appliedTokens = new();

        public bool IsFinalNode(int investigationNodeCount)
        {
            return currentNodeIndex >= investigationNodeCount;
        }

        public void AdvanceStep()
        {
            currentStepIndex++;
        }

        public void JumpToStep(int stepIndex)
        {
            currentStepIndex = stepIndex < 0 ? 0 : stepIndex;
        }

        public void AdvanceNode()
        {
            currentNodeIndex++;
            currentStepIndex = 0;
        }

        public void AdvanceReviewStep()
        {
            currentReviewStepIndex++;
        }

        public bool MarkApplied(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            if (appliedTokens.Contains(token)) return false;
            appliedTokens.Add(token);
            return true;
        }
    }
}