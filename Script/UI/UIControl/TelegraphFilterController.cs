using System.Collections.Generic;
using UnityEngine;
using Rule.Field;
using Rule.Combat.Boss;

namespace UI.UIControl
{
    public class TelegraphFilterController : MonoBehaviour
    {
        private TelegraphFilterPanelBuilder _builder = new();
        private TelegraphFilterPanelBuilder.Refs _refs;

        private HexGridBuilder _grid;
        private TelegraphOverlayApplier _applier;

        private BossController _boss;
        private List<TelegraphIntent> _intents = new();

        private int _filterIndex = -1; // -1: 전체
        private bool _inited;

        private TelegraphDockToggle _toggle;
        private TelegraphDockDragger _dragger;

        private int _lastVersion = -1;

        public void Init(RectTransform bottomCombat)
        {
            if (_inited) return;
            _inited = true;

            _refs = _builder.BuildIfMissing(bottomCombat);

            _refs.root.gameObject.SetActive(true);
            _refs.body.gameObject.SetActive(true);

            _grid = FindFirstObjectByType<HexGridBuilder>(FindObjectsInactive.Include);
            _applier = new TelegraphOverlayApplier(_grid);

            _boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);

            _toggle = _refs.root.GetComponent<TelegraphDockToggle>();
            if (_toggle == null) _toggle = _refs.root.gameObject.AddComponent<TelegraphDockToggle>();

            _toggle.Init(
                _refs.root,
                _refs.body,
                _refs.btnToggle,
                _refs.txtToggle,
                expW: 220f,
                expH: 180f,
                colW: 34f,
                colH: 34f,
                startCollapsed: false
            );

            _dragger = _refs.header.GetComponent<TelegraphDockDragger>();
            if (_dragger == null) _dragger = _refs.header.gameObject.AddComponent<TelegraphDockDragger>();
            _dragger.Init(_refs.root, bottomCombat);

            Debug.Log("[TelegraphUI] Init OK.");
            RefreshFromBoss(force: true);
        }

        private void Update()
        {
            if (!_inited) return;

            if (_boss == null)
                _boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);

            if (_boss == null) return;

            int v = _boss.TelegraphVersion;
            if (v != _lastVersion)
                RefreshFromBoss(force: true);
        }

        public void RefreshFromBoss(bool force = false)
        {
            if (_boss == null)
                _boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);

            int v = _boss != null ? _boss.TelegraphVersion : -1;
            if (!force && v == _lastVersion) return;
            _lastVersion = v;

            _intents = _boss != null ? _boss.GetTelegraphsSnapshot() : new List<TelegraphIntent>();

            var labels = new List<string> { "전체" };
            for (int i = 0; i < _intents.Count; i++)
                labels.Add((i + 1).ToString());

            Debug.Log($"[TelegraphUI] Refresh. intents={_intents.Count}, version={_lastVersion}");

            _builder.RebuildButtons(_refs, labels);

            for (int i = 0; i < _refs.buttons.Count; i++)
            {
                int idx = i;
                _refs.buttons[i].onClick.RemoveAllListeners();
                _refs.buttons[i].onClick.AddListener(() =>
                {
                    _filterIndex = (idx == 0) ? -1 : (idx - 1);
                    Debug.Log($"[TelegraphUI] Filter set: {_filterIndex}");
                    Apply();
                });
            }

            Apply();
        }

        private void Apply()
        {
            if (_applier == null) return;
            _applier.Apply(_intents, _filterIndex);
        }
    }
}
