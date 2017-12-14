#if UNITY_EDITOR
using System;
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
            List<string> sortingLayerNames = GetSortingLayerNames();

            int x_index = sortingLayerNames.FindIndex(s => s.Equals(x.sortingLayerName));
            int y_index = sortingLayerNames.FindIndex(s => s.Equals(y.sortingLayerName));

            if (x_index == y_index)
            {
                return x.sortingOrder - y.sortingOrder;
            }
            return x_index - y_index;
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