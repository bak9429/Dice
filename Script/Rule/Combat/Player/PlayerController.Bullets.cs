// Path: Assets/Script/Rule/Combat/Player/PlayerController.Bullets.cs
using System.Collections.Generic;
using UnityEngine;
using GameData.Combat;
using GameData.Combat.Registry;

namespace Rule.Combat.Player
{
    public partial class PlayerController
    {
        // --- Bullet ammo (per bulletId) ---

        // 관문 보스 클리어 수: maxAmmoBase에 +ammoGrowthPerGate씩
        [SerializeField] private int gateBossClears = 0;
        public int GateBossClears => gateBossClears;

        // Runtime ammo: bulletId -> current
        private readonly Dictionary<string, int> _bulletAmmoById = new();

        private static BulletDefSO GetDef(string bulletId)
        {
            if (string.IsNullOrWhiteSpace(bulletId)) return null;
            BulletRegistry.Warmup();
            return BulletRegistry.Get(bulletId.Trim());
        }

        private int MaxAmmo(string bulletId)
        {
            var def = GetDef(bulletId);
            if (def == null) return 0;
            int baseMax = Mathf.Max(0, def.maxAmmoBase);
            int growth = Mathf.Max(0, def.ammoGrowthPerGate);
            return Mathf.Max(0, baseMax + growth * Mathf.Max(0, gateBossClears));
        }

        private void EnsureEntry(string bulletId)
        {
            bulletId = bulletId?.Trim();
            if (string.IsNullOrWhiteSpace(bulletId)) return;
            if (_bulletAmmoById.ContainsKey(bulletId)) return;

            _bulletAmmoById[bulletId] = MaxAmmo(bulletId); // 기본은 최대치로 시작
        }

        public int GetBulletMaxAmmo(string bulletId)
        {
            bulletId = bulletId?.Trim();
            if (string.IsNullOrWhiteSpace(bulletId)) return 0;
            return MaxAmmo(bulletId);
        }

        public int GetBulletAmmo(string bulletId)
        {
            bulletId = bulletId?.Trim();
            if (string.IsNullOrWhiteSpace(bulletId)) return 0;
            EnsureEntry(bulletId);
            return _bulletAmmoById.TryGetValue(bulletId, out var v) ? v : 0;
        }

        public bool TryConsumeBulletAmmo(string bulletId, int amount = 1)
        {
            bulletId = bulletId?.Trim();
            if (amount <= 0) return true;
            if (string.IsNullOrWhiteSpace(bulletId)) return false;

            EnsureEntry(bulletId);

            int cur = _bulletAmmoById[bulletId];
            if (cur < amount) return false;
            _bulletAmmoById[bulletId] = Mathf.Max(0, cur - amount);
            return true;
        }

        public void AddBulletAmmo(string bulletId, int amount)
        {
            bulletId = bulletId?.Trim();
            if (amount == 0) return;
            if (string.IsNullOrWhiteSpace(bulletId)) return;

            EnsureEntry(bulletId);

            int max = MaxAmmo(bulletId);
            _bulletAmmoById[bulletId] = Mathf.Clamp(_bulletAmmoById[bulletId] + amount, 0, max);
        }

        public void SetGateBossClears(int clears)
        {
            gateBossClears = Mathf.Max(0, clears);

            // 기존 탄수는 새 max로 clamp
            var keys = new List<string>(_bulletAmmoById.Keys);
            foreach (var id in keys)
            {
                int max = MaxAmmo(id);
                _bulletAmmoById[id] = Mathf.Clamp(_bulletAmmoById[id], 0, max);
            }
        }
    }
}
