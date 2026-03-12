// Path: Assets/Script/GameData/Boss/BossDefSO.cs
using System.Collections.Generic;
using UnityEngine;

namespace GameData.Boss
{
    [CreateAssetMenu(fileName = "BossDef", menuName = "HexBoss/Boss Def")]
    public class BossDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string bossId = "BOSS_01";
        public string displayName = "Test Boss";

        [Header("Field")]
        public string fieldId = "F1";

        [Header("Spawn (Axial)")]
        public Vector2Int spawnAxial = new Vector2Int(0, 0);

        [Header("World Sprite (Resources path)")]
        public string worldSpriteResource = "";  // e.g. "Sprites/Boss/TestBoss"
        public int worldSortingOrder = 50;

        [Header("UI / Cutscene (Resources path or name)")]
        public string uiPortraitResource = "";   // e.g. "UI/BossPortraits/TestBoss"
        public string cinematicResource = "";    // e.g. "Video/TestBossIntro" or "Images/TestBossIntro"

        [Header("Stats")]
        public int maxHp = 100;

        [Tooltip("보스 쉴드(=그로기 게이지) 최대치")]
        public int maxShield = 50;

        [Header("Groggy (Shield Break)")]
        [Tooltip("shield가 0이 되면 그로기. 그로기 지속 턴(플레이어 턴 기준)")]
        public int groggyTurns = 1;

        [Tooltip("그로기 중 받는 피해 배수")]
        public float groggyDamageMul = 1.5f;

        [Tooltip("그로기 종료 시 쉴드를 몇 %로 회복할지(1=100%)")]
        [Range(0f, 1f)]
        public float groggyRecoverShieldRatio = 1.0f;

        [Tooltip("(MVP) 보스 패턴이 플레이어를 HIT 했을 때 적용할 기본 피해량")]
        public int damagePerHit = 5;

        [Header("Player Shield(체간) Damage (Boss -> Player)")]
        [Tooltip("보스 패턴이 플레이어 쉴드(체간)에 주는 기본 피해량. 패턴별 override 없을 때 사용")]
        public int playerShieldDamagePerHit = 5;

        [Tooltip("패턴별로 (HP / Shield) 데미지를 override 한다. patternKey는 intent.title(패턴 키)와 동일해야 함")]
        public List<BossPatternDamageOverride> patternDamageOverrides = new List<BossPatternDamageOverride>();

        [Header("AI Type (proto)")]
        public string aiType = "Composite"; // Evade / Charge / Composite ...

        [Header("Phases")]
        public List<BossPhaseDef> phases = new List<BossPhaseDef>();

        [Header("Turn FX (Telegraph/Execute Timing)")]
        [Tooltip("보스턴 시작 후, 예고(노란색)가 최소로 보이는 시간")]
        public float telegraphMinShowSeconds = 0.25f;

        [Tooltip("패턴 실행들 사이의 간격(연출 템포)")]
        public float betweenPatternSeconds = 0.15f;

        [Header("Execute Highlight (Blink)")]
        public bool executeUseBlink = true;

        [Tooltip("Blink 횟수(ON/OFF 한 사이클을 1회로)")]
        public int executeBlinkCount = 2;

        [Tooltip("Blink ON 유지 시간")]
        public float executeBlinkOnSeconds = 0.18f;

        [Tooltip("Blink OFF 유지 시간")]
        public float executeBlinkOffSeconds = 0.10f;

        [Tooltip("Blink를 쓰지 않을 때, 단발 플래시 시간")]
        public float executeFlashSeconds = 0.35f;

        [Header("Overlay Palette Override (per boss)")]
        [Tooltip("보스별로 오버레이 색상(예고/위험/실행)을 덮어쓸지")]
        public bool overrideOverlayColors = true;

        [Tooltip("예고(telegraph) 오버레이 색상")]
        public Color telegraphOverlayColor = new Color(1.0f, 0.85f, 0.2f, 0.36f);

        [Tooltip("위험(danger) 오버레이 색상")]
        public Color dangerOverlayColor = new Color(1.0f, 0.2f, 0.2f, 0.32f);

        [Tooltip("실행(execute) 오버레이 색상")]
        public Color executeOverlayColor = new Color(1.0f, 0.12f, 0.12f, 0.55f);
    }

    [System.Serializable]
    public class BossPhaseDef
    {
        public string phaseId = "P1";
        public int hpThreshold = 0; // (proto) 0이면 항상
        public List<string> patternKeys = new List<string>();
    }

    // ✅ BossDefSO 내에서 패턴별 플레이어 데미지(HP/체간) 오버라이드용
    [System.Serializable]
    public class BossPatternDamageOverride
    {
        [Tooltip("BossPatterns key. intent.title(=patternKey)와 동일해야 함")]
        public string patternKey = "RingAOE";

        [Tooltip("플레이어 HP 데미지")]
        public int hpDamage = 5;

        [Tooltip("플레이어 체간(Shield) 데미지")]
        public int shieldDamage = 5;
    }
}
