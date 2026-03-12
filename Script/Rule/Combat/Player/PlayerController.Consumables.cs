// Path: Assets/Script/Rule/Combat/Player/PlayerController.Consumables.cs
using System.Collections.Generic;
using UnityEngine;
using GameData.Items.Consumables;
using Rule.Combat.Consumables;

namespace Rule.Combat.Player
{
    public partial class PlayerController
    {
        // --- Turn rule: consumable is free AP, but only once per player turn ---
        [SerializeField] private bool consumableUsedThisTurn;

        // simple runtime inventory
        private readonly Dictionary<string, int> _consumableCounts = new();

        // Thruster effect: free move distance this turn
        [SerializeField] private int freeMoveDistanceThisTurn;

        // Dampener effect: reduce next boss hit(s)
        [SerializeField] private bool damageReductionActive;
        [SerializeField] private float damageReductionMult = 1f;
        [SerializeField] private int damageReductionHitsLeft;

        // last used id this turn (UI/debug)
        [SerializeField] private string lastConsumableUsedIdThisTurn = "";
        public string LastConsumableUsedIdThisTurn => lastConsumableUsedIdThisTurn;

        public bool ConsumableUsedThisTurn => consumableUsedThisTurn;
        public int FreeMoveDistanceThisTurn => freeMoveDistanceThisTurn;

        // called from BeginTurn()/TurnRuntime
        private void ResetConsumableTurnFlags()
        {
            consumableUsedThisTurn = false;
            freeMoveDistanceThisTurn = 0;
            lastConsumableUsedIdThisTurn = "";
        }

        // ✅ 외부(TurnRuntime 등)에서 턴 시작 시 호출
        public void ResetConsumableTurnUsage()
        {
            ResetConsumableTurnFlags();
        }

        // ✅ HP heal + event broadcast (events are invoked only inside PlayerController)
        public void ApplyHealHp(int healAmount)
        {
            int heal = Mathf.Max(0, healAmount);
            if (heal <= 0) return;

            int before = state.hp;
            state.hp = Mathf.Min(state.maxHp, state.hp + heal);

            if (before != state.hp)
                OnHpChanged?.Invoke(state.hp, state.maxHp);
        }

        // ✅ Shield restore + event broadcast
        public void RestoreShield(int amount)
        {
            int v = Mathf.Max(0, amount);
            if (v <= 0) return;

            int before = state.shield;
            state.shield = Mathf.Min(state.maxShield, state.shield + v);

            if (before != state.shield)
                OnShieldChanged?.Invoke(state.shield, state.maxShield);
        }

        // ===== Inventory =====

        public void EnsureConsumableCount(string id, int countIfMissing)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            id = id.Trim();

            if (_consumableCounts.ContainsKey(id)) return;
            _consumableCounts[id] = Mathf.Max(0, countIfMissing);
        }

        public int GetConsumableCount(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0;
            id = id.Trim();
            return _consumableCounts.TryGetValue(id, out var c) ? c : 0;
        }

        // ===== Thruster =====

        public void AddFreeMoveDistanceThisTurn(int dist)
        {
            if (dist <= 0) return;
            freeMoveDistanceThisTurn += dist;
        }

        public int ConsumeFreeMoveDistance(int usedDistance)
        {
            if (usedDistance <= 0) return freeMoveDistanceThisTurn;
            freeMoveDistanceThisTurn = Mathf.Max(0, freeMoveDistanceThisTurn - usedDistance);
            return freeMoveDistanceThisTurn;
        }

        // ===== Dampener =====

        public void ActivateDamageReduction(float mult, int hits)
        {
            damageReductionActive = true;
            damageReductionMult = Mathf.Clamp(mult, 0.1f, 1f);
            damageReductionHitsLeft = Mathf.Max(1, hits);
        }

        // ✅ PlayerController 내부 ApplyBossDamage/ApplyDamage 흐름에서 호출해서 적용해야 함
        // (이 파일은 “준비만” 해두는 역할)
        private void TryApplyDamageReduction(ref int hpDamage, ref int shieldDamage)
        {
            if (!damageReductionActive || damageReductionHitsLeft <= 0) return;

            hpDamage = Mathf.RoundToInt(hpDamage * damageReductionMult);
            shieldDamage = Mathf.RoundToInt(shieldDamage * damageReductionMult);

            damageReductionHitsLeft--;
            if (damageReductionHitsLeft <= 0)
            {
                damageReductionActive = false;
                damageReductionMult = 1f;
            }
        }

        // ===== Use =====

        public bool TryUseConsumable(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            id = id.Trim();

            // 턴당 1회
            if (consumableUsedThisTurn)
            {
                Debug.Log("[Consumable] blocked: already used this turn.");
                return false;
            }

            // 수량 체크
            int count = GetConsumableCount(id);
            if (count <= 0)
            {
                Debug.Log($"[Consumable] blocked: no count id={id}");
                return false;
            }

            var def = ConsumableRegistry.Get(id);
            if (def == null)
            {
                Debug.LogWarning($"[Consumable] def not found id={id}");
                return false;
            }

            // 적용
            bool ok = ConsumableExecutor.TryApply(this, def);
            if (!ok) return false;

            // 소모 + 턴 플래그
            _consumableCounts[id] = Mathf.Max(0, count - 1);
            consumableUsedThisTurn = true;
            lastConsumableUsedIdThisTurn = id;

            Debug.Log($"[Consumable] Used {id} => left={_consumableCounts[id]} (turnUsed=true)");
            return true;
        }
    }
}