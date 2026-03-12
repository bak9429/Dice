// Path: Assets/Script/UI/UIControl/UIStateMachine.cs
using System;

namespace UI.UIControl
{
    public class UIStateMachine
    {
        public UIState Current { get; private set; } = UIState.Idle;

        public event Action<UIState, UIState> OnStateChanged;

        public void Set(UIState next)
        {
            if (next == Current) return;
            var prev = Current;
            Current = next;
            OnStateChanged?.Invoke(prev, next);
        }
    }
}
