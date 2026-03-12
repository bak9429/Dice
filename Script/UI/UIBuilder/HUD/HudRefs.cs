// Path: Assets/Script/UI/UIBuilder/HUD/HudRefs.cs
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIBuilder.HUD
{
    public class HudRefs : MonoBehaviour
    {
        [Header("Top (Cinematic) - Turn Info")]
        public RectTransform turnInfoRoot;
        public Text turnText;
        public Text apText;

        [Header("Left (Player)")]
        public Image playerPortrait;
        public Image playerHpFill;
        public Text playerHpText;
        public Image playerShieldFill;
        public Text playerShieldText;

        [Header("Top (Cinematic) - Hit Flash")]
        public Image cinematicHitFlash;

        [Header("Right (Boss)")]
        public Image bossPortrait;
        public Text bossNameText;
        public Image bossHpFill;
        public Text bossHpText;
        public Image bossShieldFill;
        public Text bossShieldText;
        public RectTransform bossStatusIconsRoot;
        public Image nextPatternIcon;

        [Header("Bottom (Combat)")]
        public Button btnEndTurn;
    }
}
