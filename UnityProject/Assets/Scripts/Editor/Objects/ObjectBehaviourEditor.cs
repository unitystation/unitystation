using UnityEditor;

[CustomEditor(typeof(ObjectBehaviour))]
public class ObjectBehaviourEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ObjectBehaviour oTarget = (ObjectBehaviour) target;
		serializedObject.Update();
		SerializedProperty isPushable = serializedObject.FindProperty("isPushable");
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(isPushable, true);
		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
		}
	}
}