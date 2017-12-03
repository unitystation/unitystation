//C# Example

using UnityEngine;
using UnityEditor;
using System.Collections;

//namespace Light2D.Examples
//{
//    [CustomPropertyDrawer(typeof (BlockSetProfile.BlockTilingInfo))]
//    internal class BlockTilingInfoDrawer : PropertyDrawer
//    {
//        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//        {
//            label.text = "";
//            label = EditorGUI.BeginProperty(position, label, property);
//            var contentPosition = EditorGUI.PrefixLabel(position, label);
//            EditorGUIUtility.labelWidth = 12f;
//            contentPosition.width = 25f;
//            EditorGUI.indentLevel = 0;
//            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("T"));
//            contentPosition.x += contentPosition.width + 8;
//            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("L"));
//            contentPosition.x += contentPosition.width + 8;
//            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("B"));
//            contentPosition.x += contentPosition.width + 8;
//            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("R"));
//            contentPosition.x += contentPosition.width + 16;
//            EditorGUIUtility.labelWidth = 40f;
//            contentPosition.width = 240;
//            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("Sprite"));
//            EditorGUI.EndProperty();
//        }
//    }
//}