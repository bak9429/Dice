// Path: Assets/Script/Rule/Core/RunSession.cs
using System.Collections.Generic;
using UnityEngine;

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
            CurrentGateId = "";
            CurrentAreaIndex = 0;
            CurrentNodeIndex = 0;
            CurrentBossId = "";
            RunCurrency = 0;

            IsGateActive = false;
            IsGateBossPhase = false;
            IsCombatPhase = false;

            _gateBossId = "";

            CurrentBossDebuffs.Clear();
            AcquiredDrops.Clear();
            MinibossOrder.Clear();
        }

        public void BeginGate(string gateId)
        {
            ClearGateProgressOnly();

            CurrentGateId = gateId;
            CurrentAreaIndex = 0;
            CurrentNodeIndex = 0;

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
            SceneFlow.GoToHub();
        }

        public bool CanReturnToHubAfterVictory()
        {
            return IsGateActive && !IsCombatPhase;
        }

        public void ContinueAfterVictory()
        {
            if (!IsGateActive)
            {
                Debug.LogWarning("[RunSession] ContinueAfterVictory called without active gate.");
                SceneFlow.GoToHub();
                return;
            }

            Debug.Log($"[RunSession] ContinueAfterVictory bossId={CurrentBossId}, gateBossPhase={IsGateBossPhase}");

            CurrentNodeIndex = 0;
            CurrentBossDebuffs.Clear();

            if (!IsGateBossPhase)
            {
                CurrentAreaIndex++;

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

                SceneFlow.GoToNode();
                return;
            }

            ClearGateProgressOnly();
            SceneFlow.GoToHub();
        }

        public void CompleteCombatVictory()
        {
            IsCombatPhase = false;
        }

        public void CompleteCombatDefeat()
        {
            Debug.Log("[RunSession] Combat Defeat -> lose all run currency and return hub");
            IsCombatPhase = false;
            LoseAllRunCurrency();
            ClearGateProgressOnly();
            SceneFlow.GoToHub();
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