// Path: Assets/Script/UI/UIBuilder/UIEventSystemBootstrap.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.UIBuilder
{
    public class UIEventSystemBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(es);
        }
    }
}
