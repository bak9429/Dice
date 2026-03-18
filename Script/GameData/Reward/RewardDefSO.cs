// Path: Assets/Script/GameData/Reward/RewardDefSO.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Reward
{
    [CreateAssetMenu(menuName = "GameData/Reward/RewardDef")]
    public class RewardDefSO : ScriptableObject
    {
        public string rewardId = "";

        [Header("Low Currency (1~2)")]
        public int lowMin = 5;
        public int lowMax = 8;

        [Header("Mid Currency (3~4)")]
        public int midMin = 10;
        public int midMax = 14;

        [Header("High Currency (5)")]
        public int highMin = 18;
        public int highMax = 24;

        [Header("Equipment Pools (6)")]
        public List<string> meleePool = new List<string>();
        public List<string> gunPool = new List<string>();
    }
}