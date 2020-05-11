using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

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
		return go.GetComponent<ItemAttributesV2>();
	}

	public static string ExpensiveName(this GameObject go)
	{
		var item = go.Item();
		if (item != null && !String.IsNullOrWhiteSpace(item.ArticleName)) return item.ArticleName;

		var player = go.Player();
		if (player != null && !String.IsNullOrWhiteSpace(player.Name)) return player.Name;

		return go.name.Replace("NPC_", "").Replace("_", " ").Replace("(Clone)","");
	}

	public static T GetRandom<T>(this List<T> list)
	{
		return list?.Count > 0 ? list.PickRandom() : default(T);
	}

	public static uint NetId(this GameObject go)
	{
		return go ? go.GetComponent<NetworkIdentity>().netId : global::NetId.Invalid; //maxValue is invalid (see NetId.cs)
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
		return array[((index % array.Length) + array.Length) % array.Length];
	}

	/// Wraps provided index value if it's more than list length or is negative
	public static T Wrap<T>(this List<T> list, int index)
	{
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
			Logger.LogTraceFormat("Lerp speed boost exceeded by {0}", Category.Lerp, boost);
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
		Logger.LogWarning($"Vector parse failed: what the hell is '{stringifiedVector}'?", Category.NetUI);
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
}