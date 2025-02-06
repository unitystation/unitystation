using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TAGType))]
public class TAGTypePropertyDrawer : PropertyDrawer
{
	// Cache the tag values at first access
	private static string[] tagValues;

	private static string[] GetTagValues()
	{
		if (tagValues == null)
		{
			tagValues = typeof(TAG)
				.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
				.Select(f => (string)f.GetValue(null))
				.OrderByDescending(x => x)
				.ToArray();
		}
		return tagValues;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		string[] tags = GetTagValues();

		SerializedProperty tagValueProperty = property.FindPropertyRelative("tagValue");
		string currentTag = tagValueProperty.stringValue;

		int currentIndex = Array.IndexOf(tags, currentTag);
		if (currentIndex < 0) currentIndex = 0; // Default to first option if not found

		int newIndex = EditorGUI.Popup(position, label.text, currentIndex, tags);

		if (newIndex != currentIndex)
		{
			tagValueProperty.stringValue = tags[newIndex];
			property.serializedObject.ApplyModifiedProperties();
		}
	}
}