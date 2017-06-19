using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomPropertyDrawer(typeof(VLSLightLayer))]
    public class VLSLightLayerDrawer : VLSLayerDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            EditorGUILayout.PropertyField(property.FindPropertyRelative("blur"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("overlay"));
        }
    }
}
