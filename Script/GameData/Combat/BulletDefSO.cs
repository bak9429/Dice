// Path: Assets/Script/GameData/Combat/BulletDefSO.cs
using UnityEngine;

namespace GameData.Combat
{
    /// <summary>
    /// Bullet = 별도 액션(근접 옵션 아님). 스펙은 "상태이상"이 아니라 아래 5축으로 고정:
    ///  - BaseDamage: HP 피해
    ///  - ShieldDamage: 체간(=Boss Shield / Player Shield) 감소량
    ///  - GuardModifier: 방어 관통(0~1). AOE로 플레이어가 맞을 때 Guard 일부 무시 등에 사용.
    ///  - AOE: aoeRadius (0=단일, 1=반경1)
    ///  - Heat: 과부하(자기 HP-로 구현)
    ///
    /// Move 탄은 공격이 아니라 이동 규칙만 의미가 있으며(현재: backstep), 위 수치는 0으로 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "GameData/Combat/BulletDefSO")]
    public class BulletDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string bulletId = "Pierce";
        public string displayName = "관통";
        public BulletKind kind = BulletKind.Pierce;

        [Header("Damage")]
        [Min(0)] public int baseDamage = 0;      // HP
        [Min(0)] public int shieldDamage = 0;    // 체간(쉴드)
        [Range(0f, 1f)] public float guardModifier = 0f; // 방어 관통률

        [Header("AOE")]
        [Tooltip("0=single, 1=radius1 (player도 범위 내면 피격)")]
        [Min(0)] public int aoeRadius = 0;

        [Header("Heat")]
        [Tooltip("과부하: 사용 시 플레이어에게 HP- 적용(가드 무시)")]
        [Min(0)] public int heat = 0;

        [Header("Movement (Move bullet only)")]
        [Min(0)] public int moveDistance = 0; // 현재 1 고정(backstep)

        [Header("Ammo (per bulletId)")]
        [Min(0)] public int maxAmmoBase = 2;
        [Min(0)] public int ammoGrowthPerGate = 1;
    }
}
