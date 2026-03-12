using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Rule.Field.CameraTools
{
    public class FieldCameraEdgePanController : MonoBehaviour
    {
        [Header("Cameras")]
        public Camera fieldCamera;

        [Tooltip("Canvas가 Screen Space - Camera면 MainCamera 지정. Overlay면 비워도 됨.")]
        public Camera uiCamera;

        [Header("BottomCombat")]
        public string bottomCombatPath = "Canvas/UIRoot/CombatLayout/CenterRoot/BottomCombat";
        public RectTransform bottomCombat;

        [Header("Edge Scroll")]
        public float thresholdPx = 36f;
        public float speed = 1.8f;
        public float fastMultiplier = 2.2f;
        public bool smooth = true;
        public float smoothSpeed = 12f;

        [Header("Cursor Clamp (optional)")]
        public bool clampCursorToBottomCombat = false;
        public bool clampAlways = false;
        public Key clampHoldKey = Key.Space;
        public float clampPaddingPx = 6f;

        [Header("Field Bounds Clamp")]
        [Tooltip("헥스 필드 바운즈 안에서만 카메라가 움직이도록 제한")]
        public bool clampToFieldBounds = true;

        public FieldWorldBoundsProvider boundsProvider;

        private Vector2 _vel;

        private void Awake()
        {
            if (fieldCamera == null) fieldCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (bottomCombat == null)
            {
                var go = GameObject.Find(bottomCombatPath);
                if (go != null) bottomCombat = go.GetComponent<RectTransform>();
            }

            if (boundsProvider == null)
                boundsProvider = FindFirstObjectByType<FieldWorldBoundsProvider>(FindObjectsInactive.Include);

            if (boundsProvider == null)
            {
                // 없으면 자동 생성(편의)
                var go = new GameObject("FieldWorldBoundsProvider");
                boundsProvider = go.AddComponent<FieldWorldBoundsProvider>();
            }
        }

        private void Update()
        {
            if (fieldCamera == null || !fieldCamera.orthographic) return;

            if (bottomCombat == null)
            {
                var go = GameObject.Find(bottomCombatPath);
                if (go != null) bottomCombat = go.GetComponent<RectTransform>();
                else return;
            }

            Vector2 mouse = ReadMouseScreenPos();
            bool over = RectTransformUtility.RectangleContainsScreenPoint(bottomCombat, mouse, uiCamera);

            if (ShouldClampCursor())
            {
                ClampCursorIntoBottomCombat();
                mouse = ReadMouseScreenPos();
                over = RectTransformUtility.RectangleContainsScreenPoint(bottomCombat, mouse, uiCamera);
            }

            if (!over)
            {
                _vel = Vector2.Lerp(_vel, Vector2.zero, Time.unscaledDeltaTime * Mathf.Max(1f, smoothSpeed));
                return;
            }

            // UI 위면 패닝 금지
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _vel = Vector2.Lerp(_vel, Vector2.zero, Time.unscaledDeltaTime * Mathf.Max(1f, smoothSpeed));
                return;
            }

            // 패널 내부 좌표
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bottomCombat, mouse, uiCamera, out var local);
            Rect rect = bottomCombat.rect;

            float leftDist = local.x - rect.xMin;
            float rightDist = rect.xMax - local.x;
            float bottomDist = local.y - rect.yMin;
            float topDist = rect.yMax - local.y;

            float dirX = 0f;
            float dirY = 0f;

            if (leftDist < thresholdPx) dirX = -EdgeStrength(leftDist);
            else if (rightDist < thresholdPx) dirX = +EdgeStrength(rightDist);

            if (bottomDist < thresholdPx) dirY = -EdgeStrength(bottomDist);
            else if (topDist < thresholdPx) dirY = +EdgeStrength(topDist);

            Vector2 dir = new Vector2(dirX, dirY);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // 줌에 비례한 월드 속도
            float worldPerScreenY = fieldCamera.orthographicSize * 2f;
            float worldPerScreenX = worldPerScreenY * fieldCamera.aspect;

            float baseSpeedWorldX = worldPerScreenX * speed;
            float baseSpeedWorldY = worldPerScreenY * speed;

            float mult = IsFastHeld() ? fastMultiplier : 1f;
            Vector2 desiredVel = new Vector2(dir.x * baseSpeedWorldX, dir.y * baseSpeedWorldY) * mult;

            _vel = smooth
                ? Vector2.Lerp(_vel, desiredVel, Time.unscaledDeltaTime * Mathf.Max(1f, smoothSpeed))
                : desiredVel;

            // 이동
            Vector3 pos = fieldCamera.transform.position;
            pos += new Vector3(_vel.x, _vel.y, 0f) * Time.unscaledDeltaTime;

            // ✅ 헥스 필드 바운즈로 클램프
            if (clampToFieldBounds && boundsProvider != null && boundsProvider.HasBounds)
                pos = ClampCameraPosToBounds(pos, boundsProvider.Bounds);

            fieldCamera.transform.position = pos;
        }

        private Vector3 ClampCameraPosToBounds(Vector3 camPos, Bounds fieldBounds)
        {
            float halfH = fieldCamera.orthographicSize;
            float halfW = halfH * fieldCamera.aspect;

            float minX = fieldBounds.min.x + halfW;
            float maxX = fieldBounds.max.x - halfW;
            float minY = fieldBounds.min.y + halfH;
            float maxY = fieldBounds.max.y - halfH;

            // 화면이 필드보다 큰 경우(줌아웃 과도)엔 중앙 고정
            if (minX > maxX) camPos.x = fieldBounds.center.x;
            else camPos.x = Mathf.Clamp(camPos.x, minX, maxX);

            if (minY > maxY) camPos.y = fieldBounds.center.y;
            else camPos.y = Mathf.Clamp(camPos.y, minY, maxY);

            return camPos;
        }

        private float EdgeStrength(float distToEdge)
        {
            float t = Mathf.Clamp01(1f - (distToEdge / Mathf.Max(1f, thresholdPx)));
            return t * t;
        }

        private bool ShouldClampCursor()
        {
#if ENABLE_INPUT_SYSTEM
            if (!clampCursorToBottomCombat) return false;
            if (clampAlways) return true;

            var kb = Keyboard.current;
            if (kb == null) return false;
            var key = kb[clampHoldKey];
            return key != null && key.isPressed;
#else
            return false;
#endif
        }

        private void ClampCursorIntoBottomCombat()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null || bottomCombat == null) return;

            Vector3[] corners = new Vector3[4];
            bottomCombat.GetWorldCorners(corners);

            Vector2 bl = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

            float pad = Mathf.Max(0f, clampPaddingPx);

            float minX = bl.x + pad;
            float minY = bl.y + pad;
            float maxX = tr.x - pad;
            float maxY = tr.y - pad;

            Vector2 p = Mouse.current.position.ReadValue();
            float cx = Mathf.Clamp(p.x, minX, maxX);
            float cy = Mathf.Clamp(p.y, minY, maxY);

            if (Mathf.Abs(cx - p.x) > 0.01f || Mathf.Abs(cy - p.y) > 0.01f)
                Mouse.current.WarpCursorPosition(new Vector2(cx, cy));
#endif
        }

        private static Vector2 ReadMouseScreenPos()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        private static bool IsFastHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
#else
            return false;
#endif
        }
    }
}
