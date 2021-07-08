using Chemistry;
using UnityEditor;
using UnityEngine;

namespace Chemistry.Editor
{
	[CustomEditor(typeof(Reaction))]
	public class ReactionEditor : UnityEditor.Editor
	{
		private const int MaxLimit = 1000;

		public override void OnInspectorGUI()
		{
			var reaction = (Reaction) target;

			DrawDefaultInspector();

			GUILayout.Label("Temperature");
			EditorGUILayout.BeginHorizontal();
			NullableFloat(ref reaction.tempMin,0, "Min");
			NullableFloat(ref reaction.tempMax, MaxLimit, "Max");
			EditorGUILayout.EndHorizontal();

			var tempMin = reaction.tempMin ?? 0;
			var tempMax = reaction.tempMax ?? MaxLimit;
			EditorGUILayout.MinMaxSlider(
				ref tempMin,
				ref tempMax,
				0,
				MaxLimit);

			reaction.tempMin = reaction.tempMin == null ? null : (float?)tempMin;
			reaction.tempMax = reaction.tempMax == null ? null : (float?)tempMax;
		}

		private static void NullableFloat(ref float? value, float def, string name)
		{
			EditorGUI.BeginDisabledGroup(value == null);
			var newValue = EditorGUILayout.FloatField(value ?? 0);
			if (value != null)
			{
				value = newValue;
			}
			EditorGUI.EndDisabledGroup();

			if (!EditorGUILayout.ToggleLeft(name, value.HasValue))
			{
				value = null;
			}
			else if (value == null)
			{
				value = def;
			}
		}
	}
}
