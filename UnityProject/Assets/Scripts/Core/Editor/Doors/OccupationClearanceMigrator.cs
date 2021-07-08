using System.Collections.Generic;
using Systems.Clearance;
using Systems.Clearance.Utils;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.Doors
{
	public class OccupationClearanceMigrator: EditorWindow
	{
		private Vector2 scrollPosition;
		private Vector2 bigScroll;
		private string log = "";

		[SerializeField][ReorderableList][Foldout("shit")]
		private List<Occupation> occupations;

		private UnityEditor.Editor editor;

		[MenuItem("Tools/Migrations/Occupation Clearance Migration")]
		public static void ShowWindow()
		{
			GetWindow<OccupationClearanceMigrator>().Show();
		}

		private void OnGUI()
		{
			bigScroll = EditorGUILayout.BeginScrollView(bigScroll);
			if (!editor)
			{
				editor = UnityEditor.Editor.CreateEditor(this);
			}
			else
			{
				editor.OnInspectorGUI();
			}

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("By pressing the button, we will try to migrate" +
			                        " Access -> Clearance from the list of occupations", MessageType.Info);

			if (GUILayout.Button("Migrate!"))
			{
				MigrateOccupationsInFolder();
			}

			EditorGUILayout.Space();
			EditorGUILayout.TextArea(log);
			EditorGUILayout.Space();
			EditorGUILayout.EndScrollView();
		}

		private void MigrateOccupationsInFolder()
		{
			ClearLog();
			foreach (var occupation in occupations)
			{
				AddToLog($"Processing {occupation.name}...");
				var clearances = new List<Clearance>();
				foreach (var access in occupation.AllowedAccess)
				{
					if (MigrationData.Translation.ContainsKey(access))
					{
						if (!clearances.Contains(MigrationData.Translation[access]))
						{
							clearances.Add(MigrationData.Translation[access]);
						}
					}
					else
					{
						AddToLog($"{access} couldn't be found in dictionary. Skipping!");
					}
				}

				occupation.IssuedClearance = clearances;
				occupation.IssuedLowPopClearance = clearances;
				EditorUtility.SetDirty(occupation);
			}

			AddToLog("Done! Remember to control + s to save the changes.");
		}

		private void AddToLog(string newLine)
		{
			log += newLine + "\n";
		}

		private void ClearLog()
		{
			log = string.Empty;
		}
	}
}