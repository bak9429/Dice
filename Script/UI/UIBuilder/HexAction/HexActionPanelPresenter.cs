// Path: Assets/Script/UI/UIBuilder/HexAction/HexActionPanelPresenter.cs
using System.Collections;
using UnityEngine;
using Rule.Field;

namespace UI.UIBuilder.HexAction
{
    /// <summary>
    /// IMPORTANT:
    ///  - This presenter MUST NOT create UI objects.
    ///  - HexActionPanelBuilder is the single source of UI creation.
    ///  - This component only binds to existing HexActionRefs and updates view text (e.g., selected tile label).
    ///
    /// Why:
    ///  - Prevents duplicate HexActionPanel creation (Builder vs Presenter).
    ///  - Keeps responsibilities clean for the next "ui-control-layer" branch.
    /// </summary>
    public class HexActionPanelPresenter : MonoBehaviour
    {
        [Header("Bind")]
        [Tooltip("How often to retry binding HexActionRefs if UI is not ready yet.")]
        public float retryInterval = 0.25f;

        [Header("Behavior")]
        [Tooltip("Update header title when a hex is selected.")]
        public bool updateTitleOnSelection = true;

        [Tooltip("Default title when nothing is selected.")]
        public string defaultTitle = "ACTIONS";

        private HexActionRefs _refs;

        private void OnEnable()
        {
            HexSelectionController.OnHexSelected += HandleSelected;
            HexSelectionController.OnHexDeselected += HandleDeselected;
            StartCoroutine(CoBindLoop());
        }

        private void OnDisable()
        {
            HexSelectionController.OnHexSelected -= HandleSelected;
            HexSelectionController.OnHexDeselected -= HandleDeselected;
        }

        private IEnumerator CoBindLoop()
        {
            // Keep trying until refs are found (UI may spawn after this object).
            while (true)
            {
                EnsureRefs();
                yield return new WaitForSecondsRealtime(retryInterval);
            }
        }

        private void EnsureRefs()
        {
            if (_refs != null) return;

            // Builder attaches HexActionRefs to the created "HexActionPanel" object.
            _refs = FindFirstObjectByType<HexActionRefs>(FindObjectsInactive.Include);

            if (_refs != null && _refs.titleText != null && !string.IsNullOrEmpty(defaultTitle))
            {
                // Initialize title once when bound
                _refs.titleText.text = defaultTitle;
                Debug.Log("[HexActionPanelPresenter] Bound HexActionRefs (no UI creation).");
            }
        }

        private void HandleSelected(HexCoord coord, Vector3 worldPos)
        {
            EnsureRefs();
            if (!updateTitleOnSelection) return;

            if (_refs == null || _refs.titleText == null)
            {
                Debug.LogWarning("[HexActionPanelPresenter] HexActionRefs/titleText not ready. (Builder not finished?)");
                return;
            }

            _refs.titleText.text = $"HEX {coord.q},{coord.r}";
        }

        private void HandleDeselected()
        {
            EnsureRefs();

            if (_refs == null || _refs.titleText == null) return;
            if (string.IsNullOrEmpty(defaultTitle)) return;

            _refs.titleText.text = defaultTitle;
        }
    }
}
