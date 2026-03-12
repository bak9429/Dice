// Path: Assets/Script/UI/UIControl/CombatUIController.Core.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UI.UIBuilder.HUD;
using UI.UIBuilder.HexAction;
using Rule.Field;
using Rule.Combat.Player;
using GameData.Combat;
using GameData.Combat.Registry;
using Rule.Combat.Boss;

namespace UI.UIControl
{
    /// <summary>
    /// Prototype Combat UI Controller (split into partials)
    /// </summary>
    public partial class CombatUIController : MonoBehaviour
    {
        public HudRefs hud;
        public HexActionRefs action;
        public HexTileHighlightController highlight;
        private float _ignoreCloseUntilTime = -1f;

        [Header("Move")]
        public int baseMoveCost = 1;

        [Header("Example Loadout (stub)")]
        public string equippedMeleeWeaponId = "Axe_001";
        public string equippedGunId = "Gun_001";
        public string selectedBulletId = "Pierce";
        public List<string> equippedConsumables = new() { "RepairKit", "ShieldPatch" };

        protected MeleeWeaponDefSO _melee;
        protected GunDefSO _gun;
        protected BulletDefSO _bullet;
        protected AttackDefSO _selectedAttack;

        protected enum AttackTargetingState { None, Targeting }
        protected AttackTargetingState _atkState = AttackTargetingState.None;
        protected HexCoord? _atkTarget;
        protected readonly HashSet<HexCoord> _atkCandidates = new();

        protected TurnRuntime _turn = new TurnRuntime();
        protected int _turnIndex = 1;

        protected HexCoord? _playerCoord;
        protected HexCoord? _pendingMoveDest;
        protected HexCoord? _selectedHex;

        protected bool _buttonsBound;

        protected int _lastAP = int.MinValue;
        protected HexCoord? _lastPlayerCoord;
        protected bool _lastPlayerTurn = true;

        protected BossController _boss;
        protected bool _busy;

        protected Coroutine _hitFlashCo;

        protected TelegraphFilterController _teleFilter;
        protected bool _teleFilterInited;

        protected readonly List<GameObject> _dynamicRows = new();

        // --- Always-on attack range overlay (passive) ---
        [SerializeField] protected bool alwaysShowAttackRange = true;

        protected readonly HashSet<HexCoord> _passiveAtkRange = new();
        protected HexCoord? _lastPassiveFrom;
        protected int _lastPassiveMin = int.MinValue;
        protected int _lastPassiveMax = int.MinValue;

        // ✅ Dagger AP refund chance (per successful attack)
        [Header("Weapon Gimmicks")]
        [Range(0f, 1f)]
        [SerializeField] protected float daggerApRefundChance = 0.30f;

        // --- Boss shield/groggy HUD cache ---
        protected int _bossShield = 0;
        protected int _bossMaxShield = 0;
        protected bool _bossIsGroggy = false;
        protected int _bossGroggyTurnsLeft = 0;

        protected static T FindActiveFirst<T>() where T : UnityEngine.Object
        {
            var active = FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
            if (active != null) return active;
            return FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }

        private void Awake()
        {
            Debug.Log($"[CombatUIController] Awake instance={name} meleeId={equippedMeleeWeaponId}");
            hud = hud != null ? hud : FindActiveFirst<HudRefs>();
            action = action != null ? action : FindActiveFirst<HexActionRefs>();
            highlight = highlight != null ? highlight : FindActiveFirst<HexTileHighlightController>();
            _boss = FindActiveFirst<BossController>();

            if (highlight != null)
            {
                Debug.Log($"[CombatUIController] highlight={highlight.name} active={highlight.gameObject.activeInHierarchy}");
                if (highlight.grid != null)
                    Debug.Log($"[CombatUIController] highlight.grid={highlight.grid.name} active={highlight.grid.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogWarning("[CombatUIController] highlight not found.");
            }
        }

        private void OnEnable()
        {
            HexSelectionController.OnHexSelected += OnHexSelected;
            HexSelectionController.OnHexDeselected += OnHexDeselected;

            BossController.OnBossHpChanged += OnBossHpChanged;
            BossController.OnBossShieldChanged += OnBossShieldChanged;
            BossController.OnBossGroggyChanged += OnBossGroggyChanged;

            PlayerController.OnHpChanged += OnPlayerHpChanged;
            PlayerController.OnDamaged += OnPlayerDamaged;
            PlayerController.OnShieldChanged += OnPlayerShieldChanged;
        }

        private void OnDisable()
        {
            HexSelectionController.OnHexSelected -= OnHexSelected;
            HexSelectionController.OnHexDeselected -= OnHexDeselected;

            BossController.OnBossHpChanged -= OnBossHpChanged;
            BossController.OnBossShieldChanged -= OnBossShieldChanged;
            BossController.OnBossGroggyChanged -= OnBossGroggyChanged;

            PlayerController.OnHpChanged -= OnPlayerHpChanged;
            PlayerController.OnDamaged -= OnPlayerDamaged;
            PlayerController.OnShieldChanged -= OnPlayerShieldChanged;
        }

        private void Start()
        {
            if (!_turn.TryBind())
                Debug.LogWarning("[CombatUIController] TurnController not bound yet.");

            EnsurePlayerControllerAndSpawn();

            SyncPlayerCoordFromPlayerController(force: true);
            UpdatePassiveAttackRangeOverlay(force: true);

            StartCoroutine(CoStartFirstTurn());
        }

        private void Update()
        {
            EnsureRefs();
            EnsureButtonsBound();

            if (!_turn.IsReady) _turn.TryBind();

            SyncPlayerCoordFromPlayerController(force: false);

            RefreshHud();
            UpdateAlwaysMoveOverlay();
            UpdatePassiveAttackRangeOverlay(force: false);
            EnsureTelegraphFilter();

            UpdateAttackButtonState();
            UpdateBulletButtonState();
        }

        protected void EnsureRefs()
        {
            if (hud == null) hud = FindFirstObjectByType<HudRefs>(FindObjectsInactive.Include);
            if (action == null) action = FindFirstObjectByType<HexActionRefs>(FindObjectsInactive.Include);
            if (highlight == null) highlight = FindFirstObjectByType<HexTileHighlightController>(FindObjectsInactive.Include);
        }

        protected void EnsureTelegraphFilter()
        {
            if (_teleFilterInited) return;

            var layout = FindFirstObjectByType<UI.UIBuilder.CombatLayout.CombatLayoutRefs>(FindObjectsInactive.Include);
            if (layout == null || layout.bottomCombat == null) return;

            var go = GameObject.Find("TelegraphFilterController");
            if (go == null) go = new GameObject("TelegraphFilterController");

            _teleFilter = go.GetComponent<TelegraphFilterController>();
            if (_teleFilter == null) _teleFilter = go.AddComponent<TelegraphFilterController>();

            _teleFilter.Init(layout.bottomCombat);
            _teleFilterInited = true;

            Debug.Log("[CombatUI] Telegraph filter UI ready.");
        }

        protected void EnsureButtonsBound()
        {
            if (_buttonsBound) return;
            if (hud == null || action == null) return;

            if (hud.btnEndTurn != null)
                hud.btnEndTurn.onClick.AddListener(OnEndTurnClicked);

            if (action.btnMove != null) action.btnMove.onClick.AddListener(OnMoveCommitClicked);
            if (action.btnAttack != null) action.btnAttack.onClick.AddListener(OnAttackClicked);
            if (action.btnDefend != null) action.btnDefend.onClick.AddListener(OnDefendClicked);

            // ✅ Bullet: 버튼 누르면 "리스트 화면"으로 전환(Consumable과 동일 UX)
            if (action.btnBullet != null) action.btnBullet.onClick.AddListener(OnBulletButtonPressed);

            // ✅ Consumable도 리스트 열기 전에 패널 보장
            if (action.btnConsumable != null) action.btnConsumable.onClick.AddListener(() =>
            {
                ShowHexActionPanel(true);
                OpenConsumableList();
            });

            // ✅ Close: 패널 닫고 타겟팅도 정리
            if (action.btnClose != null)
                action.btnClose.onClick.AddListener(() =>
                {
                    if (Time.unscaledTime < _ignoreCloseUntilTime)
                    {
                        Debug.Log("[UI] Close ignored (debounce)");
                        return;
                    }
                    _pendingMoveDest = null;

                    if (_atkState == AttackTargetingState.Targeting)
                        ExitAttackTargeting(reopenPanel: false);

                    // Bullet targeting은 Bullet.cs에 구현되어 있으니 여기서도 빠져나오게 처리
                    ForceExitBulletTargetingIfAny();

                    ShowHexActionPanel(false);
                });

            _buttonsBound = true;
            Debug.Log("[CombatUIController] Buttons bound (late-bind ok).");
        }

        /// <summary>
        /// Bullet.cs에 있는 상태를 모르는 Core에서 "있으면 끄기"만 할 수 있게 만든 안전한 훅.
        /// (Bullet.cs에서 이 함수를 구현해둠)
        /// </summary>
        private void ForceExitBulletTargetingIfAny()
        {
            // partial에서 구현됨 (Bullet.cs)
            TryExitBulletTargetingFromCore();
        }

        protected void EnsurePlayerControllerAndSpawn()
        {
            Debug.Log($"[Spawn] Using meleeId = {equippedMeleeWeaponId}");
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc == null)
            {
                var go = new GameObject("PlayerController");
                pc = go.AddComponent<PlayerController>();
            }

            pc.EnsureSpawned();

            AttackRegistry.Warmup();
            BulletRegistry.Warmup();
            MeleeWeaponRegistry.Warmup();
            GunRegistry.Warmup();
            GameData.Items.Consumables.ConsumableRegistry.Warmup();

            _melee = MeleeWeaponRegistry.Get(equippedMeleeWeaponId) ?? MeleeWeaponRegistry.GetAny();
            if (_melee != null) equippedMeleeWeaponId = _melee.weaponId;

            _gun = GunRegistry.Get(equippedGunId) ?? GunRegistry.GetAny();
            if (_gun != null) equippedGunId = _gun.gunId;

            // Bullet must be allowed by gun (id-based)
            _bullet = BulletRegistry.Get(selectedBulletId);

            if (_gun != null)
            {
                bool allows = (_bullet != null) && _gun.AllowsBullet(_bullet.bulletId);

                if (!allows)
                {
                    // pick first allowed bullet (or null if none)
                    _bullet = null;
                    var all = BulletRegistry.GetAll();
                    for (int i = 0; i < all.Count; i++)
                    {
                        var b = all[i];
                        if (b == null) continue;
                        if (_gun.AllowsBullet(b.bulletId))
                        {
                            _bullet = b;
                            break;
                        }
                    }
                }
            }

            if (_bullet != null) selectedBulletId = _bullet.bulletId;

            Debug.Log($"[Loadout] Melee={equippedMeleeWeaponId} Gun={equippedGunId} Bullet={selectedBulletId} Cons=[{string.Join(",", equippedConsumables)}]");

            var pcInit = PlayerController.Instance;
            if (pcInit != null)
            {
                foreach (var c in equippedConsumables)
                {
                    var id = string.IsNullOrWhiteSpace(c) ? "" : c.Trim();
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    // ✅ 최소 3개로 시작(프로토용)
                    pcInit.EnsureConsumableCount(id, 3);
                }

                if (_bullet != null)
                    Debug.Log($"[Bullet] Init ammo: {_bullet.bulletId}={pcInit.GetBulletAmmo(_bullet.bulletId)}/{pcInit.GetBulletMaxAmmo(_bullet.bulletId)} (gateClears={pcInit.GateBossClears})");

                Debug.Log($"[Consumable] Init counts: " +
                          $"{(equippedConsumables.Count > 0 ? equippedConsumables[0] : "-")}={pcInit.GetConsumableCount(equippedConsumables.Count > 0 ? equippedConsumables[0] : "")}, " +
                          $"{(equippedConsumables.Count > 1 ? equippedConsumables[1] : "-")}={pcInit.GetConsumableCount(equippedConsumables.Count > 1 ? equippedConsumables[1] : "")}");
            }

            RefreshPlayerVitalsUIFromState();
        }

        // Core.cs 안의 EnsureLoadoutResolved() 함수 전체를 이것으로 교체
        protected void EnsureLoadoutResolved()
        {
            if (_melee == null || _gun == null)
            {
                AttackRegistry.Warmup();
                BulletRegistry.Warmup();
                MeleeWeaponRegistry.Warmup();
                GunRegistry.Warmup();
                GameData.Items.Consumables.ConsumableRegistry.Warmup();

                _melee = MeleeWeaponRegistry.Get(equippedMeleeWeaponId) ?? MeleeWeaponRegistry.GetAny();
                _gun = GunRegistry.Get(equippedGunId) ?? GunRegistry.GetAny();

                if (_bullet == null)
                {
                    _bullet = BulletRegistry.Get(selectedBulletId);

                    if (_gun != null)
                    {
                        bool allows = (_bullet != null) && _gun.AllowsBullet(_bullet.bulletId);
                        if (!allows)
                        {
                            _bullet = null;
                            var all = BulletRegistry.GetAll();
                            for (int i = 0; i < all.Count; i++)
                            {
                                var b = all[i];
                                if (b == null) continue;
                                if (_gun.AllowsBullet(b.bulletId))
                                {
                                    _bullet = b;
                                    break;
                                }
                            }
                        }
                    }

                    if (_bullet != null)
                        selectedBulletId = _bullet.bulletId;
                }

                var pc2 = PlayerController.Instance;
                if (pc2 != null)
                {
                    foreach (var c in equippedConsumables)
                    {
                        if (string.IsNullOrWhiteSpace(c)) continue;
                        pc2.EnsureConsumableCount(c.Trim(), 3);
                    }
                }
            }
        }
        protected void RefreshHud()
        {
            if (hud == null) return;

            if (hud.apText != null) hud.apText.text = $"AP {_turn.GetAP()}";
            if (hud.turnText != null) hud.turnText.text = $"TURN {_turnIndex}";

            var pc = PlayerController.Instance;
            if (pc != null && hud.playerHpText != null)
                hud.playerHpText.text = $"{pc.state.hp}/{pc.state.maxHp}";

            if (pc != null && hud.playerShieldFill != null)
            {
                float denom = Mathf.Max(1, pc.state.maxShield);
                hud.playerShieldFill.fillAmount = Mathf.Clamp01(pc.state.shield / denom);
            }

            var bc = BossController.Instance;
            if (bc != null && hud.bossHpText != null && bc.def != null)
                hud.bossHpText.text = $"{bc.hp}/{bc.def.maxHp}";
        }

        protected void RefreshPlayerVitalsUIFromState()
        {
            var pc = PlayerController.Instance;
            if (pc == null || pc.state == null) return;
            OnPlayerHpChanged(pc.state.hp, pc.state.maxHp);
            OnPlayerShieldChanged(pc.state.shield, pc.state.maxShield);
        }

        // --- HUD event handlers ---
        private void OnBossHpChanged(int hp, int maxHp)
        {
            if (hud != null && hud.bossHpText != null)
                hud.bossHpText.text = $"{hp}/{maxHp}";
        }

        private void OnBossShieldChanged(int shield, int maxShield)
        {
            Debug.Log($"[HUD] BossShield {shield}/{maxShield}");

            _bossShield = shield;
            _bossMaxShield = Mathf.Max(0, maxShield);
            RefreshBossShieldHud();
        }

        private void OnBossGroggyChanged(bool isGroggy, int turnsLeft)
        {
            _bossIsGroggy = isGroggy;
            _bossGroggyTurnsLeft = Mathf.Max(0, turnsLeft);
            RefreshBossShieldHud();
        }

        protected void RefreshBossShieldHud()
        {
            if (hud == null || hud.bossShieldText == null) return;

            var bc = BossController.Instance;
            if (bc != null && bc.def != null)
            {
                _bossShield = bc.shield;
                _bossMaxShield = bc.def.maxShield;
            }

            string baseText = $"{_bossShield}/{_bossMaxShield}";
            hud.bossShieldText.text = _bossIsGroggy
                ? $"{baseText}  GROGGY({_bossGroggyTurnsLeft})"
                : baseText;
        }

        private void OnPlayerHpChanged(int hp, int maxHp)
        {
            if (hud != null && hud.playerHpText != null)
                hud.playerHpText.text = $"{hp}/{maxHp}";
        }

        private void OnPlayerShieldChanged(int shield, int maxShield)
        {
            if (hud == null) return;

            if (hud.playerShieldFill != null)
            {
                float denom = Mathf.Max(1, maxShield);
                hud.playerShieldFill.fillAmount = Mathf.Clamp01(shield / denom);
            }
            if (hud.playerShieldText != null)
                hud.playerShieldText.text = $"{shield}/{maxShield}";
        }

        private void OnPlayerDamaged(int dmg)
        {
            if (hud == null || hud.cinematicHitFlash == null) return;

            if (_hitFlashCo != null) StopCoroutine(_hitFlashCo);
            _hitFlashCo = StartCoroutine(CoHitFlash(hud.cinematicHitFlash, 0.5f));
        }

        private static IEnumerator CoHitFlash(UnityEngine.UI.Image img, float duration)
        {
            if (img == null) yield break;

            float t = 0f;
            img.color = new Color(1f, 0f, 0f, 0.35f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.35f, 0f, t / duration);
                img.color = new Color(1f, 0f, 0f, a);
                yield return null;
            }
            img.color = new Color(1f, 0f, 0f, 0f);
        }

        protected int AxialDistance(HexCoord a, HexCoord b)
        {
            int ax = a.q, az = a.r, ay = -ax - az;
            int bx = b.q, bz = b.r, by = -bx - bz;

            int dx = Mathf.Abs(ax - bx);
            int dy = Mathf.Abs(ay - by);
            int dz = Mathf.Abs(az - bz);

            return (dx + dy + dz) / 2;
        }

        protected void ForceDeselectHex()
        {
            _selectedHex = null;
            _pendingMoveDest = null;

            var sel = FindFirstObjectByType<HexSelectionController>(FindObjectsInactive.Include);
            if (sel != null) sel.ForceDeselect();
        }
    }
}