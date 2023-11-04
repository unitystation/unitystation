using UnityEngine;
using UnityEditor;

namespace Core.Editor.Attributes
{
	public class PrefabModeOnlyDrawer : PropertyDrawer
	{
		private bool HideProperty => QuickLoad.HideIrrelevantFields.IsEnabled && UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null;

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
		private bool HideProperty => QuickLoad.HideIrrelevantFields.IsEnabled && UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;

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


	[CustomPropertyDrawer(typeof(PlayModeOnlyAttribute))]
	public class PlayModeOnlyDrawer : PropertyDrawer
	{
		private bool HideProperty => !Application.isPlaying;

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
