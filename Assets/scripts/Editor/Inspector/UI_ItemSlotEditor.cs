using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UI;

[CustomEditor(typeof(UI_ItemSlot))]
public class UI_ItemSlotEditor : Editor
{

    public override void OnInspectorGUI()
    {
        var itemSlot = (UI_ItemSlot)target;

        itemSlot.eventName = EditorGUILayout.TextField("Event Name", itemSlot.eventName);

        itemSlot.allowAllItems = EditorGUILayout.Toggle("Allow All Items", itemSlot.allowAllItems);

        if (itemSlot.allowAllItems)
        {
            itemSlot.maxItemSize = (ItemSize)EditorGUILayout.EnumPopup("Maximal Item Size", itemSlot.maxItemSize);
        }
        else
        {
            var tps = serializedObject.FindProperty("allowedItemTypes");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(tps, true);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
