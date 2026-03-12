// Path: Assets/Script/GameData/Combat/BulletKind.cs
namespace GameData.Combat
{
    // ✅ Bullet kinds (runtime default spec, 6 kinds)
    public enum BulletKind
    {
        Pierce,   // 관통
        Impact,   // 충격
        Break,    // 파쇄(체간 특화)
        Splash,   // 범위(AOE)
        Heat,     // 과부하(고위험)
        Move,     // 이동
    }
}
