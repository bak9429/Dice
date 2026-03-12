// Path: Assets/Script/UI/UIBuilder/UIRootRegistry.cs
using UnityEngine;

namespace UI.UIBuilder
{
    public static class UIRootRegistry
    {
        private static RectTransform _root;

        public static RectTransform Get()
        {
            if (_root != null) return _root;
            var go = GameObject.Find("UIRoot");
            if (go != null) _root = go.GetComponent<RectTransform>();
            return _root;
        }

        public static void Set(RectTransform root)
        {
            _root = root;
        }
    }
}
