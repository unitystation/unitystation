using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumerableExt
{
	public static T PickRandom<T>(this IEnumerable<T> source)
	{
		return source.PickRandom(1).Single();
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
}