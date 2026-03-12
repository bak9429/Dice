using UnityEngine;

namespace Rule.Combat
{
    /// <summary>
    /// World markers rendered in the Field area.
    ///
    /// NOTE:
    /// - The project is using a 2D-centric render setup (SpriteRenderer-friendly).
    /// - Mesh/TextMesh markers can fail to show depending on renderer/camera pipeline.
    ///
    /// So we generate a tiny Sprite at runtime (disk + letter) and draw it with SpriteRenderer.
    /// This guarantees visibility for both Player(P) and Boss(B) without needing external assets.
    /// </summary>
    public static class WorldMarkerFactory
    {
        private const int TEX = 64;

        // 5x7 bitmap glyphs (only what we need right now)
        // '1' means filled pixel.
        private static readonly string[] GLYPH_P =
        {
            "11110",
            "10001",
            "10001",
            "11110",
            "10000",
            "10000",
            "10000",
        };

        private static readonly string[] GLYPH_B =
        {
            "11110",
            "10001",
            "10001",
            "11110",
            "10001",
            "10001",
            "11110",
        };

        public static GameObject CreateLetterMarker(string name, string letter, Color color, int sortingOrder)
        {
            var root = new GameObject(name);
            root.transform.localScale = Vector3.one;

            // Put the whole marker on Field layer if present.
            int fieldLayer = LayerMask.NameToLayer(Rule.Field.HexGridBuilder.FieldLayers.Field);
            if (fieldLayer >= 0)
                SetLayerRecursive(root, fieldLayer);

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            var sprite = BuildMarkerSprite(letter, color);
            sr.sprite = sprite;

            // Slightly above tile plane (avoid Z-fighting with tile sprites/overlays)
            root.transform.position += new Vector3(0, 0, -0.15f);

            return root;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
                SetLayerRecursive(t.gameObject, layer);
        }

        private static Sprite BuildMarkerSprite(string letter, Color baseColor)
        {
            // Disk: baseColor with alpha, Letter: white.
            var tex = new Texture2D(TEX, TEX, TextureFormat.RGBA32, mipChain: false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var clear = new Color(0, 0, 0, 0);
            var disk = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
            var rim = new Color(baseColor.r, baseColor.g, baseColor.b, 0.65f);
            var white = new Color(1, 1, 1, 0.95f);

            int cx = TEX / 2;
            int cy = TEX / 2;
            float r = TEX * 0.40f;
            float r2 = r * r;
            float rimR = TEX * 0.42f;
            float rimR2 = rimR * rimR;

            for (int y = 0; y < TEX; y++)
            {
                for (int x = 0; x < TEX; x++)
                {
                    float dx = x - cx + 0.5f;
                    float dy = y - cy + 0.5f;
                    float d2 = dx * dx + dy * dy;

                    if (d2 <= r2) tex.SetPixel(x, y, disk);
                    else if (d2 <= rimR2) tex.SetPixel(x, y, rim);
                    else tex.SetPixel(x, y, clear);
                }
            }

            // Letter glyph
            var glyph = letter == "B" ? GLYPH_B : GLYPH_P;
            DrawGlyph(tex, glyph, white);

            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return Sprite.Create(tex, new Rect(0, 0, TEX, TEX), new Vector2(0.5f, 0.5f), pixelsPerUnit: 64);
        }

        private static void DrawGlyph(Texture2D tex, string[] glyph, Color c)
        {
            // Center glyph in texture
            int gw = glyph[0].Length;
            int gh = glyph.Length;

            // Scale up each glyph pixel to a block
            int block = 6; // 5*6=30 px wide, 7*6=42 px high
            int totalW = gw * block;
            int totalH = gh * block;
            int x0 = (TEX - totalW) / 2;
            int y0 = (TEX - totalH) / 2;

            for (int gy = 0; gy < gh; gy++)
            {
                for (int gx = 0; gx < gw; gx++)
                {
                    if (glyph[gy][gx] != '1') continue;
                    for (int by = 0; by < block; by++)
                    {
                        for (int bx = 0; bx < block; bx++)
                        {
                            int px = x0 + gx * block + bx;
                            int py = y0 + (gh - 1 - gy) * block + by;
                            if (px >= 0 && px < TEX && py >= 0 && py < TEX)
                                tex.SetPixel(px, py, c);
                        }
                    }
                }
            }
        }
    }
}
