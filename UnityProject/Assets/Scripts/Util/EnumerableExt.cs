using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

public static class EnumerableExt
{
	public static T PickRandom<T>(this IEnumerable<T> source)
	{
		return source.PickRandom(1).SingleOrDefault();
	}

	public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
	{
		return source.Shuffle().Take(count);
	}

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
	{
		return source.OrderBy(x => Guid.NewGuid());
	}

	public static bool AreEquivalent<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
	{
		return list1.Count() == list2.Count() && !list1.Except(list2).Any();
	}

	/// <summary>
	/// Projects a sequence into an <see cref="IEnumerable{T}"/> of value tuples containing the object and the index.
	/// </summary>
	public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> source)
	{
		return source.Select((t, i) => (t, i));
	}

	/// <summary>
	/// Filters out all null objects in a sequence.
	/// </summary>
	public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source)
	{
		return source.Where(obj => obj != null);
	}

	#region GameObject and Components
	// Unfortunately there is no common interface for GameObjects and Components to access the GetComponent*
	// functions.

	/// <summary>
	/// Projects a sequence of GameObjects into a flattened <see cref="IEnumerable{T}"/> of their components filtered
	/// by type.
	/// Avoids garbage from retrieving components by using a pooled list, but still causes enumerator allocations.
	/// </summary>
	/// <param name="source">A sequence of GameObjects.</param>
	/// <typeparam name="T">The type of <see cref="Component"/> to filter for.</typeparam>
	/// <returns>A flattened sequence of components.</returns>
	public static IEnumerable<T> Components<T>(this IEnumerable<GameObject> source)
	{
		return source.GetComponentsInternal<GameObject, T>((go, results) => go.GetComponents(results));
	}

	/// <summary>
	/// Projects a sequence of Components into a flattened <see cref="IEnumerable{T}"/> of the parent GameObject's
	/// components filtered by type.
	/// Avoids garbage from retrieving components by using a pooled list, but still causes enumerator allocations.
	/// </summary>
	/// <param name="source">A sequence of Components.</param>
	/// <typeparam name="T">The type of <see cref="Component"/> to filter for.</typeparam>
	/// <returns>A flattened sequence of components.</returns>
	public static IEnumerable<T> Components<T>(this IEnumerable<Component> source)
	{
		return source?.GetComponentsInternal<Component, T>((comp, results) => comp.GetComponents(results));
	}


	/// <summary>
	/// Projects a sequence of GameObjects into an <see cref="IEnumerable{T}"/> of the child component filtered by type.
	/// Avoids garbage from retrieving components by using a pooled list, but still causes enumerator allocations.
	/// </summary>
	/// <param name="source">A sequence of GameObjects.</param>
	/// <typeparam name="T">The type of <see cref="Component"/> to filter for.</typeparam>
	/// <returns>A flattened sequence of components.</returns>
	public static IEnumerable<T> ComponentInChildren<T>(this IEnumerable<GameObject> source)
	{
		return source?.Select(go => go.GetComponentInChildren<T>());
	}

	/// <summary>
	/// Projects a sequence of Components into an <see cref="IEnumerable{T}"/> of the parent GameObject's child component
	/// filtered by type.
	/// Avoids garbage from retrieving components by using a pooled list, but still causes enumerator allocations.
	/// </summary>
	/// <param name="source">A sequence of Components.</param>
	/// <typeparam name="T">The type of <see cref="Component"/> to filter for.</typeparam>
	/// <returns>A flattened sequence of components.</returns>
	public static IEnumerable<T> ComponentInChildren<T>(this IEnumerable<Component> source)
	{
		return source?.Select(go => go.GetComponentInChildren<T>());
	}

	/// <summary>
	/// Projects a sequence of GameObjects into a flattened <see cref="IEnumerable{T}"/> of all child components
	/// filtered by type.
	/// Avoids garbage from retrieving components by using a pooled list, but still causes enumerator allocations.
	/// </summary>
	/// <param name="source">A sequence of GameObjects.</param>
	/// <typeparam name="T">The type of <see cref="Component"/> to filter for.</typeparam>
	/// <returns>A flattened sequence of components.</returns>
	public static IEnumerable<T> ComponentsInChildren<T>(this IEnumerable<GameObject> source)
	{
		return source?.GetComponentsInternal<GameObject, T>((go, results) => go.GetComponentsInChildren(results));
	}

	/// <summary>
	/// Projects a sequence of Components into a flattened <see cref="IEnumerable{T}"/> of all child components
	/// filtered by type.
	/// Avoids garbage from retrieving components by using a pooled list, but still causes enumerator allocations.
	/// </summary>
	/// <param name="source">A sequence of Components.</param>
	/// <typeparam name="T">The type of <see cref="Component"/> to filter for.</typeparam>
	/// <returns>A flattened sequence of components.</returns>
	public static IEnumerable<T> ComponentsInChildren<T>(this IEnumerable<Component> source)
	{
		return source?.GetComponentsInternal<Component, T>((comp, results) => comp.GetComponentsInChildren(results));
	}

	private static IEnumerable<TResult> GetComponentsInternal<TSource, TResult>(
		this IEnumerable<TSource> source,
		Action<TSource, List<TResult>> getComponentsCallback)
	{
		if (getComponentsCallback is null) yield break;

		using var pool = ListPool<TResult>.Get(out var results);

		foreach (var obj in source)
		{
			if (obj == null) continue;

			getComponentsCallback(obj, results);

			foreach (var result in results)
			{
				yield return result;
			}
		}
	}

	#endregion
}
