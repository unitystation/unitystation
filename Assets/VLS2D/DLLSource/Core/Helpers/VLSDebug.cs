using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSDebug
    {
        public static bool IsModeActive(VLSDebugMode _mode)
        {
            return ((VLSGlobals.DEBUG_MODE & _mode) != 0);
        }

        public static void AddDebugMode(VLSDebugMode _mode)
        {
            VLSGlobals.DEBUG_MODE |= _mode;
            VLSGlobals.SaveEditorPrefs();
        }

        public static void SetDebugMode(VLSDebugMode _mode)
        {
            VLSGlobals.DEBUG_MODE = _mode;
            VLSGlobals.SaveEditorPrefs();
        }

        public static void RemoveDebugMode(VLSDebugMode _mode)
        {
            VLSGlobals.DEBUG_MODE &= ~_mode;
            VLSGlobals.SaveEditorPrefs();
        }
    }
}