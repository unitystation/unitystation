//C# Example

using UnityEditor;
using UnityEngine;

namespace Light2D
{
    [CustomPropertyDrawer(typeof(Point2))]
    internal class Point2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            var contentPosition = EditorGUI.PrefixLabel(position, label);
            EditorGUIUtility.labelWidth = 12f;
            contentPosition.width = 94f;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("x"), new GUIContent("X"));
            contentPosition.x += contentPosition.width + 2;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("y"), new GUIContent("Y"));
            EditorGUI.EndProperty();
        }
    }
}