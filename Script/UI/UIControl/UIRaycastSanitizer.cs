// Path: Assets/Script/UI/UIControl/UIRaycastSanitizer.cs
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIControl
{
    /// <summary>
    /// UI 생성이 코드 기반일 때 자주 생기는 "투명 막(overlay)이 클릭을 먹는 문제"를 자동 완화.
    /// - Selectable(Button/Toggle/Slider/...)은 유지
    /// - 그 외 Graphic(Image/Text 등) 중 raycastTarget=true 인 것들은 기본적으로 false로 내림
    ///   (단, 이름에 "Blocker"가 들어가면 유지하도록 옵션 가능)
    /// </summary>
    public class UIRaycastSanitizer : MonoBehaviour
    {
        [Tooltip("시작할 때 한 번만 정리")]
        public bool sanitizeOnStart = true;

        [Tooltip("이름에 이 문자열이 포함되면 raycastTarget을 유지(예: 'Blocker' 'ModalBG')")]
        public string keepIfNameContains = "";

        [Tooltip("로그 출력")]
        public bool verboseLog = true;

        private void Start()
        {
            if (!sanitizeOnStart) return;
            Sanitize();
        }

        [ContextMenu("Sanitize Now")]
        public void Sanitize()
        {
            int changed = 0;
            int kept = 0;

            // Canvas 아래 모든 Graphic 가져오기
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[UIRaycastSanitizer] Canvas not found.");
                return;
            }

            var graphics = canvas.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g == null) continue;
                if (!g.raycastTarget) continue;

                // 클릭 가능한 UI는 유지
                if (g.GetComponentInParent<Selectable>(true) != null)
                {
                    kept++;
                    continue;
                }

                // 특정 이름 포함이면 유지(모달 배경 등 의도적으로 막고 싶은 경우)
                if (!string.IsNullOrEmpty(keepIfNameContains) && g.name.Contains(keepIfNameContains))
                {
                    kept++;
                    continue;
                }

                // 나머지 Graphic은 raycastTarget 끄기
                g.raycastTarget = false;
                changed++;

                if (verboseLog)
                    Debug.Log($"[UIRaycastSanitizer] raycastTarget OFF: {GetPath(g.transform)}");
            }

            Debug.Log($"[UIRaycastSanitizer] done. changed={changed}, kept={kept}");
        }

        private string GetPath(Transform t)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (t != null)
            {
                sb.Insert(0, "/" + t.name);
                t = t.parent;
            }
            return sb.ToString();
        }
    }
}
