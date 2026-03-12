using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.UIControl
{
    // RectMask2D + Content(자식 버튼들)을 수동 스크롤
    public class SimpleVerticalScroll : MonoBehaviour, IScrollHandler
    {
        public RectTransform viewport;   // 마스크 영역
        public RectTransform content;    // 실제 버튼들이 들어있는 컨테이너
        public float wheelSpeed = 40f;

        public void Init(RectTransform _viewport, RectTransform _content)
        {
            viewport = _viewport;
            content = _content;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (viewport == null || content == null) return;

            // wheel: 위로 스크롤하면 positive일 수 있음 → UI는 보통 아래로 내려가게(-)
            float delta = eventData.scrollDelta.y * wheelSpeed;

            var pos = content.anchoredPosition;
            pos.y -= delta;

            pos.y = Mathf.Clamp(pos.y, 0f, GetMaxScrollY());
            content.anchoredPosition = pos;
        }

        private float GetMaxScrollY()
        {
            float viewH = viewport.rect.height;
            float contentH = content.rect.height;
            return Mathf.Max(0f, contentH - viewH);
        }
    }
}
