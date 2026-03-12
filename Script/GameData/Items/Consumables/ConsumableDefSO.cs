using UnityEngine;

namespace GameData.Items.Consumables
{
    [CreateAssetMenu(menuName = "GameData/Items/ConsumableDef")]
    public class ConsumableDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string id = "RepairKit";
        public ConsumableType type = ConsumableType.RepairKit;
        public string displayName = "Repair Kit";

        [Header("Tuning (proto)")]
        [Tooltip("RepairKit: MaxHP ratio heal (0.3 = 30%)")]
        [Range(0f, 1f)] public float hpHealRatio = 0.30f;

        [Tooltip("ShieldPatch: MaxShield ratio restore (0.5 = 50%)")]
        [Range(0f, 1f)] public float shieldRestoreRatio = 0.50f;

        [Tooltip("Thruster: free move distance this turn")]
        [Range(0, 10)] public int thrusterFreeMoveDistance = 2;

        [Tooltip("Dampener: damage multiplier for next boss hit (0.5 = half)")]
        [Range(0.1f, 1f)] public float dampenerDamageMult = 0.50f;

        [Tooltip("Dampener: number of boss hits affected")]
        [Range(1, 5)] public int dampenerHits = 1;
    }
}