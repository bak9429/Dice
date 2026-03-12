// Path: Assets/Script/GameData/Combat/MeleeWeaponDefSO.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Combat
{
    public enum MeleeArchetype
    {
        Rapier,
        Axe,
        Greatsword,

        // ✅ 신규
        Dagger,
        OneHandSword,

        Other
    }

    public enum ScalingGrade
    {
        S, A, B, C, D, E
    }

    public static class ScalingGradeTable
    {
        public static float ToCoeff(ScalingGrade g)
        {
            return g switch
            {
                ScalingGrade.S => 0.05f,
                ScalingGrade.A => 0.04f,
                ScalingGrade.B => 0.03f,
                ScalingGrade.C => 0.02f,
                ScalingGrade.D => 0.01f,
                _ => 0f,
            };
        }
    }

    [CreateAssetMenu(menuName = "GameData/Combat/MeleeWeaponDef", fileName = "MeleeWeapon_")]
    public class MeleeWeaponDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string weaponId = "";
        public string displayName = "Melee";
        public MeleeArchetype archetype = MeleeArchetype.Other;

        [Header("Core Values")]
        [Tooltip("기본 피해(공격력). 근접 공격 계산식의 '기본 피해'")]
        [Min(0)] public int baseDamage = 1;

        [Tooltip("쉴드 간섭 기본값. 근접 공격 계산식의 '간섭 피해'")]
        [Min(0)] public int interfereDamage = 0;

        [Range(1, 3)] public int rangeMin = 1;
        [Range(1, 3)] public int rangeMax = 1;
        public HitRule hitRule = HitRule.AlwaysHit;

        [Header("Stat Scaling Grades (0~15)")]
        public ScalingGrade strGrade = ScalingGrade.E;
        public ScalingGrade dexGrade = ScalingGrade.E;
        public ScalingGrade syncGrade = ScalingGrade.E;

        [Header("Attacks (2 recommended)")]
        [Tooltip("기본 공격(약공). 2개로 나눌 때 자동 선택의 '기본'이 됨.")]
        public AttackDefSO basicAttack;

        [Tooltip("강공/특수 공격. Greatsword의 첫 공격처럼 자동 선택의 '강공'이 됨.")]
        public AttackDefSO heavyAttack;

        [Tooltip("참고용 리스트(에디터에서 목록 관리용). basic/heavy가 비어있으면 여기에서 fallback.")]
        public List<AttackDefSO> attacks = new();

        [TextArea] public string description;

        public float GetBonusDamagePercent(int str, int dex, int sync)
        {
            float p =
                (Mathf.Clamp(str, 0, 15) * ScalingGradeTable.ToCoeff(strGrade)) +
                (Mathf.Clamp(dex, 0, 15) * ScalingGradeTable.ToCoeff(dexGrade)) +
                (Mathf.Clamp(sync, 0, 15) * ScalingGradeTable.ToCoeff(syncGrade));
            return p; // ex: 0.12 => +12%
        }
    }
}
