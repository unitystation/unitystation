using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;


namespace Core.Editor.Attributes
{
	[CustomPropertyDrawer(typeof(PrefabModeOnlyAttribute))]
	public class PrefabModeOnlyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return PrefabStageUtility.GetCurrentPrefabStage() == null ? 0 : EditorGUI.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (PrefabStageUtility.GetCurrentPrefabStage() == null) return;
			EditorGUI.PropertyField(position, property, label);
		}
	}

	[CustomPropertyDrawer(typeof(SceneModeOnlyAttribute))]
	public class SceneModeOnlyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return PrefabStageUtility.GetCurrentPrefabStage() != null ? 0 : EditorGUI.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
			EditorGUI.PropertyField(position, property, label);
		}
	}
}
