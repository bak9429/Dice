// Path: Assets/Script/Rule/Combat/Boss/BossController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rule.Field;
using GameData.Boss;
using GameData.Combat;

namespace Rule.Combat.Boss
{
    public class BossController : MonoBehaviour
    {
        [Header("Input")]
        public string bossId = "BOSS_01";

        [Header("Runtime")]
        public BossDefSO def;
        public int hp;
        public int shield;

        [Header("Groggy (runtime)")]
        [SerializeField] private bool isGroggy;
        [SerializeField] private int groggyTurnsLeft;

        public bool IsAlive => hp > 0;
        public bool IsGroggy => isGroggy;
        public int GroggyTurnsLeft => groggyTurnsLeft;

        [Header("Pattern (proto)")]
        public int patternsPerTurn = 2;

        private HexGridBuilder _grid;
        private TelegraphOverlayApplier _overlay;

        private GameObject _bossGo;
        private SpriteRenderer _sr;
        private HexCoord _coord;

        private readonly List<TelegraphIntent> _prepared = new();
        private bool _hasPrepared;
        private int _patternCursor = 0;

        private int _telegraphVersion = 0;
        public int TelegraphVersion => _telegraphVersion;

        public static BossController Instance { get; private set; }
        public HexGridBuilder grid;

        // UI hooks
        public static event System.Action<int, int> OnBossHpChanged;
        public static event System.Action<int, int> OnBossShieldChanged;              // (shield, maxShield)
        public static event System.Action<bool, int> OnBossGroggyChanged;             // (isGroggy, turnsLeft)

        private void Awake()
        {
            Instance = this;
            if (grid == null)
                grid = FindFirstObjectByType<HexGridBuilder>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            _grid = FindFirstObjectByType<HexGridBuilder>(FindObjectsInactive.Include);
            if (_grid == null)
            {
                Debug.LogError("[BossController] HexGridBuilder not found.");
                return;
            }

            _overlay = new TelegraphOverlayApplier(_grid);

            BossRegistry.Warmup();
            def = BossRegistry.Get(bossId);
            if (def == null) def = BossRegistry.GetAny();

            if (def == null)
            {
                Debug.LogError("[BossController] BossDefSO not found. Put assets under Resources/GameData/BossDefs/");
                return;
            }

            hp = def.maxHp;
            shield = Mathf.Max(0, def.maxShield);
            Debug.Log($"[Boss] Init hp={hp}/{def.maxHp} shield={shield}/{def.maxShield}");

            isGroggy = false;
            groggyTurnsLeft = 0;

            _coord = new HexCoord(def.spawnAxial.x, def.spawnAxial.y);

            // ✅ 보스별 오버레이 팔레트 적용(예고/위험/실행 색)
            ApplyOverlayPaletteFromDef();

            SpawnWorldSprite();

            // 초기 UI 브로드캐스트
            OnBossHpChanged?.Invoke(hp, def.maxHp);
            OnBossShieldChanged?.Invoke(shield, def.maxShield);
            OnBossGroggyChanged?.Invoke(isGroggy, groggyTurnsLeft);

            PrepareNextTelegraph(); // 첫 예고
        }

        private void ApplyOverlayPaletteFromDef()
        {
            if (_grid == null || def == null) return;
            if (!def.overrideOverlayColors) return;

            int changed = 0;
            foreach (var kv in _grid.Tiles)
            {
                var tile = kv.Value;
                if (tile == null) continue;

                tile.telegraphOverlayColor = def.telegraphOverlayColor;
                tile.dangerOverlayColor = def.dangerOverlayColor;
                tile.executeOverlayColor = def.executeOverlayColor;
                changed++;
            }

            Debug.Log($"[BossController] Applied overlay palette from Def. tiles={changed}");
        }

        private void SpawnWorldSprite()
        {
            if (_bossGo != null) Destroy(_bossGo);

            // (1) 기본: Boss_{id} 오브젝트 생성
            _bossGo = new GameObject($"Boss_{def.bossId}");
            _bossGo.layer = _grid.gameObject.layer;
            _bossGo.transform.SetParent(_grid.tilesRoot != null ? _grid.tilesRoot : _grid.transform, true);

            // 보스 위치
            var pos = HexMath.AxialToWorld(_coord, _grid.config.hexSize, _grid.config.y);

            // (2) 리소스 스프라이트가 있으면 SpriteRenderer로 표시
            Sprite spr = null;
            if (!string.IsNullOrWhiteSpace(def.worldSpriteResource))
            {
                spr = Resources.Load<Sprite>(def.worldSpriteResource);
                if (spr == null)
                    Debug.LogWarning($"[BossController] worldSpriteResource not found: {def.worldSpriteResource}");
            }

            if (spr != null)
            {
                _sr = _bossGo.AddComponent<SpriteRenderer>();
                _sr.sprite = spr;

                // sorting이 너무 낮으면 타일/오버레이 뒤로 묻힐 수 있어서 최소값 보정
                _sr.sortingOrder = Mathf.Max(def.worldSortingOrder, 22);

                _bossGo.transform.position = pos;
                Debug.Log($"[BossController] Spawned sprite boss '{def.displayName}' at {_coord.q},{_coord.r} fieldId={def.fieldId}");
                return;
            }

            // (3) ✅ fallback: 항상 보이게 "B" 마커 생성
            var marker = Rule.Combat.WorldMarkerFactory.CreateLetterMarker("BossMarker", "B", Color.red, Mathf.Max(def.worldSortingOrder, 22));
            marker.transform.SetParent(_bossGo.transform, worldPositionStays: true);
            marker.transform.position = pos;

            Debug.Log($"[BossController] Spawned fallback marker boss '{def.displayName}' at {_coord.q},{_coord.r} fieldId={def.fieldId}");
        }

        public HexCoord GetCoord() => _coord;

        // ------------------------------------------------------------
        // Shield / Groggy Core
        // ------------------------------------------------------------

        private void EnterGroggy()
        {
            if (isGroggy) return;

            isGroggy = true;
            groggyTurnsLeft = Mathf.Max(1, def != null ? def.groggyTurns : 1);

            Debug.Log($"[Boss] GROGGY ENTER turns={groggyTurnsLeft}");
            OnBossGroggyChanged?.Invoke(isGroggy, groggyTurnsLeft);

            // ✅ 그로기 진입 시, 기존 텔레그래프는 무효화 (보스턴 스킵)
            _overlay?.ClearAll();
            _prepared.Clear();
            _hasPrepared = false;
            _telegraphVersion++;
        }

        private void ExitGroggyAndRestoreShield()
        {
            isGroggy = false;
            groggyTurnsLeft = 0;

            // groggy 종료 시 shield 풀복구
            shield = Mathf.Max(0, def != null ? def.maxShield : shield);

            Debug.Log($"[Boss] GROGGY EXIT => shield restored {shield}/{(def != null ? def.maxShield : shield)}");
            OnBossShieldChanged?.Invoke(shield, def != null ? def.maxShield : shield);
            OnBossGroggyChanged?.Invoke(isGroggy, groggyTurnsLeft);

            // ✅ 다음 보스턴을 위해 텔레그래프를 즉시 준비
            PrepareNextTelegraph();
        }

        /// <summary>
        /// ✅ 플레이어 턴 시작 시 호출해줘야 "그로기 1턴"이 정확히 굴러감.
        /// CombatUIController가 StartPlayerTurn 직후 BossController.Instance.NotifyPlayerTurnStarted()를 1회 호출하면 됨.
        /// </summary>
        public void NotifyPlayerTurnStarted()
        {
            if (!IsAlive) return;
            if (!isGroggy) return;

            groggyTurnsLeft = Mathf.Max(0, groggyTurnsLeft - 1);
            Debug.Log($"[Boss] GROGGY tick => left={groggyTurnsLeft}");
            OnBossGroggyChanged?.Invoke(isGroggy, groggyTurnsLeft);

            if (groggyTurnsLeft <= 0)
                ExitGroggyAndRestoreShield();
        }

        // ------------------------------------------------------------
        // Damage APIs (called from CombatUIController)
        // ------------------------------------------------------------

        public void ApplyDamage(int dmg)
        {
            dmg = Mathf.Max(0, dmg);

            // ✅ 그로기 중 피해 배수
            if (isGroggy && def != null)
            {
                float mul = Mathf.Max(1f, def.groggyDamageMul);
                dmg = Mathf.RoundToInt(dmg * mul);
            }

            hp = Mathf.Max(0, hp - dmg);

            Debug.Log($"[Boss] Damage {dmg} => hp={hp}/{def.maxHp} (groggy={isGroggy})");

            OnBossHpChanged?.Invoke(hp, def.maxHp);

            if (hp <= 0)
            {
                Debug.Log("[Boss] DEAD");
                // 기존 사망 로직 유지
            }
        }

        public void ApplyShieldInterfere(int interfere)
        {
            if (!IsAlive) return;

            // 정책: 그로기 중에는 shield는 의미 없으니 더 안 깎음
            if (isGroggy) return;

            if (def == null) return;

            interfere = Mathf.Max(0, interfere);
            if (interfere <= 0) return;

            if (shield <= 0) return;

            shield = Mathf.Max(0, shield - interfere);

            Debug.Log($"[Boss] Interfere {interfere} => shield={shield}/{def.maxShield}");

            OnBossShieldChanged?.Invoke(shield, def.maxShield);

            if (shield <= 0)
                EnterGroggy();
        }
        // ------------------------------------------------------------
        // Player-hit helpers (centralize damage math so UI doesn't diverge)
        // ------------------------------------------------------------

        /// <summary>
        /// Applies a melee hit using the current canonical rules:
        /// - HP dmg: melee.baseDamage (min 1) * (GS heavy ? 1.2 : 1.0)
        /// - Shield interfere: melee.interfereDamage (min 0) * (Axe chain ramp)
        /// NOTE: AoE preview does not imply multi-target here (boss-only apply).
        /// </summary>
        public void ApplyMeleeHit(MeleeWeaponDefSO melee, bool gsHeavy, ref int axeChainCount)
        {
            if (!IsAlive) return;
            if (melee == null) return;

            int baseDmg = Mathf.Max(1, melee.baseDamage);
            float dmgMul = gsHeavy ? 1.2f : 1f;
            int finalHpDmg = Mathf.RoundToInt(baseDmg * dmgMul);

            int interfereBase = Mathf.Max(0, melee.interfereDamage);
            float interfereMul = 1f;

            if (melee.weaponId == "Axe_001")
            {
                // chainCount: 0 -> 1.0, 1 -> 1.15, 2 -> 1.30 ...
                interfereMul *= (1f + 0.15f * axeChainCount);
                axeChainCount = Mathf.Clamp(axeChainCount + 1, 0, 99);
            }
            else
            {
                axeChainCount = 0;
            }

            int finalInterfere = Mathf.RoundToInt(interfereBase * interfereMul);

            ApplyDamage(finalHpDmg);
            if (finalInterfere > 0) ApplyShieldInterfere(finalInterfere);
        }

        /// <summary>
        /// Applies a bullet hit (boss-only) using already-resolved numbers from UI/rules.
        /// Keeps groggy mul inside ApplyDamage and shield policy inside ApplyShieldInterfere.
        /// </summary>
        public void ApplyBulletHit(int hpDmg, int shieldDmg)
        {
            if (!IsAlive) return;

            if (hpDmg > 0) ApplyDamage(hpDmg);
            if (shieldDmg > 0) ApplyShieldInterfere(shieldDmg);
        }


        // ------------------------------------------------------------
        // Player Damage (HP/Shield) per Pattern
        // ------------------------------------------------------------
        private void GetPlayerDamageForPattern(string patternKey, out int hpDmg, out int shieldDmg)
        {
            hpDmg = def != null ? Mathf.Max(0, def.damagePerHit) : 0;
            shieldDmg = def != null ? Mathf.Max(0, def.playerShieldDamagePerHit) : 0;

            if (def == null || def.patternDamageOverrides == null) return;

            for (int i = 0; i < def.patternDamageOverrides.Count; i++)
            {
                var ov = def.patternDamageOverrides[i];
                if (ov == null) continue;
                if (!string.Equals(ov.patternKey, patternKey, System.StringComparison.OrdinalIgnoreCase)) continue;

                hpDmg = ov.hpDamage;
                shieldDmg = ov.shieldDamage;
                return;
            }
        }


        // ------------------------------------------------------------
        // Telegraph / Turn
        // ------------------------------------------------------------

        public List<TelegraphIntent> GetTelegraphsSnapshot()
        {
            var list = new List<TelegraphIntent>();
            if (!_hasPrepared || _prepared.Count == 0) return list;

            for (int i = 0; i < _prepared.Count; i++)
            {
                var src = _prepared[i];
                list.Add(new TelegraphIntent(src.id, src.title, new HashSet<HexCoord>(src.tiles))
                {
                    visible = src.visible
                });
            }
            return list;
        }

        public IEnumerator CoDoBossTurn(HexCoord playerCoord)
        {
            // ✅ 그로기 동안에는 보스턴 스킵(패턴 실행/피해 없음)
            if (isGroggy)
            {
                Debug.Log($"[BossTurn] GROGGY... skip. turnsLeft={groggyTurnsLeft}");
                _telegraphVersion++;
                yield return null;
                yield break;
            }

            if (_hasPrepared)
                yield return CoExecutePrepared(playerCoord);
            else
                Debug.Log("[BossTurn] No prepared telegraph. (first turn?)");

            PrepareNextTelegraph();
            yield return null;
        }

        private IEnumerator CoExecutePrepared(HexCoord playerCoord)
        {
            float teleMin = Mathf.Max(0f, def.telegraphMinShowSeconds);
            if (teleMin > 0f)
                yield return new WaitForSecondsRealtime(teleMin);

            for (int i = 0; i < _prepared.Count; i++)
            {
                var intent = _prepared[i];
                if (intent != null && intent.tiles != null)
                    yield return CoExecuteHighlight(intent.tiles);

                bool hit = intent.tiles.Contains(playerCoord);
                Debug.Log(hit
                    ? $"[BossTurn] EXECUTE '{intent.title}' HIT player at {playerCoord.q},{playerCoord.r}"
                    : $"[BossTurn] EXECUTE '{intent.title}' MISS");

                // MVP: hit 시 플레이어 HP/쉴드를 실제로 적용한다
                if (hit)
                {
                    int hpDmg, shieldDmg;
                    GetPlayerDamageForPattern(intent.title, out hpDmg, out shieldDmg);

                    var pc = Rule.Combat.Player.PlayerController.Instance;
                    if (pc != null)
                    {
                        // HP 데미지 + 체간(쉴드) 데미지를 "한 번"에 처리한다.
                        // PlayerController.ApplyBossDamage 내부에서:
                        //  - HP 데미지는 Guard/Shield로 먼저 흡수
                        //  - HP가 실제로 깎였으면 체간 데미지 2.0x
                        //  - HP가 안 깎이고 Guard가 사용되었으면 체간 데미지 1.5x
                        pc.ApplyBossDamage(hpDmg, shieldDmg, intent.title);
                    }

                    Debug.Log($"[Boss] ToPlayer dmg hp={hpDmg} shieldBase={shieldDmg} (pattern={intent.title})");
                }

                float gap = Mathf.Max(0f, def.betweenPatternSeconds);
                if (gap > 0f)
                    yield return new WaitForSecondsRealtime(gap);
            }

            _prepared.Clear();
            _hasPrepared = false;

            _telegraphVersion++;
        }

        private IEnumerator CoExecuteHighlight(HashSet<HexCoord> tiles)
        {
            if (_overlay == null) yield break;

            if (def.executeUseBlink)
            {
                int n = Mathf.Clamp(def.executeBlinkCount, 1, 10);
                float onT = Mathf.Max(0.01f, def.executeBlinkOnSeconds);
                float offT = Mathf.Max(0.01f, def.executeBlinkOffSeconds);

                for (int i = 0; i < n; i++)
                {
                    _overlay.ApplyExecute(tiles);
                    yield return new WaitForSecondsRealtime(onT);

                    _overlay.ClearExecuteAll();
                    yield return new WaitForSecondsRealtime(offT);
                }
            }
            else
            {
                float t = Mathf.Max(0.01f, def.executeFlashSeconds);
                _overlay.ApplyExecute(tiles);
                yield return new WaitForSecondsRealtime(t);
                _overlay.ClearExecuteAll();
            }
        }

        private void PrepareNextTelegraph()
        {
            _prepared.Clear();

            var keys = GetNextPatternKeys(patternsPerTurn);
            if (keys.Count == 0)
            {
                keys.Add("RingAOE");
                keys.Add("LaserLine");
            }

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var tilesRaw = BossPatterns.BuildTelegraphTiles(key, _coord);

                var tiles = new HashSet<HexCoord>();
                foreach (var c in tilesRaw)
                    if (_grid.Tiles.ContainsKey(c))
                        tiles.Add(c);

                bool hasTelegraph = !(key.StartsWith("Instant_"));

                _prepared.Add(new TelegraphIntent($"T{i + 1}", key, tiles)
                {
                    visible = hasTelegraph
                });
            }

            _hasPrepared = true;
            _telegraphVersion++;

            Debug.Log($"[BossTurn] PREPARE intents={_prepared.Count}, version={_telegraphVersion}");
        }

        private List<string> GetNextPatternKeys(int count)
        {
            var list = new List<string>();
            if (def == null || def.phases == null || def.phases.Count == 0) return list;

            var phase = def.phases[0];
            if (phase.patternKeys == null || phase.patternKeys.Count == 0) return list;

            int n = Mathf.Clamp(count, 1, 8);
            for (int i = 0; i < n; i++)
            {
                var key = phase.patternKeys[_patternCursor % phase.patternKeys.Count];
                _patternCursor++;
                list.Add(key);
            }
            return list;
        }
    }
}