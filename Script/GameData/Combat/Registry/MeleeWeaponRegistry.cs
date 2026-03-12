// Path: Assets/Script/GameData/Combat/Registry/MeleeWeaponRegistry.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Combat.Registry
{
    public static class MeleeWeaponRegistry
    {
        private static bool _warmed;
        private static readonly Dictionary<string, MeleeWeaponDefSO> _byId = new();
        private const string RES_ROOT = "GameData/Combat/MeleeWeapons";

        public static void Warmup()
        {
            if (_warmed) return;
            _warmed = true;

            _byId.Clear();
            var assets = Resources.LoadAll<MeleeWeaponDefSO>(RES_ROOT);
            foreach (var a in assets)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.weaponId)) continue;
                _byId[a.weaponId] = a;
            }

            if (_byId.Count == 0)
            {
                // runtime defaults (proto)
                // NOTE: 무기별 계산식/스탯 스케일링을 반영한 최소 샘플

                // --- Rapier ---
                var rapier = ScriptableObject.CreateInstance<MeleeWeaponDefSO>();
                rapier.weaponId = "Rapier_001";
                rapier.displayName = "레이피어";
                rapier.archetype = MeleeArchetype.Rapier;
                rapier.baseDamage = 2;
                rapier.interfereDamage = 0;
                rapier.rangeMin = 1;
                // ✅ 요구사항: 레이피어/한손검/단검은 사거리 1
                rapier.rangeMax = 1;
                rapier.hitRule = HitRule.AlwaysHit;
                rapier.dexGrade = ScalingGrade.A;
                rapier.strGrade = ScalingGrade.D;
                rapier.syncGrade = ScalingGrade.C;

                rapier.basicAttack = AttackRegistry.CreateRuntime("Rapier_Basic", "찌르기", 1, TargetShape.SingleHex, "기본 공격");
                rapier.heavyAttack = AttackRegistry.CreateRuntime("Rapier_Heavy", "연속 찌르기", 2, TargetShape.SingleHex, "연속 공격 수에 따라 피해 증가");
                rapier.attacks = new List<AttackDefSO> { rapier.basicAttack, rapier.heavyAttack };
                _byId[rapier.weaponId] = rapier;

                // --- Axe ---
                var axe = ScriptableObject.CreateInstance<MeleeWeaponDefSO>();
                axe.weaponId = "Axe_001";
                axe.displayName = "도끼";
                axe.archetype = MeleeArchetype.Axe;
                axe.baseDamage = 1;
                axe.interfereDamage = 10;
                axe.rangeMin = 1;
                axe.rangeMax = 1;
                axe.hitRule = HitRule.AlwaysHit;
                axe.strGrade = ScalingGrade.A;
                axe.dexGrade = ScalingGrade.D;
                axe.syncGrade = ScalingGrade.C;

                axe.basicAttack = AttackRegistry.CreateRuntime("Axe_Basic", "횡베기", 2, TargetShape.SideFlanks, "대상 기준 양옆 타격");
                axe.heavyAttack = AttackRegistry.CreateRuntime("Axe_Heavy", "내려치기", 3, TargetShape.SingleHex, "쉴드 간섭 특화");
                axe.attacks = new List<AttackDefSO> { axe.basicAttack, axe.heavyAttack };
                _byId[axe.weaponId] = axe;

                // --- Greatsword ---
                var gs = ScriptableObject.CreateInstance<MeleeWeaponDefSO>();
                gs.weaponId = "Greatsword_001";
                gs.displayName = "양손검";
                gs.archetype = MeleeArchetype.Greatsword;
                gs.baseDamage = 4;
                gs.interfereDamage = 1;
                gs.rangeMin = 1;
                gs.rangeMax = 1;
                gs.hitRule = HitRule.AlwaysHit;
                gs.strGrade = ScalingGrade.S;
                gs.dexGrade = ScalingGrade.E;
                gs.syncGrade = ScalingGrade.C;

                // ✅ 요구사항: 양손검 약공격은 도끼처럼 3헥사(대상 기준 양옆)
                gs.basicAttack = AttackRegistry.CreateRuntime("GS_Basic", "기본 공격", 2, TargetShape.SideFlanks, "약공: 대상 기준 양옆 타격");
                gs.heavyAttack = AttackRegistry.CreateRuntime("GS_Heavy", "강공", 3, TargetShape.Radius1, "첫 공격은 강공(이후 기본 공격)");
                gs.attacks = new List<AttackDefSO> { gs.basicAttack, gs.heavyAttack };
                _byId[gs.weaponId] = gs;

                // --- Dagger (✅ 새) ---
                var dag = ScriptableObject.CreateInstance<MeleeWeaponDefSO>();
                dag.weaponId = "Dagger_001";
                dag.displayName = "대거";
                dag.archetype = MeleeArchetype.Dagger;
                dag.baseDamage = 2;
                dag.interfereDamage = 0;
                dag.rangeMin = 1;
                dag.rangeMax = 1;
                dag.hitRule = HitRule.AlwaysHit;
                dag.dexGrade = ScalingGrade.S;
                dag.strGrade = ScalingGrade.E;
                dag.syncGrade = ScalingGrade.C;

                // 공격 정의는 있어도, 대거는 코드에서 AP 소모를 항상 1로 고정 처리됨
                dag.basicAttack = AttackRegistry.CreateRuntime("DAG_Basic", "빠른 찌르기", 2, TargetShape.SingleHex, "대거: AP 소모 1로 고정");
                dag.heavyAttack = AttackRegistry.CreateRuntime("DAG_Heavy", "연속 베기", 3, TargetShape.SingleHex, "대거: AP 소모 1로 고정");
                dag.attacks = new List<AttackDefSO> { dag.basicAttack, dag.heavyAttack };
                _byId[dag.weaponId] = dag;

                // --- OneHandSword (✅ 새) ---
                var sw = ScriptableObject.CreateInstance<MeleeWeaponDefSO>();
                sw.weaponId = "Sword_001";
                sw.displayName = "한손검";
                sw.archetype = MeleeArchetype.OneHandSword;
                sw.baseDamage = 3;
                sw.interfereDamage = 3;
                sw.rangeMin = 1;
                sw.rangeMax = 1;
                sw.hitRule = HitRule.AlwaysHit;
                sw.strGrade = ScalingGrade.C;
                sw.dexGrade = ScalingGrade.C;
                sw.syncGrade = ScalingGrade.D;

                sw.basicAttack = AttackRegistry.CreateRuntime("SW_Basic", "베기", 2, TargetShape.SingleHex, "한손검: 가드 효율 1.5배");
                sw.heavyAttack = AttackRegistry.CreateRuntime("SW_Heavy", "강베기", 3, TargetShape.SingleHex, "한손검: 가드 효율 1.5배");
                sw.attacks = new List<AttackDefSO> { sw.basicAttack, sw.heavyAttack };
                _byId[sw.weaponId] = sw;

                Debug.Log("[MeleeWeaponRegistry] No assets found. Using runtime default melee weapons (5).");
            }
            else
            {
                Debug.Log($"[MeleeWeaponRegistry] Loaded MeleeWeaponDefSO count={_byId.Count}");
            }
        }

        public static MeleeWeaponDefSO Get(string weaponId)
        {
            Warmup();
            if (string.IsNullOrWhiteSpace(weaponId)) return null;
            return _byId.TryGetValue(weaponId, out var w) ? w : null;
        }

        public static MeleeWeaponDefSO GetAny()
        {
            Warmup();
            foreach (var kv in _byId) return kv.Value;
            return null;
        }
    }
}
