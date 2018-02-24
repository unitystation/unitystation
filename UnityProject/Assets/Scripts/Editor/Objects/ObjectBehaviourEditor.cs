using UnityEditor;

[CustomEditor(typeof(ObjectBehaviour))]
public class ObjectBehaviourEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ObjectBehaviour oTarget = (ObjectBehaviour) target;
		serializedObject.Update();
		SerializedProperty isPushable = serializedObject.FindProperty("isPushable");
		SerializedProperty renderersToIgnore = serializedObject.FindProperty("ignoredSpriteRenderers");
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(isPushable, true);
		EditorGUILayout.LabelField("For ignoring renderers from VisibleBehaviour updates:");
		EditorGUILayout.PropertyField(renderersToIgnore,true);
		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
		}
	}
}