using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace US13.Core.Editor
{
	using Transform = UnityEngine.Transform;

	public class MenuVisibilityTool : EditorWindow
	{
		private const string OriginalStateKey = "MenuVisibilityTool.OriginalActive";

		private readonly List<MenuEntry> entries = new List<MenuEntry>();
		private readonly List<MenuGroup> groups = new List<MenuGroup>();
		private readonly Dictionary<string, bool> originalActive = new Dictionary<string, bool>();
		private readonly HashSet<string> expanded = new HashSet<string>();
		private string filter = "";
		private string soloedKey;
		private Vector2 scrollPosition;

		[MenuItem("Tools/Windows/Menu Visibility Tool")]
		private static void ShowWindow()
		{
			GetWindow<MenuVisibilityTool>("Menu Visibility");
		}

		private void OnEnable()
		{
			RefreshMenus();
			LoadOriginalStates();
		}

		private void OnGUI()
		{
			EditorGUILayout.Space();

			EditorGUILayout.HelpBox(
				"Menus are grouped by their parent in the hierarchy; loose ones are grouped by the first word of " +
				"their name. Search to jump to a menu group. Click Solo to isolate a group or menu and hide the " +
				"rest. To unhide click Unsolo. Restore All to return the scene to its default state.",
				MessageType.Info);

			EditorGUILayout.Space();

			filter = EditorGUILayout.TextField("Search", filter);

			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Refresh"))
				{
					RefreshMenus();
				}
				if (GUILayout.Button("Restore All"))
				{
					RestoreAll();
				}
			}

			if (originalActive.Count > 0)
			{
				EditorGUILayout.HelpBox($"Changed the active state of {originalActive.Count} object(s). Press \"Restore All\" before saving the scene.", MessageType.Warning);
			}

			EditorGUILayout.Space();

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			foreach (MenuGroup group in groups)
			{
				DrawGroup(group);
			}
			EditorGUILayout.EndScrollView();
		}

		private void DrawGroup(MenuGroup group)
		{
			List<MenuEntry> matches = MatchingMenus(group);
			if (matches.Count == 0) return;

			bool filtering = string.IsNullOrEmpty(filter) == false;
			bool open = filtering || expanded.Contains(group.Header);

			using (new EditorGUILayout.HorizontalScope())
			{
				if (filtering == false)
				{
					if (GUILayout.Button(open ? "▾" : "▸", EditorStyles.label, GUILayout.Width(14)))
					{
						SetExpanded(group.Header, open == false);
					}
				}
				else
				{
					GUILayout.Space(14f);
				}

				string key = "G:" + group.Header;
				switch (DrawSoloButton(key))
				{
					case SoloButtonResult.Solo:
						soloedKey = key;
						SoloGroup(group);
						break;
					case SoloButtonResult.Unsolo:
						Unsolo();
						break;
				}

				EditorGUILayout.LabelField($"{group.Header}  ({matches.Count})", EditorStyles.boldLabel);
			}

			if (open == false) return;
			foreach (MenuEntry entry in matches)
			{
				DrawMenuRow(entry, 1);
			}
		}

		private void DrawMenuRow(MenuEntry entry, int depth)
		{
			GameObject panel = entry.Target.gameObject;
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(16f + depth * 14f);

				bool hidden = SceneVisibilityManager.instance.IsHidden(panel, true);
				if (GUILayout.Button(EyeIcon(hidden), GUILayout.Width(28)))
				{
					SetHidden(panel, hidden == false);
				}

				string key = "M:" + panel.GetInstanceID();
				switch (DrawSoloButton(key))
				{
					case SoloButtonResult.Solo:
						soloedKey = key;
						Solo(entry.Target);
						break;
					case SoloButtonResult.Unsolo:
						Unsolo();
						break;
				}

				string label = panel.activeInHierarchy ? entry.Label : entry.Label + "  (inactive)";
				if (GUILayout.Button(label, EditorStyles.label))
				{
					SelectAndPing(panel);
				}
			}
		}

		private void RefreshMenus()
		{
			entries.Clear();
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded == false) continue;
				foreach (GameObject root in scene.GetRootGameObjects())
				{
					Collect(root.transform);
				}
			}

			BuildGroups();
		}

		private void Collect(Transform node)
		{
			if (TryGetMenuLabel(node, out string label))
			{
				entries.Add(new MenuEntry { Target = node, Label = label, GroupNode = GroupNodeOf(node) });
				return;
			}
			foreach (Transform child in node)
			{
				Collect(child);
			}
		}

		private void BuildGroups()
		{
			groups.Clear();
			List<MenuEntry> leftovers = new List<MenuEntry>();
			GroupByContainer(leftovers);
			GroupLeftovers(leftovers);
			SortGroups();
		}

		private void GroupByContainer(List<MenuEntry> leftovers)
		{
			Dictionary<Transform, MenuGroup> byNode = new Dictionary<Transform, MenuGroup>();
			foreach (MenuEntry entry in entries)
			{
				if (byNode.TryGetValue(entry.GroupNode, out MenuGroup group) == false)
				{
					group = new MenuGroup { Node = entry.GroupNode, Header = entry.GroupNode.name };
					byNode[entry.GroupNode] = group;
				}
				group.Menus.Add(entry);
			}

			foreach (MenuGroup group in byNode.Values)
			{
				if (group.Menus.Count == 1 && group.Menus[0].Target == group.Node)
				{
					leftovers.Add(group.Menus[0]);
					continue;
				}
				groups.Add(group);
			}
		}

		private void GroupLeftovers(List<MenuEntry> leftovers)
		{
			foreach (MenuEntry leftover in leftovers)
			{
				string word = FirstWord(leftover.Target.name);
				MenuGroup group = GroupWithFirstWord(word);
				if (group == null)
				{
					group = new MenuGroup { Header = word };
					groups.Add(group);
				}
				group.Menus.Add(leftover);
			}
		}

		private MenuGroup GroupWithFirstWord(string word)
		{
			foreach (MenuGroup group in groups)
			{
				if (string.Equals(FirstWord(group.Header), word, StringComparison.OrdinalIgnoreCase))
				{
					return group;
				}
			}
			return null;
		}

		private static string FirstWord(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			for (int i = 1; i < name.Length; i++)
			{
				if (char.IsLetter(name[i]) == false || char.IsUpper(name[i]))
				{
					return name.Substring(0, i);
				}
			}
			return name;
		}

		private void SortGroups()
		{
			foreach (MenuGroup group in groups)
			{
				group.Menus.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
			}
			groups.Sort((a, b) => string.Compare(a.Header, b.Header, StringComparison.OrdinalIgnoreCase));
		}

		private SoloButtonResult DrawSoloButton(string key)
		{
			bool isSoloed = soloedKey == key;
			bool dimmed = string.IsNullOrEmpty(soloedKey) == false && isSoloed == false;

			Color previousColor = GUI.color;
			if (dimmed)
			{
				GUI.color = new Color(1f, 1f, 1f, 0.5f);
			}

			SoloButtonResult result = SoloButtonResult.None;
			if (GUILayout.Button(isSoloed ? "Unsolo" : "Solo", GUILayout.Width(56)))
			{
				result = isSoloed ? SoloButtonResult.Unsolo : SoloButtonResult.Solo;
			}

			GUI.color = previousColor;
			return result;
		}

		private void Unsolo()
		{
			RestoreAll();
		}

		private void Solo(Transform node)
		{
			HideAllUi();
			Reveal(node);
		}

		private void SoloGroup(MenuGroup group)
		{
			HideAllUi();
			if (group.Node != null)
			{
				Reveal(group.Node);
				return;
			}
			foreach (MenuEntry entry in group.Menus)
			{
				Reveal(entry.Target);
			}
		}

		private void Reveal(Transform node)
		{
			SwitchOnWithAncestors(node.gameObject);
			SceneVisibilityManager.instance.Show(node.gameObject, true);
		}

		private void RestoreAll()
		{
			foreach (KeyValuePair<string, bool> entry in originalActive)
			{
				GameObject panel = FindPanel(entry.Key);
				if (panel == null) continue;
				panel.SetActive(entry.Value);
				EditorSceneManager.MarkSceneDirty(panel.scene);
			}
			originalActive.Clear();
			SaveOriginalStates();

			SceneVisibilityManager.instance.ShowAll();
			soloedKey = null;
		}

		private static void HideAllUi()
		{
			foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				SceneVisibilityManager.instance.Hide(canvas.gameObject, true);
			}
		}

		private void SwitchOnWithAncestors(GameObject panel)
		{
			for (Transform current = panel.transform; current != null; current = current.parent)
			{
				SwitchOn(current.gameObject);
			}
		}

		private void SwitchOn(GameObject panel)
		{
			if (panel.activeSelf) return;
			RememberOriginalState(panel);
			panel.SetActive(true);
			EditorSceneManager.MarkSceneDirty(panel.scene);
		}

		private void RememberOriginalState(GameObject panel)
		{
			string id = GetId(panel);
			if (originalActive.ContainsKey(id)) return;
			originalActive[id] = panel.activeSelf;
			SaveOriginalStates();
		}

		private List<MenuEntry> MatchingMenus(MenuGroup group)
		{
			List<MenuEntry> result = new List<MenuEntry>();
			foreach (MenuEntry entry in group.Menus)
			{
				if (entry.Target == null) continue;
				if (string.IsNullOrEmpty(filter)
					|| entry.Label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
					|| group.Header.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					result.Add(entry);
				}
			}
			return result;
		}

		private void SetExpanded(string header, bool open)
		{
			if (open)
			{
				expanded.Add(header);
				return;
			}
			expanded.Remove(header);
		}

		private void LoadOriginalStates()
		{
			originalActive.Clear();
			string saved = SessionState.GetString(OriginalStateKey, "");
			if (string.IsNullOrEmpty(saved)) return;
			foreach (string line in saved.Split('\n'))
			{
				int separator = line.LastIndexOf('=');
				if (separator < 0) continue;
				string id = line.Substring(0, separator);
				originalActive[id] = line.Substring(separator + 1) == "1";
			}
		}

		private void SaveOriginalStates()
		{
			List<string> lines = new List<string>();
			foreach (KeyValuePair<string, bool> entry in originalActive)
			{
				lines.Add($"{entry.Key}={(entry.Value ? "1" : "0")}");
			}
			SessionState.SetString(OriginalStateKey, string.Join("\n", lines));
		}

		private static Transform GroupNodeOf(Transform menu)
		{
			for (Transform node = menu; node != null; node = node.parent)
			{
				if (node.GetComponent<Canvas>() != null) return node;
				if (node.parent == null || node.parent.GetComponent<Canvas>() != null) return node;
			}
			return menu;
		}

		private static bool TryGetMenuLabel(Transform node, out string label)
		{
			label = null;
			if (node.childCount == 0) return false;
			foreach (MonoBehaviour behaviour in node.GetComponents<MonoBehaviour>())
			{
				if (behaviour == null) continue;
				string typeName = behaviour.GetType().Name;
				if (IsMenuComponent(typeName) == false) continue;
				string controller = StripPrefix(typeName);
				label = node.name == controller ? node.name : $"{node.name}  ({controller})";
				return true;
			}
			return false;
		}

		private static bool IsMenuComponent(string typeName)
		{
			if (typeName == "WindowDrag") return true;
			if (typeName.StartsWith("GUI_")) return true;
			return typeName.EndsWith("Menu") || typeName.EndsWith("Window") || typeName.EndsWith("Panel")
				|| typeName.EndsWith("Screen") || typeName.EndsWith("Dialogue") || typeName.EndsWith("Tab");
		}

		private static string StripPrefix(string typeName)
		{
			return typeName.StartsWith("GUI_") ? typeName.Substring(4) : typeName;
		}

		private static void SetHidden(GameObject panel, bool hidden)
		{
			if (hidden)
			{
				SceneVisibilityManager.instance.Hide(panel, true);
				return;
			}
			SceneVisibilityManager.instance.Show(panel, true);
		}

		private static void SelectAndPing(GameObject panel)
		{
			Selection.activeGameObject = panel;
			EditorGUIUtility.PingObject(panel);
		}

		private static string GetId(GameObject panel)
		{
			return GlobalObjectId.GetGlobalObjectIdSlow(panel).ToString();
		}

		private static GameObject FindPanel(string id)
		{
			if (GlobalObjectId.TryParse(id, out GlobalObjectId globalId) == false) return null;
			return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
		}

		private static GUIContent EyeIcon(bool hidden)
		{
			return EditorGUIUtility.IconContent(hidden ? "animationvisibilitytoggleoff" : "animationvisibilitytoggleon");
		}

		private enum SoloButtonResult
		{
			None,
			Solo,
			Unsolo
		}

		private class MenuEntry
		{
			public Transform Target;
			public string Label;
			public Transform GroupNode;
		}

		private class MenuGroup
		{
			public Transform Node;
			public string Header;
			public readonly List<MenuEntry> Menus = new List<MenuEntry>();
		}
	}
}
