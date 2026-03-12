// Path: Assets/Script/UI/UIBuilder/UICanvasBootstrap.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace UI.UIBuilder
{
    public class UICanvasBootstrap : MonoBehaviour
    {
        [Header("Canvas Settings")]
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        public string uiRootName = "UIRoot";

        [Header("Raycast Sanitizer")]
        public bool sanitizeRaycasts = true;
        public string keepRaycastIfNameContains = "ModalBG";
        public bool verboseSanitizeLog = false;

        [Header("Input Module Auto Select")]
        public bool autoSelectInputModule = true;

        private void Awake()
        {
            EnsureCanvasAndRoot();
            EnsureEventSystem();
        }

        private void Start()
        {
            if (autoSelectInputModule)
                StartCoroutine(CoSelectInputModuleNextFrame());

            if (sanitizeRaycasts)
                StartCoroutine(CoSanitizeNextFrame());
        }

        private IEnumerator CoSelectInputModuleNextFrame()
        {
            yield return null;

            var es = EventSystem.current;
            if (es == null) yield break;

            var standalone = es.GetComponent<StandaloneInputModule>();
#if ENABLE_INPUT_SYSTEM
            var inputSystem = es.GetComponent<InputSystemUIInputModule>();
            bool newInputAlive = (Keyboard.current != null || Mouse.current != null);
#else
            bool newInputAlive = false;
#endif
            // ✅ 선택 규칙:
            // - New Input이 살아있으면(InputSystem 디바이스 존재) InputSystemUIInputModule 사용
            // - 아니면 StandaloneInputModule 사용
#if ENABLE_INPUT_SYSTEM
            if (inputSystem != null && standalone != null)
            {
                inputSystem.enabled = newInputAlive;
                standalone.enabled = !newInputAlive;
                Debug.Log($"[UICanvasBootstrap] InputModule selected: {(newInputAlive ? "InputSystemUIInputModule" : "StandaloneInputModule")}");
            }
#endif
            if (standalone != null && !newInputAlive)
            {
                standalone.enabled = true;
                Debug.Log("[UICanvasBootstrap] InputModule selected: StandaloneInputModule");
            }
        }

        private IEnumerator CoSanitizeNextFrame()
        {
            yield return null;
            SanitizeRaycasts();
        }

        private void EnsureCanvasAndRoot()
        {
            var existingRoot = GameObject.Find(uiRootName);
            if (existingRoot != null && existingRoot.GetComponentInParent<Canvas>() != null)
            {
                UIRootRegistry.Set(existingRoot.GetComponent<RectTransform>());
                return;
            }

            var canvasGo = new GameObject("Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 5;
            canvasGo.layer = uiLayer;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            var rootGo = new GameObject(uiRootName, typeof(RectTransform));
            rootGo.layer = uiLayer;
            rootGo.transform.SetParent(canvasGo.transform, false);

            var rt = rootGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            UIRootRegistry.Set(rt);
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var esGo = new GameObject("EventSystem", typeof(EventSystem));
            esGo.transform.SetParent(null);

            // 둘 다 달아두고, Start에서 하나만 enable 처리
            var standalone = esGo.AddComponent<StandaloneInputModule>();
            standalone.enabled = true;

#if ENABLE_INPUT_SYSTEM
            var inputSystem = esGo.AddComponent<InputSystemUIInputModule>();
            inputSystem.enabled = true;
#endif
            Debug.Log("[UICanvasBootstrap] EventSystem created.");
        }

        private void SanitizeRaycasts()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[UICanvasBootstrap] SanitizeRaycasts: Canvas not found.");
                return;
            }

            int changed = 0;
            int kept = 0;

            var graphics = canvas.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g == null) continue;
                if (!g.raycastTarget) continue;

                if (g.GetComponentInParent<Selectable>(true) != null)
                {
                    kept++;
                    continue;
                }

                if (!string.IsNullOrEmpty(keepRaycastIfNameContains) && g.name.Contains(keepRaycastIfNameContains))
                {
                    kept++;
                    continue;
                }

                g.raycastTarget = false;
                changed++;

                if (verboseSanitizeLog)
                    Debug.Log($"[UICanvasBootstrap] raycastTarget OFF: {GetPath(g.transform)}");
            }

            Debug.Log($"[UICanvasBootstrap] SanitizeRaycasts done. changed={changed}, kept={kept}");
        }

        private string GetPath(Transform t)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (t != null)
            {
                sb.Insert(0, "/" + t.name);
                t = t.parent;
            }
            return sb.ToString();
        }
    }
}
