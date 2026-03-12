// Path: Assets/Script/UI/UIControl/CombatUIController.Attack.cs
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
        private enum ResolvedMeleeMode
        {
            Default = 0,
            GreatswordLight = 1,
            GreatswordHeavy = 2,
        }

        // --- Attack targeting / confirm ---
        private HexCoord? _atkSelectedTile = null;
        private string _atkLastReason = "";

        private ResolvedMeleeMode _atkResolvedMode = ResolvedMeleeMode.Default;
        private int _atkResolvedRangeMin = 1;
        private int _atkResolvedRangeMax = 1;

        // Greatsword stance: use PlayerController.state flag (exists in your project logs)
        private bool IsGreatswordNextHeavy()
        {
            if (_melee == null || _melee.weaponId != "Greatsword_001") return false;
            var pc = PlayerController.Instance;
            if (pc == null || pc.state == null) return false;
            // ✅ Fix: heavy-first, then light.
            return pc.state.greatswordFirstAttackUsedThisTurn == false;
        }

        private bool IsGreatswordHeavyAttack(AttackDefSO atk)
        {
            if (_melee == null || _melee.weaponId != "Greatsword_001") return false;
            if (atk == null) return false;
            // We no longer depend on comparing SO references. Use resolved mode.
            return _atkResolvedMode == ResolvedMeleeMode.GreatswordHeavy;
        }

        private void ResolveAttackForCurrentMelee()
        {
            EnsureLoadoutResolved();

            _atkResolvedMode = ResolvedMeleeMode.Default;
            _atkResolvedRangeMin = 1;
            _atkResolvedRangeMax = 1;

            if (_melee == null)
            {
                _selectedAttack = null;
                return;
            }

            // Ensure fallback attacks exist (AttackDefSO only has apCost + shape)
            if (_melee.basicAttack == null)
                _melee.basicAttack = AttackRegistry.CreateRuntime(
                    "Basic",
                    "Attack",
                    2,
                    TargetShape.SingleHex,
                    "Basic melee"
                );

            if (_melee.heavyAttack == null)
                _melee.heavyAttack = AttackRegistry.CreateRuntime(
                    "Heavy",
                    "Heavy",
                    3,
                    TargetShape.Radius1,
                    "Heavy melee"
                );

            bool isGS = _melee.weaponId == "Greatsword_001";
            bool isAxe = _melee.weaponId.StartsWith("Axe");
            bool isSingle = _melee.weaponId.StartsWith("Rapier") || _melee.weaponId.StartsWith("Sword") || _melee.weaponId.StartsWith("Dagger");

            if (isGS)
            {
                bool heavy = IsGreatswordNextHeavy();
                _atkResolvedMode = heavy ? ResolvedMeleeMode.GreatswordHeavy : ResolvedMeleeMode.GreatswordLight;

                _atkResolvedRangeMin = 1;
                _atkResolvedRangeMax = heavy ? 2 : 1; // ✅ GS heavy range 2

                int ap = heavy ? Mathf.Max(0, _melee.heavyAttack.apCost) : Mathf.Max(0, _melee.basicAttack.apCost);
                var shape = heavy ? TargetShape.Radius1 : TargetShape.SideFlanks;
                var title = heavy ? "Heavy" : "Attack";

                _selectedAttack = AttackRegistry.CreateRuntime(
                    heavy ? "GS_Heavy" : "GS_Light",
                    title,
                    ap,
                    shape,
                    heavy ? "GS heavy (range 2, radius1)" : "GS light (3-hex)"
                );
                return;
            }

            if (isAxe)
            {
                _atkResolvedRangeMin = 1;
                _atkResolvedRangeMax = 1;
                int ap = Mathf.Max(0, _melee.basicAttack.apCost);
                _selectedAttack = AttackRegistry.CreateRuntime(
                    "Axe_Sweep",
                    "Attack",
                    ap,
                    TargetShape.SideFlanks,
                    "Axe 3-hex"
                );
                return;
            }

            if (isSingle)
            {
                _atkResolvedRangeMin = 1;
                _atkResolvedRangeMax = 1;
                int ap = Mathf.Max(0, _melee.basicAttack.apCost);
                _selectedAttack = AttackRegistry.CreateRuntime(
                    "Melee_Single",
                    "Attack",
                    ap,
                    TargetShape.SingleHex,
                    "Single-hex"
                );
                return;
            }

            // fallback
            _atkResolvedRangeMin = Mathf.Max(1, _melee.rangeMin);
            _atkResolvedRangeMax = Mathf.Max(_atkResolvedRangeMin, _melee.rangeMax);
            _selectedAttack = _melee.basicAttack;
        }

        // Axe ramp (shield interfere increases, AP cost increases) - reset each turn
        private int _axeChainCount = 0;

        // 6 directions (axial neighbors)
        private static readonly HexCoord[] DIRS = new HexCoord[]
        {
            new HexCoord(+1, 0),
            new HexCoord(+1,-1),
            new HexCoord( 0,-1),
            new HexCoord(-1, 0),
            new HexCoord(-1,+1),
            new HexCoord( 0,+1),
        };

        private void OnAttackClicked()
        {
            EnsureLoadoutResolved();

            if (_busy || !IsPlayerTurn())
            {
                Debug.Log("[Attack] Not allowed now.");
                return;
            }

            // If we were already in targeting, reset it so the flow is deterministic.
            if (_atkState == AttackTargetingState.Targeting)
            {
                ExitAttackTargeting(reopenPanel: false);
            }

            // ✅ Prevent click-through: panel opens this frame and the close button can receive the same click
            _ignoreCloseUntilTime = Time.unscaledTime + 0.12f;

            // ✅ Attack click should NOT open attack list. It resolves the attack automatically,
            // then enters target->preview->confirm flow.
            ResolveAttackForCurrentMelee();
            BeginAttackTargeting(closePanelFirst: true);
        }

        private bool CanAttackNow(out string reason)
        {
            reason = "";

            EnsureLoadoutResolved();

            if (!IsPlayerTurn()) { reason = "not player turn"; return false; }
            if (_busy) { reason = "busy"; return false; }
            if (_melee == null) { reason = "no melee"; return false; }
            if (!_playerCoord.HasValue) { reason = "no player coord"; return false; }

            if (_boss == null) _boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
            if (_boss == null || !_boss.IsAlive) { reason = "no boss"; return false; }

            // Resolve the attack *as it would be resolved on click* (Greatsword auto Light/Heavy, etc)
            bool isGS = _melee.weaponId == "Greatsword_001";
            bool isAxe = _melee.weaponId.StartsWith("Axe");
            bool isSingle = _melee.weaponId.StartsWith("Rapier") || _melee.weaponId.StartsWith("Sword") || _melee.weaponId.StartsWith("Dagger");

            int rmin = 1;
            int rmax = 1;
            int apCost = 0;

            // Ensure fallback attacks exist (AttackDefSO only has apCost + shape)
            if (_melee.basicAttack == null)
                _melee.basicAttack = AttackRegistry.CreateRuntime("Basic", "Attack", 2, TargetShape.SingleHex, "Basic melee");

            if (_melee.heavyAttack == null)
                _melee.heavyAttack = AttackRegistry.CreateRuntime("Heavy", "Heavy", 3, TargetShape.Radius1, "Heavy melee");

            if (isGS)
            {
                bool heavy = IsGreatswordNextHeavy();
                rmax = heavy ? 2 : 1; // ✅ GS heavy range 2
                apCost = heavy ? Mathf.Max(0, _melee.heavyAttack.apCost) : Mathf.Max(0, _melee.basicAttack.apCost);
            }
            else if (isAxe)
            {
                rmax = 1;
                apCost = Mathf.Max(0, _melee.basicAttack.apCost);
            }
            else if (isSingle)
            {
                rmax = 1;
                apCost = Mathf.Max(0, _melee.basicAttack.apCost);
            }
            else
            {
                rmin = Mathf.Max(1, _melee.rangeMin);
                rmax = Mathf.Max(rmin, _melee.rangeMax);
                apCost = (_melee.basicAttack != null) ? Mathf.Max(0, _melee.basicAttack.apCost) : 0;
            }

            // AP check (Axe ramp etc. is handled in Execute; button enable is "currently possible" -> base cost only)
            if (_turn.GetAP() < apCost) { reason = "not enough AP"; return false; }

            var p = _playerCoord.Value;
            var e = _boss.GetCoord();
            int d = AxialDistance(p, e);
            if (d < rmin || d > rmax) { reason = "out of range"; return false; }

            return true;
        }

        private void UpdateAttackButtonState()
        {
            if (action == null || action.btnAttack == null) return;

            bool ok = CanAttackNow(out _);
            action.btnAttack.interactable = ok;
        }

        // NOTE: Panel.cs might call this; keep it to avoid compile errors.
        private void OpenAttackList()
        {
            // Compatibility: treat as clicking Attack.
            ResolveAttackForCurrentMelee();
            BeginAttackTargeting(closePanelFirst: true);
        }

        private void BeginAttackTargeting(bool closePanelFirst)
        {
            EnsureLoadoutResolved();

            _atkState = AttackTargetingState.Targeting;
            _atkTarget = null;
            _atkSelectedTile = null;
            _atkLastReason = "";

            _atkCandidates.Clear();

            if (closePanelFirst) ShowHexActionPanel(false);
            else ShowHexActionPanel(true);

            highlight?.HighlightAttackPreview(System.Array.Empty<HexCoord>());

            if (_selectedAttack == null || _melee == null)
            {
                _atkLastReason = "no attack/weapon";
                if (!closePanelFirst) BuildAttackConfirmPanel();
                return;
            }

            if (_boss == null) _boss = FindActiveFirst<BossController>();
            if (_boss == null || !_boss.IsAlive)
            {
                _atkLastReason = "no boss";
                if (!closePanelFirst) BuildAttackConfirmPanel();
                return;
            }

            if (!_playerCoord.HasValue)
            {
                _atkLastReason = "no playerCoord";
                if (!closePanelFirst) BuildAttackConfirmPanel();
                return;
            }

            // ---- Range rules (resolved at Attack click) ----
            int rmin = Mathf.Max(1, _atkResolvedRangeMin);
            int rmax = Mathf.Max(rmin, _atkResolvedRangeMax);

            var p = _playerCoord.Value;
            var e = _boss.GetCoord();
            int d = AxialDistance(p, e);

            if (d >= rmin && d <= rmax)
                _atkCandidates.Add(e);

            // candidate outline (red)
            highlight?.HighlightAttackRange(_atkCandidates, fill: false);

            if (!closePanelFirst) BuildAttackConfirmPanel();
        }

        private void ExitAttackTargeting(bool reopenPanel)
        {
            _atkState = AttackTargetingState.None;
            _atkTarget = null;
            _atkSelectedTile = null;
            _atkLastReason = "";
            _atkCandidates.Clear();

            highlight?.HighlightAttackPreview(System.Array.Empty<HexCoord>());

            if (reopenPanel)
            {
                ShowHexActionPanel(true);
                ReturnToRootActions();
            }
            else
            {
                ShowHexActionPanel(false);
            }
        }

        // called from Move.cs when a tile is clicked during attack targeting
        private void HandleAttackHexSelected(HexCoord coord)
        {
            if (_atkState != AttackTargetingState.Targeting) return;

            _atkSelectedTile = coord;

            if (!_atkCandidates.Contains(coord))
            {
                // ✅ UX fix: invalid click clears target/preview and keeps confirm disabled.
                _atkTarget = null;
                _atkLastReason = "invalid target";
                highlight?.HighlightAttackPreview(System.Array.Empty<HexCoord>());
                BuildAttackConfirmPanel();
                return;
            }

            _atkTarget = coord;

            // ✅ selection preview (green via AttackPreview overlay color)
            var affected = BuildAffectedCoordsForCurrentAttack(coord);
            Debug.Log($"[AttackPreview] target={coord.q},{coord.r} affected={affected.Count} mode={_atkResolvedMode}");
            highlight?.HighlightAttackPreview(affected);

            _atkLastReason = "ok";

            // reopen panel for confirm
            ShowHexActionPanel(true);
            BuildAttackConfirmPanel();
        }

        private void BuildAttackConfirmPanel()
        {
            ClearDynamicRows();
            ShowRootRows(false);

            string atkName = _selectedAttack != null ? _selectedAttack.displayName : "None";
            SetPanelTitle($"ATTACK: {atkName}");

            AddDynamicRow("< Cancel", "Return to actions", "<", () =>
            {
                ExitAttackTargeting(reopenPanel: true);
            });

            string selText = _atkSelectedTile.HasValue ? $"{_atkSelectedTile.Value.q},{_atkSelectedTile.Value.r}" : "(none)";
            bool ok = _atkTarget.HasValue && _atkCandidates.Contains(_atkTarget.Value);

            AddDynamicRow($"Selected: {selText}", ok ? "ok" : _atkLastReason, "i", () => { }, interactable: false);

            AddDynamicRow("Confirm", ok ? "Execute attack" : $"Disabled: {_atkLastReason}", "✓", () =>
            {
                // ✅ Safety: don't assume nullable still has value at click time.
                if (!_atkTarget.HasValue) return;
                var target = _atkTarget.Value;
                if (!_atkCandidates.Contains(target)) return;

                ExecuteAttackAt(target);
                // ✅ After execution, return to root actions (user requirement)
                ExitAttackTargeting(reopenPanel: true);
            }, interactable: ok);
        }

        private List<HexCoord> BuildAffectedCoordsForCurrentAttack(HexCoord target)
        {
            var list = new List<HexCoord>(16);
            if (highlight == null || highlight.grid == null || highlight.grid.Tiles == null) return list;

            if (_selectedAttack == null || _melee == null) return list;

            bool isGS = _melee.weaponId == "Greatsword_001";
            bool gsHeavy = isGS && (_atkResolvedMode == ResolvedMeleeMode.GreatswordHeavy);

            // ✅ GS Heavy: radius1 splash
            if (gsHeavy)
            {
                foreach (var kv in highlight.grid.Tiles)
                {
                    var c = kv.Key;
                    if (AxialDistance(target, c) <= 1) list.Add(c);
                }
                return list;
            }

            // ✅ Axe: 3-hex (player-side flanks)
            if (_melee.weaponId.StartsWith("Axe"))
            {
                return BuildSideFlanks(target);
            }

            // ✅ GS Light: same as Axe (3-hex)
            if (isGS)
            {
                return BuildSideFlanks(target);
            }

            // ✅ Rapier/Sword/Dagger: single
            if (_melee.weaponId.StartsWith("Rapier") || _melee.weaponId.StartsWith("Sword") || _melee.weaponId.StartsWith("Dagger"))
            {
                list.Add(target);
                return list;
            }

            // Fallback: use AttackDefSO.shape only (no aoeRadius exists)
            if (_selectedAttack.shape == TargetShape.Radius1)
            {
                foreach (var kv in highlight.grid.Tiles)
                {
                    var c = kv.Key;
                    if (AxialDistance(target, c) <= 1) list.Add(c);
                }
                return list;
            }

            list.Add(target);
            return list;
        }

        // ✅ 방향이 정확히 6방향으로 안떨어져도 "가장 가까운 방향"을 고른다.
        // (dir을 못 찾으면 0으로 박혀서 모양이 뒤집히는 문제 방지)
        private int ResolveDirIndex(HexCoord diff)
        {
            // exact match first
            for (int i = 0; i < DIRS.Length; i++)
            {
                if (DIRS[i].q == diff.q && DIRS[i].r == diff.r)
                    return i;
            }

            // best dot (in axial space)
            int best = 0;
            int bestDot = int.MinValue;
            for (int i = 0; i < DIRS.Length; i++)
            {
                int dot = diff.q * DIRS[i].q + diff.r * DIRS[i].r;
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = i;
                }
            }
            return best;
        }

        // ✅ FIXED: "플레이어 쪽 3타일" = target + (player의 좌/우 1칸)
        // 기존(target 기준 좌/우)면 타겟 뒤쪽처럼 보일 수 있음.
        private List<HexCoord> BuildSideFlanks(HexCoord target)
        {
            var list = new List<HexCoord>(4);
            if (highlight == null || highlight.grid == null || highlight.grid.Tiles == null) return list;

            // 항상 타겟은 포함
            if (highlight.grid.Tiles.ContainsKey(target))
                list.Add(target);

            if (!_playerCoord.HasValue) return list;

            var from = _playerCoord.Value;

            // player -> target 방향
            var diff = new HexCoord(target.q - from.q, target.r - from.r);
            int dir = ResolveDirIndex(diff);

            int left = (dir + 5) % 6;
            int right = (dir + 1) % 6;

            // ✅ 플레이어 기준 좌/우
            var c1 = new HexCoord(from.q + DIRS[left].q, from.r + DIRS[left].r);
            var c2 = new HexCoord(from.q + DIRS[right].q, from.r + DIRS[right].r);

            if (highlight.grid.Tiles.ContainsKey(c1)) list.Add(c1);
            if (highlight.grid.Tiles.ContainsKey(c2)) list.Add(c2);

            return list;
        }

        // --- EXECUTION (keep minimal + compatible) ---
        private void ExecuteAttackAt(HexCoord target)
        {
            EnsureLoadoutResolved();
            if (_melee == null || _selectedAttack == null) return;

            if (_boss == null) _boss = FindActiveFirst<BossController>();
            if (_boss == null || !_boss.IsAlive) return;

            if (!_playerCoord.HasValue) return;
            if (!_atkCandidates.Contains(target)) return;

            bool isGS = (_melee.weaponId == "Greatsword_001");
            bool gsHeavy = isGS && (_atkResolvedMode == ResolvedMeleeMode.GreatswordHeavy);

            // AP check/spend
            int cost = Mathf.Max(0, _selectedAttack.apCost);

            // ✅ Axe AP ramp (0,1,2...) per successive hit, reset each turn
            if (_melee.weaponId == "Axe_001")
                cost += _axeChainCount;

            if (_turn.GetAP() < cost)
            {
                Debug.Log($"[Attack] Not enough AP. need={cost} ap={_turn.GetAP()}");
                return;
            }
            if (!_turn.TrySpendAP(cost))
            {
                Debug.Log("[Attack] Failed to spend AP.");
                return;
            }

            // Damage / interfere
            int baseDmg = Mathf.Max(1, _melee.baseDamage);
            int interfereBase = Mathf.Max(0, _melee.interfereDamage);

            float dmgMul = 1f;
            float interfereMul = 1f;

            // GS heavy bonus 1.2x
            if (gsHeavy) dmgMul *= 1.2f;

            // Axe interfere ramp +15% per chain step (example)
            if (_melee.weaponId == "Axe_001")
            {
                interfereMul *= (1f + 0.15f * _axeChainCount);
                _axeChainCount = Mathf.Clamp(_axeChainCount + 1, 0, 99);
            }
            else
            {
                _axeChainCount = 0;
            }

            int dmg = Mathf.RoundToInt(baseDmg * dmgMul * cost);
            int interfere = Mathf.RoundToInt(interfereBase * interfereMul);

            _boss.ApplyDamage(dmg);
            if (interfere > 0) _boss.ApplyShieldInterfere(interfere);

            // Mark GS heavy as used this turn (so next becomes light)
            if (isGS && gsHeavy)
            {
                var pc = PlayerController.Instance;
                if (pc != null && pc.state != null)
                    pc.state.greatswordFirstAttackUsedThisTurn = true;
            }

            Debug.Log($"[Attack] HIT boss. atk={_selectedAttack.displayName} apUsed={cost} dmg={dmg} interfere={interfere} gsHeavy={gsHeavy}");

            RefreshHud();
        }

        // Panel.cs compatibility: some versions call this
        private void TryExecuteAttack(AttackDefSO atk)
        {
            // Panel에서 넘겨준 공격을 "선택"으로 반영하고, 현재 타겟이 있으면 실행
            if (atk != null) _selectedAttack = atk;

            if (_atkTarget.HasValue)
                ExecuteAttackAt(_atkTarget.Value);
        }

        // passive always-on melee range outline (red outline)
        private void UpdatePassiveAttackRangeOverlay(bool force)
        {
            if (!alwaysShowAttackRange) return;
            if (highlight == null) return;
            if (!_playerCoord.HasValue) return;

            EnsureLoadoutResolved();
            if (_melee == null) return;

            int rmin = 1, rmax = 1;

            // Use your rule-set for passive too (keep simple)
            if (_melee.weaponId.StartsWith("Rapier") || _melee.weaponId.StartsWith("Sword") || _melee.weaponId.StartsWith("Dagger"))
            {
                rmin = 1; rmax = 1;
            }
            else if (_melee.weaponId.StartsWith("Axe"))
            {
                rmin = 1; rmax = 1;
            }
            else if (_melee.weaponId == "Greatsword_001")
            {
                // ✅ Greatsword: 이번 턴 "다음 공격"이 Heavy(자동 확정)면 사거리 2를 패시브 테두리에서도 보여준다.
                // (Light면 1)
                rmin = 1;
                rmax = IsGreatswordNextHeavy() ? 2 : 1;
            }
            else
            {
                rmin = Mathf.Max(1, _melee.rangeMin);
                rmax = Mathf.Max(rmin, _melee.rangeMax);
            }

            bool changed = force;
            if (!force && _lastPassiveFrom.HasValue && _lastPassiveFrom.Value.Equals(_playerCoord.Value) &&
                _lastPassiveMin == rmin && _lastPassiveMax == rmax)
            {
                changed = false;
            }
            if (!changed) return;

            _lastPassiveFrom = _playerCoord.Value;
            _lastPassiveMin = rmin;
            _lastPassiveMax = rmax;

            _passiveAtkRange.Clear();

            foreach (var kv in highlight.grid.Tiles)
            {
                var c = kv.Key;
                int d = AxialDistance(_playerCoord.Value, c);
                if (d >= rmin && d <= rmax)
                    _passiveAtkRange.Add(c);
            }

            highlight.HighlightAttackRange(_passiveAtkRange, fill: false);
        }

        private void ResetMeleeTurnState()
        {
            _axeChainCount = 0;
        }
    }
}