// Path: Assets/Script/UI/UIBuilder/HexAction/HexActionPanelBuilder.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI.UIBuilder.CombatLayout;

namespace UI.UIBuilder.HexAction
{
    // MVP: 패널 생성 + Show/Hide API만 제공 (기존 구조 유지)
    public class HexActionPanelBuilder : MonoBehaviour
    {
        [Header("Bootstrap")]
        public int maxWaitFrames = 30;

        [Header("Panel Layout")]
        public int panelWidth = 420;
        public int panelMaxHeight = 320;
        public int padding = 12;

        [Header("Row")]
        public int rowHeight = 48;
        public int rowSpacing = 8;

        private Font _font;

        private void Start()
        {
            _font = LoadBuiltinFont();
            Debug.Log($"[HexActionPanelBuilder] font={( _font ? _font.name : "NULL")}");
            if (_font == null) return;

            StartCoroutine(BuildWhenReady());
        }

        private IEnumerator BuildWhenReady()
        {
            for (int i = 0; i < maxWaitFrames; i++)
            {
                var layoutRefs = FindLayoutRefs();
                if (layoutRefs != null)
                {
                    BuildIfMissing(layoutRefs);
                    yield break;
                }
                yield return null;
            }
            Debug.LogError("[HexActionPanelBuilder] CombatLayoutRefs not found. Ensure CombatLayoutBuilder is active.");
        }

        private Font LoadBuiltinFont()
        {
            Font f = null;

            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f != null) return f;

            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f != null) return f;

            try { f = Resources.GetBuiltinResource<Font>("Tahoma.ttf"); } catch { }
            if (f != null) return f;

            Debug.LogError("[HexActionPanelBuilder] Failed to load builtin font. Tried: Arial.ttf, LegacyRuntime.ttf, Tahoma.ttf");
            return null;
        }

        private CombatLayoutRefs FindLayoutRefs()
        {
            var uiRoot = UI.UIBuilder.UIRootRegistry.Get();
            if (uiRoot == null) return null;

            var layout = uiRoot.Find("CombatLayout");
            if (layout == null) return null;

            return layout.GetComponent<CombatLayoutRefs>();
        }

        public HexActionRefs BuildIfMissing(CombatLayoutRefs layoutRefs)
        {
            var hudRoot = layoutRefs.root.Find("HUD") as RectTransform;
            var parent = hudRoot != null ? hudRoot : layoutRefs.root;

            var existing = parent.Find("HexActionPanel");
            if (existing != null)
            {
                var r0 = existing.GetComponent<HexActionRefs>();
                if (r0 != null) return r0;
            }

            var bottom = layoutRefs.bottomCombat;

            var panel = CreatePanel(bottom, "HexActionPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-panelWidth / 2, 16),
                new Vector2(panelWidth / 2, 16 + panelMaxHeight));

            AddBg(panel.gameObject, new Color(0, 0, 0, 0.22f), raycastTarget: true);
            AddOutline(panel.gameObject, new Color(0, 0, 0, 0.35f));

            var cg = panel.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            var refs = panel.gameObject.AddComponent<HexActionRefs>();
            refs.panelRoot = panel;
            refs.canvasGroup = cg;

            // Header
            var header = CreatePanel(panel, "Header",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(padding, -(padding + 40)),
                new Vector2(-padding, -padding));

            refs.titleText = CreateText(header, "Title", "ACTIONS", 16, TextAnchor.MiddleLeft, Color.white);

            var closeHost = CreatePanel(header, "CloseHost",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-44, -18), new Vector2(0, 18));

            refs.btnClose = CreateButton(closeHost, "Close", "X", 44, 36, stretch: true);
            refs.btnClose.onClick.AddListener(() => Hide(refs));

            // ===========================
            // ListRoot with ScrollRect
            // ===========================
            var listRoot = CreatePanel(panel, "ListRoot",
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(padding, padding),
                new Vector2(-padding, -(padding + 52)));

            var scroll = listRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            // Viewport
            var viewport = CreatePanel(listRoot, "Viewport",
                new Vector2(0, 0), new Vector2(1, 1),
                Vector2.zero, Vector2.zero);

            var viewportImg = viewport.gameObject.AddComponent<Image>();
            viewportImg.color = new Color(1, 1, 1, 0.02f); // mask용 거의 투명
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            scroll.viewport = viewport;

            // Content
            var content = CreatePanel(viewport, "Content",
                new Vector2(0, 1), new Vector2(1, 1),
                Vector2.zero, Vector2.zero);

            content.pivot = new Vector2(0.5f, 1f);

            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = rowSpacing;
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;

            // ✅ rows는 content 아래로 쌓이게
            refs.listRoot = content;

            // Row Prefab (inactive)
            refs.rowPrefab = CreateRowPrefab(refs.listRoot, "RowPrefab");
            refs.rowPrefab.gameObject.SetActive(false);

            // 기본 액션
            refs.btnMove = AddRow(refs, "Move", "Move to selected hex (AP cost)", icon: "M");
            refs.btnAttack = AddRow(refs, "Attack", "Melee attack (AP cost)", icon: "A");
            refs.btnDefend = AddRow(refs, "Defend", "Spend all remaining AP to reduce incoming damage", icon: "D");

            // Bullet/Consumable
            refs.btnBullet = AddRow(refs, "Bullet", "Open bullet list (no AP, ammo cost)", icon: "B");
            refs.btnConsumable = AddRow(refs, "Consumable", "Open consumable list (item cost)", icon: "C");

            return refs;
        }

        public void Show(HexActionRefs refs)
        {
            if (refs == null) return;
            refs.canvasGroup.alpha = 1f;
            refs.canvasGroup.interactable = true;
            refs.canvasGroup.blocksRaycasts = true;
        }

        public void Hide(HexActionRefs refs)
        {
            if (refs == null) return;
            refs.canvasGroup.alpha = 0f;
            refs.canvasGroup.interactable = false;
            refs.canvasGroup.blocksRaycasts = false;
        }

        // ----- Row building -----

        private RectTransform CreateRowPrefab(RectTransform parent, string name)
        {
            var row = CreatePanel(parent, name,
                new Vector2(0, 0), new Vector2(1, 0),
                Vector2.zero, new Vector2(0, rowHeight));

            AddBg(row.gameObject, new Color(1, 1, 1, 0.06f), raycastTarget: true);
            AddOutline(row.gameObject, new Color(0, 0, 0, 0.35f));

            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 10;
            h.padding = new RectOffset(10, 10, 8, 8);

            // ✅ 핵심: 가로폭을 LayoutGroup이 관리하게 해서 Text Wrap이 폭=0으로 죽는 문제를 막는다
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            // Icon (fixed)
            var icon = CreateIconText(row, "Icon", "?", 16, 26);
            icon.GetComponent<Image>().color = new Color(1, 1, 1, 0.10f);

            var iconLE = icon.gameObject.AddComponent<LayoutElement>();
            iconLE.minWidth = 26;
            iconLE.preferredWidth = 26;
            iconLE.flexibleWidth = 0;
            iconLE.minHeight = rowHeight;
            iconLE.flexibleHeight = 0;

            // Texts column (takes remaining width)
            var col = CreatePanel(row, "Texts",
                new Vector2(0, 0), new Vector2(1, 1),
                Vector2.zero, Vector2.zero);

            var colLE = col.gameObject.AddComponent<LayoutElement>();
            colLE.minWidth = 200;
            colLE.flexibleWidth = 1;
            colLE.minHeight = rowHeight;
            colLE.flexibleHeight = 0;

            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleLeft;
            v.spacing = 2;
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var nameT = CreateText(col, "Name", "Action", 16, TextAnchor.MiddleLeft, Color.white);
            var descT = CreateText(col, "Desc", "Description...", 12, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.75f));

            nameT.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameT.verticalOverflow = VerticalWrapMode.Truncate;

            descT.horizontalOverflow = HorizontalWrapMode.Wrap;
            descT.verticalOverflow = VerticalWrapMode.Truncate;

            var btn = row.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            return row;
        }
                private Button AddRow(HexActionRefs refs, string name, string desc, string icon)
        {
            var row = Instantiate(refs.rowPrefab, refs.listRoot);
            row.gameObject.name = $"Row_{name}";
            row.gameObject.SetActive(true);

            var iconRt = row.Find("Icon");
            if (iconRt != null)
            {
                var t = iconRt.GetComponentInChildren<Text>();
                if (t != null) t.text = icon;
            }

            var nameT = row.Find("Texts/Name")?.GetComponent<Text>();
            if (nameT != null) nameT.text = name;

            var descT = row.Find("Texts/Desc")?.GetComponent<Text>();
            if (descT != null) descT.text = desc;

            return row.GetComponent<Button>();
        }

        // ----- UI primitives -----

        private Text CreateText(RectTransform parent, string goName, string text, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, size + 10);

            var txt = go.AddComponent<Text>();
            txt.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = text;
            txt.fontSize = size;
            txt.alignment = anchor;
            txt.color = color;
            txt.raycastTarget = false;

            return txt;
        }

        private RectTransform CreateIconText(RectTransform parent, string name, string text, int fontSize, int size)
        {
            var rt = CreatePanel(parent, name,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, -size / 2), new Vector2(size, size / 2));

            AddBg(rt.gameObject, new Color(1, 1, 1, 0.08f), raycastTarget: false);
            AddOutline(rt.gameObject, new Color(0, 0, 0, 0.35f));

            var t = CreateText(rt, "Text", text, fontSize, TextAnchor.MiddleCenter, Color.white);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = Vector2.zero;

            return rt;
        }

        private Button CreateButton(RectTransform parent, string name, string label, int w, int h, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.sizeDelta = new Vector2(w, h);
            }

            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.14f);
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.layer = LayerMask.NameToLayer("UI");
            textGo.transform.SetParent(go.transform, false);

            var tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            var t = textGo.GetComponent<Text>();
            t.font = _font;
            t.text = label;
            t.fontSize = 16;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;

            var ol = go.GetComponent<Outline>();
            ol.effectDistance = new Vector2(1, -1);
            ol.effectColor = new Color(0, 0, 0, 0.35f);

            return btn;
        }

        private RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static void AddBg(GameObject go, Color c, bool raycastTarget)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = raycastTarget;
        }

        private static void AddOutline(GameObject go, Color c)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = c;
            outline.effectDistance = new Vector2(1, -1);
        }
    }
}