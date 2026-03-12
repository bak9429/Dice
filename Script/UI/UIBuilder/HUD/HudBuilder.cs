// Path: Assets/Script/UI/UIBuilder/HUD/HudBuilder.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI.UIBuilder.CombatLayout;

namespace UI.UIBuilder.HUD
{
    public class HudBuilder : MonoBehaviour
    {
        [Header("Bootstrap")]
        public int maxWaitFrames = 30;

        [Header("Common")]
        public int padding = 12;
        public int smallFontSize = 14;
        public int valueFontSize = 22;
        public int barHeight = 16;

        [Header("Portrait")]
        public int portraitSize = 64;

        [Header("Status (Dynamic)")]
        public int statusIconSize = 30;   // 너가 30으로 바꿨다 해서 기본도 30으로 맞춤
        public int statusSpacing = 6;

        [Header("End Turn (Right Panel)")]
        public int endTurnHeight = 52;
        public int endTurnMinWidth = 0;   // 0이면 stretch
        public int endTurnTopPadding = 10;

        private Font _font;

        private void Start()
        {
            _font = LoadBuiltinFont();
            if (_font == null)
            {
                Debug.LogError("[HudBuilder] Failed to load builtin font. (LegacyRuntime.ttf)");
                return;
            }
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
            Debug.LogError("[HudBuilder] CombatLayoutRefs not found. Ensure CombatLayoutBuilder is active.");
        }

        Font LoadBuiltinFont()
        {
            Font f = null;

            // 가장 보편적인 내장 폰트
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f != null) return f;

            // Unity 버전에 따라 존재할 수 있는 대안들
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f != null) return f;

            try { f = Resources.GetBuiltinResource<Font>("Tahoma.ttf"); } catch { }
            if (f != null) return f;

            Debug.LogError("[HudBuilder] Failed to load builtin font. Tried: LegacyRuntime.ttf, LegacyRuntime.ttf, Tahoma.ttf");
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

        public HudRefs BuildIfMissing(CombatLayoutRefs layoutRefs)
        {
            var existing = layoutRefs.root.Find("HUD");
            if (existing != null)
            {
                var r0 = existing.GetComponent<HudRefs>();
                if (r0 != null) return r0;
            }

            var hudRoot = CreatePanel(layoutRefs.root, "HUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hudRoot.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = true;

            var refs = hudRoot.gameObject.AddComponent<HudRefs>();

            BuildTopTurnInfo(layoutRefs.topCinematic, refs);
            BuildLeftPlayer(layoutRefs.leftInfo, refs);
            BuildRightBoss(layoutRefs.rightInfo, refs);

            // ✅ EndTurn은 BottomCombat이 아니라 RightInfo에 들어간다
            BuildEndTurnInBottomCombat(layoutRefs.bottomCombat, refs);

            return refs;
        }

        // ---------------- TOP: Turn / AP ----------------

        private void BuildTopTurnInfo(RectTransform topCinematic, HudRefs refs)
        {
            var content = topCinematic.Find("CinematicContent") as RectTransform;
            if (content == null) content = topCinematic;

            // Hit flash overlay (red for 0.5s on damage)
            // Placed under TurnInfo but above cinematic content.
            var flash = content.Find("HitFlash") as RectTransform;
            if (flash == null)
            {
                flash = CreatePanel(content, "HitFlash",
                    new Vector2(0, 0), new Vector2(1, 1),
                    Vector2.zero, Vector2.zero);
                var img = flash.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 0f, 0f, 0f);
                img.raycastTarget = false;
                refs.cinematicHitFlash = img;
            }
            else
            {
                refs.cinematicHitFlash = flash.GetComponent<Image>();
                if (refs.cinematicHitFlash == null)
                {
                    refs.cinematicHitFlash = flash.gameObject.AddComponent<Image>();
                    refs.cinematicHitFlash.raycastTarget = false;
                }
                refs.cinematicHitFlash.color = new Color(1f, 0f, 0f, 0f);
            }

            refs.turnInfoRoot = CreatePanel(
                content,
                "TurnInfo",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(padding, -(padding + 60)),
                new Vector2(padding + 220, -padding)
            );

            AddBg(refs.turnInfoRoot.gameObject, new Color(0, 0, 0, 0.22f));
            AddOutline(refs.turnInfoRoot.gameObject, new Color(0, 0, 0, 0.35f));

            var col = CreatePanel(refs.turnInfoRoot, "Col", Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-10, -8));
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = 4;
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            refs.turnText = CreateText(col, "TurnText", "TURN: --", smallFontSize, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.9f));
            refs.apText = CreateText(col, "APText", "AP: --", valueFontSize, TextAnchor.MiddleLeft, Color.white);
        }

        // ---------------- LEFT: Player ----------------

        private void BuildLeftPlayer(RectTransform leftInfo, HudRefs refs)
        {
            var root = EnsureContentColumn(leftInfo, "PlayerPanel");

            CreateText(root, "PlayerTitle", "PLAYER", smallFontSize + 2, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.9f));
            refs.playerPortrait = CreateIcon(root, "PlayerPortrait", portraitSize);

            refs.playerHpFill = CreateBar(root, "PlayerHP", "HP", barHeight);
            refs.playerHpText = CreateBarValueText(refs.playerHpFill, "HPValue", "--/--");
            refs.playerShieldFill = CreateBar(root, "PlayerShield", "SHIELD", barHeight);
            refs.playerShieldText = CreateBarValueText(refs.playerShieldFill, "HPValue", "--/--");
        }

        private Text CreateBarValueText(Image fillImg, string name, string initial)
        {
            if (fillImg == null) return null;

            // Fill -> BG -> Wrap
            var bg = fillImg.transform.parent as RectTransform;
            var wrap = bg != null ? bg.transform.parent as RectTransform : null;
            if (wrap == null) return null;

            var existing = wrap.Find(name) as RectTransform;
            if (existing != null)
            {
                var t0 = existing.GetComponent<Text>();
                if (t0 != null) return t0;
            }

            var go = new GameObject(name, typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            go.layer = uiLayer >= 0 ? uiLayer : 0;
            go.transform.SetParent(wrap, false);
            var rt = go.GetComponent<RectTransform>();
            // Position inside bar area (right aligned)
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, barHeight);

            var txt = go.AddComponent<Text>();
            txt.font = _font;
            txt.text = initial;
            txt.fontSize = smallFontSize;
            txt.alignment = TextAnchor.MiddleRight;
            txt.color = new Color(1, 1, 1, 0.92f);
            txt.raycastTarget = false;
            return txt;
        }

        // ---------------- RIGHT: Boss ----------------

        private void BuildRightBoss(RectTransform rightInfo, HudRefs refs)
        {
            var root = EnsureContentColumn(rightInfo, "BossPanel");

            CreateText(root, "BossTitle", "BOSS", smallFontSize + 2, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.9f));
            refs.bossPortrait = CreateIcon(root, "BossPortrait", portraitSize);

            refs.bossNameText = CreateText(root, "BossName", "Boss: --", valueFontSize, TextAnchor.MiddleLeft, Color.white);

            refs.bossHpFill = CreateBar(root, "BossHP", "HP", barHeight);
            refs.bossHpText = CreateBarValueText(refs.bossHpFill, "HPValue", "--/--");
            refs.bossShieldFill = CreateBar(root, "BossShield", "SHIELD", barHeight);
            refs.bossShieldText = CreateBarValueText(refs.bossShieldFill, "HPValue", "--/--");

            CreateText(root, "StatusLabel", "STATUS", smallFontSize, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.85f));
            refs.bossStatusIconsRoot = CreateStatusFlow(root, "StatusIconsFlow");
            CreateStatusPrefab(refs.bossStatusIconsRoot, "StatusPrefab");

            CreateText(root, "NextLabel", "NEXT", smallFontSize, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.85f));
            refs.nextPatternIcon = CreateIcon(root, "NextPatternIcon", 48);
        }

        // ✅ EndTurn: RightInfo에 별도 패널로 "하단 고정"
        private void BuildEndTurnInRight(RectTransform rightInfo, HudRefs refs)
        {
            // BossPanel을 찾아서 그 아래 "하단 고정 영역"을 만든다.
            var bossPanel = rightInfo.Find("BossPanel") as RectTransform;
            if (bossPanel == null) bossPanel = rightInfo;

            // BossPanel 내부에 EndTurnDock를 만들고, 아래쪽에 붙인다.
            var dock = bossPanel.Find("EndTurnDock") as RectTransform;
            if (dock == null)
            {
                dock = CreatePanel(bossPanel, "EndTurnDock",
                    new Vector2(0, 0), new Vector2(1, 0),
                    new Vector2(0, 0), new Vector2(0, endTurnHeight + endTurnTopPadding));

                // 이 dock가 레이아웃에 의해 밀려나지 않도록 LayoutElement를 붙여 "고정 높이"로
                var le = dock.gameObject.AddComponent<LayoutElement>();
                le.minHeight = endTurnHeight + endTurnTopPadding;
                le.preferredHeight = endTurnHeight + endTurnTopPadding;
            }

            // 버튼
            var btnHost = dock.Find("EndTurnHost") as RectTransform;
            if (btnHost == null)
            {
                btnHost = CreatePanel(dock, "EndTurnHost",
                    new Vector2(0, 0), new Vector2(1, 1),
                    new Vector2(0, endTurnTopPadding), new Vector2(0, 0));
            }

            refs.btnEndTurn = CreateButton(btnHost, "EndTurn", "End Turn",
                endTurnMinWidth <= 0 ? 0 : endTurnMinWidth,
                endTurnHeight,
                stretch: endTurnMinWidth <= 0);

            refs.btnEndTurn.onClick.AddListener(() => Debug.Log("[UI] End Turn"));
        }

        private void BuildEndTurnInBottomCombat(RectTransform bottomCombat, HudRefs refs)
        {
            if (bottomCombat == null)
            {
                Debug.LogError("[HudBuilder] bottomCombat is null (CombatLayoutRefs.bottomCombat).");
                return;
            }

            int endW = (endTurnMinWidth > 0) ? endTurnMinWidth : 180;
            int endH = (endTurnHeight > 0) ? endTurnHeight : 48;
            int toggleW = 34;

            float rightPad = 0f;                   // 오른쪽 딱 붙이기
            float bottomPad = Mathf.Max(0, padding);

            void DisableBgRaycast(GameObject go)
            {
                var img = go.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }

            // 1) Dock (우측하단 고정)
            var dock = bottomCombat.Find("EndTurnDock") as RectTransform;
            if (dock == null)
            {
                dock = CreatePanel(
                    bottomCombat,
                    "EndTurnDock",
                    new Vector2(1, 0), new Vector2(1, 0),
                    new Vector2(-(rightPad + toggleW), bottomPad),
                    new Vector2(-rightPad, bottomPad + endH)
                );
                dock.pivot = new Vector2(1, 0);

                AddBg(dock.gameObject, new Color(0, 0, 0, 0f));
                DisableBgRaycast(dock.gameObject);
            }
            else
            {
                dock.anchorMin = new Vector2(1, 0);
                dock.anchorMax = new Vector2(1, 0);
                dock.pivot = new Vector2(1, 0);
                dock.offsetMin = new Vector2(-(rightPad + toggleW), bottomPad);
                dock.offsetMax = new Vector2(-rightPad, bottomPad + endH);
                DisableBgRaycast(dock.gameObject);
            }

            // 2) Toggle 패널 (항상 오른쪽 끝)
            var toggle = dock.Find("Toggle") as RectTransform;
            if (toggle == null)
            {
                toggle = CreatePanel(
                    dock,
                    "Toggle",
                    new Vector2(1, 0), new Vector2(1, 1),
                    new Vector2(-toggleW, 0),
                    new Vector2(0, 0)
                );
                toggle.pivot = new Vector2(1, 0.5f);

                AddBg(toggle.gameObject, new Color(0, 0, 0, 0.15f));
                DisableBgRaycast(toggle.gameObject);
            }
            else
            {
                toggle.anchorMin = new Vector2(1, 0);
                toggle.anchorMax = new Vector2(1, 1);
                toggle.pivot = new Vector2(1, 0.5f);
                toggle.offsetMin = new Vector2(-toggleW, 0);
                toggle.offsetMax = new Vector2(0, 0);
                DisableBgRaycast(toggle.gameObject);
            }

            // 3) Content 패널 (토글 "왼쪽"에 딱 붙게, 오른쪽 기준)
            var content = dock.Find("Content") as RectTransform;
            if (content == null)
            {
                content = CreatePanel(
                    dock,
                    "Content",
                    new Vector2(1, 0), new Vector2(1, 1),
                    new Vector2(-(toggleW + endW), 0),
                    new Vector2(-toggleW, 0)
                );
                content.pivot = new Vector2(1, 0.5f);

                AddBg(content.gameObject, new Color(0, 0, 0, 0.15f));
                DisableBgRaycast(content.gameObject);
            }
            else
            {
                content.anchorMin = new Vector2(1, 0);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(1, 0.5f);
                content.offsetMin = new Vector2(-(toggleW + endW), 0);
                content.offsetMax = new Vector2(-toggleW, 0);
                DisableBgRaycast(content.gameObject);
            }

            // 4) Toggle 버튼
            var oldToggleBtn = toggle.Find("Btn");
            if (oldToggleBtn != null) Destroy(oldToggleBtn.gameObject);

            var toggleBtn = CreateButton(toggle, "Btn", "▶", toggleW, endH, stretch: true);
            var toggleText = toggleBtn.GetComponentInChildren<Text>(true);

            // 5) EndTurn 버튼은 content에 (stretch)
            if (refs.btnEndTurn != null)
            {
                refs.btnEndTurn.transform.SetParent(content, false);
                var rt = refs.btnEndTurn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                var oldEnd = content.Find("EndTurn");
                if (oldEnd != null) Destroy(oldEnd.gameObject);

                refs.btnEndTurn = CreateButton(content, "EndTurn", "End Turn", endW, endH, stretch: true);
                refs.btnEndTurn.onClick.AddListener(() => Debug.Log("[UI] End Turn"));
            }

            // 6) 토글 컴포넌트
            var toggler = dock.GetComponent<UI.UIBuilder.HUD.EndTurnDockToggle>();
            if (toggler == null) toggler = dock.gameObject.AddComponent<UI.UIBuilder.HUD.EndTurnDockToggle>();

            toggler.Init(
                bottomCombat,
                dock,
                content,
                toggleBtn,
                toggleText,
                endW,
                toggleW,
                endH,
                rightPad,
                bottomPad,
                startCollapsed: true
            );
        }

        private RectTransform CreateStatusFlow(RectTransform parent, string name)
        {
            var rt = CreatePanel(parent, name,
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, statusIconSize + 8));

            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = statusSpacing;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rt;
        }

        private void CreateStatusPrefab(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(statusIconSize, statusIconSize);

            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.08f);
            img.raycastTarget = false;

            var ol = go.GetComponent<Outline>();
            ol.effectDistance = new Vector2(1, -1);
            ol.effectColor = new Color(0, 0, 0, 0.35f);
        }

        // ---------------- Common helpers ----------------

        private RectTransform EnsureContentColumn(RectTransform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null) return existing;

            var rt = CreatePanel(parent, name, Vector2.zero, Vector2.one, new Vector2(padding, padding), new Vector2(-padding, -padding));

            AddBg(rt.gameObject, new Color(0, 0, 0, 0.12f));
            AddOutline(rt.gameObject, new Color(0, 0, 0, 0.35f));

            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = 8;
            v.padding = new RectOffset(10, 10, 10, 10);
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rt;
        }

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
            txt.font = _font;
            txt.text = text;
            txt.fontSize = size;
            txt.alignment = anchor;
            txt.color = color;
            txt.raycastTarget = false;

            return txt;
        }

        private Image CreateBar(RectTransform parent, string name, string label, int height)
        {
            var wrap = new GameObject(name, typeof(RectTransform));
            wrap.layer = LayerMask.NameToLayer("UI");
            wrap.transform.SetParent(parent, false);

            var wrapRt = wrap.GetComponent<RectTransform>();
            wrapRt.anchorMin = new Vector2(0, 0);
            wrapRt.anchorMax = new Vector2(1, 0);
            wrapRt.pivot = new Vector2(0.5f, 0);
            wrapRt.sizeDelta = new Vector2(0, height + 20);

            var lab = new GameObject("Label", typeof(RectTransform));
            lab.layer = LayerMask.NameToLayer("UI");
            lab.transform.SetParent(wrap.transform, false);

            var labRt = lab.GetComponent<RectTransform>();
            labRt.anchorMin = new Vector2(0, 1);
            labRt.anchorMax = new Vector2(1, 1);
            labRt.pivot = new Vector2(0.5f, 1);
            labRt.offsetMin = new Vector2(0, -18);
            labRt.offsetMax = new Vector2(0, 0);

            var labTxt = lab.AddComponent<Text>();
            labTxt.font = _font;
            labTxt.text = label;
            labTxt.fontSize = smallFontSize;
            labTxt.alignment = TextAnchor.MiddleLeft;
            labTxt.color = new Color(1, 1, 1, 0.8f);
            labTxt.raycastTarget = false;

            var bg = new GameObject("BG", typeof(RectTransform), typeof(Image), typeof(Outline));
            bg.layer = LayerMask.NameToLayer("UI");
            bg.transform.SetParent(wrap.transform, false);

            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0);
            bgRt.anchorMax = new Vector2(1, 0);
            bgRt.pivot = new Vector2(0.5f, 0);
            bgRt.offsetMin = new Vector2(0, 0);
            bgRt.offsetMax = new Vector2(0, height);

            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.35f);
            bgImg.raycastTarget = false;

            var ol = bg.GetComponent<Outline>();
            ol.effectDistance = new Vector2(1, -1);
            ol.effectColor = new Color(0, 0, 0, 0.35f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.layer = LayerMask.NameToLayer("UI");
            fill.transform.SetParent(bg.transform, false);

            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(1, 1, 1, 0.45f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            fillImg.raycastTarget = false;

            return fillImg;
        }

        private Image CreateIcon(RectTransform parent, string name, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.12f);
            img.raycastTarget = false;

            var ol = go.GetComponent<Outline>();
            ol.effectDistance = new Vector2(1, -1);
            ol.effectColor = new Color(0, 0, 0, 0.35f);

            return img;
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
            t.fontSize = smallFontSize + 2;
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
            outline.effectDistance = new Vector2(1, -1);
        }
    }
}
