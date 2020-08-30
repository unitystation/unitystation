using UnityEditor;
using Health.Sickness;

namespace CustomInspectors
{
	[CustomEditor(typeof(Sickness))]
	public class SicknessEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			Sickness sickness = (Sickness)target;

			EditorGUILayout.HelpBox("Create stages of a sickness with their symptoms", MessageType.Info);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("sicknessName"));

			serializedObject.FindProperty("contagious").boolValue = EditorGUILayout.Toggle("Contagious", sickness.Contagious);

			EditorGUI.BeginChangeCheck();
			SicknessStages(sickness);
			if (EditorGUI.EndChangeCheck())
			{
				EditorUtility.SetDirty(sickness);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void SicknessStages(Sickness sickness)
		{
			int stageCount = EditorGUILayout.IntField("Number of Stages", sickness.SicknessStages.Count);

			if (stageCount != sickness.SicknessStages.Count)
			{
				while (stageCount > sickness.SicknessStages.Count)
				{
					sickness.SicknessStages.Insert(sickness.SicknessStages.Count, new SicknessStage());
				}
				while (stageCount < sickness.SicknessStages.Count)
				{
					sickness.SicknessStages.RemoveAt(sickness.SicknessStages.Count - 1);
				}
			}

			for (int stage = 0; stage < sickness.SicknessStages.Count; stage++)
			{				
				SerializedProperty currentStage = serializedObject.FindProperty("sicknessStages").GetArrayElementAtIndex(stage);
				SerializedProperty symptom = currentStage.FindPropertyRelative("symptom");
				EditorGUILayout.PropertyField(symptom);

				EditorGUI.indentLevel++;

				SerializedProperty repeatSymptom = currentStage.FindPropertyRelative("repeatSymptom");
				EditorGUILayout.PropertyField(repeatSymptom);

				if (repeatSymptom.boolValue)
				{
					EditorGUILayout.PropertyField(currentStage.FindPropertyRelative("repeatMinDelay"));
					EditorGUILayout.PropertyField(currentStage.FindPropertyRelative("repeatMaxDelay"));
				}

				EditorGUILayout.PropertyField(currentStage.FindPropertyRelative("secondsBeforeNextStage"));

				sickness.SicknessStages[stage].Symptom = (SymptomType)symptom.intValue;
				if (symptom.intValue == (int)SymptomType.CustomMessage)
				{
					ShowCustomMessageOptions((CustomMessageParameter)sickness.SicknessStages[stage].ExtendedSymptomParameters);
				}

				EditorGUI.indentLevel--;
			}
		}

		private static void ShowCustomMessageOptions(CustomMessageParameter customMessageParameter)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.HelpBox("Enter a list of message that can be shown at random\nIf a message is blank, only its public/private counterpart is shown\nYou can use %PLAYERNAME% expression to insert the player's name in the message", MessageType.Info);

			int messageCount = EditorGUILayout.IntField("Number of messages", customMessageParameter.CustomMessages.Count);

			if (messageCount != customMessageParameter.CustomMessages.Count)
			{
				while (messageCount > customMessageParameter.CustomMessages.Count)
				{
					customMessageParameter.CustomMessages.Insert(customMessageParameter.CustomMessages.Count, new CustomMessage());
				}
				while (messageCount < customMessageParameter.CustomMessages.Count)
				{
					customMessageParameter.CustomMessages.RemoveAt(customMessageParameter.CustomMessages.Count - 1);
				}
			}

			int nCount = 0;
			foreach (CustomMessage customMessage in customMessageParameter.CustomMessages)
			{
				nCount++;
				customMessage.privateMessage = EditorGUILayout.TextField($"Private message #{nCount}", customMessage.privateMessage);
				customMessage.publicMessage = EditorGUILayout.TextField($"Public message #{nCount}", customMessage.publicMessage);
			}
			EditorGUI.indentLevel--;
		}
	}
}
