using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;


[CustomPropertyDrawer(typeof(SpriteSheet))]
public class SpriteSheetPropertyDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var amountRect = new Rect(position.x, position.y, position.width, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("Texture"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;



		EditorGUI.EndProperty();
	}
}
#endif
