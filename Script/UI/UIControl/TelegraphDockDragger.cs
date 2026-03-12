using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.UIControl
{
    // 헤더를 드래그하면 패널 Y 위치를 아래로 내릴 수 있음 (BottomCombat 내부에서만)
    public class TelegraphDockDragger : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform dock;
        public RectTransform clampArea; // bottomCombat
        private Vector2 _startDockPos;
        private Vector2 _startPointerLocal;

        public void Init(RectTransform _dock, RectTransform _clampArea)
        {
            dock = _dock;
            clampArea = _clampArea;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dock == null || clampArea == null) return;
            _startDockPos = dock.anchoredPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                clampArea, eventData.position, eventData.pressEventCamera, out _startPointerLocal);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dock == null || clampArea == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                clampArea, eventData.position, eventData.pressEventCamera, out var curLocal);

            var delta = curLocal - _startPointerLocal;

            // 우측 상단 도킹이므로 X는 고정(0), Y만 조절
            var pos = _startDockPos;
            pos.y += delta.y;

            // Y 클램프(너무 위/아래로 못 가게)
            float minY = -clampArea.rect.height + 40f;
            float maxY = -0f;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            dock.anchoredPosition = pos;
        }
    }
}
