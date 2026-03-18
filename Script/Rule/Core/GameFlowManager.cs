// Path: Assets/Script/Rule/Core/GameFlowManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using Rule.Combat.Boss;
using Rule.Combat.Player;
using Rule.Combat.Reward;

namespace Rule.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        private bool _resolved;
        private bool _bossSeenAlive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
            if (existing != null) return;

            var go = new GameObject("GameFlowManager");
            DontDestroyOnLoad(go);
            go.AddComponent<GameFlowManager>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            PlayerController.OnDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PlayerController.OnDied -= OnPlayerDied;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _resolved = false;
            _bossSeenAlive = false;
        }

        private void Update()
        {
            if (_resolved) return;
            if (SceneManager.GetActiveScene().name != SceneFlow.CombatScene) return;
            if (!RunSession.Instance.IsGateActive) return;

            var boss = BossController.Instance;
            if (boss == null) return;

            if (boss.IsAlive)
            {
                _bossSeenAlive = true;
                return;
            }

            if (_bossSeenAlive && !boss.IsAlive)
            {
                _resolved = true;

                CombatRewardResult reward = CombatRewardResolver.Resolve(
                    RunSession.Instance.GetCurrentBossId(),
                    RunSession.Instance.IsGateBossPhase);

                RunSession.Instance.SetPendingCombatRewards(reward);

                Debug.Log(
                    $"[GameFlowManager] Boss defeated -> dice={reward.diceRoll}, currency={reward.currency}, equip={reward.equipmentId}");

                RunSession.Instance.CompleteCombatVictory();
            }
        }

        private void OnPlayerDied()
        {
            if (_resolved) return;
            if (SceneManager.GetActiveScene().name != SceneFlow.CombatScene) return;
            if (!RunSession.Instance.IsGateActive) return;

            _resolved = true;
            Debug.Log("[GameFlowManager] Player defeated -> ResultScene");
            RunSession.Instance.CompleteCombatDefeat();
        }
    }
}