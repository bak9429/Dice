// Path: Assets/Script/Rule/Field/Hex/HexGridBuilder.cs
using System.Collections.Generic;
using UnityEngine;

namespace Rule.Field
{
    [System.Flags]
    public enum HexOverlayChannel
    {
        None          = 0,
        Move          = 1 << 0,
        Danger        = 1 << 1,
        Telegraph     = 1 << 2,
        Selected      = 1 << 3,
        Execute       = 1 << 4,   // ✅ 실행 하이라이트(최우선)
        AttackRange   = 1 << 5,   // ✅ 공격/타겟 후보(빨강)
        AttackPreview = 1 << 6,   // ✅ 공격 범위 미리보기(시안)
        GunRange      = 1 << 7,   // ✅ 총 사거리(파랑)
    }

    public class HexGridBuilder : MonoBehaviour
    {
        public static class FieldLayers
        {
            public const string Field = "Field";
        }

        public HexGridConfigSO config;

        [Header("Generated")]
        public Transform tilesRoot;

        [Header("Appearance")]
        public Color tileColor = new Color(0.2f, 0.9f, 0.4f, 0.35f);
        public Color selectedColor = new Color(1f, 1f, 1f, 0.60f);

        [Header("Overlay Colors (alpha recommended)")]
        public Color moveOverlayColor = new Color(0.2f, 0.55f, 1.0f, 0.18f);
        public Color dangerOverlayColor = new Color(1.0f, 0.2f, 0.2f, 0.32f);
        public Color telegraphOverlayColor = new Color(1.0f, 0.85f, 0.2f, 0.36f);
        public Color executeOverlayColor = new Color(1.0f, 0.12f, 0.12f, 0.55f); // ✅ 더 “때린다” 느낌

        [Header("Overlay Colors (Attack)")]
        public Color attackRangeOverlayColor = new Color(1.0f, 0.12f, 0.12f, 0.55f);   // 타겟 후보(빨강)
        // ✅ 선택 프리뷰(공격/불릿/AOE) 색상: 초록
        public Color attackPreviewOverlayColor = new Color(0.2f, 1.0f, 0.2f, 0.28f);    // preview (green)
        public Color gunRangeOverlayColor = new Color(0.2f, 0.55f, 1.0f, 0.18f);        // 총 사거리(파랑)

        [Header("Shape Tuning (Visual Only)")]
        [Range(0.90f, 1.20f)] public float widthMul = 1.06f;
        [Range(0.90f, 1.20f)] public float heightMul = 1.00f;

        [Header("Layer (Auto)")]
        [Tooltip("생성되는 필드/타일들을 이 레이어로 자동 지정합니다. (Project Settings > Tags and Layers에 생성 필요)")]
        public string fieldLayerName = FieldLayers.Field;

        private readonly Dictionary<HexCoord, SpriteTile> _tiles = new();
        public IReadOnlyDictionary<HexCoord, SpriteTile> Tiles => _tiles;

        private Sprite _hexSprite;
        private int _fieldLayer = -1;
        private bool _warnedMissingLayer = false;

        private void Awake()
        {
            ResolveFieldLayer();

            if (tilesRoot == null)
            {
                var go = new GameObject("Tiles");
                go.transform.SetParent(transform, false);
                tilesRoot = go.transform;
            }

            if (_fieldLayer >= 0)
                tilesRoot.gameObject.layer = _fieldLayer;
        }

        private void Start()
        {
            BuildIfNeeded();
            ApplyPaletteToTiles();
        }

        private void ResolveFieldLayer()
        {
            if (string.IsNullOrEmpty(fieldLayerName))
                fieldLayerName = FieldLayers.Field;

            _fieldLayer = LayerMask.NameToLayer(fieldLayerName);

            if (_fieldLayer < 0 && !_warnedMissingLayer)
            {
                _warnedMissingLayer = true;
                Debug.LogError($"[HexGridBuilder] Layer '{fieldLayerName}' not found. " +
                               $"Create it in Project Settings > Tags and Layers. (Fallback: Default)");
            }
        }

        private int GetEffectiveLayer() => _fieldLayer >= 0 ? _fieldLayer : 0;

        public void BuildIfNeeded()
        {
            if (config == null)
            {
                Debug.LogError("[HexGridBuilder] config is null. Assign HexGridConfigSO.");
                return;
            }
            if (_tiles.Count > 0) return;

            _hexSprite = HexSpriteFactory.GetOrCreateHexSprite();
            Build(config.radius, config.hexSize, config.y);
        }

        public void ApplyPaletteToTiles()
        {
            foreach (var kv in _tiles)
            {
                var tile = kv.Value;
                if (tile == null) continue;

                tile.baseColor = tileColor;
                tile.selectedColor = selectedColor;

                tile.moveOverlayColor = moveOverlayColor;
                tile.dangerOverlayColor = dangerOverlayColor;
                tile.telegraphOverlayColor = telegraphOverlayColor;
                tile.executeOverlayColor = executeOverlayColor;

                tile.attackRangeOverlayColor = attackRangeOverlayColor;
                tile.attackPreviewOverlayColor = attackPreviewOverlayColor;
                tile.gunRangeOverlayColor = gunRangeOverlayColor;
            }

            Debug.Log($"[HexGridBuilder] Palette applied to tiles={_tiles.Count}");
        }

        public void Build(int radius, float size, float y)
        {
            Clear();
            ResolveFieldLayer();

            int fieldLayer = GetEffectiveLayer();

            float baseW = Mathf.Sqrt(3f) * size;
            float baseH = 2f * size;

            float targetW = baseW * widthMul;
            float targetH = baseH * heightMul;

            int count = 0;
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);

                for (int r = r1; r <= r2; r++)
                {
                    var c = new HexCoord(q, r);
                    var pos = HexMath.AxialToWorld(c, size, y);

                    var go = new GameObject($"Hex_{q}_{r}");
                    go.layer = fieldLayer;
                    go.transform.SetParent(tilesRoot, false);
                    go.transform.position = pos;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _hexSprite;
                    sr.color = tileColor;
                    sr.sortingOrder = 0;

                    var b = sr.sprite.bounds.size;
                    float sx = targetW / Mathf.Max(0.0001f, b.x);
                    float sy = targetH / Mathf.Max(0.0001f, b.y);
                    go.transform.localScale = new Vector3(sx, sy, 1f);

                    var col = go.AddComponent<PolygonCollider2D>();
                    col.isTrigger = false;

                    var tile = go.AddComponent<SpriteTile>();
                    tile.coord = c;
                    tile.sr = sr;
                    tile.baseColor = tileColor;
                    tile.selectedColor = selectedColor;

                    // overlay init
                    tile.overlaySprite = _hexSprite;
                    tile.moveOverlayColor = moveOverlayColor;
                    tile.dangerOverlayColor = dangerOverlayColor;
                    tile.telegraphOverlayColor = telegraphOverlayColor;
                    tile.executeOverlayColor = executeOverlayColor;
                    tile.attackRangeOverlayColor = attackRangeOverlayColor;
                    tile.attackPreviewOverlayColor = attackPreviewOverlayColor;
                    tile.gunRangeOverlayColor = gunRangeOverlayColor;

                    _tiles[c] = tile;
                    count++;
                }
            }
            ApplyPaletteToTiles();
            Debug.Log($"[HexGridBuilder] Built hex grid radius={radius}, size={size}, tiles={count}, widthMul={widthMul}, heightMul={heightMul}, layer={fieldLayerName}({fieldLayer})");
        }

        public void Clear()
        {
            _tiles.Clear();
            if (tilesRoot == null) return;

            for (int i = tilesRoot.childCount - 1; i >= 0; i--)
                Destroy(tilesRoot.GetChild(i).gameObject);
        }
    }

    public class SpriteTile : MonoBehaviour
    {
        private static int s_previewDbgLeft = 5;

        public HexCoord coord;
        public SpriteRenderer sr;

        public Color baseColor;
        public Color selectedColor;

        // overlay config
        [HideInInspector] public Sprite overlaySprite;
        [HideInInspector] public Color moveOverlayColor;
        [HideInInspector] public Color dangerOverlayColor;
        [HideInInspector] public Color telegraphOverlayColor;
        [HideInInspector] public Color executeOverlayColor;
        [HideInInspector] public Color attackRangeOverlayColor;
        [HideInInspector] public Color attackPreviewOverlayColor;
        [HideInInspector] public Color gunRangeOverlayColor;

        // 2 overlays
        private SpriteRenderer _ovMove;
        private SpriteRenderer _ovTop;

        private HexOverlayChannel _mask = HexOverlayChannel.None;

        // ✅ 기존 단일 outline -> 2개로 분리
        private LineRenderer _gunRangeOutline;
        private LineRenderer _attackRangeOutline;

        public void SetSelected(bool sel)
        {
            if (sr == null) return;
            sr.color = sel ? selectedColor : baseColor;
        }

        public void SetOverlay(HexOverlayChannel ch, bool on)
        {
            EnsureOverlays();
            if (on) _mask |= ch;
            else _mask &= ~ch;
            ApplyOverlay();
        }

        public void ClearOverlayAll()
        {
            _mask = HexOverlayChannel.None;
            ApplyOverlay();
        }

        private void EnsureOverlays()
        {
            if (_ovMove == null) _ovMove = EnsureOverlayRenderer("OverlayMove", orderAdd: 10);
            if (_ovTop == null) _ovTop = EnsureOverlayRenderer("OverlayTop", orderAdd: 60);
        }

        private SpriteRenderer EnsureOverlayRenderer(string name, int orderAdd)
        {
            var t = transform.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                t = go.transform;
                t.localPosition = name == "OverlayTop" ? new Vector3(0, 0, -0.002f) : new Vector3(0, 0, -0.001f);
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }

            t.gameObject.layer = gameObject.layer;

            var r = t.GetComponent<SpriteRenderer>();
            if (r == null) r = t.gameObject.AddComponent<SpriteRenderer>();

            r.sprite = overlaySprite;

            // ✅ Fallback: overlaySprite가 비어있으면 기본 타일 스프라이트를 사용 (프리뷰/오버레이가 안 보이는 문제 방지)
            if (r.sprite == null && sr != null) r.sprite = sr.sprite;

            if (sr != null)
            {
                r.sortingLayerID = sr.sortingLayerID;
                r.sortingOrder = sr.sortingOrder + orderAdd;

                // ✅ URP/2D Renderer 환경에서 overlay가 안 그려지는 문제 방지
                r.sharedMaterial = sr.sharedMaterial;

                // fallback: material이 없거나 깨졌으면 기본 Sprite 머티리얼로
                if (r.sharedMaterial == null)
                    r.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

                // 일부 플랫폼/렌더러에서 occlusion/culling 이슈 방지
                r.forceRenderingOff = false;
                r.allowOcclusionWhenDynamic = false;

                try { r.maskInteraction = sr.maskInteraction; } catch { }
                try { r.renderingLayerMask = sr.renderingLayerMask; } catch { }
            }
            else
            {
                r.sortingLayerID = 0;
                r.sortingOrder = orderAdd;
                if (r.sharedMaterial == null)
                    r.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                r.forceRenderingOff = false;
                r.allowOcclusionWhenDynamic = false;
            }

            r.enabled = false;
            r.color = new Color(1, 1, 1, 0);
            return r;
        }

        // -------------------------------
        // ✅ NEW: 채널별 outline API
        // -------------------------------
        public void SetGunRangeOutline(bool on, Color color, float widthWorld)
        {
            if (_gunRangeOutline == null)
                _gunRangeOutline = EnsureOutline("GunRangeOutline", orderAdd: 18, widthWorld: widthWorld);

            _gunRangeOutline.startColor = color;
            _gunRangeOutline.endColor = color;
            _gunRangeOutline.startWidth = widthWorld;
            _gunRangeOutline.endWidth = widthWorld;
            _gunRangeOutline.enabled = on;
        }

        public void SetAttackRangeOutline(bool on, Color color, float widthWorld)
        {
            if (_attackRangeOutline == null)
                _attackRangeOutline = EnsureOutline("AttackRangeOutline", orderAdd: 42, widthWorld: widthWorld);

            _attackRangeOutline.startColor = color;
            _attackRangeOutline.endColor = color;
            _attackRangeOutline.startWidth = widthWorld;
            _attackRangeOutline.endWidth = widthWorld;
            _attackRangeOutline.enabled = on;
        }

        // ✅ 기존 API 유지(호환): AttackRangeOutline로 매핑
        public void SetRangeOutline(bool on, Color color, float widthWorld)
        {
            SetAttackRangeOutline(on, color, widthWorld);
        }

        private LineRenderer EnsureOutline(string name, int orderAdd, float widthWorld)
        {
            var t = transform.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                t = go.transform;
                t.localPosition = new Vector3(0, 0, -0.003f);
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }

            t.gameObject.layer = gameObject.layer;

            var lr = t.GetComponent<LineRenderer>();
            if (lr == null) lr = t.gameObject.AddComponent<LineRenderer>();

            lr.useWorldSpace = false;
            lr.loop = true;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.alignment = LineAlignment.TransformZ;

            if (sr != null)
            {
                lr.sortingLayerID = sr.sortingLayerID;
                lr.sortingOrder = sr.sortingOrder + orderAdd;
            }

            lr.startWidth = widthWorld;
            lr.endWidth = widthWorld;

            if (lr.sharedMaterial == null)
                lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            lr.enabled = false;

            // 육각 포인트(로컬)
            var b = sr != null && sr.sprite != null ? sr.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            float rr = Mathf.Min(b.extents.x, b.extents.y);
            lr.positionCount = 6;

            for (int i = 0; i < 6; i++)
            {
                float ang = Mathf.Deg2Rad * (60f * i + 30f);
                lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr, 0));
            }

            return lr;
        }

        private void ApplyOverlay()
        {
            // ✅ 바닥 오버레이: AttackPreview / AttackRange / GunRange / Move
            // (현재 _ovTop이 카메라/정렬/URP 환경에서 안 보이는 상황을 우회하기 위해
            //  AttackPreview를 _ovMove(이미 정상 출력되는 경로)에 합쳐서 강제 출력)
            if (_ovMove != null)
            {
                bool hasAtkPrev     = (_mask & HexOverlayChannel.AttackPreview) != 0;
                bool hasAttackRange = (_mask & HexOverlayChannel.AttackRange) != 0;
                bool hasGunRange    = (_mask & HexOverlayChannel.GunRange) != 0;
                bool hasMove        = (_mask & HexOverlayChannel.Move) != 0;

                bool on = hasAtkPrev || hasAttackRange || hasGunRange || hasMove;

                _ovMove.enabled = on;

                if (!on) _ovMove.color = new Color(1, 1, 1, 0);
                else if (hasAtkPrev) _ovMove.color = attackPreviewOverlayColor; // ✅ 프리뷰 최우선
                else if (hasAttackRange) _ovMove.color = attackRangeOverlayColor;
                else if (hasGunRange) _ovMove.color = gunRangeOverlayColor;
                else _ovMove.color = moveOverlayColor;

                // (디버그) 프리뷰가 들어온 타일만 몇 번 찍기
                if (hasAtkPrev && s_previewDbgLeft-- > 0)
                    Debug.Log($"[PreviewDBG] MOVE-ROUTE AttackPreview tile={coord.q},{coord.r} ovMoveEnabled={_ovMove.enabled} col={_ovMove.color}");
            }

            // ✅ Top 우선순위: Execute > Telegraph > Danger
            // (AttackPreview는 위에서 _ovMove로 처리하므로 여기서 제외)
            if (_ovTop != null)
            {
                bool hasExec   = (_mask & HexOverlayChannel.Execute) != 0;
                bool hasTele   = (_mask & HexOverlayChannel.Telegraph) != 0;
                bool hasDanger = (_mask & HexOverlayChannel.Danger) != 0;

                if (hasExec)
                {
                    _ovTop.enabled = true;
                    _ovTop.color = executeOverlayColor;
                }
                else if (hasTele)
                {
                    _ovTop.enabled = true;
                    _ovTop.color = telegraphOverlayColor;
                }
                else if (hasDanger)
                {
                    _ovTop.enabled = true;
                    _ovTop.color = dangerOverlayColor;
                }
                else
                {
                    _ovTop.enabled = false;
                    _ovTop.color = new Color(1, 1, 1, 0);
                }
            }
        }
    }
}