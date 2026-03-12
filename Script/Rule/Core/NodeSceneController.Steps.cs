// Path: Assets/Script/Rule/Core/NodeSceneController.Steps.cs
using GameData.Nodes;
using Rule.NodeSystem;

namespace Rule.Core
{
    public partial class NodeSceneController
    {
        private void ShowTextStep(TextStepDef step)
        {
            _view.SetDescription(step != null ? step.text : "");
            _view.SetNextButtonVisible(true);

            for (int i = 0; i < _view.choiceButtons.Length; i++)
                _view.SetChoiceButton(i, false, "");
        }

        private void ShowChoiceStep(ChoiceStepDef step)
        {
            _view.SetDescription(step != null ? step.promptText : "");
            _view.SetNextButtonVisible(false);

            for (int i = 0; i < _view.choiceButtons.Length; i++)
            {
                if (step != null && step.choices != null && i < step.choices.Count)
                {
                    int idx = i;
                    var choice = step.choices[idx];
                    _view.SetChoiceButton(i, true, choice.text);
                    _view.choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(step, idx));
                }
                else
                {
                    _view.SetChoiceButton(i, false, "");
                }
            }
        }

        private void ShowResultStep(ResultStepDef step)
        {
            _view.SetDescription(step != null ? step.text : "");
            _view.SetNextButtonVisible(true);

            for (int i = 0; i < _view.choiceButtons.Length; i++)
                _view.SetChoiceButton(i, false, "");

            if (step == null) return;

            string token = $"result:{_state.currentNodeIndex}:{_state.currentStepIndex}";
            if (_state.MarkApplied(token))
            {
                ApplyHint(step.grantedHint);
                AddAdvantage(step.advantageValue);
            }
        }

        private void OnChoiceSelected(ChoiceStepDef step, int choiceIndex)
        {
            if (step == null || step.choices == null) return;
            if (choiceIndex < 0 || choiceIndex >= step.choices.Count) return;

            var choice = step.choices[choiceIndex];
            string token = $"choice:{_state.currentNodeIndex}:{_state.currentStepIndex}:{choice.choiceId}";

            if (_state.MarkApplied(token))
            {
                ApplyHint(choice.grantedHint);
                AddAdvantage(choice.advantageValue);
            }

            _view.SetResultText(choice.resultText ?? "");
            _view.SetHintTexts(ToHintTexts(_state.collectedHints));
            _view.SetNextButtonVisible(true);

            for (int i = 0; i < _view.choiceButtons.Length; i++)
                _view.SetChoiceInteractable(i, false);

            _pendingNextStepIndex = choice.nextStepIndex;
        }

        private void CompleteCurrentNode(NodeVariantBySpawnPlaceDef variant)
        {
            if (variant != null)
            {
                if (!string.IsNullOrWhiteSpace(variant.reviewSummaryText))
                    _state.reviewSummaries.Add(variant.reviewSummaryText);

                if (!_state.resolvedNodeIds.Contains(variant.spawnPlaceId + ":" + variant.displayName))
                    _state.resolvedNodeIds.Add(variant.spawnPlaceId + ":" + variant.displayName);
            }

            _pendingNextStepIndex = -1;
            _state.AdvanceNode();
            RefreshView();
        }

        private void ApplyHint(HintDef hint)
        {
            if (hint == null || string.IsNullOrWhiteSpace(hint.hintId))
                return;

            _state.collectedHints.Add(new InvestigationHintData
            {
                hintId = hint.hintId,
                text = hint.text,
                qualityName = hint.quality.ToString(),
                categoryName = hint.category.ToString(),
                relatedAnswerId = hint.relatedAnswerId
            });

            if (hint.quality == HintQuality.Good)
                AddAdvantage(2);
        }

        private void AddAdvantage(int amount)
        {
            if (amount <= 0) return;
            _state.accumulatedAdvantage += amount;
        }
    }
}