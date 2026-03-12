// Path: Assets/Script/Rule/Core/TurnController.cs
using UnityEngine;
using Rule.Dice;
using Rule.Combat.Player;

namespace Rule.Core
{
    public class TurnController : MonoBehaviour
    {
        [Header("Field Rule (optional in proto)")]
        [Tooltip("지금은 비어 있어도 됨. 비어 있으면 DiceRoller가 2d6로 fallback.")]
        public Object fieldRuleSO; // 타입 의존 줄이기(네임스페이스/이동 안전)

        [Header("Runtime")]
        [SerializeField] private int ap;
        [SerializeField] private int spentThisTurn;

        // -1이면 기본 규칙 사용
        private int _moveCostOverride = -1;

        // ✅ carry 저장
        [SerializeField] private int _carryAP = 0;

        public int AP => ap;
        public int SpentThisTurn => spentThisTurn;

        public int MoveCost
        {
            get
            {
                if (_moveCostOverride > 0) return _moveCostOverride;

                // fieldRuleSO.baseMoveCost 읽기 시도, 없으면 1
                if (fieldRuleSO == null) return 1;
                var t = fieldRuleSO.GetType();
                var p = t.GetProperty("baseMoveCost");
                if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(fieldRuleSO);

                var f = t.GetField("baseMoveCost");
                if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(fieldRuleSO);

                return 1;
            }
        }

        /// <summary>
        /// 턴 종료 시 남은 AP를 carry로 저장하고 AP를 0으로 만든다.
        /// carry 최대치 = 주사위 최대값 / 2
        /// </summary>
        public void EndPlayerTurnWithCarry()
        {
            int remaining = ap;

            // 주사위 최대값 계산 (2d6 → 12)
            int maxDice = DiceRoller.GetMaxValue(fieldRuleSO);
            int carryMax = maxDice / 2;

            _carryAP = Mathf.Min(remaining, carryMax);

            Debug.Log($"[TurnController] Carry AP = {_carryAP} (remaining={remaining}, maxCarry={carryMax})");

            // ✅ read-only 프로퍼티(AP)가 아니라 필드 ap를 0으로
            ap = 0;
        }

        public void StartPlayerTurn()
        {
            int rolled = DiceRoller.Roll(fieldRuleSO);

            ap = rolled + _carryAP;

            Debug.Log($"[TurnController] StartPlayerTurn rolled={rolled} carry={_carryAP} => AP={ap}");

            // carry는 사용 후 초기화
            _carryAP = 0;

            spentThisTurn = 0;
            _moveCostOverride = -1;

            // 턴 스코프 리셋(연속공격 등)
            if (PlayerController.Instance != null)
                PlayerController.Instance.BeginTurn();

            Debug.Log($"[TurnController] StartPlayerTurn rolled={rolled} AP={ap} MoveCost={MoveCost}");
        }

        public bool TrySpendAP(int cost)
        {
            if (cost <= 0) return true;
            if (ap < cost) return false;
            ap -= cost;
            spentThisTurn += cost;
            return true;
        }

        /// <summary>
        /// Adds AP (used by weapon gimmicks such as Dagger AP refund).
        /// </summary>
        public void AddAP(int amount)
        {
            if (amount <= 0) return;
            ap += amount;
            Debug.Log($"[TurnController] AddAP +{amount} => AP={ap}");
        }

        public int GetAP() => ap;

        public void SetMoveCostOverride(int cost)
        {
            _moveCostOverride = Mathf.Max(1, cost);
            Debug.Log($"[TurnController] MoveCost override => {_moveCostOverride}");
        }
    }
}
