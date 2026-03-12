// Path: Assets/Script/Rule/Field/Camera/FieldTurnZoomController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UI.UIBuilder.CombatLayout;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Rule.Field.CameraTools
{
    public class FieldTurnZoomController : MonoBehaviour
    {
        [Header("Cameras")]
        public Camera fieldCamera;

        [Tooltip("Canvas가 Screen Space - Camera면 MainCamera 지정. Overlay면 비워도 됨.")]
        public Camera uiCamera;

        [Header("Bind")]
        public float maxWaitSeconds = 5f;

        [Header("Wheel Zoom")]
        public bool enableWheelZoom = true;

        [Tooltip("휠 1틱당 orthoSize 변화량 (wheel↑ => 줌인: orthoSize 감소)")]
        public float zoomStep = 0.8f;

        public float minOrthoSize = 2.5f;
        public float maxOrthoSize = 12.0f;

        [Header("Smooth")]
        public bool smooth = true;
        public float smoothSpeed = 14f;

        private CombatLayoutRefs _refs;
        private float _targetOrtho;

        private void Awake()
        {
            if (fieldCamera == null) fieldCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            StartCoroutine(CoBindRefs());

            if (fieldCamera != null && fieldCamera.orthographic)
                _targetOrtho = fieldCamera.orthographicSize;
        }

        private IEnumerator CoBindRefs()
        {
            float t = 0f;
            while ((_refs == null || _refs.bottomCombat == null) && t < maxWaitSeconds)
            {
                _refs = FindFirstObjectByType<CombatLayoutRefs>();
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_refs == null || _refs.bottomCombat == null)
                Debug.LogError("[FieldTurnZoomController] CombatLayoutRefs/bottomCombat not found.");
        }

        private void Update()
        {
            if (!enableWheelZoom) return;
            if (fieldCamera == null || !fieldCamera.orthographic) return;
            if (_refs == null || _refs.bottomCombat == null) return;

            // BottomCombat 위에서만 줌
            if (!IsMouseOverRect(_refs.bottomCombat))
            {
                ApplySmoothOnly();
                return;
            }

            // UI 위면 줌 금지 (UI 스크롤/버튼이 휠 먹게)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ApplySmoothOnly();
                return;
            }

            float wheel = ReadMouseWheelY();
            if (Mathf.Abs(wheel) > 0.001f)
            {
                // wheel > 0 : 줌인(orthoSize 감소)
                _targetOrtho -= wheel * zoomStep;
                _targetOrtho = Mathf.Clamp(_targetOrtho, minOrthoSize, maxOrthoSize);
            }

            ApplySmoothOnly();
        }

        private void ApplySmoothOnly()
        {
            if (fieldCamera == null) return;

            if (!smooth)
            {
                fieldCamera.orthographicSize = _targetOrtho;
                return;
            }

            float cur = fieldCamera.orthographicSize;
            float next = Mathf.Lerp(cur, _targetOrtho, Time.unscaledDeltaTime * Mathf.Max(1f, smoothSpeed));
            fieldCamera.orthographicSize = next;
        }

        private bool IsMouseOverRect(RectTransform rt)
        {
            if (rt == null) return false;
            Vector2 mouse = ReadMouseScreenPos();
            return RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, uiCamera);
        }

        private static Vector2 ReadMouseScreenPos()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 p = Mouse.current.position.ReadValue();
                return p;
            }
            return Vector2.zero;
#else
            // 구 Input fallback (프로젝트 설정이 Old면 여기 타게 됨)
            return Input.mousePosition;
#endif
        }

        private static float ReadMouseWheelY()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                // 보통 y에 휠 델타가 들어옴(틱 단위가 플랫폼마다 다를 수 있음)
                return Mouse.current.scroll.ReadValue().y;
            }
            return 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        // (호환용) 예전 코드가 SetPhase를 호출하더라도 no-op
        public enum Phase { Player, Boss }
        public void SetPhase(Phase phase) { }
    }
}
