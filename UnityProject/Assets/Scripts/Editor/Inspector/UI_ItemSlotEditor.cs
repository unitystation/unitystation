using UnityEditor;

[CustomEditor(typeof(UI_ItemSlot))]
public class UI_ItemSlotEditor : Editor
{
	public override void OnInspectorGUI()
	{
		UI_ItemSlot itemSlot = (UI_ItemSlot) target;

		itemSlot.eventName = EditorGUILayout.TextField("Slot Name", itemSlot.eventName);
		itemSlot.hoverName = EditorGUILayout.TextField("Hover Name", itemSlot.hoverName);
		itemSlot.equipSlot = (EquipSlot) EditorGUILayout.EnumPopup("EquipSlot", itemSlot.equipSlot);
		itemSlot.allowAllItems = EditorGUILayout.Toggle("Allow All Items", itemSlot.allowAllItems);

		if (itemSlot.allowAllItems)
		{
			itemSlot.maxItemSize = (ItemSize) EditorGUILayout.EnumPopup("Maximal Item Size", itemSlot.maxItemSize);
		}
		else
		{
			SerializedProperty tps = serializedObject.FindProperty("allowedItemTypes");
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(tps, true);
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}