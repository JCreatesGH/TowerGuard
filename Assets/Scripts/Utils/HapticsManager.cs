using System.Runtime.InteropServices;
using UnityEngine;

namespace TowerGuard.Utils
{
    /// <summary>
    /// Thin wrapper around the iOS UIImpactFeedbackGenerator / UINotificationFeedbackGenerator
    /// APIs. Compiles on every platform but only actually triggers haptics on iOS hardware.
    /// Reads PlayerPrefs "haptics_on" — when 0 (off), every call is a no-op.
    /// </summary>
    public static class HapticsManager
    {
        private const string Pref = "haptics_on";

        private static bool Enabled => PlayerPrefs.GetInt(Pref, 1) == 1;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _TriggerHaptic(int type);
#else
        // Editor + non-iOS stub: no-op.
        private static void _TriggerHaptic(int type) { /* no haptic hardware */ }
#endif

        public static void Light()   { if (Enabled) _TriggerHaptic(0); }
        public static void Medium()  { if (Enabled) _TriggerHaptic(1); }
        public static void Heavy()   { if (Enabled) _TriggerHaptic(2); }
        public static void Success() { if (Enabled) _TriggerHaptic(3); }
        public static void Warning() { if (Enabled) _TriggerHaptic(4); }
    }
}
