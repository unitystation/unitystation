using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomPropertyDrawer(typeof(VLSLayer))]
    public class VLSLayerDrawer : PropertyDrawer
    {
        private GUIContent maskLabel = new GUIContent("Layer Mask", "");
        private GUIContent nameLabel = new GUIContent("Layer Name", "");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUILayout.Space(-16);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("name"), nameLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("layerMask"), maskLabel);
        }
	}
}
