// Path: Assets/Script/Rule/Core/SceneFlow.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rule.Core
{
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenuScene";
        public const string HubScene = "HubScene";
        public const string NodeScene = "NodeScene";
        public const string CombatScene = "CombatScene";

        public static void GoToMainMenu()
        {
            Load(MainMenuScene);
        }

        public static void GoToHub()
        {
            Load(HubScene);
        }

        public static void GoToNode()
        {
            Load(NodeScene);
        }

        public static void GoToCombat()
        {
            Load(CombatScene);
        }

        private static void Load(string sceneName)
        {
            Debug.Log($"[SceneFlow] LoadScene => {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}