// Path: Assets/Script/GameData/Combat/TargetShape.cs
namespace GameData.Combat
{
    // Attack/Bullet targeting shapes ("aerial" in your terms)
    public enum TargetShape
    {
        SingleHex,   // one hex
        SideFlanks,  // target hex + left/right (relative to attacker->target direction)
        Radius1,     // target hex + neighbors (ring radius 1)
        Line         // line from attacker through target direction
    }
}
