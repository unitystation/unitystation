using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectBehaviour))]
public class ObjectBehaviourEditor : Editor
{

    public override void OnInspectorGUI()
    {
        ObjectBehaviour oTarget = (ObjectBehaviour)target;
        serializedObject.Update();
		var isPushable = serializedObject.FindProperty("isPushable");
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(isPushable, true);
		if (EditorGUI.EndChangeCheck())
			serializedObject.ApplyModifiedProperties();
    }

}
