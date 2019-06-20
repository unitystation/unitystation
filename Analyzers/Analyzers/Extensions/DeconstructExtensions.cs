using System.Linq;
using System.Collections.Generic;

namespace Analyzers.Extensions
{
	public static class DeconstructExtensions
	{
		/// <summary>
		/// Deconstructs an IEnumerable containing at least two entries.
		/// </summary>
		/// <example>
		///		var (first, second) = someIterable
		/// </example>
		public static void Deconstruct<T>(this IEnumerable<T> nodes, out T a, out T b)
		{
			a = default(T);
			b = default(T);

			using (var iter = nodes.GetEnumerator())
			{
				iter.MoveNext();
				a = iter.Current;
				iter.MoveNext();
				b = iter.Current;
			}
		}
	}
}
