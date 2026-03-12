// Path: Assets/Script/Rule/Field/HexGridConfigSO.cs
using UnityEngine;

namespace Rule.Field
{
    [CreateAssetMenu(menuName = "Game/Field/Hex Grid Config", fileName = "HexGridConfig")]
    public class HexGridConfigSO : ScriptableObject
    {
        [Header("Shape")]
        [Range(1, 12)] public int radius = 5;     // 4~7 추천 범위
        [Header("Size")]
        [Min(0.1f)] public float hexSize = 1.0f;  // 타일 크기(월드 단위)
        [Header("Appearance")]
        public float y = 0f;                      // 타일 높이(평면)
    }
}
