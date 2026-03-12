// Path: Assets/Script/UI/UIControl/CombatUIController.Move.cs
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Rule.Field;
using Rule.Combat.Player;

namespace UI.UIControl
{
    public partial class CombatUIController
    {
        private void SyncPlayerCoordFromPlayerController(bool force)
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;

            var c = pc.state.coord;

            if (!force && _playerCoord.HasValue && _playerCoord.Value.Equals(c))
                return;

            _playerCoord = c;
            ForceRebuildMoveOverlay();
        }

        private void OnHexSelected(HexCoord coord, Vector3 worldPos)
        {
            _selectedHex = coord;

            // Attack targeting: selection drives preview + confirm panel
            if (_atkState == AttackTargetingState.Targeting)
            {
                HandleAttackHexSelected(coord);
                return;
            }

            // Bullet targeting has its own selection rules
            if (_bulletState == BulletTargetingState.Targeting)
            {
                HandleBulletHexSelected(coord);
                return;
            }

            // Normal selection: open panel and allow move
            ShowHexActionPanel(true);

            if (IsPlayerTurn() && _turn.GetAP() > 0 && _playerCoord.HasValue && IsReachable(coord))
            {
                _pendingMoveDest = coord;
                Debug.Log($"[Move] pending dest={coord.q},{coord.r} (press MOVE to commit)");
            }
        }

        private void OnHexDeselected()
        {
            _pendingMoveDest = null;
            _selectedHex = null;

            if (_atkState == AttackTargetingState.Targeting)
            {
                _atkTarget = null;
                if (highlight != null)
                    highlight.HighlightAttackPreview(System.Array.Empty<HexCoord>());
            }
        }

        private void OnMoveCommitClicked()
        {
            Debug.Log("[Move] Commit button clicked");

            if (!_playerCoord.HasValue)
            {
                Debug.Log("[Move] Player coord not ready yet.");
                return;
            }

            if (!IsPlayerTurn())
            {
                Debug.Log("[Move] Not player turn.");
                return;
            }

            if (_turn.GetAP() <= 0)
            {
                Debug.Log("[Move] No AP.");
                return;
            }

            CommitMoveIfPossible();
        }

        private void CommitMoveIfPossible()
        {
            if (!_pendingMoveDest.HasValue)
            {
                Debug.Log("[Move] Pick a destination tile first.");
                return;
            }

            var from = _playerCoord.Value;
            var to = _pendingMoveDest.Value;

            int dist = AxialDistance(from, to);
            int costPerHex = GetMoveCost();

            int freeDist = 0;
            var pc = PlayerController.Instance;
            if (pc != null) freeDist = pc.FreeMoveDistanceThisTurn;

            int paidDist = Mathf.Max(0, dist - freeDist);
            int total = paidDist * costPerHex;

            if (total <= 0)
            {
                Debug.Log("[Move] No move needed.");
                _pendingMoveDest = null;
                return;
            }

            if (_turn.TrySpendAP(total))
            {
                _playerCoord = to;
                _pendingMoveDest = null;

                if (pc != null) pc.SetCoord(to);

                Debug.Log($"[Move] COMMIT {from.q},{from.r} -> {to.q},{to.r} dist={dist} cost={total} ap={_turn.GetAP()}");

                RefreshHud();
                ForceRebuildMoveOverlay();

                ReturnToRootActions();
                ShowHexActionPanel(false);
            }
            else
            {
                Debug.Log($"[Move] Not enough AP. need={total} have={_turn.GetAP()}");
            }
            if (pc != null && dist > 0)
            {
                pc.ConsumeFreeMoveDistance(dist);
            }
        }

        private void UpdateAlwaysMoveOverlay()
        {
            if (highlight == null) return;
            if (!_playerCoord.HasValue) { highlight.Clear(); return; }

            int ap = _turn.GetAP();
            bool playerTurn = IsPlayerTurn();

            if (!playerTurn || ap <= 0)
            {
                if (_lastAP != ap || _lastPlayerTurn != playerTurn)
                {
                    highlight.Clear();
                    _lastAP = ap;
                    _lastPlayerTurn = playerTurn;
                }
                return;
            }

            if (_lastAP == ap && _lastPlayerTurn == playerTurn && _lastPlayerCoord.HasValue && _lastPlayerCoord.Value.Equals(_playerCoord.Value))
                return;

            var reachable = BuildReachableSet();
            if (reachable != null)
                highlight.HighlightMove(reachable);

            _lastAP = ap;
            _lastPlayerTurn = playerTurn;
            _lastPlayerCoord = _playerCoord;
        }

        private void ForceRebuildMoveOverlay()
        {
            // Force other cached overlays to rebuild as well, in a strict priority order:
            //  1) Bullet (blue) range
            //  2) Melee  (red) range  <-- must be drawn last so it stays visually on top
            _lastAP = int.MinValue;
            _lastPlayerCoord = null;
            _lastPlayerTurn = !_lastPlayerTurn;

            // Repaint range outlines immediately when player coord changes.
            // (No feature flags here; keep it deterministic.)
            if (highlight != null && _playerCoord.HasValue)
            {
                // Blue first
                UpdateGunRangePersistentOverlay();
                // Red last
                UpdatePassiveAttackRangeOverlay(true);
            }
        }

        private int GetMoveCost()
        {
            int mc = _turn.GetMoveCost();
            return mc > 0 ? mc : baseMoveCost;
        }

        private HashSet<HexCoord> BuildReachableSet()
        {
            var grid = (highlight != null && highlight.grid != null) ? highlight.grid : FindFirstObjectByType<HexGridBuilder>();
            if (grid == null)
            {
                Debug.LogWarning("[Move] HexGridBuilder not found.");
                return null;
            }

            int ap = _turn.GetAP();
            int cost = GetMoveCost();
            return HexRange.Reachable(grid.Tiles, _playerCoord.Value, ap, cost);
        }

        private bool IsReachable(HexCoord dest)
        {
            var reachable = BuildReachableSet();
            return reachable != null && reachable.Contains(dest);
        }
    }
}