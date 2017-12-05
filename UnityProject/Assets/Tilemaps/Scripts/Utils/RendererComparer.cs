#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;

namespace Tilemaps.Scripts.Utils
{
    public static class RendererComparer
    {
        public static int Compare(Renderer x, Renderer y)
        {
            var sortingLayerNames = GetSortingLayerNames();

            var x_index = sortingLayerNames.FindIndex(s => s.Equals(x.sortingLayerName));
            var y_index = sortingLayerNames.FindIndex(s => s.Equals(y.sortingLayerName));

            if (x_index == y_index)
            {
                return x.sortingOrder - y.sortingOrder;
            }
            return x_index - y_index;
        }

        private static List<string> GetSortingLayerNames()
        {
            var internalEditorUtilityType = typeof(InternalEditorUtility);
            var sortingLayersProperty =
                internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            var sortingLayerNames = (string[])sortingLayersProperty?.GetValue(null, new object[0]);

            return sortingLayerNames != null ? new List<string>(sortingLayerNames) : null;
        }
    }
}
#endif