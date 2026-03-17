// Path: Assets/Script/Rule/Core/GameFlowManager.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rule.Combat.Boss;
using Rule.Combat.Player;

namespace Rule.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Scenes")]
        public string hubSceneName = SceneFlow.HubScene;
        public string combatSceneName = SceneFlow.CombatScene;

        [Header("UI")]
        public int canvasSortOrder = 5000;

        private Canvas _canvas;
        private GameObject _panel;
        private Text _title;
        private Text _body;
        private Button _btnPrimary;
        private Button _btnSecondary;

        private bool _resolved;
        private bool _bossSeenAlive;
        private bool _showingResult;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
            if (existing != null) return;

            var go = new GameObject("GameFlowManager");
            DontDestroyOnLoad(go);
            go.AddComponent<GameFlowManager>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            PlayerController.OnDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PlayerController.OnDied -= OnPlayerDied;
        }

        private void Start()
        {
            EnsureOverlay();
            RefreshOverlayForScene(SceneManager.GetActiveScene().name);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _resolved = false;
            _bossSeenAlive = false;
            _showingResult = false;

            EnsureOverlay();
            RefreshOverlayForScene(scene.name);
        }

        private void Update()
        {
            if (_resolved) return;
            if (_showingResult) return;
            if (SceneManager.GetActiveScene().name != combatSceneName) return;
            if (!RunSession.Instance.IsGateActive) return;

            var boss = BossController.Instance;
            if (boss == null) return;

            if (boss.IsAlive)
            {
                _bossSeenAlive = true;
                return;
            }

            if (_bossSeenAlive && !boss.IsAlive)
            {
                _resolved = true;
                RunSession.Instance.CompleteCombatVictory();
                ShowVictoryPanel();
            }
        }

        private void OnPlayerDied()
        {
            if (_resolved) return;
            if (SceneManager.GetActiveScene().name != combatSceneName) return;
            if (!RunSession.Instance.IsGateActive) return;

            _resolved = true;
            _showingResult = true;

            ShowPanel(
                title: "DEFEATED",
                body: "전투에서 패배했다.\n보유 중이던 런 재화를 모두 잃고 허브로 복귀한다.",
                primaryText: "Return to Hub",
                primaryAction: () => RunSession.Instance.CompleteCombatDefeat(),
                secondaryText: "",
                secondaryAction: null
            );
        }

        private void ShowVictoryPanel()
        {
            _showingResult = true;

            if (RunSession.Instance.IsGateBossPhase)
            {
                ShowPanel(
                    title: "GATE CLEARED",
                    body: "관문보스를 격파했다.\n허브로 복귀해 정비할 수 있다.",
                    primaryText: "Return to Hub",
                    primaryAction: () => RunSession.Instance.ContinueAfterVictory(),
                    secondaryText: "",
                    secondaryAction: null
                );
                return;
            }

            ShowPanel(
                title: "VICTORY",
                body: "중간보스를 격파했다.\n다음 조사로 진행하거나 허브로 복귀할 수 있다.",
                primaryText: "Next Investigation",
                primaryAction: () => RunSession.Instance.ContinueAfterVictory(),
                secondaryText: "Return to Hub",
                secondaryAction: () => RunSession.Instance.AbortRunToHub()
            );
        }

        private void RefreshOverlayForScene(string sceneName)
        {
            HidePanel();
        }

        private void EnsureOverlay()
        {
            if (_canvas != null) return;

            var canvasGo = new GameObject("FlowOverlay", typeof(RectTransform));
            DontDestroyOnLoad(canvasGo);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = canvasSortOrder;

            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = BuildPanel(canvasGo.transform);
            HidePanel();
        }

        private GameObject BuildPanel(Transform parent)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(620, 320);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.72f);

            var cg = panel.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panel.transform, false);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.offsetMin = new Vector2(24, -72);
            trt.offsetMax = new Vector2(-24, -24);

            _title = titleGo.AddComponent<Text>();
            _title.font = font;
            _title.fontSize = 34;
            _title.alignment = TextAnchor.UpperLeft;
            _title.color = Color.white;
            _title.text = "";

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(panel.transform, false);
            var brt = bodyGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 1);
            brt.offsetMin = new Vector2(24, 96);
            brt.offsetMax = new Vector2(-24, -92);

            _body = bodyGo.AddComponent<Text>();
            _body.font = font;
            _body.fontSize = 22;
            _body.alignment = TextAnchor.UpperLeft;
            _body.color = Color.white;
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            _body.text = "";

            _btnPrimary = BuildButton(panel.transform, "Primary", font, new Vector2(24, 24), new Vector2(288, 84));
            _btnSecondary = BuildButton(panel.transform, "Secondary", font, new Vector2(332, 24), new Vector2(596, 84));

            return panel;
        }

        private static Button BuildButton(Transform parent, string name, Font font, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.offsetMin = min;
            rt.offsetMax = max;

            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.12f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var txt = textGo.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = name;

            return btn;
        }

        private void ShowPanel(
            string title,
            string body,
            string primaryText,
            Action primaryAction,
            string secondaryText,
            Action secondaryAction)
        {
            if (_panel == null) return;

            _panel.SetActive(true);

            if (_title != null) _title.text = title;
            if (_body != null) _body.text = body;

            ConfigureButton(_btnPrimary, primaryText, primaryAction, visible: !string.IsNullOrEmpty(primaryText));
            ConfigureButton(_btnSecondary, secondaryText, secondaryAction, visible: !string.IsNullOrEmpty(secondaryText));
        }

        private void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private static void ConfigureButton(Button btn, string text, Action action, bool visible)
        {
            if (btn == null) return;

            btn.gameObject.SetActive(visible);
            btn.onClick.RemoveAllListeners();

            var txt = btn.GetComponentInChildren<Text>(true);
            if (txt != null) txt.text = text;

            if (action != null)
                btn.onClick.AddListener(() => action());
        }
    }
}