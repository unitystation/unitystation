using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Objects.Atmospherics;


namespace Core.Editor.Tools.Mapping
{
	/// <summary>
	/// <para>An editor window to assist in verifying certain objects have good names.</para>
	/// <para>Ensuring consistent names is necessary because some scripts, such as <see cref="AirController"/>
	/// require them in order to display a label on the UI.</para>
	/// <remarks>Ideally, we'd continue using /tg/ rooms just for ease of mapping.
	/// In this case, the rooms would automatically assign all devices within them the appropriate name.</remarks>
	/// </summary>
	public class ObjectNameValidator : EditorWindow
	{
		private static readonly string[] windowTabs = new string[] { "ACUs", "Vents", "Scrubbers" };
		private static readonly Type[] types = new Type[] { typeof(AirController), typeof(AirVent), typeof(Scrubber) };
		private static readonly string[] regexes =
				new string[] { "^ACU - .+ - [A-z]{5}$", "^Vent - .+ - [A-z]{5}$", "^Scrubber - .+ - [A-z]{5}$" };
		
		private List<GameObject> gameObjects;
		private List<GameObject> badObjects = new List<GameObject>();
		private List<GameObject> modifiedObjects = new List<GameObject>();

		private int activeWindowTab = 0;
		private int reviewedObjectIndex = -1;

		private string enteredName = string.Empty;
		private string randomIdentifier = string.Empty;

		private void OnEnable()
		{
			titleContent = new GUIContent("Name Validator");
			var windowSize = minSize;
			windowSize.x = 250;
			minSize = windowSize;
		}

		private void OnGUI()
		{
			if (gameObjects == null)
			{
				PopulateList();
				ScanList();
			}

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox(
				"Name Validator assists mappers in verifying certain objects have valid names.\n\n" +
				"Some objects require conforming names so they can be represented nicely in-game. " +
				"This is typically for any object that should reference their location.\n\n" +
				"For example, vents, so that the ACU-user knows exactly which vent the controller refers to.", MessageType.Info);
			EditorGUILayout.Space();

			int newTab = GUILayout.Toolbar(activeWindowTab, windowTabs);
			if (newTab != activeWindowTab)
			{
				activeWindowTab = newTab;
				PopulateList();
				ScanList();
				reviewedObjectIndex = -1;
			}
			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label($"<b>{badObjects.Count}</b> / <b>{gameObjects.Count}</b> objects failed validation", EditorUIUtils.LabelStyle);
			if (GUILayout.Button("Refresh"))
			{
				PopulateList();
				ScanList();
				reviewedObjectIndex = -1;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			if (badObjects.Count > 0)
			{
				SectionReviewNames();
			}
		}

		[MenuItem("Tools/Mapping/Name Validator", priority = 120)]
		public static void ShowWindow()
		{
			GetWindow<ObjectNameValidator>().Show();
		}

		private void SectionReviewNames()
		{
			EditorGUILayout.LabelField("Review Failed Names", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			bool btnPrevious = GUILayout.Button("Previous");
			bool btnNext = GUILayout.Button("Next");
			GUILayout.EndHorizontal();

			if (btnPrevious)
			{
				reviewedObjectIndex--;
			}
			if (btnNext)
			{
				reviewedObjectIndex++;
			}

			if (btnPrevious || btnNext)
			{
				reviewedObjectIndex = Mathf.Clamp(reviewedObjectIndex, 0, badObjects.Count - 1);

				Selection.activeGameObject = badObjects[reviewedObjectIndex].gameObject;
				SceneView.FrameLastActiveSceneView();

				enteredName = string.Empty;
				randomIdentifier = GenerateRandomIdentifier();
			}

			if (reviewedObjectIndex > -1)
			{
				var reviewedObject = badObjects[reviewedObjectIndex];

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Current name:");
				GUI.enabled = false;
				GUILayout.TextField(reviewedObject.name);
				GUI.enabled = true;
				GUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Object's location:");
				enteredName = EditorGUILayout.TextField(enteredName);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.HelpBox("Expecting a room name, e.g. 'Engineering Lobby' or 'Central Primary Hallway'.", MessageType.Info);

				if (string.IsNullOrWhiteSpace(enteredName) == false)
				{
					string devicePrefix = regexes[activeWindowTab].Split(' ')[0].Substring(1);
					string proposedName = $"{devicePrefix} - {enteredName} - {randomIdentifier}";

					EditorGUILayout.BeginHorizontal();
					GUI.enabled = false;
					EditorGUILayout.TextField(proposedName);
					GUI.enabled = true;

					if (Regex.Match(proposedName, regexes[activeWindowTab]).Success)
					{
						GUILayout.Label("Proposed name is valid");
						reviewedObject.name = proposedName;
						modifiedObjects.Add(reviewedObject);
					}
					else
					{
						GUILayout.Label("Proposed name is invalid");
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space(10);
				if (modifiedObjects.Count > 0 && EditorUIUtils.BigAssButton("Save All"))
				{
					foreach (var gameObject in modifiedObjects)
					{
						EditorUtility.SetDirty(gameObject);
					}
					EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

					reviewedObjectIndex = -1;
					modifiedObjects.Clear();
					PopulateList();
					ScanList();
				}
			}
		}

		// TODO: generics, you fool!
		private void PopulateList()
		{
			var type = types[activeWindowTab];

			if (type == typeof(AirController))
			{
				gameObjects = new List<GameObject>(FindObjectsOfType<AirController>().Select(entity => entity.gameObject));
			}
			else if (type == typeof(AirVent))
			{
				gameObjects = new List<GameObject>(FindObjectsOfType<AirVent>().Select(entity => entity.gameObject));
			}
			else if (type == typeof(Scrubber))
			{
				gameObjects = new List<GameObject>(FindObjectsOfType<Scrubber>().Select(entity => entity.gameObject));
			}
		}

		private void ScanList()
		{
			badObjects.Clear();
			foreach (var gameObject in gameObjects)
			{
				if (Regex.Match(gameObject.name, regexes[activeWindowTab]).Success == false)
				{
					badObjects.Add(gameObject);
				}
			}
		}

		private static string GenerateRandomIdentifier(int length = 5)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < length; i++)
			{
				// 30 % chance for a letter to be uppercase (I think it looks better...)
				int asciiStart = DMMath.Prob(30) ? 65 : 97;
				int charIndex = UnityEngine.Random.Range(0, 26);
				sb.Append(Convert.ToChar(asciiStart + charIndex));
			}

			return sb.ToString();
		}
	}
}
