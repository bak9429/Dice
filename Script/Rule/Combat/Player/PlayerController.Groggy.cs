// Path: Assets/Script/Rule/Combat/Player/PlayerController.Groggy.cs
using System;
using UnityEngine;

namespace Rule.Combat.Player
{
    // ✅ 200줄 제한을 위해 Groggy 로직을 partial로 분리
    public partial class PlayerController
    {
        [Header("Groggy (runtime)")]
        [SerializeField] private bool isGroggy;
        [SerializeField] private int groggyTurnsLeft;

        [Tooltip("플레이어 그로기 지속 턴(턴 스킵 횟수)")]
        public int defaultGroggyTurns = 1;

        public bool IsGroggy => isGroggy;
        public int GroggyTurnsLeft => groggyTurnsLeft;

        public static event Action<bool, int> OnGroggyChanged; // (isGroggy, turnsLeft)

        private void ResetGroggyRuntime()
        {
            isGroggy = false;
            groggyTurnsLeft = 0;
        }

        private void BroadcastGroggy()
        {
            OnGroggyChanged?.Invoke(isGroggy, groggyTurnsLeft);
        }

        private void EnterGroggy()
        {
            if (isGroggy) return;

            isGroggy = true;
            groggyTurnsLeft = Mathf.Max(1, defaultGroggyTurns);

            Debug.Log($"[Player] GROGGY ENTER turns={groggyTurnsLeft}");
            BroadcastGroggy();
        }

        /// <summary>
        /// 플레이어 턴 시작 시 호출.
        /// 그로기 상태라면 턴을 스킵해야 하므로 true를 반환한다.
        /// out recoverAfterThisSkip: 이번 스킵이 마지막이면 true (스킵 후 ExitGroggy 호출 권장)
        /// </summary>
        public bool ConsumeGroggyTurnOnPlayerTurnStart(out bool recoverAfterThisSkip)
        {
            recoverAfterThisSkip = false;
            if (!isGroggy) return false;

            groggyTurnsLeft = Mathf.Max(0, groggyTurnsLeft - 1);
            recoverAfterThisSkip = (groggyTurnsLeft <= 0);

            Debug.Log($"[Player] GROGGY tick => left={groggyTurnsLeft}");
            BroadcastGroggy();
            return true;
        }

        public void ExitGroggyAndRestoreShield()
        {
            if (!isGroggy) return;

            isGroggy = false;
            groggyTurnsLeft = 0;

            // ✅ 그로기 종료 시 shield 복구(보스와 동일한 정책: 풀복구)
            state.shield = Mathf.Max(0, state.maxShield);

            Debug.Log($"[Player] GROGGY EXIT => shield restored {state.shield}/{state.maxShield}");
            OnShieldChanged?.Invoke(state.shield, state.maxShield);
            BroadcastGroggy();
        }
    }
}