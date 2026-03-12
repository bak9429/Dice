using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

namespace UI.UIBuilder.Node
{
    public static class NodeSceneBuilder
    {
        public static NodeSceneViewRefs Build(Transform parent = null)
        {
            EnsureEventSystem();

            GameObject rootGo = new GameObject("NodeSceneUI");
            if (parent != null) rootGo.transform.SetParent(parent, false);

            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            rootGo.AddComponent<GraphicRaycaster>();

            var refs = rootGo.AddComponent<NodeSceneViewRefs>();
            refs.canvas = canvas;
            refs.root = rootGo.GetComponent<RectTransform>();
            Stretch(refs.root);

            GameObject bg = CreatePanel("Background", refs.root, new Color(0.08f, 0.08f, 0.09f, 1f));
            Stretch(bg.GetComponent<RectTransform>());

            refs.leftPanel = CreateRect("LeftPanel", refs.root,
                new Vector2(0f, 0f), new Vector2(0.25f, 1f),
                new Vector2(16f, 16f), new Vector2(-8f, -16f));

            refs.centerPanel = CreateRect("CenterPanel", refs.root,
                new Vector2(0.25f, 0.18f), new Vector2(0.72f, 1f),
                new Vector2(8f, 16f), new Vector2(-8f, -16f));

            refs.rightPanel = CreateRect("RightPanel", refs.root,
                new Vector2(0.72f, 0.18f), new Vector2(1f, 1f),
                new Vector2(8f, 16f), new Vector2(-16f, -16f));

            refs.bottomPanel = CreateRect("BottomPanel", refs.root,
                new Vector2(0.25f, 0f), new Vector2(1f, 0.18f),
                new Vector2(8f, 8f), new Vector2(-16f, -8f));

            AddPanelBack(refs.leftPanel, new Color(0.15f, 0.15f, 0.17f, 0.92f));
            AddPanelBack(refs.centerPanel, new Color(0.12f, 0.12f, 0.14f, 0.94f));
            AddPanelBack(refs.rightPanel, new Color(0.15f, 0.15f, 0.17f, 0.92f));
            AddPanelBack(refs.bottomPanel, new Color(0.10f, 0.10f, 0.12f, 0.96f));

            BuildLeft(refs);
            BuildCenter(refs);
            BuildRight(refs);
            BuildBottom(refs);

            refs.ShowInvestigationMode();
            refs.SetHintPanelVisible(false);

            return refs;
        }

        private static void BuildLeft(NodeSceneViewRefs refs)
        {
            refs.environmentImage = CreateImage("EnvironmentImage", refs.leftPanel, new Color(0.25f, 0.30f, 0.28f, 1f));
            SetRect(refs.environmentImage.rectTransform,
                new Vector2(0f, 0.42f), new Vector2(1f, 1f),
                new Vector2(12f, -12f), new Vector2(-12f, -12f));

            refs.bossSilhouetteImage = CreateImage("BossSilhouetteImage", refs.leftPanel, new Color(0.07f, 0.07f, 0.07f, 0.95f));
            SetRect(refs.bossSilhouetteImage.rectTransform,
                new Vector2(0f, 0.12f), new Vector2(1f, 0.40f),
                new Vector2(12f, 0f), new Vector2(-12f, 0f));

            refs.bossNameText = CreateText("BossNameText", refs.leftPanel, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(refs.bossNameText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0.10f),
                new Vector2(12f, 8f), new Vector2(-12f, -8f));
            refs.bossNameText.text = "Unknown Target";
        }

        private static void BuildCenter(NodeSceneViewRefs refs)
        {
            refs.titleText = CreateText("TitleText", refs.centerPanel, 34, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(refs.titleText.rectTransform,
                new Vector2(0f, 0.88f), new Vector2(1f, 1f),
                new Vector2(18f, -8f), new Vector2(-18f, -8f));
            refs.titleText.text = "Investigation";

            refs.descriptionText = CreateText("DescriptionText", refs.centerPanel, 28, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(refs.descriptionText.rectTransform,
                new Vector2(0f, 0.24f), new Vector2(1f, 0.88f),
                new Vector2(18f, 0f), new Vector2(-18f, -8f));
            refs.descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            refs.descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            refs.descriptionText.text = "Narrative";

            refs.resultText = CreateText("ResultText", refs.centerPanel, 22, FontStyle.Italic, TextAnchor.UpperLeft);
            SetRect(refs.resultText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0.22f),
                new Vector2(18f, 8f), new Vector2(-18f, -8f));
            refs.resultText.color = new Color(0.84f, 0.84f, 0.72f, 1f);
            refs.resultText.text = "";
        }

        private static void BuildRight(NodeSceneViewRefs refs)
        {
            const int choiceCount = 4;
            const float top = 0.84f;
            const float height = 0.15f;
            const float gap = 0.035f;

            for (int i = 0; i < choiceCount; i++)
            {
                float yMax = top - ((height + gap) * i);
                float yMin = yMax - height;

                Button btn = CreateButton("ChoiceButton_" + i, refs.rightPanel, "Choice " + (i + 1));
                RectTransform rt = btn.GetComponent<RectTransform>();
                SetRect(rt,
                    new Vector2(0f, yMin), new Vector2(1f, yMax),
                    new Vector2(12f, 0f), new Vector2(-12f, 0f));

                Text txt = btn.GetComponentInChildren<Text>();
                txt.fontSize = 22;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = Color.white;

                refs.choiceButtons[i] = btn;
                refs.choiceButtonTexts[i] = txt;
            }
        }

        private static void BuildBottom(NodeSceneViewRefs refs)
        {
            refs.progressText = CreateText("ProgressText", refs.bottomPanel, 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(refs.progressText.rectTransform,
                new Vector2(0f, 0.52f), new Vector2(0.42f, 1f),
                new Vector2(16f, -6f), new Vector2(-6f, -6f));
            refs.progressText.text = "● ○ ○ ○ ○";

            refs.hintButton = CreateButton("HintButton", refs.bottomPanel, "힌트");
            SetRect(refs.hintButton.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(0.12f, 0.42f),
                new Vector2(16f, 6f), new Vector2(-6f, -8f));
            refs.hintButton.GetComponentInChildren<Text>().fontSize = 22;

            refs.hintPanel = CreatePanel("HintPanel", refs.bottomPanel, new Color(0.18f, 0.18f, 0.20f, 0.98f));
            RectTransform hintRt = refs.hintPanel.GetComponent<RectTransform>();
            SetRect(hintRt,
                new Vector2(0.14f, 0f), new Vector2(0.52f, 0.42f),
                new Vector2(0f, 6f), new Vector2(0f, -8f));

            refs.hintText = CreateText("HintText", hintRt, 20, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(refs.hintText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(10f, 10f), new Vector2(-10f, -10f));
            refs.hintText.text = "힌트 없음";

            refs.nextButton = CreateButton("NextButton", refs.bottomPanel, "다음");
            SetRect(refs.nextButton.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0f), new Vector2(0.66f, 0.42f),
                new Vector2(0f, 6f), new Vector2(-6f, -8f));
            refs.nextButtonText = refs.nextButton.GetComponentInChildren<Text>();
            refs.nextButtonText.fontSize = 22;

            refs.submitDeductionButton = CreateButton("SubmitDeductionButton", refs.bottomPanel, "추리 제출");
            SetRect(refs.submitDeductionButton.GetComponent<RectTransform>(),
                new Vector2(0.68f, 0f), new Vector2(0.82f, 0.42f),
                new Vector2(0f, 6f), new Vector2(-6f, -8f));
            refs.submitDeductionButtonText = refs.submitDeductionButton.GetComponentInChildren<Text>();
            refs.submitDeductionButtonText.fontSize = 20;

            CreateLabel("WeaknessLabel", refs.bottomPanel, "약점",
                new Vector2(0.00f, 0.00f), new Vector2(0.12f, 0.20f),
                new Vector2(520f, 6f), new Vector2(0f, -8f));

            CreateLabel("PlaceLabel", refs.bottomPanel, "장소",
                new Vector2(0.00f, 0.22f), new Vector2(0.12f, 0.42f),
                new Vector2(520f, 0f), new Vector2(0f, -6f));

            for (int i = 0; i < 3; i++)
            {
                refs.weaknessButtons[i] = CreateButton("WeaknessButton_" + i, refs.bottomPanel, "약점 " + (i + 1));
                SetRect(refs.weaknessButtons[i].GetComponent<RectTransform>(),
                    new Vector2(0.14f + i * 0.12f, 0f), new Vector2(0.24f + i * 0.12f, 0.20f),
                    new Vector2(0f, 6f), new Vector2(-6f, -8f));

                refs.placeButtons[i] = CreateButton("PlaceButton_" + i, refs.bottomPanel, "장소 " + (i + 1));
                SetRect(refs.placeButtons[i].GetComponent<RectTransform>(),
                    new Vector2(0.14f + i * 0.12f, 0.22f), new Vector2(0.24f + i * 0.12f, 0.42f),
                    new Vector2(0f, 0f), new Vector2(-6f, -6f));
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            SetRect(rt, aMin, aMax, oMin, oMax);
            return rt;
        }

        private static void AddPanelBack(RectTransform rt, Color color)
        {
            Image img = rt.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            img.color = color;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.preserveAspect = true;
            return img;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.26f, 1f);

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.22f, 0.22f, 0.26f, 1f);
            cb.highlightedColor = new Color(0.30f, 0.30f, 0.35f, 1f);
            cb.pressedColor = new Color(0.16f, 0.16f, 0.20f, 1f);
            cb.selectedColor = cb.highlightedColor;
            btn.colors = cb;

            Text text = CreateText("Label", go.transform, 20, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.text = label;

            return btn;
        }

        private static Text CreateLabel(string name, Transform parent, string label, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            Text t = CreateText(name, parent, 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(t.rectTransform, aMin, aMax, oMin, oMax);
            t.text = label;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
        }
    }
}