using UnityEngine;
using UnityEngine.UI;

namespace UI.UIBuilder.HUD
{
    public class EndTurnDockToggle : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform bottomCombat; // 기준 패널 (우측하단)
        public RectTransform dock;         // 도킹 루트(우측하단 고정)
        public RectTransform content;      // EndTurn 들어있는 영역
        public Button toggleButton;
        public Text toggleLabel;

        [Header("Layout")]
        public float contentWidth = 180f;
        public float toggleWidth = 34f;
        public float height = 48f;

        [Header("Padding")]
        public float rightPad = 0f;    // ✅ 오른쪽 딱 붙이기
        public float bottomPad = 8f;

        public bool collapsed = true;

        public void Init(
            RectTransform _bottomCombat,
            RectTransform _dock,
            RectTransform _content,
            Button _toggleBtn,
            Text _toggleText,
            float _contentW,
            float _toggleW,
            float _h,
            float _rightPad,
            float _bottomPad,
            bool startCollapsed
        )
        {
            bottomCombat = _bottomCombat;
            dock = _dock;
            content = _content;
            toggleButton = _toggleBtn;
            toggleLabel = _toggleText;

            contentWidth = _contentW;
            toggleWidth = _toggleW;
            height = _h;

            rightPad = _rightPad;
            bottomPad = _bottomPad;

            collapsed = startCollapsed;

            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveAllListeners();
                toggleButton.onClick.AddListener(Toggle);
            }

            Apply();
        }

        public void Toggle()
        {
            collapsed = !collapsed;
            Apply();
        }

        private void Apply()
        {
            if (dock == null) return;

            // content on/off
            if (content != null)
                content.gameObject.SetActive(!collapsed);

            // ✅ dock 폭을 "오프셋"으로 정확히 제어 (겹침 방지)
            float w = collapsed ? toggleWidth : (toggleWidth + contentWidth);

            // 우측/하단 딱 붙이기: anchor=(1,0)-(1,0)
            dock.anchorMin = new Vector2(1, 0);
            dock.anchorMax = new Vector2(1, 0);
            dock.pivot = new Vector2(1, 0);

            // offsetMax가 ( -rightPad, bottomPad+height )가 되도록
            dock.offsetMax = new Vector2(-rightPad, bottomPad + height);
            dock.offsetMin = new Vector2(-(rightPad + w), bottomPad);

            if (toggleLabel != null)
                toggleLabel.text = collapsed ? "▶" : "◀";
        }
    }
}
