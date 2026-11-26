using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace US13.UI.Core
{
	/// <summary>
	/// Reusable scroll content for path-based data. Assign a row template and listen for a leaf's original path.
	/// Owns content height and row positioning; do not combine with a layout group or content size fitter.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class PathTreeView : MonoBehaviour
	{
		[SerializeField] private PathTreeRow rowTemplate;
		[SerializeField, Min(1)] private float rowHeight = 30;
		[SerializeField, Min(0)] private float indentation = 18;
		[SerializeField, Min(0)] private float spacing = 2;
		[SerializeField] private RectOffset padding;
		[SerializeField] private UnityEvent<string> onLeafSelected = new UnityEvent<string>();
		public UnityEvent<string> OnLeafSelected => onLeafSelected;

		private PathTreeNode root = PathTreeNode.Build(Array.Empty<string>());
		private readonly HashSet<string> expanded = new HashSet<string>(StringComparer.Ordinal);
		private readonly HashSet<PathTreeNode> matches = new HashSet<PathTreeNode>();
		private readonly List<PathTreeRow> rows = new List<PathTreeRow>();
		private Func<string, string> getLabel;
		private string filter = string.Empty;
		private int visibleCount;

		public void SetPaths(IEnumerable<string> paths, Func<string, string> labelForPath = null)
		{
			root = PathTreeNode.Build(paths);
			getLabel = labelForPath;
			SetFilter(filter);
		}

		/// <summary>Searches full paths and display labels, showing ancestors and temporarily expanding them.</summary>
		public void SetFilter(string value)
		{
			filter = value?.Trim() ?? string.Empty;
			matches.Clear();
			if (filter.Length > 0) FindMatches(root);
			Refresh();
			// A new result set starts at the viewport's top, even after scrolling or a prefab position override.
			((RectTransform)transform).anchoredPosition = Vector2.zero;
		}

		private string Label(PathTreeNode node) => node.IsFolder ? node.Name : getLabel?.Invoke(node.Path) ?? node.Name;

		private bool FindMatches(PathTreeNode node)
		{
			var match = !node.IsFolder && (node.Path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || Label(node).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

			foreach (var child in node.Children)
			{
				match |= FindMatches(child);
			}

			if (match)
			{
				matches.Add(node);
			}

			return match;
		}

		private void Refresh()
		{
			if (rowTemplate == null) return;

			padding ??= new RectOffset(6, 6, 6, 6);
			var content = (RectTransform)transform;
			// Row coordinates are measured from the top of content, whose width must follow the viewport.
			content.anchorMin = new Vector2(0, 1);
			content.anchorMax = Vector2.one;
			content.pivot = new Vector2(0.5f, 1);
			content.sizeDelta = new Vector2(0, content.sizeDelta.y);
			content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
			rowTemplate.gameObject.SetActive(false);
			visibleCount = 0;

			foreach (var child in root.Children)
			{
				Show(child, 0);
			}

			for (var i = visibleCount; i < rows.Count; i++)
			{
				rows[i].gameObject.SetActive(false);
			}
			content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding.top + padding.bottom + visibleCount * rowHeight + Mathf.Max(0, visibleCount - 1) * spacing);
		}

		private void Show(PathTreeNode node, int depth)
		{
			if (filter.Length > 0 && matches.Contains(node) == false) return;

			if (visibleCount == rows.Count)
			{
				rows.Add(Instantiate(rowTemplate, transform, false));
			}

			PathTreeRow row = rows[visibleCount];
			var rect = (RectTransform)row.transform;
			rect.anchorMin = new Vector2(0, 1);
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0, 1);

			float left = padding.left + depth * indentation;
			rect.anchoredPosition = new Vector2(left, -padding.top - visibleCount * (rowHeight + spacing));
			rect.sizeDelta = new Vector2(-left - padding.right, rowHeight);

			bool isExpanded = filter.Length > 0 || expanded.Contains(node.Path);

			row.Bind(Label(node), node.IsFolder, isExpanded, () =>
			{
				if (node.IsFolder == false)
				{
					onLeafSelected.Invoke(node.Path);
				}
				else if (filter.Length == 0)
				{
					if (expanded.Add(node.Path) == false)
					{
						expanded.Remove(node.Path);
					}
					Refresh();
				}
			});
			row.gameObject.SetActive(true);
			visibleCount++;
			if (node.IsFolder == false || isExpanded == false) return;
			foreach (var child in node.Children)
			{
				Show(child, depth + 1);
			}
		}
	}
}
