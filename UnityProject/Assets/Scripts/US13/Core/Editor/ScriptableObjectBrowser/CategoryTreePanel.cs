using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	/// <summary>
	/// Left panel: assembly filter toggles and collapsible namespace category tree.
	/// Uses Unity's built-in TreeView for clean, readable hierarchy display.
	/// </summary>
	public class CategoryTreePanel
	{
		private readonly BrowserState state;
		private readonly TypeDiscoveryService discovery;
		private readonly Action onFilterChanged;

		private TreeViewState treeViewState;
		private CategoryTreeView treeView;
		private bool needsReload;

		public CategoryTreePanel(BrowserState state, TypeDiscoveryService discovery, Action onFilterChanged)
		{
			this.state = state;
			this.discovery = discovery;
			this.onFilterChanged = onFilterChanged;

			treeViewState = new TreeViewState();
			treeView = new CategoryTreeView(treeViewState, discovery);
			treeView.SelectByFullPath(state.SelectedNamespaceFilter);
		}

		public void Draw(Rect rect)
		{
			if (needsReload)
			{
				treeView.Reload();
				treeView.SelectByFullPath(state.SelectedNamespaceFilter);
				needsReload = false;
			}

			GUILayout.BeginArea(rect);

			// Assembly filter toggles
			EditorGUILayout.LabelField("Assemblies", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			state.ShowGameAssemblies = EditorGUILayout.ToggleLeft("Game", state.ShowGameAssemblies);
			state.ShowThirdPartyAssemblies = EditorGUILayout.ToggleLeft("Third-Party", state.ShowThirdPartyAssemblies);
			if (EditorGUI.EndChangeCheck())
			{
				discovery.ShowGameAssemblies = state.ShowGameAssemblies;
				discovery.ShowThirdPartyAssemblies = state.ShowThirdPartyAssemblies;
				state.Save();
				treeView.Reload();
				treeView.SelectByFullPath(state.SelectedNamespaceFilter);
				onFilterChanged?.Invoke();
			}

			EditorGUILayout.Space(8);
			DrawDividerLine();
			EditorGUILayout.Space(4);

			EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);

			// TreeView needs a fixed rect, calculate remaining space
			Rect last = GUILayoutUtility.GetLastRect();
			float usedHeight = last.yMax + EditorGUIUtility.standardVerticalSpacing;
			float treeHeight = rect.height - usedHeight;

			if (treeHeight > 0)
			{
				var treeRect = new Rect(0, usedHeight, rect.width, treeHeight);

				// Detect selection changes
				var selectionBefore = treeView.GetSelection();

				treeView.OnGUI(treeRect);

				var selectionAfter = treeView.GetSelection();
				if (SelectionChanged(selectionBefore, selectionAfter) && selectionAfter.Count > 0)
				{
					string newFilter = treeView.GetFullPathForId(selectionAfter[0]);
					state.SelectedNamespaceFilter = newFilter;
					onFilterChanged?.Invoke();
				}
			}

			GUILayout.EndArea();
		}

		/// <summary>
		/// Call when the discovery data has changed externally and the tree needs rebuilding.
		/// </summary>
		public void MarkDirty()
		{
			needsReload = true;
		}

		private bool SelectionChanged(IList<int> before, IList<int> after)
		{
			if (before.Count != after.Count) return true;
			for (int i = 0; i < before.Count; i++)
			{
				if (before[i] != after[i]) return true;
			}
			return false;
		}

		private void DrawDividerLine()
		{
			var lineRect = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
		}
	}
}
