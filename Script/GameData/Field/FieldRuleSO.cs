// Path: Assets/Script/Rule/Field/FieldRuleSO.cs
using System.Collections.Generic;
using UnityEngine;
using Rule.Dice;

namespace Rule.Field
{
    [CreateAssetMenu(fileName = "FieldRule", menuName = "HexBoss/Field Rule")]
    public class FieldRuleSO : ScriptableObject
    {
        [Header("Identity")]
        public string fieldId;       // e.g. "F1"
        public string displayName;   // e.g. "Tutorial"

        [Header("Dice Rule")]
        public List<DiceTerm> diceTerms = new List<DiceTerm>();
        public int minClamp = 0;

        [Header("Movement")]
        public int baseMoveCost = 1;
    }
}
