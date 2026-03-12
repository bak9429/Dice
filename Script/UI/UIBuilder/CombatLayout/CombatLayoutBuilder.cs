// Path: Assets/Script/UI/UIBuilder/CombatLayout/CombatLayoutBuilder.cs

using UnityEngine;
using UnityEngine.UI;

namespace UI.UIBuilder.CombatLayout
{
    public class CombatLayoutBuilder : MonoBehaviour
    {
        [Header("Layout")]
        public int sideWidth = 140;
        public int padding = 16;
        [Range(0.1f, 0.9f)] public float topHeightRatio = 0.45f; // 중립값(턴 연출은 다음 브랜치)

        [Header("Debug Colors (optional)")]
        public bool debugBackgrounds = true;

        // ✅ Awake에서 만들지 말고 Start에서 만든다(부트스트랩 Awake 보장 후)
        private void Start()
        {
            BuildIfMissing();
        }

        public CombatLayoutRefs BuildIfMissing()
        {
            var uiRoot = UI.UIBuilder.UIRootRegistry.Get();
            if (uiRoot == null)
            {
                Debug.LogError("[CombatLayoutBuilder] UIRoot not found. Ensure UICanvasBootstrap is active in scene.");
                return null;
            }

            var existing = uiRoot.Find("CombatLayout");
            if (existing != null)
            {
                var refs0 = existing.GetComponent<CombatLayoutRefs>();
                if (refs0 != null) return refs0;
            }

            var layout = CreatePanel(uiRoot, "CombatLayout", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var refs = layout.gameObject.AddComponent<CombatLayoutRefs>();
            refs.root = layout;

            // Left / Right
            refs.leftInfo = CreatePanel(layout, "LeftInfo",
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(padding, padding), new Vector2(padding + sideWidth, -padding));

            refs.rightInfo = CreatePanel(layout, "RightInfo",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-(padding + sideWidth), padding), new Vector2(-padding, -padding));

            // CenterRoot (between side panels)
            refs.centerRoot = CreatePanel(layout, "CenterRoot",
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(padding + sideWidth + padding, padding),
                new Vector2(-(padding + sideWidth + padding), -padding));

            // Split CenterRoot into Top/Bottom by ratio
            refs.topCinematic = CreatePanel(refs.centerRoot, "TopCinematic",
                new Vector2(0, topHeightRatio), new Vector2(1, 1),
                Vector2.zero, Vector2.zero);

            refs.bottomCombat = CreatePanel(refs.centerRoot, "BottomCombat",
                new Vector2(0, 0), new Vector2(1, topHeightRatio),
                Vector2.zero, Vector2.zero);

            // Top contents + QTE placeholder
            refs.cinematicContent = CreatePanel(refs.topCinematic, "CinematicContent",
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(12, 12), new Vector2(-12, -12));

            // Placeholder for future 3D cinematic rendering.
            // You can bind a RenderTexture here (e.g., from a CinematicCamera) later.
            // Keeping it as RawImage makes swapping 2D/video -> 3D straightforward.
            var cinematic3D = new GameObject("Cinematic3D", typeof(RectTransform), typeof(RawImage));
            {
                int uiLayer = LayerMask.NameToLayer("UI");
                cinematic3D.layer = uiLayer >= 0 ? uiLayer : 0;
            }
            cinematic3D.transform.SetParent(refs.cinematicContent, false);
            var crt = cinematic3D.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            var ri = cinematic3D.GetComponent<RawImage>();
            ri.color = new Color(0, 0, 0, 0); // transparent until you bind a RenderTexture
            ri.raycastTarget = false;

            refs.qteRoot = CreatePanel(refs.topCinematic, "QTE",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-250, -60), new Vector2(250, 60));

            if (debugBackgrounds)
            {
                AddBg(refs.topCinematic.gameObject, new Color(0, 0, 0, 0.08f));
                AddBg(refs.bottomCombat.gameObject, new Color(1, 0, 0, 0.06f));
                AddBg(refs.leftInfo.gameObject, new Color(1, 1, 0, 0.06f));
                AddBg(refs.rightInfo.gameObject, new Color(1, 1, 0, 0.06f));
                AddOutline(refs.topCinematic.gameObject, new Color(0, 0, 0, 0.35f));
                AddOutline(refs.bottomCombat.gameObject, new Color(1, 0, 0, 0.35f));
                AddOutline(refs.leftInfo.gameObject, new Color(1, 1, 0, 0.35f));
                AddOutline(refs.rightInfo.gameObject, new Color(1, 1, 0, 0.35f));
            }

            return refs;
        }

        private static RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static void AddBg(GameObject go, Color c)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
        }

        private static void AddOutline(GameObject go, Color c)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = c;
            outline.effectDistance = new Vector2(2, -2);
        }
    }
}
