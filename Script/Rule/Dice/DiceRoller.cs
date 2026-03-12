// Path: Assets/Script/Rule/Dice/DiceRoller.cs
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Rule.Dice
{
    public static class DiceRoller
    {
        /// <summary>
        /// FieldRuleSO가 없거나 구조가 예상과 다르면 안전하게 2d6로 fallback한다.
        /// (프로토 단계에서 데이터 미세팅으로 게임이 죽지 않게 하기 위함)
        /// </summary>
        public static int Roll(object fieldRuleSo)
        {
            // ✅ fallback: 2d6
            if (fieldRuleSo == null)
            {
                int fb = RollNdM(2, 6);
                Debug.LogWarning($"[DiceRoller] FieldRuleSO is null -> fallback 2d6 = {fb}");
                return fb;
            }

            try
            {
                // 기대: fieldRuleSo.diceTerms (IEnumerable)
                // 각 term: sides(int), count(int)
                var t = fieldRuleSo.GetType();

                var diceTermsMember =
                    (MemberInfo)t.GetProperty("diceTerms", BindingFlags.Public | BindingFlags.Instance) ??
                    (MemberInfo)t.GetField("diceTerms", BindingFlags.Public | BindingFlags.Instance);

                object diceTermsObj = diceTermsMember switch
                {
                    PropertyInfo pi => pi.GetValue(fieldRuleSo),
                    FieldInfo fi => fi.GetValue(fieldRuleSo),
                    _ => null
                };

                if (diceTermsObj is not IEnumerable enumerable)
                {
                    int fb = RollNdM(2, 6);
                    Debug.LogWarning($"[DiceRoller] diceTerms not found on {t.Name} -> fallback 2d6 = {fb}");
                    return fb;
                }

                int sum = 0;
                int termCount = 0;

                foreach (var term in enumerable)
                {
                    if (term == null) continue;
                    var tt = term.GetType();

                    int sides = ReadInt(tt, term, "sides");
                    int count = ReadInt(tt, term, "count");

                    if (sides <= 0 || count <= 0) continue;

                    sum += RollNdM(count, sides);
                    termCount++;
                }

                if (termCount == 0)
                {
                    int fb = RollNdM(2, 6);
                    Debug.LogWarning($"[DiceRoller] diceTerms empty/invalid -> fallback 2d6 = {fb}");
                    return fb;
                }

                return sum;
            }
            catch (Exception e)
            {
                int fb = RollNdM(2, 6);
                Debug.LogWarning($"[DiceRoller] exception -> fallback 2d6 = {fb}\n{e}");
                return fb;
            }
        }

        public static int GetMaxValue(object fieldRuleSo)
        {
            // fallback: 2d6
            if (fieldRuleSo == null)
                return 12;

            try
            {
                var t = fieldRuleSo.GetType();

                var diceTermsMember =
                    (MemberInfo)t.GetProperty("diceTerms", BindingFlags.Public | BindingFlags.Instance) ??
                    (MemberInfo)t.GetField("diceTerms", BindingFlags.Public | BindingFlags.Instance);

                object diceTermsObj = diceTermsMember switch
                {
                    PropertyInfo pi => pi.GetValue(fieldRuleSo),
                    FieldInfo fi => fi.GetValue(fieldRuleSo),
                    _ => null
                };

                if (diceTermsObj is not IEnumerable enumerable)
                    return 12; // fallback 2d6 max

                int max = 0;
                int termCount = 0;

                foreach (var term in enumerable)
                {
                    if (term == null) continue;
                    var tt = term.GetType();

                    int sides = ReadInt(tt, term, "sides");
                    int count = ReadInt(tt, term, "count");

                    if (sides <= 0 || count <= 0) continue;

                    max += count * sides;   // 🔥 최대값 계산
                    termCount++;
                }

                if (termCount == 0)
                    return 12;

                return max;
            }
            catch
            {
                return 12;
            }
        }


        private static int ReadInt(Type tt, object obj, string name)
        {
            var p = tt.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(obj);

            var f = tt.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(obj);

            return 0;
        }

        private static int RollNdM(int n, int sides)
        {
            int s = 0;
            for (int i = 0; i < n; i++)
                s += UnityEngine.Random.Range(1, sides + 1);
            return s;
        }
    }
}
