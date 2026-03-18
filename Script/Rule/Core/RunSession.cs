// Path: Assets/Script/Rule/Core/RunSession.cs
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Rule.Combat.Reward;

namespace Rule.Core
{
    public class RunSession : MonoBehaviour
    {
        private static RunSession _instance;

        public static RunSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(RunSession));
                    _instance = go.AddComponent<RunSession>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        public string CurrentGateId { get; private set; } = "";
        public int CurrentAreaIndex { get; private set; } = 0;
        public int CurrentNodeIndex { get; private set; } = 0;
        public string CurrentBossId { get; private set; } = "";
        public int RunCurrency { get; private set; } = 0;

        public bool IsGateActive { get; private set; } = false;
        public bool IsGateBossPhase { get; private set; } = false;
        public bool IsCombatPhase { get; private set; } = false;

        public bool HasPendingResult { get; private set; } = false;
        public bool LastCombatWon { get; private set; } = false;
        public bool LastCombatWasGateBoss { get; private set; } = false;
        public string LastBossId { get; private set; } = "";
        public int LastRewardCurrency { get; private set; } = 0;
        public int LastLostCurrency { get; private set; } = 0;
        public int LastRewardDiceRoll { get; private set; } = 0;
        public string LastRewardEquipmentId { get; private set; } = "";
        public string LastRewardEquipmentType { get; private set; } = "";

        public readonly List<string> CurrentBossDebuffs = new List<string>();
        public readonly List<string> AcquiredDrops = new List<string>();
        public readonly List<string> MinibossOrder = new List<string>();

        private string _gateBossId = "";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetAll()
        {
            ClearGateProgressOnly();
            ClearPendingResultOnly();
            RunCurrency = 0;
            AcquiredDrops.Clear();
        }

        public void BeginGate(string gateId)
        {
            ClearGateProgressOnly();
            ClearPendingResultOnly();
            RunCurrency = 0;
            AcquiredDrops.Clear();

            CurrentGateId = gateId;
            CurrentAreaIndex = 0;
            CurrentNodeIndex = 0;
            CurrentBossId = "";

            IsGateActive = true;
            IsGateBossPhase = false;
            IsCombatPhase = false;

            _gateBossId = ResolveGateBossId(gateId);

            var pool = ResolveMidBossPool(gateId);
            Shuffle(pool);

            MinibossOrder.Clear();
            MinibossOrder.AddRange(pool);

            CurrentBossId = GetCurrentMinibossId();

            Debug.Log(
                $"[RunSession] BeginGate gateId={gateId}, gateBoss={_gateBossId}, order={string.Join(", ", MinibossOrder)}");
        }

        public void SetCurrentNodeIndex(int nodeIndex)
        {
            CurrentNodeIndex = Mathf.Max(0, nodeIndex);
        }

        public void SetCurrentAreaIndex(int areaIndex)
        {
            CurrentAreaIndex = Mathf.Max(0, areaIndex);
        }

        public void SetCurrentBoss(string bossId)
        {
            CurrentBossId = bossId ?? "";
        }

        public void ClearBossDebuffs()
        {
            CurrentBossDebuffs.Clear();
        }

        public void AddBossDebuff(string debuffId)
        {
            if (string.IsNullOrWhiteSpace(debuffId)) return;
            if (!CurrentBossDebuffs.Contains(debuffId))
                CurrentBossDebuffs.Add(debuffId);
        }

        public void AddRunCurrency(int amount)
        {
            RunCurrency = Mathf.Max(0, RunCurrency + amount);
        }

        public void LoseAllRunCurrency()
        {
            RunCurrency = 0;
        }

        public void AddDrop(string dropId)
        {
            if (string.IsNullOrWhiteSpace(dropId)) return;
            if (!AcquiredDrops.Contains(dropId))
                AcquiredDrops.Add(dropId);
        }

        public void SetPendingCombatRewards(CombatRewardResult reward)
        {
            LastRewardDiceRoll = reward.diceRoll;
            LastRewardCurrency = reward.currency;
            LastRewardEquipmentId = reward.equipmentId ?? "";
            LastRewardEquipmentType = reward.equipmentType ?? "";

            if (LastRewardCurrency > 0)
                AddRunCurrency(LastRewardCurrency);

            if (!string.IsNullOrWhiteSpace(LastRewardEquipmentId))
                AddDrop(LastRewardEquipmentId);
        }

        public string GetCurrentBossId()
        {
            return CurrentBossId;
        }

        public string GetCurrentMinibossId()
        {
            if (CurrentAreaIndex < 0 || CurrentAreaIndex >= MinibossOrder.Count)
                return "";
            return MinibossOrder[CurrentAreaIndex];
        }

        public void CompleteInvestigationToCombat()
        {
            if (!IsGateActive)
            {
                Debug.LogWarning("[RunSession] CompleteInvestigationToCombat called without active gate.");
                SceneFlow.GoToHub();
                return;
            }

            IsCombatPhase = true;
            Debug.Log($"[RunSession] Investigation -> Combat : boss={CurrentBossId}");
            SceneFlow.GoToCombat();
        }

        public void AbortRunToHub()
        {
            Debug.Log("[RunSession] AbortRunToHub");
            ClearGateProgressOnly();
            ClearPendingResultOnly();
            SceneFlow.GoToHub();
        }

        public void CompleteCombatVictory()
        {
            if (HasPendingResult) return;

            if (!IsGateActive)
            {
                Debug.LogWarning("[RunSession] CompleteCombatVictory called without active gate.");
                SceneFlow.GoToHub();
                return;
            }

            IsCombatPhase = false;
            HasPendingResult = true;
            LastCombatWon = true;
            LastCombatWasGateBoss = IsGateBossPhase;
            LastBossId = CurrentBossId;
            LastLostCurrency = 0;

            Debug.Log($"[RunSession] CompleteCombatVictory boss={LastBossId}, gateBoss={LastCombatWasGateBoss}");
            SceneFlow.GoToResult();
        }

        public void CompleteCombatDefeat()
        {
            if (HasPendingResult) return;

            Debug.Log("[RunSession] CompleteCombatDefeat -> Result");
            IsCombatPhase = false;
            HasPendingResult = true;
            LastCombatWon = false;
            LastCombatWasGateBoss = IsGateBossPhase;
            LastBossId = CurrentBossId;
            LastLostCurrency = RunCurrency;

            LastRewardCurrency = 0;
            LastRewardDiceRoll = 0;
            LastRewardEquipmentId = "";
            LastRewardEquipmentType = "";

            SceneFlow.GoToResult();
        }

        public bool CanContinueAfterResult()
        {
            return HasPendingResult && LastCombatWon && !LastCombatWasGateBoss;
        }

        public void ContinueAfterResult()
        {
            if (!CanContinueAfterResult())
            {
                ReturnToHubAfterResult();
                return;
            }

            CurrentAreaIndex++;
            CurrentNodeIndex = 0;
            CurrentBossDebuffs.Clear();

            if (CurrentAreaIndex >= MinibossOrder.Count)
            {
                IsGateBossPhase = true;
                CurrentBossId = _gateBossId;
            }
            else
            {
                IsGateBossPhase = false;
                CurrentBossId = GetCurrentMinibossId();
            }

            ClearPendingResultOnly();
            SceneFlow.GoToNode();
        }

        public void ReturnToHubAfterResult()
        {
            if (HasPendingResult && !LastCombatWon)
                LoseAllRunCurrency();

            ClearGateProgressOnly();
            ClearPendingResultOnly();
            SceneFlow.GoToHub();
        }

        public string BuildResultSummary()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Boss: {LastBossId}");

            if (LastRewardDiceRoll > 0)
                sb.AppendLine($"Dice: {LastRewardDiceRoll}");

            sb.AppendLine($"Reward Currency: {LastRewardCurrency}");
            sb.AppendLine($"Run Currency: {RunCurrency}");

            if (!string.IsNullOrWhiteSpace(LastRewardEquipmentId))
                sb.AppendLine($"Equipment: [{LastRewardEquipmentType}] {LastRewardEquipmentId}");
            else
                sb.AppendLine("Equipment: None");

            if (!LastCombatWon)
                sb.AppendLine($"Lost Currency: {LastLostCurrency}");

            return sb.ToString();
        }

        private void ClearPendingResultOnly()
        {
            HasPendingResult = false;
            LastCombatWon = false;
            LastCombatWasGateBoss = false;
            LastBossId = "";
            LastRewardCurrency = 0;
            LastLostCurrency = 0;
            LastRewardDiceRoll = 0;
            LastRewardEquipmentId = "";
            LastRewardEquipmentType = "";
        }

        private void ClearGateProgressOnly()
        {
            CurrentGateId = "";
            CurrentAreaIndex = 0;
            CurrentNodeIndex = 0;
            CurrentBossId = "";

            IsGateActive = false;
            IsGateBossPhase = false;
            IsCombatPhase = false;

            _gateBossId = "";

            CurrentBossDebuffs.Clear();
            MinibossOrder.Clear();
        }

        private static List<string> ResolveMidBossPool(string gateId)
        {
            return new List<string>
            {
                "midboss_sabertooth_mutant",
                "midboss_bone_stalker",
                "midboss_cave_devourer",
            };
        }

        private static string ResolveGateBossId(string gateId)
        {
            return "gateboss_bone_shaman";
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}