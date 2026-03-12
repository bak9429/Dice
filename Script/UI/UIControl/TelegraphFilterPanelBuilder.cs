using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIControl
{
    public class TelegraphFilterPanelBuilder
    {
        public class Refs
        {
            public RectTransform root;
            public RectTransform header;
            public RectTransform body;

            public Button btnToggle;
            public Text txtToggle;

            public RectTransform viewport; // RectMask2D 영역(=body)
            public RectTransform content;  // 버튼 컨테이너
            public SimpleVerticalScroll scroll;

            public List<Button> buttons = new();
            public List<Text> labels = new();
        }

        private static Font GetFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        public Refs BuildIfMissing(RectTransform bottomCombat)
        {
            var refs = new Refs();

            // Root dock
            var root = bottomCombat.Find("TelegraphFilterDock") as RectTransform;
            if (root == null)
            {
                var go = new GameObject("TelegraphFilterDock", typeof(RectTransform), typeof(Image));
                root = go.GetComponent<RectTransform>();
                root.SetParent(bottomCombat, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0.6f); // 디버그용 진하게
                img.raycastTarget = true;
            }

            root.gameObject.SetActive(true);
            root.localScale = Vector3.one;
            root.anchorMin = new Vector2(1, 1);
            root.anchorMax = new Vector2(1, 1);
            root.pivot = new Vector2(1, 1);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(220, 180);
            refs.root = root;

            // Header
            var header = root.Find("Header") as RectTransform;
            if (header == null)
            {
                var go = new GameObject("Header", typeof(RectTransform), typeof(Image));
                header = go.GetComponent<RectTransform>();
                header.SetParent(root, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0.25f);
                img.raycastTarget = true;
            }

            header.gameObject.SetActive(true);
            header.localScale = Vector3.one;
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, 30);
            header.anchoredPosition = Vector2.zero;
            refs.header = header;

            // Toggle button
            var toggleRt = header.Find("ToggleBtn") as RectTransform;
            if (toggleRt == null)
            {
                var go = new GameObject("ToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                toggleRt = go.GetComponent<RectTransform>();
                toggleRt.SetParent(header, false);
                go.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                var trt = txtGo.GetComponent<RectTransform>();
                trt.SetParent(toggleRt, false);
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                var t = txtGo.GetComponent<Text>();
                t.font = GetFont();
                t.text = "▶";
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;

                refs.btnToggle = go.GetComponent<Button>();
                refs.txtToggle = t;
            }
            else
            {
                refs.btnToggle = toggleRt.GetComponent<Button>();
                refs.txtToggle = toggleRt.GetComponentInChildren<Text>(true);
            }

            toggleRt.gameObject.SetActive(true);
            toggleRt.localScale = Vector3.one;
            toggleRt.anchorMin = new Vector2(1, 0);
            toggleRt.anchorMax = new Vector2(1, 1);
            toggleRt.pivot = new Vector2(1, 0.5f);
            toggleRt.sizeDelta = new Vector2(30, 0);
            toggleRt.anchoredPosition = Vector2.zero;

            // Body (=viewport) : RectMask2D로 마스킹
            var body = root.Find("Body") as RectTransform;
            if (body == null)
            {
                var go = new GameObject("Body", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                body = go.GetComponent<RectTransform>();
                body.SetParent(root, false);

                var img = go.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0.12f); // 바디도 살짝 보이게
                img.raycastTarget = true; // 휠 이벤트 받게
            }

            body.gameObject.SetActive(true);
            body.localScale = Vector3.one;
            body.anchorMin = new Vector2(0, 0);
            body.anchorMax = new Vector2(1, 1);
            body.offsetMin = new Vector2(6, 6);
            body.offsetMax = new Vector2(-6, -36);
            refs.body = body;

            refs.viewport = body;

            // Content (수동 배치)
            var content = body.Find("Content") as RectTransform;
            if (content == null)
            {
                var go = new GameObject("Content", typeof(RectTransform));
                content = go.GetComponent<RectTransform>();
                content.SetParent(body, false);
            }

            content.gameObject.SetActive(true);
            content.localScale = Vector3.one;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);
            refs.content = content;

            // Simple scroll
            var sc = body.GetComponent<SimpleVerticalScroll>();
            if (sc == null) sc = body.gameObject.AddComponent<SimpleVerticalScroll>();
            sc.Init(body, content);
            refs.scroll = sc;

            return refs;
        }

        public void RebuildButtons(Refs refs, List<string> labels)
        {
            foreach (var b in refs.buttons)
                if (b != null) Object.Destroy(b.gameObject);

            refs.buttons.Clear();
            refs.labels.Clear();

            if (labels == null) labels = new List<string>();

            const float BTN_H = 28f;
            const float GAP = 6f;
            const float TOP_PAD = 0f;

            // Content 높이를 수동 확정
            float totalH = TOP_PAD + labels.Count * BTN_H + Mathf.Max(0, labels.Count - 1) * GAP;
            refs.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalH);
            refs.content.anchoredPosition = Vector2.zero;

            float y = -TOP_PAD;

            for (int i = 0; i < labels.Count; i++)
            {
                var go = new GameObject($"Btn_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(refs.content, false);

                // ✅ 수동 배치 (반드시 보이게)
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(0, BTN_H);
                rt.anchoredPosition = new Vector2(0, y);

                y -= (BTN_H + GAP);

                var img = go.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0.25f);
                img.raycastTarget = true;

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                var trt = txtGo.GetComponent<RectTransform>();
                trt.SetParent(rt, false);
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                var t = txtGo.GetComponent<Text>();
                t.font = GetFont();
                t.text = labels[i];
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.raycastTarget = false;

                refs.buttons.Add(go.GetComponent<Button>());
                refs.labels.Add(t);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
