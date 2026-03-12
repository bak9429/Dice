// Path: Assets/Script/UI/UIControl/UIRaycastBlockerDebugger.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.UIControl
{
    public class UIRaycastBlockerDebugger : MonoBehaviour
    {
        [Tooltip("키를 누르는 동안 마우스 위치에서 Raycast를 쏴서 어떤 UI가 막는지 로그로 찍는다.")]
        public KeyCode debugKey = KeyCode.F8;

        private readonly List<RaycastResult> _hits = new();

        void Update()
        {
            if (!Input.GetKeyDown(debugKey)) return;

            var es = EventSystem.current;
            if (es == null)
            {
                Debug.LogWarning("[UIRaycastDbg] EventSystem.current is null");
                return;
            }

            var ped = new PointerEventData(es)
            {
                position = Input.mousePosition
            };

            _hits.Clear();
            es.RaycastAll(ped, _hits);

            Debug.Log($"[UIRaycastDbg] hits={_hits.Count} mouse={Input.mousePosition}");

            // 상단에 있는(=가장 먼저 먹는) 순서대로 찍힘
            for (int i = 0; i < _hits.Count && i < 15; i++)
            {
                var go = _hits[i].gameObject;
                Debug.Log($"  #{i} {go.name} path={GetPath(go.transform)}");
            }
        }

        private string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }
    }
}
