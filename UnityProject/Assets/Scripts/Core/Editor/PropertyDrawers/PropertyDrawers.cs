using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;


namespace Core.Editor.Attributes
{
	[CustomPropertyDrawer(typeof(PrefabModeOnlyAttribute))]
	public class PrefabModeOnlyDrawer : PropertyDrawer
	{
		private bool HideProperty => HideIrrelevantFields.IsEnabled && PrefabStageUtility.GetCurrentPrefabStage() == null;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return HideProperty ? 0 : EditorGUI.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (HideProperty) return;
			EditorGUI.PropertyField(position, property, label);
		}
	}

	[CustomPropertyDrawer(typeof(SceneModeOnlyAttribute))]
	public class SceneModeOnlyDrawer : PropertyDrawer
	{
		private bool HideProperty => HideIrrelevantFields.IsEnabled && PrefabStageUtility.GetCurrentPrefabStage() != null;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return HideProperty ? 0 : EditorGUI.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (HideProperty) return;
			EditorGUI.PropertyField(position, property, label);
		}
	}
}
