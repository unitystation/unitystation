using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomPropertyDrawer(typeof(VLSPassLayer))]
    public class VLSPassLayerDrawer : VLSLayerDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            //EditorGUILayout.PropertyField(property.FindPropertyRelative("clearFlag"), new GUIContent("Background Flag", ""));
            //EditorGUILayout.PropertyField(property.FindPropertyRelative("backgroundColor"), new GUIContent("Background Color", ""));

            EditorGUILayout.PropertyField(property.FindPropertyRelative("useSceneAmbientColor"), new GUIContent("Scene Ambience", ""));
            if (!property.FindPropertyRelative("useSceneAmbientColor").boolValue)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ambientColor"), new GUIContent("Ambient Color", ""));
            }

            //EditorGUILayout.PropertyField(property.FindPropertyRelative("overlay"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("blur"));

            GUILayout.Space(2);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("lightsEnabled"), new GUIContent("Apply VLSLight", ""));
            GUI.enabled = property.FindPropertyRelative("lightsEnabled").boolValue;
            {
                EditorGUI.indentLevel++;
                VLSProShaderEditor.GenerateSelectionList(property);
                property.FindPropertyRelative("lightLayerMask").intValue = EditorGUILayout.MaskField("Light Layers", property.FindPropertyRelative("lightLayerMask").intValue, VLSProShaderEditor.LightPassList);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("lightIntensity"), new GUIContent("Light Intensity", ""));
                EditorGUI.indentLevel--;
            }
            GUI.enabled = true;
        }
    }
}
