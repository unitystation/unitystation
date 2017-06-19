using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomPropertyDrawer(typeof(VLSBlurSettings))]
    public class VLSBlurSettingsDrawer : PropertyDrawer
    {
        private GUIContent blurLabel = new GUIContent("Enable Blur", "");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUILayout.Space(-16);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("enabled"), blurLabel);
            if (property.FindPropertyRelative("enabled").boolValue)
            {
                EditorGUI.indentLevel++;
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("iterations"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("spread"));
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
