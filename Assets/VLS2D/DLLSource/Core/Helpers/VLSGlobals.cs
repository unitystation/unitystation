using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSGlobals
    {
        [SerializeField]
        public static VLSDebugMode DEBUG_MODE = VLSDebugMode.Geometry;
        [SerializeField]
        public static LayerMask DEFAULT_LIGHT_LAYER = 0;
        [SerializeField]
        public static float DEFAULT_LIGHT_SCALE = 10;
        [SerializeField]
        public static LayerMask DEFAULT_LIGHT_SHADOW_LAYER = -1;

        public static void SaveEditorPrefs()
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetInt("VLS_DEBUG_MODE", (int)DEBUG_MODE);
            UnityEditor.EditorPrefs.SetInt("DEFAULT_LIGHT_LAYER", DEFAULT_LIGHT_LAYER);
            UnityEditor.EditorPrefs.SetFloat("DEFAULT_LIGHT_SCALE", DEFAULT_LIGHT_SCALE);
            UnityEditor.EditorPrefs.SetInt("DEFAULT_LIGHT_SHADOW_LAYER", DEFAULT_LIGHT_SHADOW_LAYER);
#endif
        }

        public static void LoadEditorPrefs()
        {
#if UNITY_EDITOR
            DEBUG_MODE = (VLSDebugMode)UnityEditor.EditorPrefs.GetInt("VLS_DEBUG_MODE", (int)VLSDebugMode.Geometry);
            DEFAULT_LIGHT_LAYER = UnityEditor.EditorPrefs.GetInt("DEFAULT_LIGHT_LAYER", 0);
            DEFAULT_LIGHT_SCALE = UnityEditor.EditorPrefs.GetFloat("DEFAULT_LIGHT_SCALE", 10);
            DEFAULT_LIGHT_SHADOW_LAYER = UnityEditor.EditorPrefs.GetInt("DEFAULT_LIGHT_SHADOW_LAYER", -1);
#endif
        }
        
    }
}