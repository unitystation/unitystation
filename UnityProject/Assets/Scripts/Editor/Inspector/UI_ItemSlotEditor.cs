using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UI_ItemSlot))]
public class UI_ItemSlotEditor : Editor
{
	private SerializedProperty allowAllItems;

	private void OnEnable() {
		allowAllItems = serializedObject.FindProperty("allowAllItems");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("eventName"), new GUIContent("Slot Name"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverName"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("equipSlot"));
		EditorGUILayout.PropertyField(allowAllItems);

		if (allowAllItems.boolValue)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maxItemSize"), new GUIContent("Maximal Item Size"));
		}
		else
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("allowedItemTypes"), true);
		}
		serializedObject.ApplyModifiedProperties();
	}
}