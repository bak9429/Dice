// Path: Assets/Script/UI/UIControl/TurnRuntime.cs
using System;
using System.Reflection;
using UnityEngine;
using Rule.Core;
using Rule.Field;

namespace UI.UIControl
{
    public class TurnRuntime
    {
        private TurnController _tc;

        public bool IsReady => _tc != null;

        public bool TryBind()
        {
            if (_tc != null) return true;

            _tc = UnityEngine.Object.FindFirstObjectByType<TurnController>(FindObjectsInactive.Include);
            if (_tc != null)
            {
                Debug.Log("[TurnRuntime] Bound to TurnController");
                return true;
            }

            var go = new GameObject("TurnController");
            _tc = go.AddComponent<TurnController>();
            Debug.Log("[TurnRuntime] Auto-created TurnController");
            return _tc != null;
        }

        public void ApplyFieldRule(FieldRuleSO rule)
        {
            if (_tc == null || rule == null) return;

            // TurnController 내부에 FieldRuleSO를 담는 필드/프로퍼티를 찾아서 주입
            // 후보: fieldRule, fieldRuleSO, FieldRule, FieldRuleSO 등
            var t = _tc.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // 1) property
            foreach (var p in t.GetProperties(flags))
            {
                if (!p.CanWrite) continue;
                if (p.PropertyType != typeof(FieldRuleSO)) continue;

                var name = p.Name.ToLowerInvariant();
                if (name.Contains("field") && name.Contains("rule"))
                {
                    p.SetValue(_tc, rule);
                    Debug.Log($"[TurnRuntime] Applied FieldRuleSO via property '{p.Name}' id={rule.fieldId}");
                    return;
                }
            }

            // 2) field
            foreach (var f in t.GetFields(flags))
            {
                if (f.FieldType != typeof(FieldRuleSO)) continue;

                var name = f.Name.ToLowerInvariant();
                if (name.Contains("field") && name.Contains("rule"))
                {
                    f.SetValue(_tc, rule);
                    Debug.Log($"[TurnRuntime] Applied FieldRuleSO via field '{f.Name}' id={rule.fieldId}");
                    return;
                }
            }

            Debug.LogWarning("[TurnRuntime] Could not find FieldRuleSO slot in TurnController (name mismatch).");
        }

        public int GetAP()
        {
            if (_tc == null) return 0;
            // 현재 프로젝트 TurnController에 맞춰야 하는데, 최소로 리플렉션 fallback
            return ReadIntMember("CurrentAP", "currentAP", "ap", "AP");
        }

        public int GetMoveCost()
        {
            if (_tc == null) return 0;
            // TurnController가 FieldRuleSO.baseMoveCost를 직접 쓰는 구조가 아니면 0이 나올 수 있음.
            return ReadIntMember("MoveCost", "moveCost");
        }


        public int GetSpentThisTurn()
        {
            if (_tc == null) return 0;
            return ReadIntMember("SpentThisTurn", "spentThisTurn");
        }

        public void SetMoveCostOverride(int cost)
        {
            if (_tc == null) return;
            var m = _tc.GetType().GetMethod("SetMoveCostOverride", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m?.Invoke(_tc, new object[] { cost });
        }

        public bool TrySpendAP(int amount)
        {
            if (_tc == null) return false;

            var m = _tc.GetType().GetMethod("TrySpendAP", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null)
            {
                var r = m.Invoke(_tc, new object[] { amount });
                if (r is bool b) return b;
            }
            return false;
        }

        public void StartPlayerTurn()
        {
            if (_tc == null) return;

            var m = _tc.GetType().GetMethod("StartPlayerTurn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m?.Invoke(_tc, null);
            var pc = UnityEngine.Object.FindFirstObjectByType<Rule.Combat.Player.PlayerController>();
            pc?.ResetConsumableTurnUsage();
        }

        public void EndPlayerTurnWithCarry()
        {
            var tc = GetRawTurnController();
            if (tc == null) return;

            var m = tc.GetType().GetMethod("EndPlayerTurnWithCarry",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (m != null) m.Invoke(tc, null);
        }

        private int ReadIntMember(params string[] names)
        {
            if (_tc == null) return 0;

            var t = _tc.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var n in names)
            {
                var p = t.GetProperty(n, flags);
                if (p != null && p.PropertyType == typeof(int))
                    return (int)p.GetValue(_tc);

                var f = t.GetField(n, flags);
                if (f != null && f.FieldType == typeof(int))
                    return (int)f.GetValue(_tc);
            }
            return 0;
        }
        public object GetRawTurnController()
        {
            return _tc;
        }
    }
}
