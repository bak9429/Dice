// Path: Assets/Script/UI/UIControl/QteSkeletonBuilder.cs
using UnityEngine;
using UnityEngine.UI;
using UI.UIBuilder.CombatLayout;

namespace UI.UIControl
{
    /// <summary>
    /// CombatLayoutRefs.qteRoot 아래에 QTE UI 틀만 생성한다(기능은 나중).
    /// </summary>
    public class QteSkeletonBuilder : MonoBehaviour
    {
        public CombatLayoutRefs layout;
        public bool buildOnStart = true;

        private void Start()
        {
            if (!buildOnStart) return;
            if (layout == null) layout = FindFirstObjectByType<CombatLayoutRefs>(FindObjectsInactive.Include);
            if (layout == null || layout.qteRoot == null) return;

            if (layout.qteRoot.childCount > 0) return; // already built

            Build(layout.qteRoot);
        }

        private void Build(RectTransform root)
        {
            var bg = NewGO("QTE_BG", root).AddComponent<Image>();
            bg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            bg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bg.rectTransform.sizeDelta = new Vector2(520, 64);
            bg.rectTransform.anchoredPosition = Vector2.zero;
            bg.color = new Color(0, 0, 0, 0.35f);

            var bar = NewGO("QTE_BarFill", bg.rectTransform).AddComponent<Image>();
            bar.rectTransform.anchorMin = new Vector2(0, 0);
            bar.rectTransform.anchorMax = new Vector2(0, 1);
            bar.rectTransform.pivot = new Vector2(0, 0.5f);
            bar.rectTransform.sizeDelta = new Vector2(240, 0);
            bar.color = new Color(1f, 1f, 1f, 0.35f);

            var txt = NewGO("QTE_KeyPrompt", bg.rectTransform).AddComponent<Text>();
            txt.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            txt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            txt.rectTransform.anchoredPosition = Vector2.zero;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 22;
            txt.text = "QTE (placeholder)";
            txt.color = new Color(1, 1, 1, 0.85f);

            root.gameObject.SetActive(false); // 기본은 숨김 (UIController에서 켤 수 있게)
            Debug.Log("[QTE] Skeleton built (hidden by default).");
        }

        private static GameObject NewGO(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
