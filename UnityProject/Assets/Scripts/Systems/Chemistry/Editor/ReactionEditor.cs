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
			NullableFloat(ref reaction.serializableTempMin, ref reaction.hasMinTemp, 0, "Min");
			NullableFloat(ref reaction.serializableTempMax, ref reaction.hasMaxTemp, MaxLimit, "Max");
			EditorGUILayout.EndHorizontal();

			var tempMin = reaction.hasMinTemp ? reaction.serializableTempMin : 0;
			var tempMax = reaction.hasMaxTemp ? reaction.serializableTempMax : MaxLimit;
			EditorGUILayout.MinMaxSlider(
				ref tempMin,
				ref tempMax,
				0,
				MaxLimit);

			if (reaction.hasMinTemp == true) 
			{
                reaction.serializableTempMin = tempMin;
			}
            if (reaction.hasMaxTemp == true) 
			{
				reaction.serializableTempMax = tempMax;
			}

			if(GUILayout.Button("Set Dirty")) 
			{
                EditorUtility.SetDirty(target);
			}

		}

		private static void NullableFloat(ref float value, ref bool tempState, float def, string name)
		{
			EditorGUI.BeginDisabledGroup(tempState == false);
			var newValue = EditorGUILayout.FloatField(tempState ? value : 0);
			if (tempState)
			{
				value = newValue;
			}
			EditorGUI.EndDisabledGroup();

			if (!EditorGUILayout.ToggleLeft(name, tempState))
			{
				tempState = false;
				value = 0;
			}
			else if (tempState == false)
			{
				tempState = true;
				value = def;
			}
		}
	}
}