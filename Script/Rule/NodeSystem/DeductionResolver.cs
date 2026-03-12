// Path: Assets/Script/Rule/NodeSystem/DeductionResolver.cs
using System;
using System.Collections.Generic;

namespace Rule.NodeSystem
{
    [Serializable]
    public sealed class DeductionAnswerSelection
    {
        public string selectedWeaknessId = "";
        public string selectedPlaceId = "";
    }

    [Serializable]
    public sealed class DeductionResult
    {
        public bool isCorrect = false;
        public int matchedAxisCount = 0;
        public List<string> grantedDebuffIds = new List<string>();
        public bool canRollSkip = false;
    }

    public static class DeductionResolver
    {
        // 현재 구조에서는 NodeSceneController 쪽에서 직접 판정하고 있어서
        // 이 클래스는 "기존 참조 깨짐 방지 + 최소 공용 판정" 용도로만 유지한다.
        public static DeductionResult Resolve(
            string selectedWeaknessId,
            string selectedPlaceId,
            string correctWeaknessId,
            string correctPlaceId,
            List<string> successDebuffIds,
            bool allowSkip)
        {
            var result = new DeductionResult();

            int matched = 0;

            if (!string.IsNullOrEmpty(selectedWeaknessId) &&
                selectedWeaknessId == correctWeaknessId)
            {
                matched++;
            }

            if (!string.IsNullOrEmpty(selectedPlaceId) &&
                selectedPlaceId == correctPlaceId)
            {
                matched++;
            }

            result.matchedAxisCount = matched;
            result.isCorrect = matched >= 2;

            if (result.isCorrect && successDebuffIds != null)
                result.grantedDebuffIds.AddRange(successDebuffIds);

            result.canRollSkip = result.isCorrect && allowSkip;
            return result;
        }
    }
}