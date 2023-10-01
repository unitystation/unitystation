using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Text;
using System.Threading.Tasks;
using Items;
using Logs;
using Messages.Server;

public static class SweetExtensions
{
	public static Pickupable PickupableOrNull(this GameObject go)
	{
		return go.OrNull().GetComponent<Pickupable>();
	}

	public static bool TryGetPlayer(this GameObject gameObject, out PlayerInfo player)
	{
		player = PlayerList.Instance.OrNull()?.Get(gameObject);
		return player != null;
	}

	public static PlayerInfo Player(this GameObject go)
	{
		var connectedPlayer = PlayerList.Instance.OrNull()?.Get(go);
		return connectedPlayer == PlayerInfo.Invalid ? null : connectedPlayer;
	}
	public static ItemAttributesV2 Item(this GameObject go)
	{
		return go.OrNull()?.GetComponent<ItemAttributesV2>();
	}

	public static ObjectAttributes Object(this GameObject go)
	{
		return go.OrNull()?.GetComponent<ObjectAttributes>();
	}

	public static Attributes AttributesOrNull(this GameObject go)
	{
		return go.OrNull()?.GetComponent<Attributes>();
	}

	public static bool HasComponent<T>(this GameObject go) where T : Component
	{
		return go.TryGetComponent<T>(out _);
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

		if (player != null && player.Script != null && String.IsNullOrWhiteSpace(player.Script.visibleName) == false)
		{
			return player.Script.visibleName;
		}

		return go.name.Replace("NPC_", "").Replace("_", " ").Replace("(Clone)","");
	}

	public static T GetRandom<T>(this List<T> list)
	{
		return list?.Count > 0 ? list.PickRandom() : default(T);
	}

	public static Dictionary<uint, NetworkIdentity> GetSpawned()
	{
		return CustomNetworkManager.Spawned;
	}


	public static GameObject NetIdToGameObject(this uint NetID)
	{
		if ( NetID != global::NetId.Invalid && NetID != global::NetId.Empty && CustomNetworkManager.Spawned.TryGetValue(NetID, out var Object  ))
		{
			return Object.gameObject;
		}
		else
		{
			return null;
		}

	}

	public static NetworkIdentity NetWorkIdentity(this GameObject go)
	{
		if (go)
		{
			go.TryGetComponent<Matrix>(out var matrix);
			if (matrix)
			{
				return matrix.NetworkedMatrix.MatrixSync.netIdentity;
			}
			else
			{
				matrix = go.GetComponentInChildren<Matrix>();
				if (matrix != null)
				{
					return matrix.NetworkedMatrix.MatrixSync.netIdentity;
				}
				else
				{
					return go.GetComponent<NetworkIdentity>();
				}
			}
		}
		else
		{
			return null;
		}
	}

	public static uint NetId(this GameObject go)
	{
		var net = NetWorkIdentity(go);
		if (go)
		{
			return net.netId;
		}
		else
		{
			return global::NetId.Invalid; //maxValue is invalid (see NetId.cs)
		}
	}

	/// Creates garbage! Use very sparsely!
	public static Vector3 AssumedWorldPosServer(this GameObject go)
	{
		if (go == null)
		{
			Loggy.LogError("Null object passed into AssumedWorldPosServer");
			return TransformState.HiddenPos;
		}

		return GetRootGameObject(go).transform.position;
	}


	public static Matrix GetMatrixRoot(this GameObject go)
	{
		if (ComponentManager.TryGetUniversalObjectPhysics(GetRootGameObject(go), out var UOP))
		{
			return UOP.registerTile.Matrix;
		}

		return null;
	}


	/// Creates garbage! Use very sparsely!
	public static GameObject GetRootGameObject(this GameObject go)
	{
		if (ComponentManager.TryGetUniversalObjectPhysics(go, out  var UOP))
		{
			return UOP.GetRootObject;
		}
		else
		{
			return go;
		}
	}

	/// Creates garbage! Use very sparsely!
	public static CommonComponents GetCommonComponents(this GameObject go)
	{
		if (ComponentManager.TryGetCommonComponent(go, out  var commonComponent))
		{
			return commonComponent;
		}
		else
		{
			return null;
		}
	}


	//New better system for Get component That caches results
	public static T GetComponentCustom<T>(this Component go)  where T : Component
	{
		if (ComponentManager.TryGetCommonComponent(go.gameObject, out  var commonComponent))
		{
			return commonComponent.SafeGetComponent<T>();
		}
		else
		{
			return null;
		}
	}


	//New better system for Get component That cashs results
	public static UniversalObjectPhysics GetUniversalObjectPhysics(this GameObject go)
	{
		if (ComponentManager.TryGetUniversalObjectPhysics(go, out  var commonComponent))
		{
			return commonComponent;
		}
		else
		{
			return null;
		}
	}



	//New better system for Get component That cashs results
	public static T GetComponentCustom<T>(this GameObject go)  where T : Component
	{
		if (ComponentManager.TryGetCommonComponent(go, out  var commonComponent))
		{
			return commonComponent.SafeGetComponent<T>();
		}
		else
		{
			return null;
		}
	}


	public static bool TryGetComponentCustom<T>(this Component go, out T component) where T : Component
	{
		if (ComponentManager.TryGetCommonComponent(go.gameObject, out  var commonComponent))
		{
			return commonComponent.TrySafeGetComponent<T>(out component);
		}
		else
		{
			component = null;
			return false;
		}
	}

	public static bool TryGetComponentCustom<T>(this GameObject go, out T component)  where T : Component
	{
		if (ComponentManager.TryGetCommonComponent(go, out  var commonComponent))
		{
			return commonComponent.TrySafeGetComponent<T>(out component);
		}
		else
		{
			component = null;
			return false;
		}
	}


	/// <summary>
	/// Returns true for adjacent coordinates
	/// </summary>
	public static bool IsAdjacentTo(this Vector3 one, Vector3 two)
	{
		var oneInt = one.RoundTo2Int();
		var twoInt = two.RoundTo2Int();
		return Mathf.Abs(oneInt.x - twoInt.x) == 1 ||
			Mathf.Abs(oneInt.y - twoInt.y) == 1;
	}

	/// <summary>
	/// Returns true for adjacent coordinates OR if they are the same when rounded
	/// </summary>
	public static bool IsAdjacentToOrSameAs(this Vector3 one, Vector3 two)
	{
		return one.RoundTo2Int() == two.RoundTo2Int() || one.IsAdjacentTo(two);
	}
	/// Creates garbage! Use very sparsely!
	public static RegisterTile RegisterTile(this GameObject go)
	{
		return go.OrNull()?.GetComponent<RegisterTile>();
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
			Loggy.LogTraceFormat("Lerp speed boost exceeded by {0}", Category.Movement, boost);
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
		Loggy.LogWarning($"Vector parse failed: what the hell is '{stringifiedVector}'?", Category.Unknown);
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
			throw new ArgumentException($"{nameof(chunkSize)} must be greater than 0.", nameof(chunkSize));
		}

		while (list.Any())
		{
			yield return list.Take(chunkSize);
			list = list.Skip(chunkSize);
		}
	}

	/// <summary>Get the next value in the enum. Will loop to the top.</summary>
	/// <remarks>Generates garbage; use sparingly.</remarks>
	public static T Next<T>(this T src) where T : Enum
	{
		// if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

		T[] values = (T[])Enum.GetValues(src.GetType());
		int j = Array.IndexOf(values, src) + 1;
		return (values.Length == j) ? values[0] : values[j];
	}

	/// <summary>Get a random value from the given enum.</summary>
	/// <remarks>Generates garbage; use sparingly.</remarks>
	/// <returns>A random value from the enum</returns>
	public static T PickRandom<T>(this T src) where T : Enum
	{
		return ((T[])Enum.GetValues(src.GetType())).PickRandom();
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

		return value.Substring(0, Math.Min(value.Length, maxLength));
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

	/// <summary>
	/// Gets the message and stacktrace of the exception
	/// </summary>
	public static string GetStack(this Exception source)
	{
		return $"{source.Message}\n{source.StackTrace}";
	}

	/// <summary>
	/// Invokes the given action with the task result when it finishes.
	/// The action is invoked from the same thread as where the task was initialised.
	/// </summary>
	public static Task Then(this Task task, Action<Task> callback)
	{
		return task.ContinueWith(callback, TaskScheduler.FromCurrentSynchronizationContext());
	}

	///<inheritdoc cref="Then(Task, Action{Task})"/>
	public static Task Then<T>(this Task<T> task, Action<Task<T>> callback)
	{
		return task.ContinueWith(callback, TaskScheduler.FromCurrentSynchronizationContext());
	}

	/// <summary>
	/// Logs any exceptions a task might throw, handling any <c>AggregateException</c>s.
	/// </summary>
	public static void LogFaultedTask(this Task task, Category category = Category.Unknown)
	{
		if (task.IsFaulted == false) return;

		var e = task.Exception?.GetBaseException();
		// GetBaseException() does not seem to work for HttpRequestExceptions
		for (int i = 0; i < 10; i++)
		{
			if (e?.InnerException == null) break;
			e = e.InnerException;
		}

		Loggy.LogError(e?.ToString(), category);
	}

	/// <summary>
	/// Enable this component from the server for all clients, including the server.
	/// </summary>
	/// <remarks>
	/// New joins / rejoins won't have synced state with server (good TODO).
	/// Assumes the component hierarchy on the gameobject is in sync with the server.
	/// </remarks>
	/// <param name="component">The component to enable</param>
	public static void NetEnable(this Behaviour component)
	{
		if (component == null) return;

		EnableComponentMessage.Send(component, true);

		component.enabled = true;
	}

	/// <summary>
	/// Disable this component from the server for all clients, including the server.
	/// </summary>
	/// <remarks>
	/// New joins / rejoins won't have synced state with server (good TODO).
	/// Assumes the component hierarchy on the gameobject is in sync with the server.
	/// </remarks>
	/// <param name="component">The component to disable</param>
	public static void NetDisable(this Behaviour component)
	{
		if (component == null) return;

		EnableComponentMessage.Send(component, false);

		component.enabled = false;
	}

	/// <summary>
	/// Set the active state of this component from the server for all clients, including the server.
	/// </summary>
	/// <remarks>
	/// New joins / rejoins won't have synced state with server (good TODO).
	/// Assumes the component hierarchy on the gameobject is in sync with the server.
	/// </remarks>
	/// <param name="component">The component to change state on</param>
	/// <param name="value">The component's new active state</param>
	public static void NetSetActive(this Behaviour component, bool value)
	{
		if (component == null) return;

		EnableComponentMessage.Send(component, value);

		component.enabled = value;
	}

	public static string ToHexString(this string str)
	{
		var sb = new StringBuilder();

		var bytes = Encoding.Unicode.GetBytes(str);
		foreach (var t in bytes)
		{
			sb.Append(t.ToString("X2"));
		}

		return sb.ToString();
	}

	public static Vector3Int ToLocalVector3Int(this OrientationEnum @in)
	{
		return @in switch
		{
			OrientationEnum.Up_By0 => Vector3Int.up,
			OrientationEnum.Right_By270 => Vector3Int.right,
			OrientationEnum.Down_By180 => Vector3Int.down,
			OrientationEnum.Left_By90 => Vector3Int.left,
			_ => Vector3Int.zero
		};
	}


	public static float VectorToAngle360(this Vector2 vector)
	{
		float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
		if (angle < 0)
			angle += 360f;
		return angle;
	}

	public static float Rotate360By(this OrientationEnum dir, float finalAngle)
	{
		switch (dir)
		{
			case OrientationEnum.Default:
				 break;
			case OrientationEnum.Right_By270:
				 finalAngle = finalAngle + 270;
				 break;
			case OrientationEnum.Up_By0:
				 finalAngle = finalAngle + 0;
				 break;
			case OrientationEnum.Left_By90:
				finalAngle = finalAngle + 90;
				break;
			case OrientationEnum.Down_By180:
				finalAngle = finalAngle + 180;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		// If the final angle is greater than or equal to 360 or less than 0, wrap it around.
		if (finalAngle >= 360)
		{
			finalAngle -= 360;
		}
		else if (finalAngle < 0)
		{
			finalAngle += 360;
		}
		return finalAngle;
	}

	public static OrientationEnum GetOppositeDirection(this OrientationEnum dir)
	{
		switch (dir)
		{
			case OrientationEnum.Default:
				return OrientationEnum.Down_By180;
			case OrientationEnum.Right_By270:
				return OrientationEnum.Left_By90;
			case OrientationEnum.Up_By0:
				return OrientationEnum.Default;
			case OrientationEnum.Left_By90:
				return OrientationEnum.Right_By270;
			case OrientationEnum.Down_By180:
				return OrientationEnum.Up_By0;
			default:
				throw new ArgumentOutOfRangeException();
		}
		return OrientationEnum.Down_By180;
	}

	public static string RemovePunctuation(this string input)
	{
		return new string(input.Where(c => !char.IsPunctuation(c)).ToArray());
	}

	public static string GetTheyPronoun(this GameObject gameObject)
	{
		if (gameObject.TryGetComponent<PlayerScript>(out var playerScript) && playerScript.characterSettings != null)
		{
			return playerScript.characterSettings.TheyPronoun(playerScript).Capitalize();
		}

		return "It";
	}

	public static string GetTheirPronoun(this GameObject gameObject)
	{
		if (gameObject.TryGetComponent<PlayerScript>(out var playerScript) && playerScript.characterSettings != null)
		{
			return playerScript.characterSettings.TheirPronoun(playerScript).Capitalize();
		}

		return "Its";
	}

	public static string GetThemPronoun(this GameObject gameObject)
	{
		if (gameObject.TryGetComponent<PlayerScript>(out var playerScript) && playerScript.characterSettings != null)
		{
			return playerScript.characterSettings.ThemPronoun(playerScript).Capitalize();
		}

		return "It";
	}

	public static string GetTheyrePronoun(this GameObject gameObject)
	{
		if (gameObject.TryGetComponent<PlayerScript>(out var playerScript) && playerScript.characterSettings != null)
		{
			return playerScript.characterSettings.TheyrePronoun(playerScript).Capitalize();
		}

		return "Its";
	}

	/// <summary>
	/// returns a list of children of that are under a gameObject.
	/// </summary>
	public static List<GameObject> GetAllChildren(this GameObject gameObject)
	{
		return (from Transform child in gameObject.transform select child.gameObject).ToList();
	}

	/// <summary>
	/// Destroys all children that are under a gameObject. Only use this for client-side objects or UI elements.
	/// Use the despawn class when dealing with networked objects.
	/// </summary>
	public static void DestroyAllChildren(this GameObject gameObject)
	{
		foreach (var child in GetAllChildren(gameObject))
		{
			// Do not use DestroyImmediate() as that will modify the collection before the end of the frame.
			UnityEngine.Object.Destroy(child);
		}
	}

	/// <summary>
	/// Returns an offset for a single axis for a vector. Axis offset is random.
	/// </summary>
	public static Vector3 RandomOnOneAxis(this Vector3 vector3, int min, int max, bool neverZero = true)
	{
		var axis = Random.Range(0, 2);
		var y =  Random.Range(min, max);
		var x =  Random.Range(min, max);

		if (neverZero)
		{
			if (y == 0) y += min;
			if (x == 0) x += min;
		}

		if (axis == 0)
		{
			vector3.x += x;
			vector3.y += y;
		}
		else if (axis == 1)
		{
			vector3.x += x;
		}
		else if (axis == 2)
		{
			vector3.y += y;
		}
		return vector3;
	}
}
