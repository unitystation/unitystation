using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	/// <summary>
	/// Center panel: favorites, recents, search bar, type list, and creation toolbar.
	/// </summary>
	public class TypeListPanel
	{
		private readonly BrowserState state;
		private readonly TypeDiscoveryService discovery;
		private readonly Action<Type> onTypeSelected;
		private readonly Action<ScriptableObject> onInstanceCreated;

		private List<(Type type, int score)> searchResults = new List<(Type, int)>();
		private bool needsSearchRefresh = true;

		// Cached styles (recreated each OnGUI to survive domain reload)
		private GUIStyle starStyle;
		private GUIStyle namespaceMiniStyle;
		private GUIContent starOn;
		private GUIContent starOff;
		private EntityId lastSkinInstanceId;

		public TypeListPanel(BrowserState state, TypeDiscoveryService discovery,
			Action<Type> onTypeSelected, Action<ScriptableObject> onInstanceCreated)
		{
			this.state = state;
			this.discovery = discovery;
			this.onTypeSelected = onTypeSelected;
			this.onInstanceCreated = onInstanceCreated;
		}

		public void MarkSearchDirty()
		{
			needsSearchRefresh = true;
		}

		public void Draw(Rect rect)
		{
			InitStyles();

			GUILayout.BeginArea(rect);

			// --- Favorites section ---
			state.ShowFavorites = EditorGUILayout.Foldout(state.ShowFavorites, $"★ Favorites ({state.Favorites.Count})", true);
			if (state.ShowFavorites && state.Favorites.Count > 0)
			{
				DrawTypeSubset(state.Favorites.ToList());
				EditorGUILayout.Space(4);
			}

			// --- Recents section ---
			state.ShowRecents = EditorGUILayout.Foldout(state.ShowRecents, $"⏱ Recent ({state.Recents.Count})", true);
			if (state.ShowRecents && state.Recents.Count > 0)
			{
				DrawTypeSubset(state.Recents);
				EditorGUILayout.Space(4);
			}

			DrawDividerLine();
			EditorGUILayout.Space(4);

			// --- Search bar ---
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("🔍", GUILayout.Width(20));

			EditorGUI.BeginChangeCheck();
			GUI.SetNextControlName("SOBrowserSearch");
			state.SearchQuery = EditorGUILayout.TextField(state.SearchQuery);
			if (EditorGUI.EndChangeCheck())
			{
				needsSearchRefresh = true;
			}

			if (string.IsNullOrEmpty(state.SearchQuery) == false)
			{
				if (GUILayout.Button("✕", GUILayout.Width(20)))
				{
					state.SearchQuery = "";
					needsSearchRefresh = true;
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(4);

			// --- Type list ---
			RefreshSearchIfNeeded();

			state.TypeListScrollPos = EditorGUILayout.BeginScrollView(state.TypeListScrollPos);
			foreach (var (type, score) in searchResults)
			{
				DrawTypeRow(type);
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(4);
			DrawDividerLine();
			EditorGUILayout.Space(4);

			// --- Creation toolbar ---
			DrawCreationToolbar();

			GUILayout.EndArea();
		}

		/// <summary>
		/// Focus the search field. Call from the window after opening.
		/// </summary>
		public void FocusSearch()
		{
			EditorGUI.FocusTextInControl("SOBrowserSearch");
		}

		public void SelectNext()
		{
			RefreshSearchIfNeeded();
			if (searchResults.Count == 0) return;

			int currentIndex = searchResults.FindIndex(r => r.type == state.SelectedType);
			int nextIndex = Mathf.Min(currentIndex + 1, searchResults.Count - 1);
			var nextType = searchResults[nextIndex].type;
			state.SelectedType = nextType;
			state.NewAssetName = nextType.Name;
			state.NewAssetPath = state.ResolveCreatePath(nextType.FullName);
			onTypeSelected?.Invoke(nextType);
		}

		public void SelectPrevious()
		{
			RefreshSearchIfNeeded();
			if (searchResults.Count == 0) return;

			int currentIndex = searchResults.FindIndex(r => r.type == state.SelectedType);
			int prevIndex = Mathf.Max(currentIndex - 1, 0);
			var prevType = searchResults[prevIndex].type;
			state.SelectedType = prevType;
			state.NewAssetName = prevType.Name;
			state.NewAssetPath = state.ResolveCreatePath(prevType.FullName);
			onTypeSelected?.Invoke(prevType);
		}

		public void CreateSelected()
		{
			if (state.SelectedType == null) return;
			CreateAsset(state.SelectedType, state.NewAssetName, state.NewAssetPath);
		}

		public void DuplicateSelected()
		{
			if (state.SelectedInstance == null) return;

			var original = state.SelectedInstance as ScriptableObject;
			if (original == null) return;

			var clone = Object.Instantiate(original);
			string originalPath = AssetDatabase.GetAssetPath(original);
			string dir = Path.GetDirectoryName(originalPath)?.Replace("\\", "/") ?? "Assets";
			string baseName = original.name;

			string newPath = $"{dir}/{baseName}_copy.asset";
			int counter = 1;
			while (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
			{
				newPath = $"{dir}/{baseName}_copy_{counter}.asset";
				counter++;
			}

			clone.name = Path.GetFileNameWithoutExtension(newPath);
			AssetDatabase.CreateAsset(clone, newPath);
			AssetDatabase.SaveAssets();

			state.SelectedInstance = clone;
			onInstanceCreated?.Invoke(clone);
			EditorGUIUtility.PingObject(clone);
		}

		private void DrawTypeSubset(List<string> typeNames)
		{
			foreach (var fullName in typeNames.ToList())
			{
				var type = discovery.FilteredTypes.FirstOrDefault(t => t.FullName == fullName);
				if (type == null) continue;
				DrawTypeRow(type);
			}
		}

		private void DrawTypeRow(Type type)
		{
			bool isSelected = state.SelectedType == type;
			var bgColor = GUI.backgroundColor;
			if (isSelected)
			{
				GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
			}

			EditorGUILayout.BeginHorizontal(isSelected ? EditorStyles.helpBox : EditorStyles.inspectorDefaultMargins);

			// Star toggle
			string fullName = type.FullName ?? type.Name;
			bool isFav = state.IsFavorite(fullName);
			if (GUILayout.Button(isFav ? starOn : starOff, starStyle, GUILayout.Width(20), GUILayout.Height(18)))
			{
				state.ToggleFavorite(fullName);
			}

			// Type name + namespace
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel, GUILayout.Height(16));
			if (string.IsNullOrEmpty(type.Namespace) == false)
			{
				EditorGUILayout.LabelField(type.Namespace, namespaceMiniStyle, GUILayout.Height(12));
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
			GUI.backgroundColor = bgColor;

			// Handle click
			var lastRect = GUILayoutUtility.GetLastRect();
			if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
			{
				state.SelectedType = type;
				state.NewAssetName = type.Name;
				state.NewAssetPath = state.ResolveCreatePath(fullName);
				onTypeSelected?.Invoke(type);
				Event.current.Use();
			}
		}

		private void DrawCreationToolbar()
		{
			if (state.SelectedType == null)
			{
				EditorGUILayout.HelpBox("Select a type above to create a new instance.", MessageType.Info);
				return;
			}

			EditorGUILayout.LabelField("Create New", EditorStyles.boldLabel);

			// Name field
			state.NewAssetName = EditorGUILayout.TextField("Name", state.NewAssetName);

			// Path field with browse button
			EditorGUILayout.BeginHorizontal();
			state.NewAssetPath = EditorGUILayout.TextField("Path", state.NewAssetPath);
			if (GUILayout.Button("...", GUILayout.Width(30)))
			{
				string folder = EditorUtility.OpenFolderPanel("Select folder", state.NewAssetPath, "");
				if (string.IsNullOrEmpty(folder) == false)
				{
					// Convert absolute to relative
					if (folder.StartsWith(Application.dataPath))
					{
						folder = "Assets" + folder.Substring(Application.dataPath.Length);
					}
					state.NewAssetPath = folder;
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(4);

			EditorGUILayout.BeginHorizontal();

			// Create button
			if (GUILayout.Button("Create", GUILayout.Height(28)))
			{
				CreateAsset(state.SelectedType, state.NewAssetName, state.NewAssetPath);
			}

			// Batch create
			state.BatchCount = EditorGUILayout.IntField(state.BatchCount, GUILayout.Width(40));
			if (GUILayout.Button("Batch Create", GUILayout.Height(28)))
			{
				int count = Mathf.Clamp(state.BatchCount, 1, 100);
				for (int i = 1; i <= count; i++)
				{
					string name = $"{state.NewAssetName}_{i:D2}";
					CreateAsset(state.SelectedType, name, state.NewAssetPath);
				}
			}

			// Duplicate (only if an instance is selected)
			GUI.enabled = state.SelectedInstance != null;
			if (GUILayout.Button("Duplicate", GUILayout.Height(28)))
			{
				DuplicateSelected();
			}
			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();

			// Set default path context action
			if (GUILayout.Button("Set as default path for this type", EditorStyles.miniButton))
			{
				state.SetDefaultPath(state.SelectedType.FullName, state.NewAssetPath);
			}
		}

		private void CreateAsset(Type type, string name, string path)
		{
			// Sanitize name
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				name = name.Replace(c.ToString(), "");
			}

			if (string.IsNullOrEmpty(name))
			{
				name = type.Name;
			}

			// Ensure folder exists
			if (AssetDatabase.IsValidFolder(path) == false)
			{
				path = "Assets";
			}

			// Auto-increment if exists
			string fullPath = $"{path}/{name}.asset";
			int counter = 1;
			while (AssetDatabase.LoadAssetAtPath<Object>(fullPath) != null)
			{
				fullPath = $"{path}/{name}_{counter}.asset";
				counter++;
			}

			// Check for template
			ScriptableObject instance;
			string templateGuid = state.GetTemplateGuid(type.FullName);
			if (string.IsNullOrEmpty(templateGuid) == false)
			{
				string templatePath = AssetDatabase.GUIDToAssetPath(templateGuid);
				var template = AssetDatabase.LoadAssetAtPath<ScriptableObject>(templatePath);
				if (template != null && template.GetType() == type)
				{
					instance = Object.Instantiate(template);
				}
				else
				{
					instance = ScriptableObject.CreateInstance(type);
				}
			}
			else
			{
				instance = ScriptableObject.CreateInstance(type);
			}

			instance.name = Path.GetFileNameWithoutExtension(fullPath);
			AssetDatabase.CreateAsset(instance, fullPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			state.AddRecent(type.FullName);
			state.SelectedInstance = instance;
			onInstanceCreated?.Invoke(instance);

			// Ping in project window
			EditorGUIUtility.PingObject(instance);
			Selection.activeObject = instance;
		}

		private void RefreshSearchIfNeeded()
		{
			if (needsSearchRefresh == false) return;
			needsSearchRefresh = false;

			if (string.IsNullOrEmpty(state.SearchQuery))
			{
				// No query: show types for selected namespace, or all
				var types = discovery.GetTypesForNamespace(state.SelectedNamespaceFilter);
				searchResults = types.Select(t => (t, 1)).ToList();
			}
			else
			{
				// Fuzzy search ignores namespace filter (search everything)
				searchResults = discovery.Search(state.SearchQuery);
			}
		}

		private void InitStyles()
		{
			var skinId = GUI.skin.GetEntityId();
			if (starStyle != null && lastSkinInstanceId == skinId) return;
			lastSkinInstanceId = skinId;
			starStyle = new GUIStyle(GUI.skin.button)
			{
				padding = new RectOffset(0, 0, 0, 0),
				margin = new RectOffset(0, 4, 2, 0),
				fontSize = 12
			};
			namespaceMiniStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
			starOn = new GUIContent("★");
			starOff = new GUIContent("☆");
		}

		private void DrawDividerLine()
		{
			var rect = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
		}
	}
}
