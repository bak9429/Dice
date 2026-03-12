// Path: Assets/Script/Rule/Field/HexSpriteFactory.cs
using UnityEngine;

namespace Rule.Field
{
    public static class HexSpriteFactory
    {
        private static Sprite _sprite;

        // strokePx: 외곽선 두께(픽셀)
        // insetPx: 가장자리 번짐/틈 방지용으로 살짝 키우는 값(픽셀)
        public static Sprite GetOrCreateHexSprite(int texSize = 256, int strokePx = 3, int insetPx = 1)
        {
            if (_sprite != null) return _sprite;

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            // ✅ 핵심: 번짐/틈 방지
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            var clear = new Color(0, 0, 0, 0);
            var pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float cx = (texSize - 1) * 0.5f;
            float cy = (texSize - 1) * 0.5f;

            // 정육각형 반지름(픽셀) — 클리핑 방지로 약간 여유
            float R = (texSize - 1) * 0.5f - 2f;

            // pointy-top regular hex vertices (pixel space)
            Vector2 center = new Vector2(cx, cy);
            Vector2[] v = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float ang = Mathf.Deg2Rad * (90f - 60f * i);
                v[i] = center + new Vector2(Mathf.Cos(ang) * R, Mathf.Sin(ang) * R);
            }

            // Fill 색은 white(나중에 SpriteRenderer.color로 틴트)
            // Outline도 white(틴트에 같이 걸리지만, 알파로 구분해도 됨)
            Color fill = new Color(1, 1, 1, 1);
            Color stroke = new Color(1, 1, 1, 1);

            // 1) Fill: triangle fan으로 채움
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                    bool inside = false;
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 a = center;
                        Vector2 b = v[i];
                        Vector2 c = v[(i + 1) % 6];
                        if (PointInTriangle(p, a, b, c))
                        {
                            inside = true;
                            break;
                        }
                    }

                    if (inside)
                        pixels[y * texSize + x] = fill;
                }
            }

            // 2) Stroke: 각 edge에 대해 "픽셀 중심이 선분에 가까우면" 칠함
            //    insetPx로 살짝 바깥으로 밀어 타일 사이 틈을 줄임
            float strokeDist = Mathf.Max(1, strokePx) + insetPx;

            for (int i = 0; i < 6; i++)
            {
                Vector2 a = v[i];
                Vector2 b = v[(i + 1) % 6];

                // edge 주변만 스캔하도록 bbox
                int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x) - strokeDist - 2), 0, texSize - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x) + strokeDist + 2), 0, texSize - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y) - strokeDist - 2), 0, texSize - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y) + strokeDist + 2), 0, texSize - 1);

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                        // fill 내부 픽셀만 stroke 처리(외부에 테두리 번짐 방지)
                        if (pixels[y * texSize + x].a <= 0.5f) continue;

                        float d = DistancePointToSegment(p, a, b);
                        if (d <= strokeDist)
                        {
                            pixels[y * texSize + x] = stroke;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            // ✅ ppu=texSize/2 => bounds 약 2x2 (스케일 계산 예측 가능)
            float ppu = texSize * 0.5f;
            _sprite = Sprite.Create(
                tex,
                new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: ppu
            );
            _sprite.name = "HexSprite_Runtime";
            return _sprite;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 v0 = c - a;
            Vector2 v1 = b - a;
            Vector2 v2 = p - a;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float denom = dot00 * dot11 - dot01 * dot01;
            if (Mathf.Abs(denom) < 1e-6f) return false;

            float inv = 1f / denom;
            float u = (dot11 * dot02 - dot01 * dot12) * inv;
            float v = (dot00 * dot12 - dot01 * dot02) * inv;

            return u >= 0f && v >= 0f && (u + v) <= 1f;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, Vector2.Dot(ab, ab));
            t = Mathf.Clamp01(t);
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }
    }
}
