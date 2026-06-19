using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	/// <summary>
	/// Main EditorWindow shell for the ScriptableObject Browser.
	/// Orchestrates three panels: CategoryTree (left), TypeList (center), InspectorPreview (right).
	/// </summary>
	public class ScriptableObjectBrowserWindow : EditorWindow
	{
		private TypeDiscoveryService discovery;
		private BrowserState state;
		private CategoryTreePanel categoryPanel;
		private TypeListPanel typeListPanel;
		private InspectorPreviewPanel inspectorPanel;

		private bool initialized = false;
		private bool focusSearchNextFrame = false;

		// Resize state
		private bool resizingPanel1 = false;
		private bool resizingPanel2 = false;
		private const float DIVIDER_WIDTH = 4f;
		private const float MIN_PANEL_WIDTH = 150f;

		[MenuItem("Tools/Windows/ScriptableObject Browser %#o")] // Ctrl+Shift+O
		public static void OpenWindow()
		{
			// Capture the focused window before GetWindow shifts focus
			var focusedBefore = EditorWindow.focusedWindow;
			string capturedPath = CaptureCurrentProjectPath(focusedBefore);

			var window = GetWindow<ScriptableObjectBrowserWindow>();
			window.titleContent = new GUIContent("SO Browser");
			window.minSize = new Vector2(800, 400);
			window.Show();
			window.focusSearchNextFrame = true;

			if (string.IsNullOrEmpty(capturedPath) == false)
			{
				window.state.CapturedProjectPath = capturedPath;
			}
		}

		private static string CaptureCurrentProjectPath(EditorWindow focusedBefore)
		{
			var projectBrowserType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
			if (projectBrowserType != null)
			{
				var getActiveFolderPath = projectBrowserType.GetMethod("GetActiveFolderPath",
					BindingFlags.NonPublic | BindingFlags.Instance);
				if (getActiveFolderPath != null)
				{
					// Use the window that was focused when the hotkey was pressed
					object browser = null;
					if (focusedBefore != null && projectBrowserType.IsInstanceOfType(focusedBefore))
					{
						browser = focusedBefore;
					}

					// Fallback: try all project browsers
					if (browser == null)
					{
						var browsers = Resources.FindObjectsOfTypeAll(projectBrowserType);
						foreach (var b in browsers)
						{
							if (b == focusedBefore)
							{
								browser = b;
								break;
							}
						}
						if (browser == null && browsers.Length > 0)
						{
							browser = browsers[0];
						}
					}

					if (browser != null)
					{
						string path = (string)getActiveFolderPath.Invoke(browser, null);
						if (string.IsNullOrEmpty(path) == false)
						{
							return path;
						}
					}
				}
			}

			// Fallback to Selection
			if (Selection.activeObject == null) return null;

			string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (string.IsNullOrEmpty(selectionPath)) return null;

			if (AssetDatabase.IsValidFolder(selectionPath))
			{
				return selectionPath;
			}

			string dir = System.IO.Path.GetDirectoryName(selectionPath);
			if (string.IsNullOrEmpty(dir) == false)
			{
				return dir.Replace("\\", "/");
			}

			return null;
		}

		private void OnEnable()
		{
			Initialize();
			AssemblyReloadEvents.afterAssemblyReload += OnAssemblyReload;
		}

		private void OnDisable()
		{
			AssemblyReloadEvents.afterAssemblyReload -= OnAssemblyReload;
			inspectorPanel?.Cleanup();
			state?.Save();
		}

		private void OnAssemblyReload()
		{
			discovery?.Refresh();
			categoryPanel?.MarkDirty();
			typeListPanel?.MarkSearchDirty();
		}

		private void Initialize()
		{
			if (initialized) return;

			state = new BrowserState();
			discovery = new TypeDiscoveryService(state.ShowGameAssemblies, state.ShowThirdPartyAssemblies);

			categoryPanel = new CategoryTreePanel(state, discovery, OnCategoryFilterChanged);
			typeListPanel = new TypeListPanel(state, discovery, OnTypeSelected, OnInstanceCreated);
			inspectorPanel = new InspectorPreviewPanel(state);

			initialized = true;
			focusSearchNextFrame = true;
		}

		private void OnGUI()
		{
			if (initialized == false) Initialize();

			HandleKeyboardInput();

			// Calculate panel rects with dividers
			float totalWidth = position.width;
			float totalHeight = position.height;

			// Clamp panel widths
			state.PanelWidth1 = Mathf.Clamp(state.PanelWidth1, MIN_PANEL_WIDTH, totalWidth - MIN_PANEL_WIDTH * 2 - DIVIDER_WIDTH * 2);
			state.PanelWidth2 = Mathf.Clamp(state.PanelWidth2, MIN_PANEL_WIDTH, totalWidth - state.PanelWidth1 - MIN_PANEL_WIDTH - DIVIDER_WIDTH * 2);

			float panel3Width = totalWidth - state.PanelWidth1 - state.PanelWidth2 - DIVIDER_WIDTH * 2;
			panel3Width = Mathf.Max(panel3Width, MIN_PANEL_WIDTH);

			var leftRect = new Rect(0, 0, state.PanelWidth1, totalHeight);
			var divider1Rect = new Rect(state.PanelWidth1, 0, DIVIDER_WIDTH, totalHeight);
			var centerRect = new Rect(state.PanelWidth1 + DIVIDER_WIDTH, 0, state.PanelWidth2, totalHeight);
			var divider2Rect = new Rect(state.PanelWidth1 + DIVIDER_WIDTH + state.PanelWidth2, 0, DIVIDER_WIDTH, totalHeight);
			var rightRect = new Rect(state.PanelWidth1 + DIVIDER_WIDTH * 2 + state.PanelWidth2, 0, panel3Width, totalHeight);

			// Draw dividers
			EditorGUI.DrawRect(divider1Rect, new Color(0.2f, 0.2f, 0.2f, 1f));
			EditorGUI.DrawRect(divider2Rect, new Color(0.2f, 0.2f, 0.2f, 1f));

			// Handle divider dragging
			EditorGUIUtility.AddCursorRect(divider1Rect, MouseCursor.ResizeHorizontal);
			EditorGUIUtility.AddCursorRect(divider2Rect, MouseCursor.ResizeHorizontal);
			HandleDividerDrag(divider1Rect, divider2Rect);

			// Draw panels with padding
			var padding = new RectOffset(4, 4, 4, 4);
			categoryPanel.Draw(padding.Remove(leftRect));
			typeListPanel.Draw(padding.Remove(centerRect));
			inspectorPanel.Draw(padding.Remove(rightRect));

			// Focus search on first frame
			if (focusSearchNextFrame && Event.current.type == EventType.Repaint)
			{
				typeListPanel.FocusSearch();
				focusSearchNextFrame = false;
			}
		}

		private void HandleKeyboardInput()
		{
			Event e = Event.current;
			if (e.type != EventType.KeyDown) return;

			switch (e.keyCode)
			{
				case KeyCode.Escape:
					if (string.IsNullOrEmpty(state.SearchQuery) == false)
					{
						state.SearchQuery = "";
						typeListPanel.MarkSearchDirty();
						e.Use();
					}
					else
					{
						Close();
					}
					break;

				case KeyCode.F when e.control:
					typeListPanel.FocusSearch();
					e.Use();
					break;

				case KeyCode.F2:
					if (state.SelectedInstance != null)
					{
						Selection.activeObject = state.SelectedInstance;
						EditorApplication.delayCall += () =>
						{
							EditorApplication.ExecuteMenuItem("Assets/Rename");
						};
						e.Use();
					}
					break;

				case KeyCode.D when e.control:
					if (state.SelectedInstance != null)
					{
						typeListPanel.DuplicateSelected();
						e.Use();
					}
					break;

				case KeyCode.UpArrow:
					typeListPanel.SelectPrevious();
					e.Use();
					break;

				case KeyCode.DownArrow:
					typeListPanel.SelectNext();
					e.Use();
					break;

				case KeyCode.Return:
					if (state.SelectedType != null)
					{
						typeListPanel.CreateSelected();
						if (e.control == false) Close();
						e.Use();
					}
					break;
			}
		}

		private void HandleDividerDrag(Rect divider1, Rect divider2)
		{
			Event e = Event.current;

			if (e.type == EventType.MouseDown)
			{
				if (divider1.Contains(e.mousePosition))
				{
					resizingPanel1 = true;
					e.Use();
				}
				else if (divider2.Contains(e.mousePosition))
				{
					resizingPanel2 = true;
					e.Use();
				}
			}

			if (e.type == EventType.MouseDrag)
			{
				if (resizingPanel1)
				{
					state.PanelWidth1 = Mathf.Clamp(e.mousePosition.x, MIN_PANEL_WIDTH,
						position.width - MIN_PANEL_WIDTH * 2 - DIVIDER_WIDTH * 2);
					Repaint();
					e.Use();
				}
				else if (resizingPanel2)
				{
					state.PanelWidth2 = Mathf.Clamp(
						e.mousePosition.x - state.PanelWidth1 - DIVIDER_WIDTH,
						MIN_PANEL_WIDTH,
						position.width - state.PanelWidth1 - MIN_PANEL_WIDTH - DIVIDER_WIDTH * 2);
					Repaint();
					e.Use();
				}
			}

			if (e.type == EventType.MouseUp)
			{
				if (resizingPanel1 || resizingPanel2)
				{
					state.Save();
				}
				resizingPanel1 = false;
				resizingPanel2 = false;
			}
		}

		private void OnCategoryFilterChanged()
		{
			typeListPanel.MarkSearchDirty();
			inspectorPanel.OnTypeChanged();
			Repaint();
		}

		private void OnTypeSelected(System.Type type)
		{
			inspectorPanel.OnTypeChanged();
			Repaint();
		}

		private void OnInstanceCreated(ScriptableObject instance)
		{
			inspectorPanel.OnInstanceChanged();
			Repaint();
		}
	}
}
