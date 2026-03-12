// Path: Assets/Script/UI/UIBuilder/MainMenu/MainMenuBuilder.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

namespace UI.UIBuilder
{
    public static class MainMenuBuilder
    {
        public sealed class ViewRefs
        {
            public Button newGameButton;
            public Button continueButton;
            public Button settingsButton;
            public Button quitButton;
        }

        public static ViewRefs Build()
        {
            EnsureEventSystem();

            var canvas = CreateCanvas("MainMenuCanvas");
            var root = CreatePanel(canvas.transform, "Root", new Vector2(0.5f, 0.5f), new Vector2(640, 540));
            var title = CreateText(root, "Title", "HEX BOSS", 42, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(420, 60));

            var refs = new ViewRefs();
            refs.newGameButton = CreateButton(root, "NewGameButton", "New Game", new Vector2(0, 80));
            refs.continueButton = CreateButton(root, "ContinueButton", "Continue", new Vector2(0, 10));
            refs.settingsButton = CreateButton(root, "SettingsButton", "Settings", new Vector2(0, -60));
            refs.quitButton = CreateButton(root, "QuitButton", "Quit", new Vector2(0, -130));
            return refs;
        }

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            return canvas;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.10f, 0.12f, 0.92f);
            return rt;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);

            var uiText = go.GetComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.alignment = anchor;
            uiText.color = Color.white;
            return uiText;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300, 52);
            rt.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.26f, 1f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var txt = CreateText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter);
            SetRect(txt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rt.sizeDelta);

            return btn;
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindObjectOfType<EventSystem>();
            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    var oldModule = existing.GetComponent<StandaloneInputModule>();
                    if (oldModule != null)
                        Object.Destroy(oldModule);

                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}