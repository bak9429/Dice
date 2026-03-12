// Path: Assets/Script/GameData/Combat/AttackDefSO.cs
using UnityEngine;

namespace GameData.Combat
{
    [CreateAssetMenu(menuName = "GameData/Combat/AttackDef", fileName = "AttackDef_")]
    public class AttackDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string attackId = "";
        public string displayName = "Attack";

        [Header("Rules")]
        [Min(0)] public int apCost = 1; // weapon-dependent AP lives here
        public TargetShape shape = TargetShape.SingleHex;

        [TextArea] public string description;
    }
}
