// Path: Assets/Script/Rule/Core/NodeSceneController.Final.cs
using System.Collections.Generic;
using GameData.Nodes;
using UnityEngine;
using UnityEngine.UI;

namespace Rule.Core
{
    public partial class NodeSceneController
    {
        private void HandleFinalNodeNext()
        {
            var finalNode = investigationDef.finalDeductionNode;

            if (!_state.deductionPhaseStarted)
            {
                var reviewVariant = GetFinalReviewVariant(finalNode);

                if (reviewVariant != null && _state.currentReviewStepIndex < reviewVariant.reviewSteps.Count - 1)
                {
                    _state.AdvanceReviewStep();
                    RefreshView();
                    return;
                }

                _state.deductionPhaseStarted = true;
                RefreshView();
            }
        }

        private void RefreshFinalNode()
        {
            _view.ShowDeductionMode();
            _view.SetResultText("");

            if (!_state.deductionPhaseStarted)
            {
                ShowFinalReview(investigationDef.finalDeductionNode);
                return;
            }

            ShowFinalDeduction(investigationDef.finalDeductionNode);
        }

        private FinalReviewVariantDef GetFinalReviewVariant(FinalDeductionNodeDef finalNode)
        {
            if (finalNode == null || finalNode.reviewVariantsBySpawnPlace == null) return null;

            for (int i = 0; i < finalNode.reviewVariantsBySpawnPlace.Count; i++)
            {
                if (finalNode.reviewVariantsBySpawnPlace[i].spawnPlaceId == _state.spawnPlaceId)
                    return finalNode.reviewVariantsBySpawnPlace[i];
            }

            return finalNode.reviewVariantsBySpawnPlace.Count > 0 ? finalNode.reviewVariantsBySpawnPlace[0] : null;
        }

        private void ShowFinalReview(FinalDeductionNodeDef finalNode)
        {
            _view.SetTitle("종합");
            _view.SetNextButtonVisible(true);

            string reviewText = "";
            var reviewVariant = GetFinalReviewVariant(finalNode);

            if (reviewVariant != null &&
                _state.currentReviewStepIndex >= 0 &&
                _state.currentReviewStepIndex < reviewVariant.reviewSteps.Count)
            {
                reviewText = reviewVariant.reviewSteps[_state.currentReviewStepIndex].text;
            }

            string summaries = _state.reviewSummaries.Count == 0
                ? ""
                : "\n\n[조사 정리]\n- " + string.Join("\n- ", _state.reviewSummaries);

            _view.SetDescription(reviewText + summaries);
            HideDeductionButtons();
        }

        private void ShowFinalDeduction(FinalDeductionNodeDef finalNode)
        {
            _view.SetTitle("추리");
            _view.SetDescription(finalNode.questionText);
            _view.SetNextButtonVisible(false);

            _selectedWeaknessId = "";
            _selectedPlaceId = "";

            BindDeductionOptions(_view.weaknessButtons, finalNode.weaknessOptions, id => _selectedWeaknessId = id, "약점");
            BindDeductionOptions(_view.placeButtons, finalNode.placeOptions, id => _selectedPlaceId = id, "장소");
            _view.submitDeductionButton.gameObject.SetActive(true);
        }

        private void HideDeductionButtons()
        {
            _view.submitDeductionButton.gameObject.SetActive(false);

            for (int i = 0; i < _view.weaknessButtons.Length; i++)
                _view.weaknessButtons[i].gameObject.SetActive(false);

            for (int i = 0; i < _view.placeButtons.Length; i++)
                _view.placeButtons[i].gameObject.SetActive(false);
        }

        private void BindDeductionOptions(Button[] buttons, List<DeductionOption> options, System.Action<string> onSelect, string axisName)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.RemoveAllListeners();

                if (options != null && i < options.Count)
                {
                    var option = options[i];
                    buttons[i].gameObject.SetActive(true);

                    var txt = buttons[i].GetComponentInChildren<Text>();
                    if (txt != null) txt.text = option.text;

                    buttons[i].onClick.AddListener(() =>
                    {
                        onSelect(option.answerId);
                        _view.SetResultText($"{axisName} 선택: {option.text}");
                    });
                }
                else
                {
                    buttons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnSubmitDeduction()
        {
            var finalNode = investigationDef.finalDeductionNode;

            if (string.IsNullOrEmpty(_selectedWeaknessId) || string.IsNullOrEmpty(_selectedPlaceId))
            {
                _view.SetResultText("약점과 장소를 모두 선택해야 한다.");
                return;
            }

            bool weaknessCorrect = _selectedWeaknessId == finalNode.correctWeaknessId;
            bool placeCorrect = _selectedPlaceId == ResolveCorrectPlaceId(finalNode, _state.spawnPlaceId);
            bool allCorrect = weaknessCorrect && placeCorrect;

            _state.deductionResolved = true;
            _state.deductionCorrect = allCorrect;
            _state.grantedDebuffIds.Clear();

            if (allCorrect)
            {
                _state.grantedDebuffIds.AddRange(investigationDef.successDebuffIds);
                foreach (var debuffId in investigationDef.successDebuffIds)
                    RunSession.Instance.AddBossDebuff(debuffId);
            }

            float finalChance = BuildSkipChance(
                investigationDef.skipRule,
                playerSTR,
                playerDEX,
                playerSYNC,
                _state.accumulatedAdvantage);

            int roll = Random.Range(0, 100);
            bool canSkip = investigationDef.skipRule != null && investigationDef.skipRule.allowSkip && allCorrect;
            bool skipSucceeded = canSkip && roll < finalChance;

            _state.skipRolled = canSkip;
            _state.skipSucceeded = skipSucceeded;
            _state.skipChance = finalChance;
            _state.skipRollValue = roll;

            if (!canSkip)
            {
                _view.SetResultText(allCorrect
                    ? "추리에는 성공했지만 스킵 조건은 충족되지 않았다. 전투에 진입한다."
                    : "추리에 실패했다. 디버프 없이 전투에 진입한다.");

                _view.SetNextButtonVisible(true);
                _view.nextButton.onClick.RemoveAllListeners();
                _view.nextButton.onClick.AddListener(() => RunSession.Instance.CompleteInvestigationToCombat());
                return;
            }

            if (skipSucceeded)
            {
                _view.SetResultText(
                    $"추리에 성공했다.\n조사 어드벤테이지: +{_state.accumulatedAdvantage}\n" +
                    $"스킵 판정 성공! (확률 {finalChance:0.#}% / 굴림 {roll})\n허브로 복귀한다.");

                _view.SetNextButtonVisible(true);
                _view.nextButton.onClick.RemoveAllListeners();
                _view.nextButton.onClick.AddListener(() => RunSession.Instance.AbortRunToHub());
            }
            else
            {
                _view.SetResultText(
                    $"추리에 성공했다. 디버프를 부여했다.\n조사 어드벤테이지: +{_state.accumulatedAdvantage}\n" +
                    $"스킵 판정 실패. (확률 {finalChance:0.#}% / 굴림 {roll})\n전투에 진입한다.");

                _view.SetNextButtonVisible(true);
                _view.nextButton.onClick.RemoveAllListeners();
                _view.nextButton.onClick.AddListener(() => RunSession.Instance.CompleteInvestigationToCombat());
            }
        }

        private string ResolveCorrectPlaceId(FinalDeductionNodeDef finalNode, string spawnPlaceId)
        {
            if (finalNode == null || finalNode.correctPlaceMap == null)
                return "";

            for (int i = 0; i < finalNode.correctPlaceMap.Count; i++)
            {
                if (finalNode.correctPlaceMap[i].spawnPlaceId == spawnPlaceId)
                    return finalNode.correctPlaceMap[i].correctPlaceId;
            }

            return finalNode.correctPlaceMap.Count > 0 ? finalNode.correctPlaceMap[0].correctPlaceId : "";
        }

        private float BuildSkipChance(SkipRuleDef rule, int str, int dex, int sync, int investigationAdvantage)
        {
            if (rule == null || !rule.allowSkip)
                return 0f;

            int statSum = 0;
            if ((rule.statMask & SkipStatMask.STR) != 0) statSum += Mathf.Max(0, str);
            if ((rule.statMask & SkipStatMask.DEX) != 0) statSum += Mathf.Max(0, dex);
            if ((rule.statMask & SkipStatMask.SYNC) != 0) statSum += Mathf.Max(0, sync);

            float baseChance = rule.baseChance + statSum * rule.perStatBonus;
            float advBonus = Mathf.Clamp(investigationAdvantage, 0f, rule.maxInvestigationAdvantageBonus);
            return Mathf.Clamp(baseChance + advBonus, 0f, 100f);
        }
    }
}