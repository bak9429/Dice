// Path: Assets/Script/Rule/Core/HubSceneController.cs
using UnityEngine;
using UI.UIBuilder;

namespace Rule.Core
{
    public class HubSceneController : MonoBehaviour
    {
        [SerializeField] private string defaultGateId = "Gate01";

        private HubSceneBuilder.ViewRefs _view;

        private void Awake()
        {
            var _ = RunSession.Instance;
        }

        private void Start()
        {
            _view = HubSceneBuilder.Build();
            Bind();
            RefreshStatus();
        }

        private void Bind()
        {
            _view.enterGateButton.onClick.AddListener(OnClickEnterGate);
            _view.statsButton.onClick.AddListener(OnClickStats);
            _view.equipButton.onClick.AddListener(OnClickEquip);
            _view.shopButton.onClick.AddListener(OnClickShop);
            _view.backButton.onClick.AddListener(OnClickBack);
        }

        private void RefreshStatus()
        {
            var s = RunSession.Instance;
            _view.statusText.text =
                $"Gate: {(string.IsNullOrEmpty(s.CurrentGateId) ? "-" : s.CurrentGateId)}\n" +
                $"Run Currency: {s.RunCurrency}\n" +
                $"Drops: {s.AcquiredDrops.Count}\n" +
                $"Current Area: {s.CurrentAreaIndex + 1}";
        }

        private void OnClickEnterGate()
        {
            RunSession.Instance.BeginGate(defaultGateId);
            SceneFlow.GoToNode();
        }

        private void OnClickStats()
        {
            Debug.Log("[HubSceneController] Stats panel is not implemented yet.");
        }

        private void OnClickEquip()
        {
            Debug.Log("[HubSceneController] Equip panel is not implemented yet.");
        }

        private void OnClickShop()
        {
            Debug.Log("[HubSceneController] Shop panel is not implemented yet.");
        }

        private void OnClickBack()
        {
            SceneFlow.GoToMainMenu();
        }
    }
}