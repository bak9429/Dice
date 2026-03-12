// Path: Assets/Script/GameData/Boss/BossAIArchetype.cs
namespace GameData.Boss
{
    public enum BossAIArchetype
    {
        None = 0,
        Evasive,   // 회피형
        Charge,    // 돌진형
        Hybrid,    // 복합형
        Turret,    // 고정 포격형(이동 거의 없음)
        Summoner,  // 소환형
    }
}
