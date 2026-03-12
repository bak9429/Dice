using UnityEngine;
using Rule.Combat.Player;
using GameData.Items.Consumables;

namespace Rule.Combat.Consumables
{
    public static class ConsumableExecutor
    {
        public static bool TryApply(PlayerController pc, ConsumableDefSO def)
        {
            if (pc == null || def == null) return false;

            switch (def.type)
            {
                case ConsumableType.RepairKit:
                {
                    int heal = Mathf.Max(1, Mathf.RoundToInt(pc.state.maxHp * def.hpHealRatio));
                    pc.ApplyHealHp(heal); // ✅ PlayerController 내부에서 이벤트 브로드캐스트
                    Debug.Log($"[Consumable] RepairKit heal={heal} => hp={pc.state.hp}/{pc.state.maxHp}");
                    return true;
                }

                case ConsumableType.ShieldPatch:
                {
                    int restore = Mathf.Max(1, Mathf.RoundToInt(pc.state.maxShield * def.shieldRestoreRatio));
                    pc.RestoreShield(restore); // ✅ PlayerController 내부에서 이벤트 브로드캐스트
                    Debug.Log($"[Consumable] ShieldPatch restore={restore} => shield={pc.state.shield}/{pc.state.maxShield}");
                    return true;
                }

                case ConsumableType.Thruster:
                {
                    pc.AddFreeMoveDistanceThisTurn(def.thrusterFreeMoveDistance);
                    Debug.Log($"[Consumable] Thruster +freeMoveDist={def.thrusterFreeMoveDistance} (now={pc.FreeMoveDistanceThisTurn})");
                    return true;
                }

                case ConsumableType.Dampener:
                {
                    pc.ActivateDamageReduction(def.dampenerDamageMult, def.dampenerHits);
                    Debug.Log($"[Consumable] Dampener mult={def.dampenerDamageMult:0.00} hits={def.dampenerHits}");
                    return true;
                }
            }

            return false;
        }
    }
}