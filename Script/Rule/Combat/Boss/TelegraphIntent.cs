using System.Collections.Generic;
using Rule.Field;

namespace Rule.Combat.Boss
{
    // “예고 1개”를 의미하는 데이터
    public class TelegraphIntent
    {
        public string id;                // "T1" 같은 식별자
        public string title;             // UI 표시용 (예: "RingAOE")
        public HashSet<HexCoord> tiles;  // 예고 타일
        public bool visible = true;

        public TelegraphIntent(string id, string title, HashSet<HexCoord> tiles)
        {
            this.id = id;
            this.title = title;
            this.tiles = tiles ?? new HashSet<HexCoord>();
        }
    }
}
