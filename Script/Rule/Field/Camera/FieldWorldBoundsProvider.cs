using UnityEngine;
using Rule.Field;

namespace Rule.Field.CameraTools
{
    /// <summary>
    /// HexGridBuilder의 타일들을 기준으로 "필드 월드 바운즈"를 계산한다.
    /// (헥스 스프라이트/스케일이 반영된 Renderer bounds 사용)
    /// </summary>
    public class FieldWorldBoundsProvider : MonoBehaviour
    {
        public HexGridBuilder grid;

        [Tooltip("바운즈 여유(월드 단위). 너무 딱 맞으면 가장자리 헥스가 잘릴 수 있음")]
        public float paddingWorld = 0.25f;

        private bool _hasBounds;
        private Bounds _bounds;

        public bool HasBounds => _hasBounds;
        public Bounds Bounds => _bounds;

        private void Awake()
        {
            if (grid == null)
                grid = FindFirstObjectByType<HexGridBuilder>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _hasBounds = false;
            if (grid == null || grid.Tiles == null || grid.Tiles.Count == 0)
            {
                // HexGridBuilder가 아직 Build 전일 수 있음
                return;
            }

            bool first = true;
            Bounds b = default;

            foreach (var kv in grid.Tiles)
            {
                var tile = kv.Value;
                if (tile == null || tile.sr == null) continue;

                var rb = tile.sr.bounds; // 스케일 반영됨
                if (first)
                {
                    b = rb;
                    first = false;
                }
                else
                {
                    b.Encapsulate(rb);
                }
            }

            if (first) return;

            // padding 적용
            b.Expand(new Vector3(paddingWorld * 2f, paddingWorld * 2f, 0f));
            _bounds = b;
            _hasBounds = true;
        }

        private void Update()
        {
            // 런타임에 필드가 재생성되거나 타일이 늦게 생기는 케이스 대비: 늦게라도 한번 잡음
            if (!_hasBounds && grid != null && grid.Tiles != null && grid.Tiles.Count > 0)
                Rebuild();
        }
    }
}
