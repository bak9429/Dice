// Path: Assets/Script/Rule/Core/GateRunState.cs
using System.Collections.Generic;

namespace Rule.Core
{
    public class GateRunState
    {
        public string gateId = "";
        public string gateBossId = "";

        public List<string> midBossOrder = new List<string>();
        public List<string> bossDebuffs = new List<string>();

        public int currentMidBossIndex = 0;
        public int currentNodeIndex = 0;

        public BossType currentBossType = BossType.MidBoss;
        public GatePhase currentPhase = GatePhase.Investigation;

        public string currentBossId = "";

        public bool IsValidMidBossIndex()
        {
            return currentMidBossIndex >= 0 && currentMidBossIndex < midBossOrder.Count;
        }

        public bool IsAllMidBossCleared()
        {
            return currentMidBossIndex >= midBossOrder.Count;
        }
    }
}