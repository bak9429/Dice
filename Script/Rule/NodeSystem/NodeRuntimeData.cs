// Path: Assets/Script/Rule/NodeSystem/NodeRuntimeData.cs
using System.Collections.Generic;

namespace Rule.NodeSystem
{
    public sealed class NodeRuntimeData
    {
        public int nodeIndex = 0;
        public bool isDeductionNode = false;

        public string title = "";
        public string description = "";
        public string locationId = "";

        public readonly List<string> choiceTexts = new List<string>();
        public readonly List<string> collectedHintTexts = new List<string>();

        public string resultPreviewText = "";
    }
}