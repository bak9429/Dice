// Path: Assets/Script/Rule/Core/NodeSceneController.cs
using System.Collections.Generic;
using GameData.Nodes;
using Rule.NodeSystem;
using UI.UIBuilder.Node;
using UnityEngine;

namespace Rule.Core
{
    public partial class NodeSceneController : MonoBehaviour
    {
        [Header("Investigation")]
        [SerializeField] private InvestigationDefSO investigationDef;

        [Header("Debug Player Stats")]
        [SerializeField] private int playerSTR = 5;
        [SerializeField] private int playerDEX = 5;
        [SerializeField] private int playerSYNC = 5;

        private InvestigationRuntimeState _state;
        private NodeSceneViewRefs _view;
        private int _pendingNextStepIndex = -1;

        private string _selectedWeaknessId = "";
        private string _selectedPlaceId = "";

        private void Awake()
        {
            var _ = RunSession.Instance;
        }

        private void Start()
        {
            _view = NodeSceneBuilder.Build();

            if (investigationDef == null)
                investigationDef = ResolveInvestigationDefFromRunSession();

            if (investigationDef == null)
            {
                ShowFatalError(
                    "InvestigationDefSO를 찾지 못했다.",
                    "1) NodeSceneController 인스펙터에 직접 연결하거나\n" +
                    "2) Resources/GameData/Nodes/InvestigationDB 아래 asset의 bossId가 RunSession 중간보스 ID와 일치해야 한다.");
                return;
            }

            _state = InvestigationGenerator.Generate(investigationDef);

            RunSession.Instance.SetCurrentBoss(investigationDef.bossId);
            RunSession.Instance.ClearBossDebuffs();

            _view.nextButton.onClick.AddListener(OnClickNext);
            _view.submitDeductionButton.onClick.AddListener(OnSubmitDeduction);

            RefreshView();
        }

        private void RefreshView()
        {
            _view.SetBossName(investigationDef.displayName);
            _view.SetHintTexts(ToHintTexts(_state.collectedHints));
            _view.SetProgressText(BuildProgressText());

            if (_state.IsFinalNode(investigationDef.investigationNodes.Count))
            {
                RefreshFinalNode();
                return;
            }

            RefreshInvestigationNode();
        }

        private void RefreshInvestigationNode()
        {
            var node = GetCurrentNode();
            var variant = GetCurrentVariant(node);

            if (node == null || variant == null)
            {
                ShowFatalError("조사 노드 variant를 찾지 못했다.",
                    $"nodeIndex={_state.currentNodeIndex}, spawnPlace={_state.spawnPlaceId}");
                return;
            }

            if (_state.currentStepIndex < 0 || _state.currentStepIndex >= variant.steps.Count)
            {
                CompleteCurrentNode(variant);
                return;
            }

            var step = variant.steps[_state.currentStepIndex];
            _view.ShowInvestigationMode();
            _view.ClearChoiceListeners();
            _view.SetTitle(variant.displayName);
            _view.SetResultText("");

            switch (step.stepType)
            {
                case InvestigationStepType.Text:
                    ShowTextStep(step.textStep);
                    break;
                case InvestigationStepType.Choice:
                    ShowChoiceStep(step.choiceStep);
                    break;
                case InvestigationStepType.Result:
                    ShowResultStep(step.resultStep);
                    break;
                default:
                    ShowFatalError("알 수 없는 stepType", step.stepType.ToString());
                    break;
            }
        }

        private void OnClickNext()
        {
            if (_state.IsFinalNode(investigationDef.investigationNodes.Count))
            {
                HandleFinalNodeNext();
                return;
            }

            if (_pendingNextStepIndex >= 0)
            {
                _state.JumpToStep(_pendingNextStepIndex);
                _pendingNextStepIndex = -1;
            }
            else
            {
                var node = GetCurrentNode();
                var variant = GetCurrentVariant(node);
                var step = GetCurrentStep(variant);

                if (step != null && step.stepType == InvestigationStepType.Result &&
                    step.resultStep != null && step.resultStep.nextStepIndex >= 0)
                    _state.JumpToStep(step.resultStep.nextStepIndex);
                else
                    _state.AdvanceStep();
            }

            RefreshView();
        }

        private InvestigationDefSO ResolveInvestigationDefFromRunSession()
        {
            string bossId = RunSession.Instance.GetCurrentBossId();
            if (string.IsNullOrWhiteSpace(bossId))
            {
                Debug.LogWarning("[NodeSceneController] RunSession에서 현재 보스 ID를 가져오지 못했다.");
                return null;
            }

            var def = InvestigationDatabase.FindByBossId(bossId);
            if (def != null)
                Debug.Log($"[NodeSceneController] Auto resolved InvestigationDefSO: bossId={bossId}, asset={def.name}");

            return def;
        }

        private InvestigationNodeDef GetCurrentNode()
        {
            if (investigationDef == null) return null;
            if (_state.currentNodeIndex < 0 || _state.currentNodeIndex >= investigationDef.investigationNodes.Count)
                return null;

            return investigationDef.investigationNodes[_state.currentNodeIndex];
        }

        private NodeVariantBySpawnPlaceDef GetCurrentVariant(InvestigationNodeDef node)
        {
            if (node == null)
            {
                Debug.LogError("[NodeSceneController] node is null");
                return null;
            }

            int nodeIndex = _state != null ? _state.currentNodeIndex : -1;

            if (node.variantsBySpawnPlace == null || node.variantsBySpawnPlace.Count == 0)
            {
                Debug.LogError($"[NodeSceneController] nodeIndex={nodeIndex} has no variantsBySpawnPlace");
                return null;
            }

            for (int i = 0; i < node.variantsBySpawnPlace.Count; i++)
            {
                var v = node.variantsBySpawnPlace[i];
                if (v != null && v.spawnPlaceId == _state.spawnPlaceId)
                {
                    Debug.Log($"[NodeSceneController] matched variant: nodeIndex={nodeIndex}, spawnPlace={_state.spawnPlaceId}");
                    return v;
                }
            }

            Debug.LogWarning($"[NodeSceneController] no exact spawnPlace match. fallback first variant. nodeIndex={nodeIndex}, spawnPlace={_state.spawnPlaceId}");
            return node.variantsBySpawnPlace[0];
        }

        private InvestigationStepDef GetCurrentStep(NodeVariantBySpawnPlaceDef variant)
        {
            if (variant == null || variant.steps == null) return null;
            if (_state.currentStepIndex < 0 || _state.currentStepIndex >= variant.steps.Count)
                return null;

            return variant.steps[_state.currentStepIndex];
        }

        private string BuildProgressText()
        {
            int total = investigationDef.investigationNodes.Count + 1;
            int current = Mathf.Clamp(_state.currentNodeIndex + 1, 1, total);
            return $"Node {current}/{total}";
        }

        private static List<string> ToHintTexts(List<InvestigationHintData> hints)
        {
            var result = new List<string>();

            for (int i = 0; i < hints.Count; i++)
                result.Add($"- {hints[i].text} [{hints[i].qualityName}]");

            return result;
        }

        private void ShowFatalError(string title, string description)
        {
            if (_view == null) return;

            _view.ShowInvestigationMode();
            _view.SetTitle(title);
            _view.SetDescription(description);
            _view.SetResultText("");
            _view.SetNextButtonVisible(false);
        }
    }
}