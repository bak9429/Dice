// Path: Assets/Script/UI/UIBuilder/HexAction/HexActionSelectionBinder.cs
using UnityEngine;
using UI.UIBuilder.CombatLayout;
using Rule.Field;

namespace UI.UIBuilder.HexAction
{
    public class HexActionSelectionBinder : MonoBehaviour
    {
        private HexActionPanelBuilder _builder;
        private HexActionRefs _refs;
        private bool _shown;

        private void OnEnable()
        {
            HexSelectionController.OnHexSelected += HandleSelected;
            HexSelectionController.OnHexDeselected += HandleDeselected;
        }

        private void OnDisable()
        {
            HexSelectionController.OnHexSelected -= HandleSelected;
            HexSelectionController.OnHexDeselected -= HandleDeselected;
        }

        private void Start()
        {
            _builder = FindFirstObjectByType<HexActionPanelBuilder>();
        }

        private void HandleSelected(HexCoord coord, Vector3 worldPos)
        {
            EnsureRefs();
            if (_builder == null || _refs == null) return;

            if (!_shown)
            {
                _builder.Show(_refs);
                _shown = true;
            }
        }

        private void HandleDeselected()
        {
            if (_builder == null || _refs == null) return;

            if (_shown)
            {
                _builder.Hide(_refs);
                _shown = false;
            }
        }

        private void EnsureRefs()
        {
            if (_refs != null) return;

            var layout = FindFirstObjectByType<CombatLayoutRefs>();
            if (layout == null) return;

            _refs = FindFirstObjectByType<HexActionRefs>();
            if (_refs != null) return;

            if (_builder == null)
                _builder = FindFirstObjectByType<HexActionPanelBuilder>();

            if (_builder == null) return;

            _refs = _builder.BuildIfMissing(layout);
        }
    }
}
