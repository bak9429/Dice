// Path: Assets/Script/Rule/Field/RuntimeFieldContext.cs
using UnityEngine;

namespace Rule.Field
{
    public class RuntimeFieldContext : MonoBehaviour
    {
        public string currentFieldId = "F1";
        public FieldRuleSO currentRule;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
