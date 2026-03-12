// Path: Assets/Script/UI/UIControl/CombatUIController.Turn.cs
using System.Collections;
using System.Reflection;
using UnityEngine;
using Rule.Combat.Player;
using GameData.Combat;

namespace UI.UIControl
{
    public partial class CombatUIController
    {
        private IEnumerator CoStartFirstTurn()
        {
            yield return null;
            yield return null;

            _turn.TryBind();
            _turn.StartPlayerTurn();
            Rule.Combat.Boss.BossController.Instance?.NotifyPlayerTurnStarted();

            // Player GROGGY: 턴 스킵(MVP)
            {
                var pc = PlayerController.Instance;
                if (pc != null && pc.ConsumeGroggyTurnOnPlayerTurnStart(out bool recoverAfterSkip))
                {
                    Debug.Log($"[PlayerTurn] GROGGY... skip. turnsLeft={pc.GroggyTurnsLeft}");
                    _turn.EndPlayerTurnWithCarry();
                    if (recoverAfterSkip) pc.ExitGroggyAndRestoreShield();

                    _busy = true;
                    StartCoroutine(CoRunBossTurnThenPlayer());
                    yield break;
                }
            }

            // Turn-start reset (Greatsword first-attack gate)
            {
                var pc = PlayerController.Instance;
                var st = pc != null ? pc.state : null;
                if (st != null)
                    st.greatswordFirstAttackUsedThisTurn = false;
            }

            RefreshHud();
            ForceRebuildMoveOverlay();
        }

        private void SpendAllAPToGuard(string reason)
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;

            EnsureLoadoutResolved();
            float guardMul = 1f;

            if (_melee != null && _melee.archetype == MeleeArchetype.OneHandSword)
                guardMul = 1.5f;

            int ap = _turn.GetAP();
            if (ap <= 0) return;

            if (_turn.TrySpendAP(ap))
            {
                int guardGain = Mathf.CeilToInt(ap * guardMul);
                pc.AddGuard(guardGain);

                RefreshHud();
                Debug.Log($"[Defend] {reason}: spentAP={ap} mul={guardMul:0.##} => guard+{guardGain}");
            }
        }

        private void OnDefendClicked()
        {
            if (!IsPlayerTurn()) return;
            if (_busy) return;

            SpendAllAPToGuard("Manual Defend");

            ShowHexActionPanel(false);
            ReturnToRootActions();
        }

        private bool IsPlayerTurn()
        {
            if (_busy) return false;

            var tc = _turn.GetRawTurnController();
            if (tc == null) return true;

            var t = tc.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var p = t.GetProperty("IsPlayerTurn", flags);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(tc);

            p = t.GetProperty("isPlayerTurn", flags);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(tc);

            var f = t.GetField("IsPlayerTurn", flags);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(tc);

            f = t.GetField("isPlayerTurn", flags);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(tc);

            return true;
        }

        private void OnEndTurnClicked()
        {
            if (_busy) return;

            if (!_playerCoord.HasValue)
            {
                Debug.Log("[BossTurn] Player coord not initialized.");
                return;
            }

            // Greatsword: EndTurn sheathe (spend 1 AP if possible)
            EnsureLoadoutResolved();
            var pc = PlayerController.Instance;
            var st = pc != null ? pc.state : null;

            if (_melee != null && _melee.archetype == MeleeArchetype.Greatsword && st != null)
            {
                st.greatswordSheathed = false;

                if (_turn.GetAP() >= 1 && _turn.TrySpendAP(1))
                {
                    st.greatswordSheathed = true;
                    Debug.Log("[Greatsword] EndTurn Sheathe success (-1 AP). Next turn first attack = Heavy");
                }
                else
                {
                    Debug.Log("[Greatsword] EndTurn Sheathe failed. Next turn first attack = Basic");
                }
            }

            _turn.EndPlayerTurnWithCarry();
            _busy = true;

            if (_boss == null)
                _boss = FindFirstObjectByType<Rule.Combat.Boss.BossController>(FindObjectsInactive.Include);

            Debug.Log("[UI] EndTurn -> BossTurn");
            StartCoroutine(CoRunBossTurnThenPlayer());
        }

        private IEnumerator CoRunBossTurnThenPlayer()
        {
            while (true)
            {
                if (_boss != null && _playerCoord.HasValue)
                    yield return _boss.CoDoBossTurn(_playerCoord.Value);

                _turnIndex++;
                _turn.StartPlayerTurn();
                Rule.Combat.Boss.BossController.Instance?.NotifyPlayerTurnStarted();

                // Turn-start reset
                {
                    var pc = PlayerController.Instance;
                    var st = pc != null ? pc.state : null;
                    if (st != null)
                        st.greatswordFirstAttackUsedThisTurn = false;
                }

                // Player GROGGY: 턴 스킵
                {
                    var pc = PlayerController.Instance;
                    if (pc != null && pc.ConsumeGroggyTurnOnPlayerTurnStart(out bool recoverAfterSkip))
                    {
                        Debug.Log($"[PlayerTurn] GROGGY... skip. turnsLeft={pc.GroggyTurnsLeft}");
                        RefreshHud();

                        _turn.EndPlayerTurnWithCarry();
                        if (recoverAfterSkip) pc.ExitGroggyAndRestoreShield();
                        continue;
                    }
                }

                RefreshHud();
                _busy = false;
                ForceRebuildMoveOverlay();
                break;
            }
        }
    }
}
