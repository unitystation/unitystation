using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using Util;
#endif

public static class ListExtensions
{
  	public static void RemoveEndsOfList<T>(List<T> list, int NumberElements)
	{
		list.RemoveRange((list.Count - 1) - NumberElements, NumberElements);
	}

	public static void RemoveAtIndexForwards<T>(List<T> list, int Index)
	{
		if (list.Count > 0 && (!((list.Count - 1) < Index)))
		{
			list.RemoveRange(Index, (list.Count - 1) - Index);
		}
	}

	/// <summary>
	/// Removes elements from a component's list. If inside the editor, this will also serialize the changes. When
	/// called while the scene is still loading, this will automatically save the scene once the scene has finished
	/// loading.
	/// </summary>
	/// <remarks>
	/// Due to the way prefab instances work, the exact component/gameObject instance where the list is located must be
	/// passed. For example, if you pass the component's associated gameObject instead, the prefab instance will not
	/// recognize any changes.
	/// </remarks>
	/// <param name="list">The list to run the filter on.</param>
	/// <param name="object">The GameObject or Component this list is located in.</param>
	/// <param name="scene">The scene this GameObject or Component is found in.</param>
	/// <param name="filter">Function to filter out elements.</param>
	public static void RemoveAndSerialize<T>(this IList<T> list, Object @object, Scene scene, Predicate<T> filter)
	{
		if (list is null || filter is null || @object == null) return;

		var count = list.Count;

		if (count == 0) return;

#if UNITY_EDITOR
		var isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(@object);

		if (isPrefabInstance == false) Undo.RecordObject(@object, $"Remove elements from list on {@object.name}");
#endif

		for (var i = count - 1; i >= 0; i--)
		{
			if (filter(list[i])) list.RemoveAt(i);
		}

#if UNITY_EDITOR
		if (Application.isPlaying || count == list.Count) return;

		if (isPrefabInstance) PrefabUtility.RecordPrefabInstancePropertyModifications(@object);

		SceneModifiedOnLoad.RequestSaveScene(scene);

		Loggy.Log($"\"{@object.name}\" contained a list that had elements removed.", Category.Editor);
#endif
	}
}
