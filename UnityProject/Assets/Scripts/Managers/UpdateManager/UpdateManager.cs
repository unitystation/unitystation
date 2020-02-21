using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

/// <summary>
///     Handles the update methods for in game objects
///     Handling the updates from a single point decreases cpu time
///     and increases performance
/// </summary>
public class UpdateManager : MonoBehaviour
{
	private Dictionary<CallbackType, CallbackCollection> collections;

    public static bool IsInitialized { get { return instance != null; } }
	private static UpdateManager instance;

	// TODO: Obsolete, remove when no longer used.
	public static UpdateManager Instance { get { return instance; } }

	private class NamedAction
	{
		public Action Action;
		public string Name;
		public bool WaitingForRemove;
	}

	private class CallbackCollection
	{
		// Double collection: List for fast iteration, dictionary for O(1) removal.
		// Trading memory for cpu perf.

		public readonly List<NamedAction> ActionList = new List<NamedAction>(128);
		public readonly Dictionary<Action, NamedAction> ActionDictionary = new Dictionary<Action, NamedAction>(128);
	}

	public static void Add(CallbackType type, Action action)
	{
		instance.AddCallbackInternal(type, action);
	}

	[Obsolete("This will be removed in the future. Use UpdateManager.Add(CallbackType, Action) instead.")]
	public void Add(Action updatable)
	{
		Add(CallbackType.UPDATE, updatable);
	}

	[Obsolete("This will be removed in the future. Use UpdateManager.Remove(CallbackType, Action) instead.")]
	public void Remove(Action updatable)
	{
		Add(CallbackType.UPDATE, updatable);
	}

	public static void Add(ManagedNetworkBehaviour networkBehaviour)
	{
		instance.AddCallbackInternal(CallbackType.UPDATE, networkBehaviour.UpdateMe);
		instance.AddCallbackInternal(CallbackType.FIXED_UPDATE, networkBehaviour.FixedUpdateMe);
		instance.AddCallbackInternal(CallbackType.LATE_UPDATE, networkBehaviour.LateUpdateMe);
	}

	public static void Remove(CallbackType type, Action action)
	{
		var callbackCollection = instance.collections[type];

		instance.RemoveCallbackInternal(callbackCollection, action);
	}

	public static void Remove(ManagedNetworkBehaviour networkBehaviour)
	{
		Remove(CallbackType.UPDATE, networkBehaviour.UpdateMe);
		Remove(CallbackType.FIXED_UPDATE, networkBehaviour.FixedUpdateMe);
		Remove(CallbackType.LATE_UPDATE, networkBehaviour.LateUpdateMe);
	}

	private void ProcessCallbacks(CallbackCollection collection)
	{
		List<NamedAction> callbackList = collection.ActionList;

		// Iterate backwards so we can remove at O(1) while still iterating the rest.
		int startCount = callbackList.Count;
		int count = startCount;
		for (int i = count - 1; i >= 0; --i)
		{
			NamedAction namedAction = callbackList[i];
			Action callback = namedAction.Action;

			if (namedAction.WaitingForRemove)
			{
				// When removing from a list, everything else will shift to fill in the gaps.
				// To avoid this, we swap this item to the back of the list.
				// At the end of iteration, we remove the items marked for removal from the back (can be multiple) so no other memory has to shift.
				NamedAction last = callbackList[count - 1];
				callbackList[count - 1] = namedAction;
				callbackList[i] = last;
				count--;
				continue;
			}

			try
			{
				callback?.Invoke();
			}
			catch (Exception e)
			{
				// Catch the exception so it does not break flow of all callbacks
				// But still log it to Unity console so we know something happened
				Debug.LogException(e);

				// Get rid of it.
				RemoveCallbackInternal(collection, callback);
			}
		}

		callbackList.RemoveRange(count, startCount - count);
	}

	private void AddCallbackInternal(CallbackType type, Action action)
	{
		NamedAction namedAction = new NamedAction();

		// Give that shit a name so we can refer to it in profiler.
#if UNITY_EDITOR
		namedAction.Name = action.Target != null ?
			action.Target.GetType().ToString() + "." + action.Method.ToString() :
			action.Method.ToString();
#endif
		namedAction.Action = action;

        CallbackCollection callbackCollection = collections[type];

		// Check if it's already been added, should never be the case so avoiding the overhead in build.
#if UNITY_EDITOR
		if (callbackCollection.ActionDictionary.ContainsKey(action))
		{
			Debug.LogErrorFormat("Failed to add callback '{0}' to CallbackEvent '{1}' because it is already added.", namedAction.Name, type.ToString());
			return;
		}
#endif

		callbackCollection.ActionList.Add(namedAction);
		callbackCollection.ActionDictionary.Add(action, namedAction);
	}

	private void RemoveCallbackInternal(CallbackCollection collection, Action callback)
	{
		NamedAction namedAction;
		if (collection.ActionDictionary.TryGetValue(callback, out namedAction))
		{
			namedAction.WaitingForRemove = true;
			collection.ActionDictionary.Remove(callback);
		}
	}

	private void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}

		collections = new Dictionary<CallbackType, CallbackCollection>(3, new CallbackTypeComparer());
		foreach (CallbackType callbackType in Enum.GetValues(typeof(CallbackType)))
		{
			collections.Add(callbackType, new CallbackCollection());
		}

		instance = this;
	}

	private void Update()
	{
		ProcessCallbacks(collections[CallbackType.UPDATE]);
	}

	private void FixedUpdate()
	{
		ProcessCallbacks(collections[CallbackType.FIXED_UPDATE]);
	}

	private void LateUpdate()
	{
		ProcessCallbacks(collections[CallbackType.LATE_UPDATE]);
	}

	private void OnDestroy()
	{
		if (instance == this)
			instance = null;
	}
}

public enum CallbackType : byte
{
	UPDATE,
	FIXED_UPDATE,
	LATE_UPDATE
}

/// <summary>
/// Used to prevent garbage from boxing when comparing enums.
/// </summary>
public struct CallbackTypeComparer : IEqualityComparer<CallbackType>
{
	public bool Equals(CallbackType x, CallbackType y)
	{
		return x == y;
	}

	public int GetHashCode(CallbackType obj)
	{
		return (int)obj;
	}
}