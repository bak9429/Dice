// Path: Assets/Script/Rule/Core/GameFlowManager.cs
// MVP Flow Manager
// - hp 기반 사망 -> 허브(HubScene) -> 재도전(CombatScene) 루프
// - 장비/탄종/소모품 로직과 독립 (UI/전투 수치 반영은 나중)

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rule.Combat.Player;

namespace Rule.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Scenes")]
        public string hubSceneName = "HubScene";
        public string combatSceneName = "CombatScene";

        [Header("UI")]
        public int canvasSortOrder = 5000;

        private Canvas _canvas;
        private GameObject _panel;
        private Text _title;
        private Button _btnPrimary;
        private Button _btnSecondary;

        private bool _dead;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Ensure singleton exists even if no scene has it.
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
            _dead = false;
            EnsureOverlay();
            RefreshOverlayForScene(scene.name);
        }

        private void OnPlayerDied()
        {
            _dead = true;
            EnsureOverlay();

            ShowPanel(
                title: "DEFEATED",
                primaryText: "Return to Hub",
                primaryAction: () => LoadHub(),
                secondaryText: "Retry",
                secondaryAction: () => LoadCombat()
            );
        }

        private void RefreshOverlayForScene(string sceneName)
        {
            // 기본은 숨김. 허브에서는 'Start/Retry'만 보이게.
            if (sceneName == hubSceneName)
            {
                ShowPanel(
                    title: "HUB",
                    primaryText: "Enter Combat",
                    primaryAction: () => LoadCombat(),
                    secondaryText: "",
                    secondaryAction: null
                );
                return;
            }

            HidePanel();
        }

        private void LoadHub()
        {
            HidePanel();
            SceneManager.LoadScene(hubSceneName);
        }

        private void LoadCombat()
        {
            HidePanel();
            SceneManager.LoadScene(combatSceneName);
        }

        // ---------------- UI Overlay ----------------

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
            prt.sizeDelta = new Vector2(520, 260);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.65f);

            var cg = panel.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panel.transform, false);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.offsetMin = new Vector2(24, -80);
            trt.offsetMax = new Vector2(-24, -24);

            _title = titleGo.AddComponent<Text>();
            _title.font = font;
            _title.fontSize = 36;
            _title.alignment = TextAnchor.UpperLeft;
            _title.color = Color.white;
            _title.text = "";

            // Buttons
            _btnPrimary = BuildButton(panel.transform, "Primary", font, new Vector2(24, 24), new Vector2(248, 84));
            _btnSecondary = BuildButton(panel.transform, "Secondary", font, new Vector2(272, 24), new Vector2(472, 84));

            return panel;
        }

        private static Button BuildButton(Transform parent, string name, Font font, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
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

        private void ShowPanel(string title, string primaryText, Action primaryAction, string secondaryText, Action secondaryAction)
        {
            if (_panel == null) return;
            _panel.SetActive(true);

            if (_title != null) _title.text = title;

            ConfigureButton(_btnPrimary, primaryText, primaryAction, visible: true);
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
