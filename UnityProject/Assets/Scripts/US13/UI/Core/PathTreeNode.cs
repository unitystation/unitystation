using System;
using System.Collections.Generic;
using System.Linq;

namespace US13.UI.Core
{
	/// <summary>
	/// A path hierarchy independent of its UI. Leaf paths retain their original address.
	/// </summary>
	public sealed class PathTreeNode
	{
		public string Name { get; }
		public string Path { get; }
		public bool IsFolder { get; }
		public List<PathTreeNode> Children { get; } = new List<PathTreeNode>();

		private PathTreeNode(string name, string path, bool isFolder)
		{
			Name = name;
			Path = path;
			IsFolder = isFolder;
		}

		public static PathTreeNode Build(IEnumerable<string> paths)
		{
			var root = new PathTreeNode(string.Empty, string.Empty, true);
			var folders = new Dictionary<string, PathTreeNode>(StringComparer.Ordinal);
			foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.Ordinal))
			{
				string[] segments = path.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				if (segments.Length == 0) continue;

				PathTreeNode parent = root;
				var folderPath = string.Empty;

				for (var i = 0; i < segments.Length - 1; i++)
				{
					folderPath += "/" + segments[i];

					if (folders.TryGetValue(folderPath, out var folder) == false)
					{
						folder = new PathTreeNode(segments[i], folderPath, true);
						folders.Add(folderPath, folder);
						parent.Children.Add(folder);
					}

					parent = folder;
				}
				parent.Children.Add(new PathTreeNode(segments[segments.Length - 1], path, false));
			}
			Sort(root);
			return root;
		}

		private static void Sort(PathTreeNode node)
		{
			node.Children.Sort((a, b) =>
			{
				var folderOrder = b.IsFolder.CompareTo(a.IsFolder);
				if (folderOrder != 0) return folderOrder;
				var nameOrder = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
				return nameOrder != 0 ? nameOrder : StringComparer.Ordinal.Compare(a.Path, b.Path);
			});

			foreach (var child in node.Children)
			{
				Sort(child);
			}
		}
	}
}
