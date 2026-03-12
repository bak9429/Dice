// Path: Assets/Script/UI/UIBuilder/HexAction/HexActionPanelDebugToggle.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UI.UIBuilder.CombatLayout;
using Rule.Field;

namespace UI.UIBuilder.HexAction
{
    public class HexActionPanelDebugToggle : MonoBehaviour
    {
        [Header("Toggle Key (New Input System)")]
        public Key toggleKey = Key.H;

        [Header("Follow Tile")]
        public bool followSelectedTile = true;

        [Tooltip("패널을 타일 스크린 포인트 기준으로 얼마나 옮길지(px)")]
        public Vector2 screenOffset = new Vector2(24, 0);

        [Tooltip("BottomCombat 내부에서 패널이 너무 가장자리로 붙지 않게")]
        public float clampPadding = 12f;

        private HexActionPanelBuilder _builder;
        private HexActionRefs _refs;
        private CombatLayoutRefs _layout;

        private bool _shown;

        private Camera _fieldCam; // Field 레이어를 보는 카메라

        private void OnEnable()
        {
            HexSelectionController.OnHexSelected += OnHexSelected;
            HexSelectionController.OnHexDeselected += OnHexDeselected;
        }

        private void OnDisable()
        {
            HexSelectionController.OnHexSelected -= OnHexSelected;
            HexSelectionController.OnHexDeselected -= OnHexDeselected;
        }

        private void Start()
        {
            _builder = FindFirstObjectByType<HexActionPanelBuilder>();
            ResolveFieldCamera();
        }

        private void Update()
        {
            // 기존 H키 토글 유지
            if (Keyboard.current == null) return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                EnsureRefs();
                if (_refs == null || _builder == null) return;

                _shown = !_shown;
                if (_shown) _builder.Show(_refs);
                else _builder.Hide(_refs);
            }
        }

        private void OnHexSelected(HexCoord coord, Vector3 worldPos)
        {
            EnsureRefs();
            if (_refs == null || _builder == null) return;

            _builder.Show(_refs);
            _shown = true;

            if (followSelectedTile)
            {
                ResolveFieldCamera();
                if (_fieldCam != null && _layout != null && _layout.bottomCombat != null)
                {
                    PlacePanelNearWorld(worldPos);
                }
            }
        }

        private void OnHexDeselected()
        {
            EnsureRefs();
            if (_refs == null || _builder == null) return;

            _builder.Hide(_refs);
            _shown = false;
        }

        private void EnsureRefs()
        {
            if (_refs != null && _layout != null && _layout.bottomCombat != null) return;

            // CombatLayoutRefs가 먼저 있어야 함
            _layout = FindFirstObjectByType<CombatLayoutRefs>(FindObjectsInactive.Include);
            if (_layout == null || _layout.bottomCombat == null) return;

            // 이미 만들어져 있으면 재사용
            _refs = FindFirstObjectByType<HexActionRefs>(FindObjectsInactive.Include);
            if (_refs != null) return;

            if (_builder == null) _builder = FindFirstObjectByType<HexActionPanelBuilder>(FindObjectsInactive.Include);
            if (_builder == null) return;

            _refs = _builder.BuildIfMissing(_layout);
        }

        private void ResolveFieldCamera()
        {
            if (_fieldCam != null) return;

            int fieldLayer = LayerMask.NameToLayer("Field");
            if (fieldLayer >= 0)
            {
                int fieldMask = 1 << fieldLayer;
                foreach (var c in Camera.allCameras)
                {
                    if ((c.cullingMask & fieldMask) != 0)
                    {
                        _fieldCam = c;
                        break;
                    }
                }
            }

            if (_fieldCam == null) _fieldCam = Camera.main;
        }

        private void PlacePanelNearWorld(Vector3 worldPos)
        {
            var panel = _refs.panelRoot;
            var bottom = _layout.bottomCombat;

            if (panel == null || bottom == null) return;

            // 1) 월드→스크린
            Vector2 sp = _fieldCam.WorldToScreenPoint(worldPos);
            sp += screenOffset;

            // 2) 스크린→BottomCombat 로컬
            // Canvas가 ScreenSpaceOverlay면 camera=null로 OK
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bottom, sp, null, out Vector2 local);

            // 3) 패널을 BottomCombat 기준으로 배치하기 위해 anchor/pivot 고정
            // local은 BottomCombat의 pivot 기준 좌표
            panel.SetParent(bottom, worldPositionStays: false);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f); // 패널 왼쪽이 기준(타일 오른쪽에 붙이기 쉬움)

            // 4) BottomCombat 내부에서 클램프
            // BottomCombat의 로컬 bounds
            Vector2 bottomSize = bottom.rect.size;
            Vector2 bottomPivot = bottom.pivot;

            float minX = -bottomSize.x * bottomPivot.x;
            float maxX = bottomSize.x * (1f - bottomPivot.x);
            float minY = -bottomSize.y * bottomPivot.y;
            float maxY = bottomSize.y * (1f - bottomPivot.y);

            // 패널 크기 (pivot 고려: pivot x=0이므로 anchoredPosition.x는 "왼쪽")
            Vector2 pSize = panel.rect.size;
            float x = Mathf.Clamp(local.x, minX + clampPadding, maxX - pSize.x - clampPadding);
            float y = Mathf.Clamp(local.y, minY + (pSize.y * 0.5f) + clampPadding, maxY - (pSize.y * 0.5f) - clampPadding);

            panel.anchoredPosition = new Vector2(x, y);

            // 제목도 같이 바꿔주고 싶으면(선택):
            if (_refs.titleText != null)
                _refs.titleText.text = "ACTIONS";
        }
    }
}
