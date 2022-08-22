using System.Collections.Generic;
using UnityEditor;
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
			if (transform == null) return string.Empty;

			using var pool = ListPool<string>.Get(out var names);
			var current = transform;

			while (current != null)
			{
				names.Add(current.name);
				current = current.transform.parent;
			}

			names.Reverse();
			return string.Join(separator, names);
		}

		public static string NameAndPosition(this Transform transform, string prefix = null)
		{
			if (transform == null) return string.Empty;

			prefix = prefix is null ? string.Empty : $"{prefix} ";
			return $"{prefix}\"{transform.HierarchyName()}\" at {transform.localPosition}";
		}
	}
}