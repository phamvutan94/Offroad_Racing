using UnityEngine;

namespace ALIyerEdon
{
    /// <summary>
    /// Keyboard reading that works in the Game view AND the Device Simulator.
    /// The Simulator only forwards the keyboard to the new Input System package, so read that first
    /// (when it is enabled in Player Settings > Active Input Handling) and fall back to the legacy Input Manager.
    /// </summary>
    public static class KeyInput
    {
        public static bool Held(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = global::UnityEngine.InputSystem.Keyboard.current;
            var k = ToKey(key);
            if (kb != null && k != global::UnityEngine.InputSystem.Key.None && kb[k].isPressed) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        public static bool Down(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = global::UnityEngine.InputSystem.Keyboard.current;
            var k = ToKey(key);
            if (kb != null && k != global::UnityEngine.InputSystem.Key.None && kb[k].wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool Up(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = global::UnityEngine.InputSystem.Keyboard.current;
            var k = ToKey(key);
            if (kb != null && k != global::UnityEngine.InputSystem.Key.None && kb[k].wasReleasedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyUp(key);
#else
            return false;
#endif
        }

        /// -1..1 like Input.GetAxisRaw("Vertical"): W / Up arrow vs S / Down arrow.
        public static float Vertical() => Axis(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow, "Vertical");

        /// -1..1 like Input.GetAxisRaw("Horizontal"): D / Right arrow vs A / Left arrow.
        public static float Horizontal() => Axis(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow, "Horizontal");

        static float Axis(KeyCode pos, KeyCode posAlt, KeyCode neg, KeyCode negAlt, string legacyAxis)
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Held(pos) || Held(posAlt)) v += 1f;
            if (Held(neg) || Held(negAlt)) v -= 1f;
            if (v != 0f) return v;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            v = Input.GetAxis(legacyAxis);
#endif
            return v;
        }

#if ENABLE_INPUT_SYSTEM
        static global::UnityEngine.InputSystem.Key ToKey(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.W: return global::UnityEngine.InputSystem.Key.W;
                case KeyCode.A: return global::UnityEngine.InputSystem.Key.A;
                case KeyCode.S: return global::UnityEngine.InputSystem.Key.S;
                case KeyCode.D: return global::UnityEngine.InputSystem.Key.D;
                case KeyCode.C: return global::UnityEngine.InputSystem.Key.C;
                case KeyCode.R: return global::UnityEngine.InputSystem.Key.R;
                case KeyCode.UpArrow: return global::UnityEngine.InputSystem.Key.UpArrow;
                case KeyCode.DownArrow: return global::UnityEngine.InputSystem.Key.DownArrow;
                case KeyCode.LeftArrow: return global::UnityEngine.InputSystem.Key.LeftArrow;
                case KeyCode.RightArrow: return global::UnityEngine.InputSystem.Key.RightArrow;
                case KeyCode.Space: return global::UnityEngine.InputSystem.Key.Space;
                case KeyCode.LeftShift: return global::UnityEngine.InputSystem.Key.LeftShift;
                case KeyCode.RightShift: return global::UnityEngine.InputSystem.Key.RightShift;
                case KeyCode.Escape: return global::UnityEngine.InputSystem.Key.Escape;
                case KeyCode.Return: return global::UnityEngine.InputSystem.Key.Enter;
                case KeyCode.P: return global::UnityEngine.InputSystem.Key.P;
                case KeyCode.N: return global::UnityEngine.InputSystem.Key.N;
                default: return global::UnityEngine.InputSystem.Key.None;
            }
        }
#endif
    }
}
