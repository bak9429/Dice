// Path: Assets/Script/UI/UIBuilder/HexAction/HexActionPanelRuntime.cs
using UnityEngine;

namespace UI.UIBuilder.HexAction
{
    /// <summary>
    /// HexActionPanelBuilder가 만든 패널의 표시/입력 상태를 한 곳에서 제어.
    /// </summary>
    public class HexActionPanelRuntime : MonoBehaviour
    {
        public CanvasGroup group;
        public RectTransform blockerRect; // 패널 전체 클릭을 막는 배경 (raycast target true)

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            HideImmediate();
        }

        public void Show()
        {
            if (group == null) return;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            if (blockerRect != null) blockerRect.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (group == null) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            if (blockerRect != null) blockerRect.gameObject.SetActive(false);
        }

        public void HideImmediate() => Hide();
    }
}
