// Path: Assets/Script/Rule/Dice/DiceTerm.cs
namespace Rule.Dice
{
    [System.Serializable]
    public struct DiceTerm
    {
        public int sides; // 주사위 면 수 (e.g. 6)
        public int count; // 개수 (e.g. 2 => 2d6)

        public DiceTerm(int sides, int count)
        {
            this.sides = sides;
            this.count = count;
        }

        public override string ToString()
        {
            return $"{count}d{sides}";
        }
    }
}
