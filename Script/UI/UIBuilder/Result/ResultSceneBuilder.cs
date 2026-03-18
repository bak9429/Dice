// Path: Assets/Script/UI/UIBuilder/Result/ResultSceneBuilder.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UI.UIBuilder.Result
{
    public static class ResultSceneBuilder
    {
        public sealed class ViewRefs
        {
            public Canvas canvas;
            public RectTransform wheelRoot;
            public Text titleText;
            public Text bodyText;
            public Text rouletteResultText;
            public Button primaryButton;
            public Button secondaryButton;
        }

        public static ViewRefs Build()
        {
            EnsureEventSystem();

            var existing = Object.FindFirstObjectByType<ResultSceneMarker>(FindObjectsInactive.Include);
            if (existing != null)
                return existing.refs;

            var refs = new ViewRefs();

            var canvasGo = new GameObject("ResultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            refs.canvas = canvas;

            var marker = canvasGo.AddComponent<ResultSceneMarker>();
            marker.refs = refs;

            var panel = CreatePanel(canvas.transform, "Root", new Vector2(1180, 720));

            refs.titleText = CreateText(panel, "Title", 42, FontStyle.Bold, TextAnchor.UpperLeft);
            SetStretch(refs.titleText.rectTransform, new Vector2(24, -28), new Vector2(-24, -86));

            refs.bodyText = CreateText(panel, "Body", 24, FontStyle.Normal, TextAnchor.UpperLeft);
            refs.bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            refs.bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(refs.bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(24, 112), new Vector2(-20, -100));

            refs.wheelRoot = CreateWheel(panel);

            refs.rouletteResultText = CreateText(panel, "RouletteResult", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(refs.rouletteResultText.rectTransform, new Vector2(0.56f, 0f), new Vector2(0.98f, 0f), new Vector2(0, 122), new Vector2(0, 170));

            refs.primaryButton = CreateButton(panel, "PrimaryButton", "Primary", new Vector2(24, 24));
            refs.secondaryButton = CreateButton(panel, "SecondaryButton", "Secondary", new Vector2(344, 24));

            return refs;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.10f, 0.96f);

            return rt;
        }

        private static RectTransform CreateWheel(Transform parent)
        {
            var wheel = new GameObject("WheelRoot", typeof(RectTransform), typeof(Image));
            wheel.transform.SetParent(parent, false);

            var rt = wheel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.77f, 0.56f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360, 360);

            var bg = wheel.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

            string[] labels = { "1\nLOW", "2\nLOW", "3\nMID", "4\nMID", "5\nHIGH", "6\nEQUIP" };
            float radius = 132f;

            for (int i = 0; i < labels.Length; i++)
            {
                float angle = Mathf.Deg2Rad * (90f - i * 60f);

                var txt = CreateText(wheel.transform, $"Seg{i + 1}", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
                txt.text = labels[i];

                var tr = txt.rectTransform;
                tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.sizeDelta = new Vector2(92, 52);
                tr.anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                tr.localEulerAngles = new Vector3(0f, 0f, i * 60f);
            }

            var pointer = new GameObject("Pointer", typeof(RectTransform), typeof(Image));
            pointer.transform.SetParent(parent, false);

            var prt = pointer.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.77f, 0.56f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.sizeDelta = new Vector2(24, 44);
            prt.anchoredPosition = new Vector2(0f, 198f);

            pointer.GetComponent<Image>().color = new Color(0.95f, 0.25f, 0.25f, 1f);

            return rt;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = "";

            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(300, 58);
            rt.anchoredPosition = anchoredPos;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.26f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;

            var txt = CreateText(go.transform, "Label", 22, FontStyle.Normal, TextAnchor.MiddleCenter);
            txt.text = label;
            SetRect(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }

        private static void SetStretch(RectTransform rt, Vector2 topLeft, Vector2 bottomRight)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(topLeft.x, bottomRight.y);
            rt.offsetMax = new Vector2(bottomRight.x, topLeft.y);
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private sealed class ResultSceneMarker : MonoBehaviour
        {
            public ViewRefs refs;
        }
    }
}