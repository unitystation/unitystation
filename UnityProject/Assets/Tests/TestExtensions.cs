using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace Tests
{
	/// <summary>
	/// Extensions designed for testing purposes only.
	/// </summary>
	public static class TestExtensions
	{
		/// <summary>
		/// Get the name of the unity object including the name of all its parents.
		/// </summary>
		public static string HierarchyName(this Transform transform, char separator = '/')
		{
			using var pool = ListPool<string>.Get(out var parentNames);
			var parent = transform.parent;

			while (parent != null)
			{
				parentNames.Add(parent.name);
				parent = parent.transform.parent;
			}

			parentNames.Reverse();
			return parentNames.Count == 0
				? string.Empty
				: parentNames.Aggregate(parentNames[0], (current, name) => $"{current}{separator}{name}");
		}
	}
}