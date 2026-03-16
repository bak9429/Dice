// Path: Assets/Scripts/Rule/Core/GateRunState.cs

using System.Collections.Generic;

namespace Rule.Core
{
    public class GateRunState
    {
        public string gateId;

        public List<string> midBossOrder = new List<string>();

        public int currentMidBossIndex;

        public BossType currentBossType;

        public string currentBossId;

        public GatePhase currentPhase;

        public List<string> bossDebuffs = new List<string>();

        public bool IsMidBossFinished()
        {
            return currentMidBossIndex >= midBossOrder.Count;
        }
    }
}