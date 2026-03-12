// Path: Assets/Script/UI/UIBuilder/HexAction/HexActionRefs.cs
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIBuilder.HexAction
{
    public class HexActionRefs : MonoBehaviour
    {
        [Header("Root")]
        public RectTransform panelRoot;
        public CanvasGroup canvasGroup;

        [Header("Header")]
        public Text titleText;
        public Button btnClose;

        [Header("List")]
        public RectTransform listRoot;

        [Header("Row Prefab")]
        public RectTransform rowPrefab;

        [Header("Quick Access")]
        public Button btnMove;
        public Button btnAttack;

        public Button btnDefend;     // ✅ NEW

        // Bullet / Consumable
        public Button btnBullet;
        public Button btnConsumable;
    }
}
