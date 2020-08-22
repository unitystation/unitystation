using Assets.Scripts.Health.Sickness;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor.Health
{
	[CustomEditor(typeof(Sickness))]
	public class SicknessEditor: UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			Sickness sickness = (Sickness)target;

			EditorGUILayout.HelpBox("Create stages of a sickness with their symptoms", MessageType.Info);

			sickness.SicknessName = EditorGUILayout.TextField("Sickness name", sickness.Name);
			sickness.Contagious = EditorGUILayout.Toggle("Contagious", sickness.Contagious);

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

			foreach (SicknessStage sicknessStage in sickness.SicknessStages)
			{
				sicknessStage.Symptom = (SymptomType)EditorGUILayout.EnumPopup(sicknessStage.Symptom);

				EditorGUI.indentLevel++;				
				sicknessStage.RepeatSymptom = EditorGUILayout.Toggle("Repeatable symptom", sicknessStage.RepeatSymptom);

				if (sicknessStage.RepeatSymptom)
				{
					sicknessStage.RepeatMinDelay = EditorGUILayout.IntField("Minimum Delay", sicknessStage.RepeatMinDelay);
					sicknessStage.RepeatMaxDelay = EditorGUILayout.IntField("Maximum Delay", sicknessStage.RepeatMaxDelay);
				}

				sicknessStage.SecondsBeforeNextStage = EditorGUILayout.IntField("Seconds before next stage", sicknessStage.SecondsBeforeNextStage);

				if (sicknessStage.Symptom == SymptomType.CustomMessage)
				{
					ShowCustomMessageOptions(sicknessStage);
				}

				EditorGUI.indentLevel--;
			}

			serializedObject.Update();
		}

		private static void ShowCustomMessageOptions(SicknessStage sicknessStage)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.HelpBox("Enter a list of message that can be shown at random\nIf a message is blank, only its public/private counterpart is shown\nYou can use %PLAYERNAME% expression to insert the player's name in the message", MessageType.Info);

			CustomMessageParameter customMessageParameter = (CustomMessageParameter)sicknessStage.SymptomParameter;

			int messageCount = EditorGUILayout.IntField("Number of messages", customMessageParameter.customMessages.Count);

			if (messageCount != customMessageParameter.customMessages.Count)
			{
				while (messageCount > customMessageParameter.customMessages.Count)
				{
					customMessageParameter.customMessages.Insert(customMessageParameter.customMessages.Count, new CustomMessage());
				}
				while (messageCount < customMessageParameter.customMessages.Count)
				{
					customMessageParameter.customMessages.RemoveAt(customMessageParameter.customMessages.Count - 1);
				}
			}

			int nCount = 0;
			foreach (CustomMessage customMessage in customMessageParameter.customMessages)
			{
				nCount++;
				customMessage.privateMessage = EditorGUILayout.TextField($"Private message #{nCount}", customMessage.privateMessage);
				customMessage.publicMessage = EditorGUILayout.TextField($"Public message #{nCount}", customMessage.publicMessage);
			}
			EditorGUI.indentLevel--;
		}
	}
}
