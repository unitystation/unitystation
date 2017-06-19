using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomPropertyDrawer(typeof(VLSOverlaySettings))]
    public class VLSOverlaySettingsDrawer : PropertyDrawer
    {
        private GUIContent overlayLabel = new GUIContent("Enable Overlay", "");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUILayout.Space(-16);

            EditorGUILayout.PropertyField(property.FindPropertyRelative("enabled"), overlayLabel);
            if (property.FindPropertyRelative("enabled").boolValue)
            {
                EditorGUI.indentLevel++;
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("texture"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("intensity"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("scale"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("xScrollSpeed"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("yScrollSpeed"));
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
