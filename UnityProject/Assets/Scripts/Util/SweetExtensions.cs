using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Text;
using Items;

public static class SweetExtensions
{
	public static IPushable Pushable(this GameObject go)
	{
		return go.GetComponent<IPushable>();
	}

	public static ConnectedPlayer Player(this GameObject go)
	{
		var connectedPlayer = PlayerList.Instance?.Get(go);
		return connectedPlayer == ConnectedPlayer.Invalid ? null : connectedPlayer;
	}
	public static ItemAttributesV2 Item(this GameObject go)
	{
		return go.OrNull()?.GetComponent<ItemAttributesV2>();
	}

	public static ObjectAttributes Object(this GameObject go)
	{
		return go.OrNull()?.GetComponent<ObjectAttributes>();
	}

	/// <summary>
	/// Returns human-readable object name for IC texts
	/// </summary>
	public static string ExpensiveName(this GameObject go)
	{
		var item = go.Item();
		if (item)
		{
			// try get current instance name
			if (!String.IsNullOrWhiteSpace(item.ArticleName))
			{
				return item.ArticleName;
			}

			// maybe it's non-instanced prefab - get initial name
			if (!String.IsNullOrWhiteSpace(item.InitialName))
			{
				return item.InitialName;
			}
		}

		var entityObject = go.Object();
		if (entityObject != null)
		{
			if (!string.IsNullOrWhiteSpace(entityObject.ArticleName))
			{
				return entityObject.ArticleName;
			}

			if (!string.IsNullOrWhiteSpace(entityObject.InitialName))
			{
				return entityObject.InitialName;
			}
		}

		var player = go.Player();
		if (player != null && !String.IsNullOrWhiteSpace(player.Script.visibleName))
		{
			return player.Script.visibleName;
		}

		return go.name.Replace("NPC_", "").Replace("_", " ").Replace("(Clone)","");
	}

	public static T GetRandom<T>(this List<T> list)
	{
		return list?.Count > 0 ? list.PickRandom() : default(T);
	}

	public static uint NetId(this GameObject go)
	{
		if (go)
		{
			go.TryGetComponent<Matrix>(out var matrix);
			if (matrix)
			{
				return matrix.NetworkedMatrix.MatrixSync.netId;
			}
			else
			{
				matrix = go.GetComponentInChildren<Matrix>();
				if (matrix != null)
				{
					return matrix.NetworkedMatrix.MatrixSync.netId;
				}
				else
				{
					return go.GetComponent<NetworkIdentity>().netId;
				}
			}
		}
		else
		{
			return global::NetId.Invalid; //maxValue is invalid (see NetId.cs)
		}
	}

	/// Creates garbage! Use very sparsely!
	public static Vector3 AssumedWorldPosServer(this GameObject go)
	{
		return go.GetComponent<ObjectBehaviour>()?.AssumedWorldPositionServer() ?? WorldPosServer(go);
	}
	/// Creates garbage! Use very sparsely!
	public static Vector3 WorldPosServer(this GameObject go)
	{
		return go.GetComponent<RegisterTile>()?.WorldPositionServer ?? go.transform.position;
	}
	/// Creates garbage! Use very sparsely!
	public static Vector3 WorldPosClient(this GameObject go)
	{
		return go.GetComponent<RegisterTile>()?.WorldPositionClient ?? go.transform.position;
	}

	/// <summary>
	/// Returns true for adjacent coordinates
	/// </summary>
	public static bool IsAdjacentTo(this Vector3 one, Vector3 two)
	{
		var oneInt = one.To2Int();
		var twoInt = two.To2Int();
		return Mathf.Abs(oneInt.x - twoInt.x) == 1 ||
			Mathf.Abs(oneInt.y - twoInt.y) == 1;
	}

	/// <summary>
	/// Returns true for adjacent coordinates OR if they are the same when rounded
	/// </summary>
	public static bool IsAdjacentToOrSameAs(this Vector3 one, Vector3 two)
	{
		return one.To2Int() == two.To2Int() || one.IsAdjacentTo(two);
	}
	/// Creates garbage! Use very sparsely!
	public static RegisterTile RegisterTile(this GameObject go)
	{
		return go.GetComponent<RegisterTile>();
	}

	/// Wraps provided index value if it's more than array length or is negative
	public static T Wrap<T>(this T[] array, int index)
	{
		if (array == null || array.Length == 0)
		{
			return default(T);
		}
		return array[((index % array.Length) + array.Length) % array.Length];
	}

	/// Wraps provided index value if it's more than list length or is negative
	public static T Wrap<T>(this List<T> list, int index)
	{
		if (list == null || list.Count == 0)
		{
			return default(T);
		}
		return list[((index % list.Count) + list.Count) % list.Count];
	}

	/// <summary>
	/// Returns valid, wrapped index of overflown provided index
	/// </summary>
	public static int WrappedIndex<T>(this List<T> list, int index)
	{
		return ((index % list.Count) + list.Count) % list.Count;
	}
	/// <summary>
	/// Returns valid, wrapped index of overflown provided index
	/// </summary>
	public static int WrappedIndex<T>(this T[] array, int index)
	{
		return ((index % array.Length) + array.Length) % array.Length;
	}

	public static BoundsInt BoundsAround(this Vector3Int pos)
	{
		return new BoundsInt(pos - new Vector3Int(1, 1, 0), new Vector3Int(3, 3, 1));
	}

	private const float NO_BOOST_THRESHOLD = 1.5f;

	/// Lerp speed modifier
	public static float SpeedTo(this Vector3 lerpFrom, Vector3 lerpTo)
	{
		float distance = Vector2.Distance(lerpFrom, lerpTo);
		if (distance <= NO_BOOST_THRESHOLD)
		{
			return 1;
		}

		float boost = (distance - NO_BOOST_THRESHOLD) * 2;
		if (boost > 0)
		{
			Logger.LogTraceFormat("Lerp speed boost exceeded by {0}", Category.Movement, boost);
		}
		return 1 + boost;
	}

	/// Randomized hitzone. 0f for totally random, 0.99f for 99% chance of provided one
	/// <param name="aim"></param>
	/// <param name="hitProbability">0f to 1f: chance of hitting the requested body part</param>
	public static BodyPartType Randomize(this BodyPartType aim, float hitProbability = 0.8f)
	{
		float normalizedRange = Mathf.Clamp(hitProbability, 0f, 1f);
		if (Random.value < (normalizedRange / 100f))
		{
			return aim;
		}
		int t = (int) Mathf.Floor(Random.value * 50);
		//	3/50
		if (t <= 3)
			return BodyPartType.Head;
		if (t <= 10)
			//	7/50
			return BodyPartType.LeftArm;
		if (t <= 17)
			//	7/50
			return BodyPartType.RightArm;
		if (t <= 24)
			//	7/50
			return BodyPartType.LeftLeg;
		if (t <= 31)
			//	7/50
			return BodyPartType.RightLeg;
		if (t <= 41)
			//	7/50
			return BodyPartType.Chest;
		//	9/50
		return BodyPartType.Groin;
	}

	/// Serializing Vector2 (rounded to int) into plaintext
	public static string Stringified(this Vector2 pos)
	{
		return (int) pos.x + "x" + (int) pos.y;
	}

	/// Deserializing Vector2(Int) from plaintext.
	/// In case of parse error returns HiddenPos
	public static Vector3 Vectorized(this string stringifiedVector)
	{
		var posData = stringifiedVector.Split('x');
		int x, y;
		if (posData.Length > 1 && int.TryParse(posData[0], out x) && int.TryParse(posData[1], out y))
		{
			return new Vector2(x, y);
		}
		Logger.LogWarning($"Vector parse failed: what the hell is '{stringifiedVector}'?", Category.Unknown);
		return TransformState.HiddenPos;
	}

	/// Good looking job name
	public static string JobString(this JobType job)
	{
		return job.ToString().Equals("NULL") ? "*just joined" : textInfo.ToTitleCase(job.ToString().ToLower()).Replace("_", " ");
	}
	//For job formatting purposes
	private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

	/// <summary>
	/// Gets the components only in immediate children of parent.
	/// </summary>
	/// <returns>The components only in children.</returns>
	/// <param name="script">MonoBehaviour Script, e.g. "this".</param>
	/// <param name="isRecursive">If set to <c>true</c> recursive search of children is performed.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	public static T[] GetComponentsOnlyInChildren<T>(this MonoBehaviour script, bool isRecursive = false) where T : class
	{
		if (isRecursive)
			return script.GetComponentsOnlyInChildren_Recursive<T>();
		return script.GetComponentsOnlyInChildren_NonRecursive<T>();
	}

	/// <summary>
	/// Gets the components only in children transform search. Not recursive, ie not grandchildren!
	/// </summary>
	/// <returns>The components only in children transform search.</returns>
	/// <param name="parent">Parent, ie "this".</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	static T[] GetComponentsOnlyInChildren_NonRecursive<T>(this MonoBehaviour parent) where T : class
	{
		if (parent.transform.childCount <= 0) return null;

		var output = new List<T>();

		for (int i = 0; i < parent.transform.childCount; i++)
		{
			var component = parent.transform.GetChild(i).GetComponent<T>();
			if (component != null)
				output.Add(component);
		}
		if (output.Count > 0)
			return output.ToArray();

		return null;
	}

	/// <summary>
	/// Gets the components only in children, recursively for children of children.
	/// </summary>
	/// <returns>The components only in children of calling parent.</returns>
	/// <param name="parent">Parent.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	static T[] GetComponentsOnlyInChildren_Recursive<T>(this MonoBehaviour parent) where T : class
	{
		if (parent.transform.childCount <= 0) return null;

		var transforms = new HashSet<Transform>(parent.GetComponentsInChildren<Transform>());
		transforms.Remove(parent.transform);

		var output = new List<T>();
		foreach (var child in transforms)
		{
			var component = child.GetComponent<T>();
			if (component != null)
			{
				output.Add(component);
			}
		}

		if (output.Count > 0)
			return output.ToArray();

		return null;
	}

	public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator) {
		while ( enumerator.MoveNext() ) {
			yield return enumerator.Current;
		}
	}

	/// <summary>
	/// Splits an enumerable into chunks of a specified size
	/// Credit to: https://extensionmethod.net/csharp/ienumerable/ienumerable-chunk
	/// </summary>
	/// <param name="list"></param>
	/// <param name="chunkSize"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int chunkSize)
	{
		if (chunkSize <= 0)
		{
			throw new ArgumentException("chunkSize must be greater than 0.");
		}

		while (list.Any())
		{
			yield return list.Take(chunkSize);
			list = list.Skip(chunkSize);
		}
	}

	/// <summary>
	/// Helped function for enums to get the next value when sorted by their base type
	/// </summary>
	public static T Next<T>(this T src) where T : Enum
	{
		// if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

		T[] Arr = (T[])Enum.GetValues(src.GetType());
		int j = Array.IndexOf<T>(Arr, src) + 1;
		return (Arr.Length==j) ? Arr[0] : Arr[j];
	}

	/// <summary>
	/// Removes all KeyValuePairs where each pair matches the given predicate.
	/// Courtesy of https://www.codeproject.com/Tips/494499/Implementing-Dictionary-RemoveAll.
	/// </summary>
	public static void RemoveAll<K, V>(this IDictionary<K, V> dict, Func<K, V, bool> match)
	{
		foreach (var key in dict.Keys.ToArray()
				.Where(key => match(key, dict[key])))
			dict.Remove(key);
	}

	/// <summary>
	/// Enumerate all flags as IEnumerable
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public static IEnumerable<Enum> GetFlags(this Enum input)
	{
		foreach (Enum value in Enum.GetValues(input.GetType()))
			if (input.HasFlag(value))
				yield return value;
	}

	/// <summary>
	/// direct port of java's Map.getOrDefault
	/// </summary>
	public static V GetOrDefault<T,V>(this Dictionary<T, V> dic, T key, V defaultValue)
	{
		V v;
		return (dic.ContainsKey(key) && ((v = dic[key]) != null))
			? v
			: defaultValue;

	}

	/// <summary>
	/// Removes the last instance of the given string from the given StringBuilder.
	/// </summary>
	/// <returns>the final StringBuilder</returns>
	public static StringBuilder RemoveLast(this StringBuilder sb, string str)
	{
		if (sb.Length < 1) return sb;

		sb.Remove(sb.ToString().LastIndexOf(str), str.Length);
		return sb;
	}

	public static Vector3 GetRandomPoint(this Bounds bounds)
	{
		return new Vector3(
			UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
			UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
			UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
		);
	}

	public static Vector3 GetRandomPoint(this BoundsInt bounds)
	{
		return new Vector3(
			UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
			UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
			UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
		);
	}

	public static string Capitalize(this string text)
	{
		return text[0].ToString().ToUpper() + text.Substring(1);
	}

	/// <summary>
	/// Extension for all IComparables, like numbers and dates. Returns true if given data
	/// is between the min and max values. By default, it is inclusive.
	/// </summary>
	/// <param name="value">Value to compare</param>
	/// <param name="min">Minimum value in the range</param>
	/// <param name="max">Maximum value in the range</param>
	/// <param name="inclusive">Changes the behavior for min and max, true by default</param>
	/// <typeparam name="T"></typeparam>
	/// <returns>True if the given value is  between the given range</returns>
	public static bool IsBetween<T>(this T value, T min, T max, bool inclusive=true) where T : IComparable
	{
		return inclusive
			? Comparer<T>.Default.Compare(value, min) >= 0
			  && Comparer<T>.Default.Compare(value, max) <= 0
			: Comparer<T>.Default.Compare(value, min) > 0
			  && Comparer<T>.Default.Compare(value, max) < 0;
	}

	/// <summary>
	/// See <see cref="Mathf.Approximately(float, float)"/>
	/// </summary>
	public static bool Approx(this float thisValue, float value)
	{
		return Mathf.Approximately(thisValue, value);
	}

	/// <summary>
	/// See <see cref="Mathf.Clamp(float, float, float)"/>
	/// </summary>
	public static float Clamp(this float value, float minValue, float maxValue)
	{
		return Mathf.Clamp(value, minValue, maxValue);
	}

	/// <summary>
	/// See if two colours are approximately the same
	/// </summary>
	public static bool ColorApprox(this Color a, Color b, bool checkAlpha = true)
	{
		if (checkAlpha)
		{
			return Mathf.Approximately(a.b, b.b) &&
			       Mathf.Approximately(a.r, b.r) &&
			       Mathf.Approximately(a.g, b.g) &&
			       Mathf.Approximately(a.a, b.a);
		}

		return Mathf.Approximately(a.b, b.b) &&
			   Mathf.Approximately(a.r, b.r) &&
		       Mathf.Approximately(a.g, b.g);
	}


	public static string Truncate(this string value, int maxLength)
	{
		if (string.IsNullOrEmpty(value)) return value;
		return value.Length <= maxLength ? value : value.Substring(0, maxLength);
  }

	/// <summary>
	/// <para>Get specific type from a list.</para>
	/// Credit to <see href="https://coderethinked.com/get-only-specific-types-from-list/">Karthik Chintala</see>.
	/// </summary>
	public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
	{
		if (source == null) return null;
		return OfTypeIterator<TResult>(source);
	}

	private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
	{
		foreach (object obj in source)
		{
			if (obj is TResult) yield return (TResult)obj;
		}
	}

	/// <summary>
	/// Rounds float to largest eg 1.1 => 2, -0.1 => -1
	/// </summary>
	public static int RoundToLargestInt(this float source)
	{
		if (source < 0)
		{
			return Mathf.FloorToInt(source);
		}

		return Mathf.CeilToInt(source);
	}
}
