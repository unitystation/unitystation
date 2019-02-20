using UnityEditor;

[CustomEditor(typeof(ObjectBehaviour))]
public class ObjectBehaviourEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ObjectBehaviour oTarget = (ObjectBehaviour) target;
		serializedObject.Update();
		SerializedProperty isNotPushable = serializedObject.FindProperty("isNotPushable");
		SerializedProperty renderersToIgnore = serializedObject.FindProperty("ignoredSpriteRenderers");
		SerializedProperty registerTile = serializedObject.FindProperty("registerTile");
		SerializedProperty visibleState = serializedObject.FindProperty("visibleState");
		SerializedProperty networkChannel = serializedObject.FindProperty("networkChannel");
		SerializedProperty networkSendInterval = serializedObject.FindProperty("networkSendInterval");
		EditorGUI.BeginChangeCheck();

		EditorGUILayout.LabelField("For ignoring renderers from VisibleBehaviour updates:");
		EditorGUILayout.PropertyField(renderersToIgnore,true);
		EditorGUILayout.PropertyField(registerTile, false);
		EditorGUILayout.LabelField("PushPull properties:");
		EditorGUILayout.PropertyField(visibleState, false);
		EditorGUILayout.PropertyField(isNotPushable, false);

		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
		}
	}
}