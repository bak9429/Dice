// Path: Assets/Script/Rule/Core/CombatSceneResultRouter.cs
using Rule.Combat.Boss;
using Rule.Combat.Player;
using UnityEngine;

namespace Rule.Core
{
    public class CombatSceneResultRouter : MonoBehaviour
    {
        private bool _resolved;
        private bool _bossSeenAlive;

        private void OnEnable()
        {
            PlayerController.OnDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            PlayerController.OnDied -= OnPlayerDied;
        }

        private void Update()
        {
            if (_resolved) return;

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
                Debug.Log("[CombatSceneResultRouter] Boss defeated.");
                RunSession.Instance.CompleteCombatVictory();
            }
        }

        private void OnPlayerDied()
        {
            if (_resolved) return;
            _resolved = true;
            Debug.Log("[CombatSceneResultRouter] Player defeated.");
            RunSession.Instance.CompleteCombatDefeat();
        }
    }
}