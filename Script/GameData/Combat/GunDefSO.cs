// Path: Assets/Script/GameData/Combat/GunDefSO.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Combat
{
    [CreateAssetMenu(menuName = "GameData/Combat/GunDef", fileName = "Gun_")]
    public class GunDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string gunId = "";
        public string displayName = "Gun";

        [Header("Bullet Range")]
        [Min(1)] public int rangeMin = 1; // bulletRangeMin
        [Min(1)] public int rangeMax = 4; // bulletRangeMax

        [Header("Allowed Bullets")]
        [Tooltip("If true, allowedBulletIds is ignored and all bullets are allowed.")]
        public bool allowAllBullets = true;

        [Tooltip("If allowAllBullets is false, only these bulletIds are allowed.")]
        public List<string> allowedBulletIds = new();

        [Header("Tuning")]
        [Tooltip("Global multiplier applied to bullet damage.")]
        [Min(0f)] public float bulletPowerMul = 1f;

        [Header("Kind Mul (Pierce / Impact / Break / Splash&Heat / Move)")]
        [SerializeField] private float pierceMul = 1f;
        [SerializeField] private float impactMul = 1f;
        [SerializeField] private float breakMul  = 1f;
        [SerializeField] private float elementMul = 1f; // Splash + Heat
        [SerializeField] private float moveMul   = 1f;

        public bool AllowsBullet(string bulletId)
        {
            if (string.IsNullOrWhiteSpace(bulletId)) return false;
            if (allowAllBullets) return true;
            if (allowedBulletIds == null || allowedBulletIds.Count == 0) return false;
            return allowedBulletIds.Contains(bulletId);
        }

        public float GetKindMul(BulletKind kind)
        {
            return kind switch
            {
                BulletKind.Pierce => Mathf.Max(0f, pierceMul),
                BulletKind.Impact => Mathf.Max(0f, impactMul),
                BulletKind.Break  => Mathf.Max(0f, breakMul),
                BulletKind.Splash => Mathf.Max(0f, elementMul),
                BulletKind.Heat   => Mathf.Max(0f, elementMul),
                BulletKind.Move   => Mathf.Max(0f, moveMul),
                _ => 1f
            };
        }

        // Helper for registry construction
        public void SetKindMuls(float pierce, float impact, float brk, float element, float move)
        {
            pierceMul = pierce;
            impactMul = impact;
            breakMul = brk;
            elementMul = element;
            moveMul = move;
        }

        [TextArea] public string description;
    }
}
