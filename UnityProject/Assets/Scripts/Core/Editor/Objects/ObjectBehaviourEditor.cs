// using UnityEditor;
//
// [CustomEditor(typeof(ObjectBehaviour))]
// public class ObjectBehaviourEditor : Editor
// {
// 	public override void OnInspectorGUI()
// 	{
// 		ObjectBehaviour oTarget = (ObjectBehaviour) target;
// 		serializedObject.Update();
// 		SerializedProperty isNotPushable = serializedObject.FindProperty("isNotPushable");
// 		if (isNotPushable == null) isNotPushable = serializedObject.FindProperty("isInitiallyNotPushable");
// 		//SerializedProperty renderersToIgnore = serializedObject.FindProperty("ignoredSpriteRenderers"); //#####
// 		SerializedProperty registerTile = serializedObject.FindProperty("registerTile");
// 		//SerializedProperty visibleState = serializedObject.FindProperty("visibleState"); //####
// 		SerializedProperty networkChannel = serializedObject.FindProperty("networkChannel");
// 		SerializedProperty networkSendInterval = serializedObject.FindProperty("networkSendInterval");
// 		SerializedProperty soundWhenPushedOrPulled = serializedObject.FindProperty("pushPullSound");
// 		SerializedProperty soundDelayTime = serializedObject.FindProperty("soundDelayTime");
// 		SerializedProperty soundMinimumPitchVariance = serializedObject.FindProperty("soundMinimumPitchVariance");
// 		SerializedProperty soundMaximumPitchVariance = serializedObject.FindProperty("soundMaximumPitchVariance");
//
// 		EditorGUI.BeginChangeCheck();
//
// 		EditorGUILayout.LabelField("For ignoring renderers from VisibleBehaviour updates:");
// 		//EditorGUILayout.PropertyField(renderersToIgnore,true); //#####
// 		EditorGUILayout.PropertyField(registerTile, false);
// 		EditorGUILayout.LabelField("PushPull properties:");
// 		//EditorGUILayout.PropertyField(visibleState, false); //#####
// 		EditorGUILayout.PropertyField(isNotPushable, false);
// 		EditorGUILayout.PropertyField(soundWhenPushedOrPulled, false);
// 		EditorGUILayout.PropertyField(soundDelayTime, false);
// 		EditorGUILayout.Slider(soundMinimumPitchVariance, 0, 2);
// 		EditorGUILayout.Slider(soundMaximumPitchVariance, 0, 2);
//
// 		if (EditorGUI.EndChangeCheck())
// 		{
// 			serializedObject.ApplyModifiedProperties();
// 		}
// 	}
// }