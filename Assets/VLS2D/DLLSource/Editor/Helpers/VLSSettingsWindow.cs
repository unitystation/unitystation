using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    public class VLSSettingsWindow : EditorWindow
    {
        [MenuItem("Window/VLS2D Settings")]
        static void Init()
        {
            VLSGlobals.LoadEditorPrefs();
            //VLSSettingsWindow window = 
            EditorWindow.GetWindow<VLSSettingsWindow>("VLS2D Globals");
        }

        void OnGUI()
        {
            int value = 0;
            System.Collections.Generic.List<string> layerOptions = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 31; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName.Length > 0)
                    layerOptions.Add(layerName);
            }

                EditorGUI.indentLevel++;
            {
                EditorGUILayout.LabelField("Default Settings");
                EditorGUI.indentLevel++;
                {
                    VLSGlobals.DEFAULT_LIGHT_LAYER = EditorGUILayout.LayerField("Light Layer", VLSGlobals.DEFAULT_LIGHT_LAYER);
                    VLSGlobals.DEFAULT_LIGHT_SHADOW_LAYER = EditorGUILayout.MaskField("Light ShadowLayer", (int)VLSGlobals.DEFAULT_LIGHT_SHADOW_LAYER, layerOptions.ToArray()); 
                    VLSGlobals.DEFAULT_LIGHT_SCALE = EditorGUILayout.FloatField("Scale", VLSGlobals.DEFAULT_LIGHT_SCALE);
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Debuging");
                EditorGUI.indentLevel++;
                {
                    value += (EditorGUILayout.Toggle("Geometry", VLSDebug.IsModeActive(VLSDebugMode.Geometry))) ? 1 : 0;
                    value += (EditorGUILayout.Toggle("Bounds", VLSDebug.IsModeActive(VLSDebugMode.Bounds))) ? 2 : 0;
                    value += (EditorGUILayout.Toggle("Raycasting", VLSDebug.IsModeActive(VLSDebugMode.Raycasting))) ? 4 : 0;
                }
                EditorGUI.indentLevel--;
            }
            //GUILayout.Label(System.Convert.ToString((VLSDebugMode)value));

            if (GUI.changed)
            {
                VLSDebug.SetDebugMode((VLSDebugMode)value);
                VLSGlobals.SaveEditorPrefs();
                SceneView.RepaintAll();
            }
        }
    }
}