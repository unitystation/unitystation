using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	/// <summary>
	/// Manages persistent (EditorPrefs) and session state for the SO Browser.
	/// </summary>
	public class BrowserState
	{
		private const string KEY_PREFIX = "SOBrowser.";
		private const string RECENTS_KEY = KEY_PREFIX + "Recents";
		private const string FAVORITES_KEY = KEY_PREFIX + "Favorites";
		private const string PANEL_WIDTH_1_KEY = KEY_PREFIX + "PanelWidth1";
		private const string PANEL_WIDTH_2_KEY = KEY_PREFIX + "PanelWidth2";
		private const string DEFAULT_PATHS_KEY = KEY_PREFIX + "DefaultPaths";
		private const string TEMPLATES_KEY = KEY_PREFIX + "Templates";
		private const string SHOW_GAME_KEY = KEY_PREFIX + "ShowGame";
		private const string SHOW_THIRD_PARTY_KEY = KEY_PREFIX + "ShowThirdParty";

		private const int MAX_RECENTS = 15;

		// Persistent state
		public List<string> Recents { get; private set; } = new List<string>();
		public HashSet<string> Favorites { get; private set; } = new HashSet<string>();
		public Dictionary<string, string> DefaultPaths { get; private set; } = new Dictionary<string, string>();
		public Dictionary<string, string> Templates { get; private set; } = new Dictionary<string, string>();
		public float PanelWidth1 { get; set; } = 200f;
		public float PanelWidth2 { get; set; } = 300f;
		public bool ShowGameAssemblies { get; set; } = true;
		public bool ShowThirdPartyAssemblies { get; set; } = false;

		// Session state (not persisted)
		public string CapturedProjectPath { get; set; } = "Assets";
		public Type SelectedType { get; set; }
		public string SelectedNamespaceFilter { get; set; }
		public UnityEngine.Object SelectedInstance { get; set; }
		public string SearchQuery { get; set; } = "";
		public string NewAssetName { get; set; } = "";
		public string NewAssetPath { get; set; } = "";
		public int BatchCount { get; set; } = 1;
		public Vector2 CategoryScrollPos { get; set; }
		public Vector2 TypeListScrollPos { get; set; }
		public Vector2 InspectorScrollPos { get; set; }
		public Vector2 InstanceListScrollPos { get; set; }
		public bool ShowFavorites { get; set; } = true;
		public bool ShowRecents { get; set; } = true;

		public BrowserState()
		{
			Load();
		}

		public void Load()
		{
			Recents = DeserializeList(EditorPrefs.GetString(RECENTS_KEY, "{}"));
			Favorites = new HashSet<string>(DeserializeList(EditorPrefs.GetString(FAVORITES_KEY, "{}")));
			DefaultPaths = DeserializeDict(EditorPrefs.GetString(DEFAULT_PATHS_KEY, "{}"));
			Templates = DeserializeDict(EditorPrefs.GetString(TEMPLATES_KEY, "{}"));
			PanelWidth1 = EditorPrefs.GetFloat(PANEL_WIDTH_1_KEY, 200f);
			PanelWidth2 = EditorPrefs.GetFloat(PANEL_WIDTH_2_KEY, 300f);
			ShowGameAssemblies = EditorPrefs.GetBool(SHOW_GAME_KEY, true);
			ShowThirdPartyAssemblies = EditorPrefs.GetBool(SHOW_THIRD_PARTY_KEY, false);
		}

		public void Save()
		{
			EditorPrefs.SetString(RECENTS_KEY, SerializeList(Recents));
			EditorPrefs.SetString(FAVORITES_KEY, SerializeList(new List<string>(Favorites)));
			EditorPrefs.SetString(DEFAULT_PATHS_KEY, SerializeDict(DefaultPaths));
			EditorPrefs.SetString(TEMPLATES_KEY, SerializeDict(Templates));
			EditorPrefs.SetFloat(PANEL_WIDTH_1_KEY, PanelWidth1);
			EditorPrefs.SetFloat(PANEL_WIDTH_2_KEY, PanelWidth2);
			EditorPrefs.SetBool(SHOW_GAME_KEY, ShowGameAssemblies);
			EditorPrefs.SetBool(SHOW_THIRD_PARTY_KEY, ShowThirdPartyAssemblies);
		}

		public void AddRecent(string typeFullName)
		{
			Recents.Remove(typeFullName);
			Recents.Insert(0, typeFullName);
			if (Recents.Count > MAX_RECENTS)
			{
				Recents.RemoveRange(MAX_RECENTS, Recents.Count - MAX_RECENTS);
			}
			Save();
		}

		public bool IsFavorite(string typeFullName) => Favorites.Contains(typeFullName);

		public void ToggleFavorite(string typeFullName)
		{
			if (!Favorites.Add(typeFullName))
			{
				Favorites.Remove(typeFullName);
			}

			Save();
		}

		public string GetDefaultPath(string typeFullName)
		{
			DefaultPaths.TryGetValue(typeFullName, out string path);
			return path;
		}

		public void SetDefaultPath(string typeFullName, string path)
		{
			DefaultPaths[typeFullName] = path;
			Save();
		}

		public string GetTemplateGuid(string typeFullName)
		{
			Templates.TryGetValue(typeFullName, out string guid);
			return guid;
		}

		public void SetTemplateGuid(string typeFullName, string guid)
		{
			Templates[typeFullName] = guid;
			Save();
		}

		/// <summary>
		/// Capture the currently selected Project window folder. Call this when the window opens,
		/// before focus shifts away from the Project window.
		/// </summary>
		public void CaptureProjectSelection()
		{
			if (Selection.activeObject == null) return;

			string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (string.IsNullOrEmpty(selectionPath)) return;

			if (AssetDatabase.IsValidFolder(selectionPath))
			{
				CapturedProjectPath = selectionPath;
			}
			else
			{
				string dir = System.IO.Path.GetDirectoryName(selectionPath);
				if (string.IsNullOrEmpty(dir) == false)
				{
					CapturedProjectPath = dir.Replace("\\", "/");
				}
			}
		}

		/// <summary>
		/// Resolve the creation path: configured default > captured project folder > Assets/
		/// </summary>
		public string ResolveCreatePath(string typeFullName)
		{
			// Priority 1: configured default path for this type
			string configured = GetDefaultPath(typeFullName);
			if (string.IsNullOrEmpty(configured) == false && AssetDatabase.IsValidFolder(configured))
			{
				return configured;
			}

			// Priority 2: folder that was selected when the browser opened
			if (string.IsNullOrEmpty(CapturedProjectPath) == false && AssetDatabase.IsValidFolder(CapturedProjectPath))
			{
				return CapturedProjectPath;
			}

			// Priority 3: fallback
			return "Assets";
		}

		// --- Serialization helpers using JsonUtility wrappers ---

		[Serializable]
		private class StringListWrapper { public List<string> items = new List<string>(); }

		[Serializable]
		private class StringDictWrapper { public List<string> keys = new List<string>(); public List<string> values = new List<string>(); }

		private string SerializeList(List<string> list)
		{
			var wrapper = new StringListWrapper { items = list };
			return JsonUtility.ToJson(wrapper);
		}

		private List<string> DeserializeList(string json)
		{
			try
			{
				var wrapper = JsonUtility.FromJson<StringListWrapper>(json);
				return wrapper?.items ?? new List<string>();
			}
			catch
			{
				return new List<string>();
			}
		}

		private string SerializeDict(Dictionary<string, string> dict)
		{
			var wrapper = new StringDictWrapper();
			foreach (var kvp in dict)
			{
				wrapper.keys.Add(kvp.Key);
				wrapper.values.Add(kvp.Value);
			}
			return JsonUtility.ToJson(wrapper);
		}

		private Dictionary<string, string> DeserializeDict(string json)
		{
			try
			{
				var wrapper = JsonUtility.FromJson<StringDictWrapper>(json);
				var dict = new Dictionary<string, string>();
				if (wrapper?.keys != null)
				{
					for (int i = 0; i < wrapper.keys.Count && i < wrapper.values.Count; i++)
					{
						dict[wrapper.keys[i]] = wrapper.values[i];
					}
				}
				return dict;
			}
			catch
			{
				return new Dictionary<string, string>();
			}
		}
	}
}
