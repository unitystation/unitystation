using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectBehaviour))]
public class ObjectBehaviourEditor : Editor {

	public override void OnInspectorGUI(){
		ObjectBehaviour oTarget = (ObjectBehaviour)target;
		serializedObject.Update();
		SerializedProperty isPushable = serializedObject.FindProperty("isPushable");
		EditorGUILayout.PropertyField(isPushable);
	}

}
