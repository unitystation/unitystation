﻿using UnityEngine;
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
	private static UpdateManager instance;
	public static UpdateManager Instance
	{
		get { return instance; }
	}

	private Dictionary<CallbackType, CallbackCollection> collections;

	private List<Action> updateActions = new List<Action>();
	private List<Action> fixedUpdateActions = new List<Action>();
	private List<Action> lateUpdateActions = new List<Action>();

	public bool Profile = false;

	public static bool IsInitialized
	{
		get { return instance != null; }
	}

	private class NamedAction
	{
		public Action Action = null;
		public string Name = null;
		public bool WaitingForRemove;
	}

	private class CallbackCollection
	{
		// Double collection: List for fast iteration, dictionary for O(1) removal.
		// Trading memory for cpu perf.
		public readonly List<NamedAction> ActionList = new List<NamedAction>(128);
		public readonly Dictionary<Action, NamedAction> ActionDictionary = new Dictionary<Action, NamedAction>(128);
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

	public static void Add(CallbackType type, Action action)
	{
		instance.AddCallbackInternal(type, action);
	}

	public static void Add(ManagedNetworkBehaviour networkBehaviour)
	{
		instance.AddCallbackInternal(CallbackType.UPDATE, networkBehaviour.UpdateMe);
		instance.AddCallbackInternal(CallbackType.FIXED_UPDATE, networkBehaviour.FixedUpdateMe);
		instance.AddCallbackInternal(CallbackType.LATE_UPDATE, networkBehaviour.LateUpdateMe);
	}

	public static void Remove(CallbackType type, Action action)
	{
		if (action == null || Instance == null) return;

		if (type == CallbackType.UPDATE)
		{
			if (Instance.updateActions.Contains(action))
			{
				Instance.updateActions.Remove(action);
				return;
			}
		}

		if (type == CallbackType.FIXED_UPDATE)
		{
			if (Instance.fixedUpdateActions.Contains(action))
			{
				Instance.fixedUpdateActions.Remove(action);
				return;
			}
		}

		if (type == CallbackType.LATE_UPDATE)
		{
			if (Instance.lateUpdateActions.Contains(action))
			{
				Instance.lateUpdateActions.Remove(action);
				return;
			}
		}
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
		if (type == CallbackType.UPDATE)
		{
			if (!Instance.updateActions.Contains(action))
			{
				Instance.updateActions.Add(action);
				return;
			}
		}

		if (type == CallbackType.FIXED_UPDATE)
		{
			if (!Instance.fixedUpdateActions.Contains(action))
			{
				Instance.fixedUpdateActions.Add(action);
				return;
			}
		}

		if (type == CallbackType.LATE_UPDATE)
		{
			if (!Instance.lateUpdateActions.Contains(action))
			{
				Instance.lateUpdateActions.Add(action);
				return;
			}
		}
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

	private void Update()
	{
		for (int i = updateActions.Count; i > 0; i--)
		{
			if (i > 0 && i < updateActions.Count)
			{
#if UNITY_EDITOR
				if (Profile)
				{
					Profiler.BeginSample(updateActions[i]?.Method?.ReflectedType?.FullName);
				}
#endif
				updateActions[i].Invoke();
#if UNITY_EDITOR
				if (Profile)
				{
					Profiler.EndSample();
				}
#endif
			}
		}
	}

	private void FixedUpdate()
	{
		for (int i = fixedUpdateActions.Count; i > 0; i--)
		{
			if (i > 0 && i < fixedUpdateActions.Count)
			{
				fixedUpdateActions[i].Invoke();
			}
		}
	}

	private void LateUpdate()
	{
		for (int i = lateUpdateActions.Count; i > 0; i--)
		{
			if (i > 0 && i < lateUpdateActions.Count)
			{
				lateUpdateActions[i].Invoke();
			}
		}
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
		return (int) obj;
	}
}