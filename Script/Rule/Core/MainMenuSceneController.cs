// Path: Assets/Script/Rule/Core/MainMenuSceneController.cs
using UnityEngine;
using UI.UIBuilder;

namespace Rule.Core
{
    public class MainMenuSceneController : MonoBehaviour
    {
        private MainMenuBuilder.ViewRefs _view;

        private void Awake()
        {
            var _ = RunSession.Instance;
        }

        private void Start()
        {
            _view = MainMenuBuilder.Build();
            Bind();
        }

        private void Bind()
        {
            _view.newGameButton.onClick.AddListener(OnClickNewGame);
            _view.continueButton.onClick.AddListener(OnClickContinue);
            _view.settingsButton.onClick.AddListener(OnClickSettings);
            _view.quitButton.onClick.AddListener(OnClickQuit);
        }

        private void OnClickNewGame()
        {
            RunSession.Instance.ResetAll();
            SceneFlow.GoToHub();
        }

        private void OnClickContinue()
        {
            Debug.Log("[MainMenuSceneController] Continue is not implemented yet.");
        }

        private void OnClickSettings()
        {
            Debug.Log("[MainMenuSceneController] Settings is not implemented yet.");
        }

        private void OnClickQuit()
        {
            Debug.Log("[MainMenuSceneController] Quit requested.");
            Application.Quit();
        }
    }
}