// Path: Assets/Script/UI/UIControl/CombatUIController.Bullet.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Rule.Field;
using Rule.Combat.Player;
using Rule.Combat.Boss;
using GameData.Combat;
using GameData.Combat.Registry;

namespace UI.UIControl
{
    public partial class CombatUIController
    {
        private enum BulletTargetingState { None, Targeting }

        private BulletTargetingState _bulletState = BulletTargetingState.None;
        private readonly HashSet<HexCoord> _bulletCandidates = new HashSet<HexCoord>();

        private HexCoord? _bulletSelectedTile = null; // Move: dirTile, Attack: enemyTile
        private string _bulletLastReason = "";

        [Header("Bullet Rules")]
        [SerializeField] private bool requireAdjacentEnemyForAttackBullets = false;

        // ✅ Turn limiter: 1 bullet per player turn (uses _turnIndex from Core.cs)
        private int _bulletUsedOnTurnIndex = -999;

        // ✅ Persistent gun range outline cache (blue outline)
        private HexCoord? _lastGunRangeFrom = null;
        private int _lastGunRangeMin = int.MinValue;
        private int _lastGunRangeMax = int.MinValue;

        // ✅ IMPORTANT: lock bullet during targeting so Update() can't overwrite _bullet
        private string _activeBulletId = "";
        private BulletDefSO _activeBullet = null;

        private bool IsTargeting() => _bulletState == BulletTargetingState.Targeting;

        private BulletDefSO CurrentBullet()
        {
            if (IsTargeting() && _activeBullet != null) return _activeBullet;
            return _bullet;
        }

        private void TryExitBulletTargetingFromCore()
        {
            if (_bulletState == BulletTargetingState.Targeting)
                ExitBulletTargeting(reopenPanel: true);
        }

        private bool IsBulletAllowedByGunId(string bulletId)
        {
            if (string.IsNullOrEmpty(bulletId)) return false;
            if (_gun == null) return true;
            return _gun.AllowsBullet(bulletId);
        }

        private bool TryGetEnemyCoords(out List<HexCoord> coords)
        {
            coords = new List<HexCoord>(4);

            if (_boss == null) _boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
            if (_boss != null && _boss.IsAlive)
                coords.Add(_boss.GetCoord());

            // TODO: minion 추가 지점
            return coords.Count > 0;
        }

        private bool IsEnemyAdjacentToPlayer()
        {
            if (!_playerCoord.HasValue) return false;
            if (!TryGetEnemyCoords(out var enemies)) return false;

            var p = _playerCoord.Value;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (AxialDistance(p, enemies[i]) == 1) return true;
            }
            return false;
        }

        // Move(backstep): dirTile(인접) -> dst(반대 방향)
        private bool TryComputeBackstepDestination(HexCoord dirTile, out HexCoord dst, out string reason)
        {
            dst = default;
            reason = "";

            if (!_playerCoord.HasValue) { reason = "no playerCoord"; return false; }
            if (highlight == null || highlight.grid == null || highlight.grid.Tiles == null) { reason = "grid not ready"; return false; }

            var from = _playerCoord.Value;
            if (AxialDistance(from, dirTile) != 1) { reason = "dirTile not adjacent"; return false; }

            int dq = dirTile.q - from.q;
            int dr = dirTile.r - from.r;

            dst = new HexCoord(from.q - dq, from.r - dr);

            if (!highlight.grid.Tiles.ContainsKey(dst)) { reason = "dst out of grid"; return false; }

            // 점유 금지(보스/미니언 확장 가능)
            if (_boss != null && _boss.IsAlive && dst.Equals(_boss.GetCoord())) { reason = "dst occupied (boss)"; return false; }

            return true;
        }

        private bool CanUseBulletCommon(out string reason)
        {
            reason = "";

            EnsureLoadoutResolved();
            var b = CurrentBullet();

            if (b == null) { reason = "no bullet selected"; return false; }
            if (_gun == null) { reason = "no gun"; return false; }
            if (!IsPlayerTurn()) { reason = "not player turn"; return false; }
            if (_busy) { reason = "busy"; return false; }

            // ✅ AP 소모 없음(0)

            // ✅ 1 bullet per turn
            if (_bulletUsedOnTurnIndex == _turnIndex) { reason = "bullet already used this turn"; return false; }

            if (!IsBulletAllowedByGunId(b.bulletId)) { reason = "gun disallows"; return false; }

            var pc = PlayerController.Instance;
            if (pc == null) { reason = "no player controller"; return false; }
            if (pc.GetBulletAmmo(b.bulletId) <= 0) { reason = "ammo=0"; return false; }

            return true;
        }

        private bool CanExecuteBulletWithSelectedTile(out string reason, out HexCoord moveDstIfAny)
        {
            moveDstIfAny = default;

            if (!CanUseBulletCommon(out reason)) return false;
            if (!_bulletSelectedTile.HasValue) { reason = "no target selected"; return false; }

            var b = CurrentBullet();
            if (b == null) { reason = "no bullet"; return false; }

            if (b.kind == BulletKind.Move)
            {
                var dirTile = _bulletSelectedTile.Value;
                if (!TryComputeBackstepDestination(dirTile, out var dst, out var r))
                {
                    reason = r;
                    return false;
                }
                moveDstIfAny = dst;
                reason = "ok";
                return true;
            }

            if (!_playerCoord.HasValue) { reason = "no playerCoord"; return false; }
            if (!TryGetEnemyCoords(out var enemies) || enemies.Count == 0) { reason = "no enemy"; return false; }

            if (requireAdjacentEnemyForAttackBullets && !IsEnemyAdjacentToPlayer())
            {
                reason = "need adjacent enemy";
                return false;
            }

            var chosen = _bulletSelectedTile.Value;

            bool isEnemyTile = false;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Equals(chosen)) { isEnemyTile = true; break; }
            }
            if (!isEnemyTile) { reason = "target is not enemy tile"; return false; }

            int d = AxialDistance(_playerCoord.Value, chosen);
            int rmin = Mathf.Max(1, _gun.rangeMin);
            int rmax = Mathf.Max(rmin, _gun.rangeMax);
            if (d < rmin || d > rmax) { reason = "out of gun range"; return false; }

            reason = "ok";
            return true;
        }

        private bool HasAnyUsableBulletNow()
        {
            var all = BulletRegistry.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                if (b == null) continue;

                // ⚠️ 원본 코드 스타일 유지: 스캔 중 _bullet을 바꾸지만, Targeting lock으로 안전
                _bullet = b;
                if (!CanUseBulletCommon(out _)) continue;

                if (b.kind == BulletKind.Move)
                {
                    if (!_playerCoord.HasValue) continue;
                    var p = _playerCoord.Value;

                    for (int k = 0; k < DIRS.Length; k++)
                    {
                        var dirTile = new HexCoord(p.q + DIRS[k].q, p.r + DIRS[k].r);
                        if (TryComputeBackstepDestination(dirTile, out var _, out var _))
                            return true;
                    }
                }
                else
                {
                    if (TryGetEnemyCoords(out var enemies) && enemies.Count > 0)
                        return true;
                }
            }

            _bullet = BulletRegistry.Get(selectedBulletId);
            return false;
        }

        private void OnBulletButtonPressed()
        {
            EnsureLoadoutResolved();

            if (!HasAnyUsableBulletNow())
            {
                Debug.Log("[BulletUI] ok=false reason=no usable bullets");
                return;
            }

            ShowHexActionPanel(true);
            OpenBulletList();
            Debug.Log("[BulletUI] OpenBulletList()");
        }


        private bool CanOpenBulletPanelNow(out string reason)
        {
            reason = "";
            EnsureLoadoutResolved();

            if (!IsPlayerTurn()) { reason = "not player turn"; return false; }
            if (_busy) { reason = "busy"; return false; }
            if (_gun == null) { reason = "no gun"; return false; }
            if (!_playerCoord.HasValue) { reason = "no player coord"; return false; }
            return true;
        }

        private bool CanBulletHaveAnyCandidate(BulletDefSO b, out string reason)
        {
            reason = "";
            if (b == null) { reason = "no bullet"; return false; }

            // Must have grid
            if (!_playerCoord.HasValue || highlight == null || highlight.grid == null || highlight.grid.Tiles == null)
            {
                reason = "grid/playerCoord not ready";
                return false;
            }

            var p = _playerCoord.Value;

            if (b.kind == BulletKind.Move)
            {
                for (int i = 0; i < DIRS.Length; i++)
                {
                    var adj = new HexCoord(p.q + DIRS[i].q, p.r + DIRS[i].r);
                    if (TryComputeBackstepDestination(adj, out var _, out var _))
                        return true;
                }
                reason = "no valid move";
                return false;
            }

            // Attack bullets: need an enemy in gun range (and optional adjacent requirement)
            if (!TryGetEnemyCoords(out var enemies) || enemies.Count == 0)
            {
                reason = "no enemy";
                return false;
            }
            if (requireAdjacentEnemyForAttackBullets && !IsEnemyAdjacentToPlayer())
            {
                reason = "need adjacent enemy";
                return false;
            }

            int rmin = Mathf.Max(1, _gun.rangeMin);
            int rmax = Mathf.Max(rmin, _gun.rangeMax);

            for (int i = 0; i < enemies.Count; i++)
            {
                int d = AxialDistance(p, enemies[i]);
                if (d >= rmin && d <= rmax)
                    return true;
            }

            reason = "no target in range";
            return false;
        }

        private void UpdateBulletButtonState()
        {
            if (action == null || action.btnBullet == null) return;

            // ✅ Bullet action button: always available on your turn (panel may show disabled bullets)
            bool ok = CanOpenBulletPanelNow(out _);
            action.btnBullet.interactable = ok;

            // ✅ Always-on gun range (blue outline)
            UpdateGunRangePersistentOverlay();

            // ✅ UX#1: melee(빨강) 범위가 파랑 위로 오도록 "다시" 그린다.
            if (alwaysShowAttackRange)
                UpdatePassiveAttackRangeOverlay(force: false);
        }

        private void OpenBulletList()
        {
            EnsureLoadoutResolved();

            ClearDynamicRows();
            ShowRootRows(false);
            SetPanelTitle("BULLET");

            AddDynamicRow("< Back", "Return", "<", () => { ReturnToRootActions(); });

            var pc = PlayerController.Instance;
            var all = BulletRegistry.GetAll();

            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                if (b == null) continue;

                _bullet = b;
                bool okCommon = CanUseBulletCommon(out var reasonCommon);
                bool okCandidate = CanBulletHaveAnyCandidate(b, out var reasonTarget);

                int ammo = (pc != null) ? pc.GetBulletAmmo(b.bulletId) : 0;
                bool interactable = okCommon && okCandidate;

                string sub = interactable ? $"ammo {ammo}" : $"(disable) {(okCommon ? reasonTarget : reasonCommon)}";

                AddDynamicRow($"{b.displayName} ({b.bulletId})", sub, "B", () =>
                {
                    _bullet = b;
                    selectedBulletId = b.bulletId;
                    Debug.Log($"[Bullet] Selected {b.bulletId} ({b.displayName}) kind={b.kind}");

                    // ✅ UX#2: bullet 선택 직후 패널을 닫고 타겟팅으로 진입
                    BeginBulletTargeting(closePanelFirst: true);
                }, interactable: interactable);
            }

            _bullet = BulletRegistry.Get(selectedBulletId);
        }

        // GunRange(파란 outline) 집합 계산
        private HashSet<HexCoord> BuildGunRangeSet()
        {
            var set = new HashSet<HexCoord>();
            if (!_playerCoord.HasValue) return set;
            if (highlight == null || highlight.grid == null || highlight.grid.Tiles == null) return set;

            int rmin = Mathf.Max(1, _gun.rangeMin);
            int rmax = Mathf.Max(rmin, _gun.rangeMax);
            var p = _playerCoord.Value;

            foreach (var kv in highlight.grid.Tiles)
            {
                var c = kv.Key;
                int d = AxialDistance(p, c);
                if (d >= rmin && d <= rmax)
                    set.Add(c);
            }

            return set;
        }

        private void UpdateGunRangePersistentOverlay()
        {
            if (highlight == null) return;

            if (_gun == null || !_playerCoord.HasValue)
            {
                highlight.ClearGunRangeOutline();
                _lastGunRangeFrom = null;
                _lastGunRangeMin = int.MinValue;
                _lastGunRangeMax = int.MinValue;
                return;
            }

            if (_lastGunRangeFrom.HasValue &&
                _lastGunRangeFrom.Value.Equals(_playerCoord.Value) &&
                _lastGunRangeMin == _gun.rangeMin &&
                _lastGunRangeMax == _gun.rangeMax)
            {
                return;
            }

            _lastGunRangeFrom = _playerCoord.Value;
            _lastGunRangeMin = _gun.rangeMin;
            _lastGunRangeMax = _gun.rangeMax;

            var set = BuildGunRangeSet();
            highlight.HighlightGunRangeOutline(set);
        }

        // ✅ UX#2 반영: closePanelFirst 옵션 추가
        private void BeginBulletTargeting(bool closePanelFirst)
        {
            EnsureLoadoutResolved();
            if (_bullet == null) return;

            _activeBulletId = _bullet.bulletId;
            _activeBullet = _bullet;

            _bulletState = BulletTargetingState.Targeting;
            _bulletSelectedTile = null;
            _bulletLastReason = "";

            _bulletCandidates.Clear();

            // ✅ 프리뷰는 '선택했을 때'만 보여준다(hover 없음)
            highlight?.HighlightAttackPreview(System.Array.Empty<HexCoord>());

            // ✅ bullet 선택 직후 패널 먼저 닫기
            if (closePanelFirst)
                ShowHexActionPanel(false);
            else
                ShowHexActionPanel(true);

            highlight?.ClearCandidateFill();

            if (!CanUseBulletCommon(out var reason))
            {
                _bulletLastReason = reason;
                Debug.Log($"[BulletUI] ok=false reason={reason}");
                if (!closePanelFirst) BuildBulletTargetPanel();
                return;
            }

            if (!_playerCoord.HasValue || highlight == null || highlight.grid == null || highlight.grid.Tiles == null)
            {
                _bulletLastReason = "grid/playerCoord not ready";
                Debug.Log($"[BulletUI] ok=false reason={_bulletLastReason}");
                if (!closePanelFirst) BuildBulletTargetPanel();
                return;
            }

            var p = _playerCoord.Value;
            var b = CurrentBullet();

            if (b.kind == BulletKind.Move)
            {
                for (int i = 0; i < DIRS.Length; i++)
                {
                    var adj = new HexCoord(p.q + DIRS[i].q, p.r + DIRS[i].r);
                    if (TryComputeBackstepDestination(adj, out var _, out var _))
                        _bulletCandidates.Add(adj);
                }

                highlight.HighlightCandidateFill(_bulletCandidates);
                Debug.Log($"[Bullet] Targeting ON. bullet={b.bulletId} kind=Move(backstep) candidates={_bulletCandidates.Count}");
            }
            else
            {
                if (!TryGetEnemyCoords(out var enemies) || enemies.Count == 0)
                {
                    _bulletLastReason = "no enemy";
                }
                else if (requireAdjacentEnemyForAttackBullets && !IsEnemyAdjacentToPlayer())
                {
                    _bulletLastReason = "need adjacent enemy";
                }
                else
                {
                    int rmin = Mathf.Max(1, _gun.rangeMin);
                    int rmax = Mathf.Max(rmin, _gun.rangeMax);

                    for (int i = 0; i < enemies.Count; i++)
                    {
                        var e = enemies[i];
                        int d = AxialDistance(p, e);
                        if (d >= rmin && d <= rmax)
                            _bulletCandidates.Add(e);
                    }
                }

                highlight.HighlightCandidateFill(_bulletCandidates);
                Debug.Log($"[Bullet] Targeting ON. bullet={b.bulletId} kind={b.kind} candidates={_bulletCandidates.Count}");
            }

            // ✅ closePanelFirst이면 여기서 패널을 열지 않는다.
            // 타일 선택 시점에 다시 열어 Confirm을 보여준다.
            if (!closePanelFirst)
                BuildBulletTargetPanel();
        }

        private void ExitBulletTargeting(bool reopenPanel)
        {
            _bulletState = BulletTargetingState.None;
            _bulletSelectedTile = null;
            _bulletLastReason = "";
            _bulletCandidates.Clear();

            _activeBulletId = "";
            _activeBullet = null;

            highlight?.ClearCandidateFill();
            highlight?.HighlightAttackPreview(System.Array.Empty<HexCoord>());

            UpdatePassiveAttackRangeOverlay(force: true);

            if (reopenPanel)
            {
                ShowHexActionPanel(true);
                ReturnToRootActions();
            }
            else
            {
                ShowHexActionPanel(false);
            }

            Debug.Log("[Bullet] Targeting OFF");
        }

        private void HandleBulletHexSelected(HexCoord coord)
        {
            if (_bulletState != BulletTargetingState.Targeting) return;

            var b = CurrentBullet();
            if (b == null) return;

            if (b.kind == BulletKind.Move && !_bulletCandidates.Contains(coord))
                return;

            _bulletSelectedTile = coord;

            bool ok = CanExecuteBulletWithSelectedTile(out var r, out var _);
            _bulletLastReason = ok ? "ok" : r;

            Debug.Log($"[BulletUI] bullet={b.bulletId} kind={b.kind} ok={ok} reason={_bulletLastReason} sel={coord.q},{coord.r}");

            // ✅ 선택 시 프리뷰(초록): 단일 / AOE 반경
            if (highlight != null)
            {
                if (_bulletCandidates.Contains(coord) && b.kind != BulletKind.Move)
                {
                    if (b.aoeRadius > 0){
                        var prev = BuildAoePreviewSet(coord, b.aoeRadius);
                        Debug.Log($"[BulletPreview] target={coord.q},{coord.r} radius={b.aoeRadius} affected={prev.Count}");
                        highlight.HighlightAttackPreview(BuildAoePreviewSet(coord, b.aoeRadius));
                    }
                    else
                        highlight.HighlightAttackPreview(new[] { coord });
                }
                else
                {
                    // 후보가 아니거나 Move면 프리뷰 없음
                    highlight.HighlightAttackPreview(System.Array.Empty<HexCoord>());
                }
            }

            // ✅ UX#2: 타일 클릭 시점에 패널을 다시 열어서 Confirm/Cancel을 보여준다.
            ShowHexActionPanel(true);
            BuildBulletTargetPanel();
        }

        private void BuildBulletTargetPanel()
        {
            ClearDynamicRows();
            ShowRootRows(false);

            var b = CurrentBullet();

            string ammoStr = "-";
            var pc = PlayerController.Instance;
            if (b != null && pc != null)
                ammoStr = $"{pc.GetBulletAmmo(b.bulletId)}/{pc.GetBulletMaxAmmo(b.bulletId)}";

            SetPanelTitle($"BULLET: {(b != null ? b.displayName : "None")} ({ammoStr})");

            AddDynamicRow("< Cancel", "Return to actions", "<", () =>
            {
                ExitBulletTargeting(reopenPanel: true);
            });

            bool ok = CanExecuteBulletWithSelectedTile(out var reason, out var moveDst);

            string selText = _bulletSelectedTile.HasValue ? $"{_bulletSelectedTile.Value.q},{_bulletSelectedTile.Value.r}" : "(none)";
            string detail = ok ? "ok" : (string.IsNullOrEmpty(_bulletLastReason) ? reason : _bulletLastReason);

            AddDynamicRow($"Selected: {selText}", $"[BulletUI] ok={ok} reason={detail}", "i", () => { }, interactable: false);

            if (b != null && b.kind == BulletKind.Move && ok)
                AddDynamicRow($"MoveDst: {moveDst.q},{moveDst.r}", "backstep destination", "→", () => { }, interactable: false);

            AddDynamicRow("Confirm", ok ? "Execute bullet (Ammo1)" : $"Disabled: {detail}", "✓", () =>
            {
                if (!CanExecuteBulletWithSelectedTile(out var r2, out var dst2))
                {
                    Debug.Log($"[BulletUI] ok=false reason={r2}");
                    _bulletLastReason = r2;
                    BuildBulletTargetPanel();
                    return;
                }

                var bb = CurrentBullet();
                if (bb != null && bb.kind == BulletKind.Move)
                    ExecuteBulletMoveBackstep(_bulletSelectedTile!.Value, dst2);
                else
                    ExecuteBulletHit(_bulletSelectedTile!.Value);

                ExitBulletTargeting(reopenPanel: false);
            }, interactable: ok);
        }

        private bool TrySpendBulletCommonCost(out PlayerController pc)
        {
            pc = PlayerController.Instance;
            if (pc == null) return false;

            if (!CanUseBulletCommon(out var reason))
            {
                Debug.Log($"[BulletUI] ok=false reason={reason}");
                return false;
            }

            var b = CurrentBullet();
            if (b == null) return false;

            if (!pc.TryConsumeBulletAmmo(b.bulletId, 1))
            {
                Debug.Log("[BulletUI] ok=false reason=ammo consume fail");
                return false;
            }

            // ✅ AP 소모 제거
            return true;
        }

        private void ExecuteBulletMoveBackstep(HexCoord dirTile, HexCoord dst)
        {
            if (!TrySpendBulletCommonCost(out var pc)) return;
            _bulletUsedOnTurnIndex = _turnIndex;

            if (!_playerCoord.HasValue) return;

            var from = _playerCoord.Value;

            pc.SetCoord(dst);
            _playerCoord = dst;

            ForceRebuildMoveOverlay();
            UpdatePassiveAttackRangeOverlay(force: true);

            Debug.Log($"[Bullet] EXEC Move(backstep) {from.q},{from.r} -> {dst.q},{dst.r}");
            RefreshHud();
            ReturnToRootActions();
        }

        private void ExecuteBulletHit(HexCoord enemyTile)
        {
            if (!TrySpendBulletCommonCost(out var pc)) return;
            _bulletUsedOnTurnIndex = _turnIndex;

            if (_boss == null) _boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
            if (_boss == null || !_boss.IsAlive) return;

            var b = CurrentBullet();
            if (b == null) return;

            // 현재는 보스 1개만
            var bossC = _boss.GetCoord();
            if (!enemyTile.Equals(bossC))
            {
                Debug.Log("[Bullet] invalid target (no enemy on tile)");
                return;
            }

            int baseHp = Mathf.Max(0, b.baseDamage);
            int shieldDmg = Mathf.Max(0, b.shieldDamage);
            float guardMod = Mathf.Clamp01(b.guardModifier);
            int heat = Mathf.Max(0, b.heat);
            bool isAoe = b.aoeRadius > 0;

            // ✅ 보스 타격 (canonical apply)
            _boss.ApplyBulletHit(baseHp, shieldDmg);
// ✅ 플레이어 히트(자기 피격 옵션은 BulletDefSO 확장 시 여기서 체크)
            // 현재 스펙: AOE면 플레이어도 맞는다.
            if (isAoe && _playerCoord.HasValue)
            {
                if (AxialDistance(enemyTile, _playerCoord.Value) <= b.aoeRadius)
                {
                    var self = PlayerController.Instance;
                    if (self != null)
                    {
                        // guardModifier 적용 피해
                        if (baseHp > 0)
                            self.ApplyDamageWithGuardModifier(baseHp, guardMod);

                        // shieldDamage는 플레이어 쉴드에도 적용
                        if (shieldDmg > 0)
                            self.ApplyShieldDamage(shieldDmg);
                    }
                }
            }

            // ✅ Heat: 과부하(자기 HP 감소)
            if (heat > 0)
            {
                var self = PlayerController.Instance;
                if (self != null)
                    self.ApplyTrueDamage(heat);
            }

            Debug.Log($"[Bullet] EXEC Hit bullet={b.bulletId} hp={baseHp} shield={shieldDmg} guardMod={guardMod} aoe={b.aoeRadius} heat={heat}");

            RefreshHud();
            ReturnToRootActions();
        }

        private List<HexCoord> BuildAoePreviewSet(HexCoord center, int radius)
        {
            var list = new List<HexCoord>(16);
            if (highlight == null || highlight.grid == null || highlight.grid.Tiles == null) return list;

            foreach (var kv in highlight.grid.Tiles)
            {
                var c = kv.Key;
                if (AxialDistance(center, c) <= radius)
                    list.Add(c);
            }
            return list;
        }
    }
}