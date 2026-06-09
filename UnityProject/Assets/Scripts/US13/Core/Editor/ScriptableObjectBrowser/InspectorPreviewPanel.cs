using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	/// <summary>
	/// Right panel: inline inspector preview, find-all-instances list, template controls.
	/// </summary>
	public class InspectorPreviewPanel
	{
		private readonly BrowserState state;
		private UnityEditor.Editor cachedEditor;
		private Object cachedEditorTarget;

		// Find all instances
		private List<Object> foundInstances = new List<Object>();
		private bool instancesLoaded = false;
		private System.Type instancesLoadedForType;

		public InspectorPreviewPanel(BrowserState state)
		{
			this.state = state;
		}

		public void Draw(Rect rect)
		{
			GUILayout.BeginArea(rect);

			if (state.SelectedType == null)
			{
				EditorGUILayout.HelpBox("Select a ScriptableObject type from the list.", MessageType.Info);
				GUILayout.EndArea();
				return;
			}

			// Type info header
			EditorGUILayout.LabelField(state.SelectedType.Name, EditorStyles.boldLabel);
			var miniStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray }, wordWrap = true };
			EditorGUILayout.LabelField($"Namespace: {state.SelectedType.Namespace ?? "Global"}", miniStyle);
			EditorGUILayout.LabelField($"Assembly: {state.SelectedType.Assembly.GetName().Name}", miniStyle);

			EditorGUILayout.Space(8);
			DrawDividerLine();
			EditorGUILayout.Space(4);

			// Inline inspector for selected instance
			if (state.SelectedInstance != null)
			{
				DrawInlineInspector();
				EditorGUILayout.Space(4);

				// Template controls
				DrawTemplateControls();

				EditorGUILayout.Space(4);
				DrawDividerLine();
				EditorGUILayout.Space(4);
			}

			// Find all instances
			DrawFindAllInstances();

			GUILayout.EndArea();
		}

		public void OnTypeChanged()
		{
			instancesLoaded = false;
			foundInstances.Clear();
			CleanupEditor();
		}

		public void OnInstanceChanged()
		{
			CleanupEditor();
		}

		public void Cleanup()
		{
			CleanupEditor();
		}

		private void DrawInlineInspector()
		{
			EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);

			// Create or update editor
			if (cachedEditor == null || cachedEditorTarget != state.SelectedInstance)
			{
				CleanupEditor();
				cachedEditor = UnityEditor.Editor.CreateEditor(state.SelectedInstance);
				cachedEditorTarget = state.SelectedInstance;
			}

			if (cachedEditor != null)
			{
				state.InspectorScrollPos = EditorGUILayout.BeginScrollView(state.InspectorScrollPos,
					GUILayout.MaxHeight(300));
				cachedEditor.OnInspectorGUI();
				EditorGUILayout.EndScrollView();
			}
		}

		private void DrawTemplateControls()
		{
			if (state.SelectedType == null || state.SelectedInstance == null) return;

			string typeFullName = state.SelectedType.FullName;
			string currentTemplateGuid = state.GetTemplateGuid(typeFullName);
			string instanceGuid = "";
			string instancePath = AssetDatabase.GetAssetPath(state.SelectedInstance);
			if (string.IsNullOrEmpty(instancePath) == false)
			{
				instanceGuid = AssetDatabase.AssetPathToGUID(instancePath);
			}

			bool isCurrentTemplate = string.IsNullOrEmpty(instanceGuid) == false
									 && instanceGuid == currentTemplateGuid;

			EditorGUILayout.BeginHorizontal();
			if (isCurrentTemplate)
			{
				EditorGUILayout.LabelField("✓ This is the template for " + state.SelectedType.Name,
					EditorStyles.miniLabel);
				if (GUILayout.Button("Clear Template", EditorStyles.miniButton, GUILayout.Width(100)))
				{
					state.SetTemplateGuid(typeFullName, "");
				}
			}
			else
			{
				if (GUILayout.Button("Set as Template", EditorStyles.miniButton))
				{
					state.SetTemplateGuid(typeFullName, instanceGuid);
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawFindAllInstances()
		{
			EditorGUILayout.LabelField("Instances in Project", EditorStyles.boldLabel);

			// Load instances on demand or when type changed
			if (instancesLoaded == false || instancesLoadedForType != state.SelectedType)
			{
				RefreshInstances();
			}

			EditorGUILayout.LabelField($"Found: {foundInstances.Count}", EditorStyles.miniLabel);
			EditorGUILayout.Space(2);

			state.InstanceListScrollPos = EditorGUILayout.BeginScrollView(state.InstanceListScrollPos,
				GUILayout.MaxHeight(200));

			foreach (var instance in foundInstances)
			{
				if (instance == null) continue;

				bool isSelected = state.SelectedInstance == instance;
				var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(instance.name, style))
				{
					state.SelectedInstance = instance;
					OnInstanceChanged();
				}
				if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(35)))
				{
					EditorGUIUtility.PingObject(instance);
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();

			if (GUILayout.Button("Refresh", EditorStyles.miniButton))
			{
				RefreshInstances();
			}
		}

		private void RefreshInstances()
		{
			foundInstances.Clear();

			if (state.SelectedType != null)
			{
				string[] guids = AssetDatabase.FindAssets($"t:{state.SelectedType.Name}");
				foreach (string guid in guids)
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					var obj = AssetDatabase.LoadAssetAtPath(path, state.SelectedType);
					if (obj != null)
					{
						foundInstances.Add(obj);
					}
				}
			}

			instancesLoaded = true;
			instancesLoadedForType = state.SelectedType;
		}

		private void CleanupEditor()
		{
			if (cachedEditor != null)
			{
				Object.DestroyImmediate(cachedEditor);
				cachedEditor = null;
				cachedEditorTarget = null;
			}
		}

		private void DrawDividerLine()
		{
			var rect = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
		}
	}
}
