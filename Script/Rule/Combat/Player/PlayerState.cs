using UnityEngine;
using Rule.Field;

namespace Rule.Combat.Player
{
    public enum StatScalingGrade
    {
        S, A, B, C, D, E
    }

    [System.Serializable]
    public class PlayerState
    {
        [Header("Vitals")]
        public int maxHp = 20;
        public int hp = 20;

        [Header("Shield (Shield/Gauge)")]
        public int maxShield = 50;
        public int shield = 50;

        [Header("Position")]
        public HexCoord coord;

        [Header("Stats (MVP)")]
        [Range(0, 15)] public int str = 5;
        [Range(0, 15)] public int dex = 5;
        [Range(0, 15)] public int sync = 5;

        // Turn-scoped trackers (reset at StartPlayerTurn)
        [HideInInspector] public int rapierComboCountThisTurn = 0;

        // ✅ Greatsword
        // - greatswordFirstAttackUsedThisTurn: 턴 단위 리셋
        // - greatswordSheathed: "턴 넘어 유지" (EndTurn 납도 성공 시에만 true)
        [HideInInspector] public bool greatswordFirstAttackUsedThisTurn = false;
        [HideInInspector] public bool greatswordSheathed = false;

        // ✅ Axe: 누적 행동력(턴 동안 "공격에 사용한 AP" 누적)
        [HideInInspector] public int axeAccumAttackApThisTurn = 0;

        // ✅ Axe: 연속 공격 카운트(턴 시작 시 0 리셋)
        // - 1타: 추가비용 0
        // - 2타: +1
        // - 3타: +2 ...
        [HideInInspector] public int axeAttackCountThisTurn = 0;

        // ✅ Guard (spent AP -> damage reduction pool; consumed by incoming hits)
        [HideInInspector] public int guard = 0;

        public bool IsDead => hp <= 0;

        public void ResetTo(int maxHp, HexCoord start)
        {
            this.maxHp = Mathf.Max(1, maxHp);
            this.hp = this.maxHp;
            this.coord = start;

            // shield default (proto)
            maxShield = Mathf.Max(0, maxShield);
            shield = Mathf.Clamp(shield, 0, maxShield);

            // stats default (proto)
            str = Mathf.Clamp(str, 0, 15);
            dex = Mathf.Clamp(dex, 0, 15);
            sync = Mathf.Clamp(sync, 0, 15);

            // 새 전투 시작이므로 납도 상태 초기화는 여기서만 처리
            greatswordSheathed = false;

            BeginTurnReset();
        }

        public void BeginTurnReset()
        {
            rapierComboCountThisTurn = 0;

            // ✅ 여기서 greatswordSheathed를 건드리면 안 됨!
            // greatswordSheathed는 "이전 턴 EndTurn 납도 성공/실패" 결과를 들고 와야 함.

            // ✅ Axe 누적 리셋
            axeAccumAttackApThisTurn = 0;
            axeAttackCountThisTurn = 0;

            // ✅ Greatsword 턴 단위 플래그 리셋
            greatswordFirstAttackUsedThisTurn = false;

            // ✅ Guard는 "보스턴 동안만" 유지되면 되니, 플레이어 턴 시작 시 리셋
            guard = 0;
        }

        public void ResetShieldToMax()
        {
            shield = Mathf.Clamp(maxShield, 0, maxShield);
        }

        public void AddGuard(int amount)
        {
            if (amount <= 0) return;
            guard += amount;
        }

        /// <summary>
        /// Apply damage with guard.
        /// returns: actual damage applied to HP
        /// out reduced: how much damage was reduced by guard
        /// </summary>
        public int ApplyDamage(int dmg, out int reduced)
        {
            dmg = Mathf.Max(0, dmg);

            reduced = 0;
            if (guard > 0 && dmg > 0)
            {
                reduced = Mathf.Min(guard, dmg);
                guard -= reduced;
                dmg -= reduced;
            }

            // ✅ Shield absorbs after guard
            if (shield > 0 && dmg > 0)
            {
                int s = Mathf.Min(shield, dmg);
                shield -= s;
                reduced += s;
                dmg -= s;
            }

            hp = Mathf.Max(0, hp - dmg);
            return dmg;
        }

        /// <summary>
        /// Damage shield only (does not affect HP by default).
        /// returns: (overflow) amount that could not be absorbed by shield
        /// </summary>
        public int ApplyShieldDamage(int amount, bool overflowToHp = false)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0) return 0;

            int absorbed = Mathf.Min(shield, amount);
            shield -= absorbed;

            int overflow = amount - absorbed;
            if (overflowToHp && overflow > 0)
                hp = Mathf.Max(0, hp - overflow);

            return overflow;
        }

    }
}
