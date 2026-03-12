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

        public readonly List<string> CurrentBossDebuffs = new List<string>();
        public readonly List<string> AcquiredDrops = new List<string>();
        public readonly List<string> MinibossOrder = new List<string>();

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

            CurrentBossDebuffs.Clear();
            AcquiredDrops.Clear();
            MinibossOrder.Clear();
        }

        public void BeginGate(string gateId)
        {
            ResetAll();
            CurrentGateId = gateId;
            CurrentAreaIndex = 0;
            CurrentNodeIndex = 0;

            var pool = new List<string> { "MidBoss_A", "MidBoss_B", "MidBoss_C" };
            Shuffle(pool);

            MinibossOrder.Clear();
            MinibossOrder.AddRange(pool);

            Debug.Log($"[RunSession] BeginGate gateId={gateId}, order={string.Join(", ", MinibossOrder)}");
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
            CurrentBossId = bossId;
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

        public string GetCurrentMinibossId()
        {
            if (CurrentAreaIndex < 0 || CurrentAreaIndex >= MinibossOrder.Count)
                return "";
            return MinibossOrder[CurrentAreaIndex];
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