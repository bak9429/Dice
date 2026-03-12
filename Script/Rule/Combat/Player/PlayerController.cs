using UnityEngine;
using Rule.Field;
using Rule.Combat;
using System;

namespace Rule.Combat.Player
{
    public partial class PlayerController : MonoBehaviour
    {
        [Header("Start")]
        public int startMaxHp = 20;

        // 프로토타입: 시작 좌표 고정(나중에 FieldDef/PlayerSpawnRule로 옮김)
        public HexCoord startCoord = new HexCoord(-5, 0);

        [Header("Refs")]
        public HexGridBuilder grid;

        [Header("Runtime")]
        public PlayerState state = new PlayerState();

        // Groggy는 PlayerController.Groggy.cs(Partial)에 분리

        private GameObject _marker;

        public static PlayerController Instance { get; private set; }

        // UI/FX hooks
        public static event Action<int, int> OnHpChanged; // (hp, maxHp)
        public static event Action<int, int> OnShieldChanged; // (shield, maxShield)
        public static event Action<int> OnDamaged;        // (damageAppliedToHp)
        public static event Action OnDied;               // fired once when hp reaches 0

        // OnGroggyChanged는 PlayerController.Groggy.cs(Partial)에 분리

        private bool _deadFired;

        private void Awake()
        {
            Instance = this;
            if (grid == null)
                grid = FindFirstObjectByType<HexGridBuilder>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            EnsureSpawned();
        }

        public void EnsureSpawned()
        {
            if (grid == null)
            {
                Debug.LogError("[PlayerController] HexGridBuilder not found.");
                return;
            }

            grid.BuildIfNeeded();

            state.ResetTo(startMaxHp, startCoord);
            _deadFired = false;

            ResetGroggyRuntime();

            if (_marker == null)
            {
                _marker = Rule.Combat.WorldMarkerFactory.CreateLetterMarker("PlayerMarker", "P", Color.cyan, 20);
            }

            MoveMarkerToCoord(state.coord);
            Debug.Log($"[Player] Spawn hp={state.hp}/{state.maxHp} at {state.coord.q},{state.coord.r}");

            OnHpChanged?.Invoke(state.hp, state.maxHp);
            OnShieldChanged?.Invoke(state.shield, state.maxShield);
            BroadcastGroggy();
        }

        /// <summary>
        /// TurnController가 PlayerTurn 시작 시 호출해, 턴 스코프(연속공격 등) 리셋을 보장한다.
        /// </summary>
        public void BeginTurn()
        {
            state.BeginTurnReset();
            ResetConsumableTurnFlags(); // ✅ NEW: 턴당 1회, thruster reset
        }

        public void SetCoord(HexCoord c)
        {
            state.coord = c;
            MoveMarkerToCoord(c);
        }

        // ✅ spend AP -> guard pool
        public void AddGuard(int amount)
        {
            state.AddGuard(amount);
            Debug.Log($"[Player] Guard +{amount} => guard={state.guard}");
        }

        public int ApplyDamage(int amount)
        {
            if (amount <= 0) return 0;

            int incoming = amount;

            // Guard만 HP 방어
            int guardAbsorb = Mathf.Min(state.guard, incoming);
            state.guard -= guardAbsorb;
            incoming -= guardAbsorb;

            // 남은 것만 HP에 적용
            int appliedHp = Mathf.Min(state.hp, incoming);
            state.hp -= appliedHp;

            if (appliedHp > 0)
                OnHpChanged?.Invoke(state.hp, state.maxHp);

            Debug.Log($"[Player] Damage {amount} (-{guardAbsorb} guard) => {appliedHp} applied, hp={state.hp}/{state.maxHp}, shield={state.shield}/{state.maxShield}, guardLeft={state.guard}");

            return appliedHp;
        }

        /// <summary>
        /// GuardModifier(0~1)를 반영한 HP 피해.
        ///  - guardModifier=0 : 기존 ApplyDamage와 동일(Guard로 최대한 흡수)
        ///  - guardModifier=1 : Guard 무시(전부 HP로)
        /// </summary>
        public int ApplyDamageWithGuardModifier(int amount, float guardModifier)
        {
            int dmg = Mathf.Max(0, amount);
            if (dmg <= 0) return 0;

            float gm = Mathf.Clamp01(guardModifier);

            // bypass는 Guard를 무시하고 HP로 바로 들어간다.
            int bypass = Mathf.RoundToInt(dmg * gm);
            int remaining = Mathf.Max(0, dmg - bypass);

            // remaining만 Guard로 흡수
            int guardAbsorb = Mathf.Min(state.guard, remaining);
            state.guard -= guardAbsorb;
            remaining -= guardAbsorb;

            int appliedHp = Mathf.Min(state.hp, remaining + bypass);
            state.hp -= appliedHp;

            if (appliedHp > 0)
                OnHpChanged?.Invoke(state.hp, state.maxHp);

            Debug.Log($"[Player] Damage(GM) {dmg} (gm={gm:0.00}) => bypass={bypass}, guardAbsorb={guardAbsorb}, hpApplied={appliedHp} hp={state.hp}/{state.maxHp} guardLeft={state.guard}");
            return appliedHp;
        }

        /// <summary>
        /// Heat 등 "HP-로 구현" 용: Guard 무시하고 HP를 직접 깎는다.
        /// </summary>
        public int ApplyTrueDamage(int amount)
        {
            int dmg = Mathf.Max(0, amount);
            if (dmg <= 0) return 0;

            int applied = Mathf.Min(state.hp, dmg);
            state.hp -= applied;

            if (applied > 0)
                OnHpChanged?.Invoke(state.hp, state.maxHp);

            Debug.Log($"[Player] TrueDamage {dmg} => {applied} applied, hp={state.hp}/{state.maxHp}");
            return applied;
        }

        /// <summary>HP와 무관하게 쉴드(체간)에 직접 피해를 준다 (Boss -> Player용).</summary>
        public void ApplyShieldDamage(int dmg)
        {
            int d = Mathf.Max(0, dmg);
            if (d <= 0) return;

            int before = state.shield;
            state.shield = Mathf.Max(0, state.shield - d);

            if (before != state.shield)
                OnShieldChanged?.Invoke(state.shield, state.maxShield);

            // ✅ shield가 0이 되면 그로기 진입
            if (before > 0 && state.shield <= 0)
                EnterGroggy();

            Debug.Log($"[Player] ShieldDamage {d} => shield={state.shield}/{state.maxShield}");
        }

        /// <summary>
        /// Boss hit: apply HP damage (guard/shield absorbed) and then apply extra shield(체간) damage.
        /// scaling rule:
        ///  - if HP damage applied == 0 : shieldDamage *= 1.5
        ///  - if HP damage applied  > 0 : shieldDamage *= 2.0
        /// </summary>
        public void ApplyBossDamage(int hpDamage, int shieldDamageBase, string patternKey)
        {
            int beforeGuard = state.guard;
            int shieldDamageTmp = shieldDamageBase;
            TryApplyDamageReduction(ref hpDamage, ref shieldDamageTmp);
            shieldDamageBase = shieldDamageTmp;

            int appliedHp = ApplyDamage(hpDamage); // 이제 Guard만 쓰고 HP만 깎음 (체간 건드리지 않음)

            int baseDmg = Mathf.Max(0, shieldDamageBase);
            if (baseDmg <= 0) return;

            bool usedGuard = state.guard < beforeGuard;
            bool hpReduced = appliedHp > 0;

            float mult = 1.0f;
            if (hpReduced) mult = 2.0f;
            else if (usedGuard) mult = 1.5f;

            int shieldDmg = Mathf.RoundToInt(baseDmg * mult);
            if (shieldDmg <= 0) return;

            // 체간(=shield) 데미지 적용
            int before = state.shield;
            state.shield = Mathf.Max(0, state.shield - shieldDmg);

            if (before != state.shield)
                OnShieldChanged?.Invoke(state.shield, state.maxShield);

            // ✅ shield가 0이 되면 그로기 진입
            if (before > 0 && state.shield <= 0)
                EnterGroggy();

            Debug.Log($"[Player] BossshieldDmg base={baseDmg} x{mult:0.0} => -{shieldDmg} (pattern={patternKey}) shield={state.shield}/{state.maxShield}");
        }

        private void MoveMarkerToCoord(HexCoord c)
        {
            Vector3 pos = HexMath.AxialToWorld(c, grid.config.hexSize, grid.config.y);
            if (_marker != null) _marker.transform.position = pos;
        }
    }
}