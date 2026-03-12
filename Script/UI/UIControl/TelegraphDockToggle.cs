using UnityEngine;
using UnityEngine.UI;

namespace UI.UIControl
{
    public class TelegraphDockToggle : MonoBehaviour
    {
        public RectTransform root;
        public RectTransform body;
        public Button btnToggle;
        public Text txtToggle;

        [Header("Expanded Size")]
        public float expandedWidth = 220f;
        public float expandedHeight = 180f;

        [Header("Collapsed Size")]
        public float collapsedWidth = 34f;
        public float collapsedHeight = 34f;

        public bool collapsed = false;

        public void Init(RectTransform _root, RectTransform _body, Button _btn, Text _txt,
            float expW, float expH, float colW, float colH, bool startCollapsed)
        {
            root = _root;
            body = _body;
            btnToggle = _btn;
            txtToggle = _txt;

            expandedWidth = expW;
            expandedHeight = expH;
            collapsedWidth = colW;
            collapsedHeight = colH;

            collapsed = startCollapsed;

            if (btnToggle != null)
            {
                btnToggle.onClick.RemoveAllListeners();
                btnToggle.onClick.AddListener(Toggle);
            }

            Apply();
        }

        public void Toggle()
        {
            collapsed = !collapsed;
            Apply();
        }

        public void Apply()
        {
            if (root == null) return;

            if (body != null)
                body.gameObject.SetActive(!collapsed);

            float w = collapsed ? collapsedWidth : expandedWidth;
            float h = collapsed ? collapsedHeight : expandedHeight;

            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

            if (txtToggle != null)
                txtToggle.text = collapsed ? "◀" : "▶"; // 접힘: 펼치기(◀), 펼침: 접기(▶) (우측 상단이므로 방향 감각)
        }
    }
}
