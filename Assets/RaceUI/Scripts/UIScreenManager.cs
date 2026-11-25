using System.Collections.Generic;
using UnityEngine;

namespace RaceUI
{
    /// <summary>Shows one UIScreen at a time with a back stack. Screens are assigned in the Inspector.</summary>
    public sealed class UIScreenManager : MonoBehaviour
    {
        [SerializeField] private UIScreen[] screens;
        [SerializeField] private UIScreen startScreen;

        private readonly Stack<UIScreen> history = new Stack<UIScreen>();
        private UIScreen current;

        private void Start()
        {
            history.Clear();
            foreach (UIScreen screen in screens)
                if (screen != null && screen != startScreen) screen.Hide();
            Switch(startScreen, false, false);
        }

        public void Show(UIScreen screen) => Switch(screen, true, true);

        public void Back() => Switch(history.Count > 0 ? history.Pop() : startScreen, false, true);

        private void Switch(UIScreen target, bool remember, bool animate)
        {
            if (target == null || target == current) return;
            if (remember && current != null) history.Push(current);
            if (current != null) current.Hide();
            current = target;
            current.Show(animate);
        }
    }
}
