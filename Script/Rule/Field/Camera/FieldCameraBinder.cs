// Path: Assets/Script/Rule/Field/Camera/FieldCameraBinder.cs
using System.Collections;
using UnityEngine;
using UI.UIBuilder.CombatLayout;

namespace Rule.Field.CameraTools
{
    public class FieldCameraBinder : MonoBehaviour
    {
        [Header("Camera")]
        public Camera fieldCamera;

        [Header("Auto Bind (Code-Generated UI)")]
        [Tooltip("CombatLayoutRefs를 자동으로 찾아 BottomCombat을 바인딩")]
        public bool autoBind = true;

        [Tooltip("Canvas가 Screen Space - Camera면 MainCamera를 넣어라. Overlay면 비워도 됨.")]
        public Camera uiCamera;

        [Header("Timing")]
        [Tooltip("UI가 늦게 생성될 수 있어서, 찾을 때까지 기다리는 최대 시간(초)")]
        public float maxWaitSeconds = 5f;

        [Tooltip("초기 적용 후에도 계속 맞추고 싶으면 ON (해상도/레이아웃 변동 디버그용)")]
        public bool applyEveryFrame = false;

        private RectTransform _bottomCombat;

        private void Awake()
        {
            if (fieldCamera == null) fieldCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (autoBind)
                StartCoroutine(CoBindAndApply());
        }

        private void LateUpdate()
        {
            if (applyEveryFrame && _bottomCombat != null)
                ApplyViewport(_bottomCombat);
        }

        private IEnumerator CoBindAndApply()
        {
            float t = 0f;
            while (_bottomCombat == null && t < maxWaitSeconds)
            {
                var refs = FindFirstObjectByType<CombatLayoutRefs>();
                if (refs != null && refs.bottomCombat != null)
                {
                    _bottomCombat = refs.bottomCombat;
                    break;
                }

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_bottomCombat == null)
            {
                Debug.LogError("[FieldCameraBinder] CombatLayoutRefs/bottomCombat not found in time. " +
                               "Ensure CombatLayoutBuilder runs before this, or increase maxWaitSeconds.");
                yield break;
            }

            ApplyViewport(_bottomCombat);
        }

        private void ApplyViewport(RectTransform rt)
        {
            if (fieldCamera == null)
            {
                Debug.LogError("[FieldCameraBinder] fieldCamera is null.");
                return;
            }

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners); // world corners

            // ScreenPoint 변환 (Canvas 모드에 따라 uiCamera가 필요할 수 있음)
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]); // BottomLeft
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]); // TopRight

            float xMin = bl.x / Screen.width;
            float yMin = bl.y / Screen.height;
            float xMax = tr.x / Screen.width;
            float yMax = tr.y / Screen.height;

            var rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            // 안정 클램프
            rect.x = Mathf.Clamp01(rect.x);
            rect.y = Mathf.Clamp01(rect.y);
            rect.width = Mathf.Clamp01(rect.width);
            rect.height = Mathf.Clamp01(rect.height);

            fieldCamera.rect = rect;

            Debug.Log($"[FieldCameraBinder] Applied rect={rect} bl={bl} tr={tr} screen={Screen.width}x{Screen.height}");
        }
    }
}
