// Path: Assets/Script/Rule/Core/CombatSceneBootstrap.cs
using Rule.Combat.Boss;
using UnityEngine;

namespace Rule.Core
{
    [DefaultExecutionOrder(-1000)]
    public class CombatSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var session = RunSession.Instance;
            string bossId = session.GetCurrentBossId();

            if (string.IsNullOrWhiteSpace(bossId))
            {
                Debug.LogWarning("[CombatSceneBootstrap] current boss id is empty.");
                return;
            }

            var boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
            if (boss == null)
            {
                Debug.LogWarning("[CombatSceneBootstrap] BossController not found.");
                return;
            }

            boss.bossId = bossId;
            Debug.Log($"[CombatSceneBootstrap] Injected bossId={bossId}");

            var debuffs = session.CurrentBossDebuffs;
            if (debuffs != null && debuffs.Count > 0)
                Debug.Log($"[CombatSceneBootstrap] Pending boss debuffs = {string.Join(", ", debuffs)}");
        }
    }
}