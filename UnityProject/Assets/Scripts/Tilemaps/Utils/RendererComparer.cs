#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;

namespace Tilemaps.Utils
{
	public static class RendererComparer
	{
		public static int Compare(Renderer x, Renderer y)
		{
			List<string> sortingLayerNames = GetSortingLayerNames();

			int xIndex = sortingLayerNames.FindIndex(s => s.Equals(x.sortingLayerName));
			int yIndex = sortingLayerNames.FindIndex(s => s.Equals(y.sortingLayerName));

			if (xIndex == yIndex)
			{
				return x.sortingOrder - y.sortingOrder;
			}
			return xIndex - yIndex;
		}
		
		public static int CompareDescending(Renderer x, Renderer y)
		{
			return Compare(y, x);
		}

		private static List<string> GetSortingLayerNames()
		{
			Type internalEditorUtilityType = typeof(InternalEditorUtility);
			PropertyInfo sortingLayersProperty =
				internalEditorUtilityType.GetProperty("sortingLayerNames",
					BindingFlags.Static | BindingFlags.NonPublic);
			string[] sortingLayerNames = (string[]) sortingLayersProperty?.GetValue(null, new object[0]);

			return sortingLayerNames != null ? new List<string>(sortingLayerNames) : null;
		}
	}
}
#endif