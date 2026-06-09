using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	/// <summary>
	/// TreeView implementation for the namespace category tree.
	/// Uses Unity's built-in TreeView for proper indentation, expand/collapse, and selection.
	/// </summary>
	public class CategoryTreeView : TreeView
	{
		private readonly TypeDiscoveryService discovery;

		/// <summary>
		/// Maps TreeViewItem id -> CategoryNode.FullPath (null for root "All" item).
		/// </summary>
		private readonly Dictionary<int, string> idToFullPath = new Dictionary<int, string>();

		public CategoryTreeView(TreeViewState treeViewState, TypeDiscoveryService discovery)
			: base(treeViewState)
		{
			this.discovery = discovery;
			showBorder = true;
			Reload();
		}

		protected override TreeViewItem BuildRoot()
		{
			idToFullPath.Clear();

			// Hidden root at depth -1 (required by TreeView API)
			var root = new TreeViewItem(0, -1, "Hidden Root");

			int nextId = 1;
			var rootNode = discovery.RootNode;

			// "All" item at depth 0
			var allItem = new TreeViewItem(nextId, 0, $"All ({rootNode.TotalTypeCount})");
			idToFullPath[nextId] = null;
			nextId++;

			root.AddChild(allItem);

			// Build children recursively
			foreach (var child in rootNode.Children)
			{
				nextId = AddNodeRecursive(allItem, child, 1, nextId);
			}

			SetupDepthsFromParentsAndChildren(root);
			return root;
		}

		private int AddNodeRecursive(TreeViewItem parent, CategoryNode node, int depth, int nextId)
		{
			if (node.TotalTypeCount == 0) return nextId;

			int id = nextId++;
			var item = new TreeViewItem(id, depth, $"{node.Name} ({node.TotalTypeCount})");
			idToFullPath[id] = node.FullPath;

			parent.AddChild(item);

			foreach (var child in node.Children)
			{
				nextId = AddNodeRecursive(item, child, depth + 1, nextId);
			}

			return nextId;
		}

		/// <summary>
		/// Returns the namespace FullPath for the given tree item id, or null for "All".
		/// </summary>
		public string GetFullPathForId(int id)
		{
			idToFullPath.TryGetValue(id, out string path);
			return path;
		}

		/// <summary>
		/// Selects the tree item matching the given namespace path (null for "All").
		/// </summary>
		public void SelectByFullPath(string fullPath)
		{
			foreach (var kvp in idToFullPath)
			{
				if (kvp.Value == fullPath)
				{
					SetSelection(new List<int> { kvp.Key }, TreeViewSelectionOptions.RevealAndFrame);
					return;
				}
			}
		}
	}
}
