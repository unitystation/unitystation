﻿using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;
using System.Text;
using UnityEngine.Serialization;

/// <summary>
///     Handles the update methods for in game objects
///     Handling the updates from a single point decreases cpu time
///     and increases performance
/// </summary>
public class UpdateManager : MonoBehaviour
{

	public static float CashedDeltaTime = 0;

	private static UpdateManager instance;

	public static UpdateManager Instance
	{
		get { return instance; }
	}

	private Dictionary<CallbackType, CallbackCollection> collections;

	private List<Action> updateActions = new List<Action>();
	private List<Action> fixedUpdateActions = new List<Action>();
	private List<Action> lateUpdateActions = new List<Action>();
	private List<TimedUpdate> periodicUpdateActions = new List<TimedUpdate>();

	private Queue<Tuple<CallbackType, Action>> threadSafeAddQueue = new Queue<Tuple<CallbackType, Action>>();
	private Queue<Tuple<Action, float>> threadSafeAddPeriodicQueue = new Queue<Tuple<Action, float>>();
	private Queue<Tuple<CallbackType, Action>> threadSafeRemoveQueue = new Queue<Tuple<CallbackType, Action>>();

	private static int NumberOfUpdatesAdded = 0;

	public List<TimedUpdate> pooledTimedUpdates = new List<TimedUpdate>();

	public TimedUpdate GetTimedUpdates()
	{
		if (pooledTimedUpdates.Count > 0)
		{
			var TimedUpdates = pooledTimedUpdates[0];
			pooledTimedUpdates.RemoveAt(0);
			return (TimedUpdates);
		}
		else
		{
			return (new TimedUpdate());
		}
	}

	[Tooltip("For the editor to show more detailed logging in the profiler")]
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

	public static void SafeAdd(CallbackType type, Action action)
	{
		instance.threadSafeAddQueue.Enqueue(new Tuple<CallbackType, Action>(type, action));
	}

	public static void Add(Action action, float timeInterval)
	{
		if (Instance.periodicUpdateActions.Any(x => x.Action == action)) return;
		TimedUpdate timedUpdate = Instance.GetTimedUpdates();
		timedUpdate.SetUp(action, timeInterval);
		timedUpdate.TimeTitleNext += NumberOfUpdatesAdded * 0.1f;
		NumberOfUpdatesAdded++;
		Instance.periodicUpdateActions.Add(timedUpdate);
	}

	public static void SafeAdd(Action action, float timeInterval)
	{
		instance.threadSafeAddPeriodicQueue.Enqueue(new Tuple<Action, float>(action, timeInterval));
	}

	public static void Add(ManagedNetworkBehaviour networkBehaviour)
	{
		instance.AddCallbackInternal(CallbackType.UPDATE, networkBehaviour.UpdateMe);
		instance.AddCallbackInternal(CallbackType.FIXED_UPDATE, networkBehaviour.FixedUpdateMe);
		instance.AddCallbackInternal(CallbackType.LATE_UPDATE, networkBehaviour.LateUpdateMe);
	}

	public static void Add(ManagedBehaviour managedBehaviour)
	{
		instance.AddCallbackInternal(CallbackType.UPDATE, managedBehaviour.UpdateMe);
		instance.AddCallbackInternal(CallbackType.FIXED_UPDATE, managedBehaviour.FixedUpdateMe);
		instance.AddCallbackInternal(CallbackType.LATE_UPDATE, managedBehaviour.LateUpdateMe);
	}

	public static void Remove(CallbackType type, Action action)
	{
		if (action == null || Instance == null) return;

		if (type == CallbackType.UPDATE)
		{
			Instance.updateActions.Remove(action);
			return;
		}

		if (type == CallbackType.FIXED_UPDATE)
		{
			Instance.fixedUpdateActions.Remove(action);
			return;
		}

		if (type == CallbackType.LATE_UPDATE)
		{
			Instance.lateUpdateActions.Remove(action);
			return;
		}

		if (type == CallbackType.PERIODIC_UPDATE)
		{
			TimedUpdate RemovingAction = null;
			foreach (var periodicUpdateAction in Instance.periodicUpdateActions)
			{
				if (periodicUpdateAction.Action == action)
				{
					RemovingAction = periodicUpdateAction;
				}
			}
			if (RemovingAction != null)
			{
				RemovingAction.Pool();
				Instance.periodicUpdateActions.Remove(RemovingAction);

			}

			return;
		}
	}

	public static void Remove(ManagedNetworkBehaviour networkBehaviour)
	{
		Remove(CallbackType.UPDATE, networkBehaviour.UpdateMe);
		Remove(CallbackType.FIXED_UPDATE, networkBehaviour.FixedUpdateMe);
		Remove(CallbackType.LATE_UPDATE, networkBehaviour.LateUpdateMe);
	}

	public static void Remove(ManagedBehaviour managedBehaviour)
	{
		Remove(CallbackType.UPDATE, managedBehaviour.UpdateMe);
		Remove(CallbackType.FIXED_UPDATE, managedBehaviour.FixedUpdateMe);
		Remove(CallbackType.LATE_UPDATE, managedBehaviour.LateUpdateMe);
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
			Instance.updateActions.Add(action);
			return;
		}

		if (type == CallbackType.FIXED_UPDATE)
		{
			Instance.fixedUpdateActions.Add(action);
			return;
		}

		if (type == CallbackType.LATE_UPDATE)
		{
			Instance.lateUpdateActions.Add(action);
			return;
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
		if (threadSafeAddQueue.Count > 0)
		{
			for (int i = 0; i < threadSafeAddQueue.Count; i++)
			{
				var toQueue = threadSafeAddQueue.Dequeue();
				Add(toQueue.Item1, toQueue.Item2);
			}
		}

		if (threadSafeAddPeriodicQueue.Count > 0)
		{
			for (int i = 0; i < threadSafeAddPeriodicQueue.Count; i++)
			{
				var toQueue = threadSafeAddPeriodicQueue.Dequeue();
				Add(toQueue.Item1, toQueue.Item2);
			}
		}

		if (threadSafeRemoveQueue.Count > 0)
		{
			for (int i = 0; i < threadSafeRemoveQueue.Count; i++)
			{
				var toQueue = threadSafeRemoveQueue.Dequeue();
				Remove(toQueue.Item1, toQueue.Item2);
			}
		}

		CashedDeltaTime = Time.deltaTime;
		for (int i = updateActions.Count; i >= 0; i--)
		{
			if (i < updateActions.Count)
			{
				if (Profile)
				{
					Profiler.BeginSample(updateActions[i]?.Method?.ReflectedType?.FullName);
				}

				updateActions[i].Invoke();

				if (Profile)
				{
					Profiler.EndSample();
				}
			}
		}

		if (Profile)
		{
			Profiler.BeginSample(" Periodic update Process ");
		}

		ProcessDelayUpdate();

		if (Profile)
		{
			Profiler.EndSample();
		}
	}

	/// <summary>
	///  Used to do increment the Time on Periodic updates to know when to Call them
	/// </summary>
	private void ProcessDelayUpdate()
	{
		NumberOfUpdatesAdded = 0;
		for (int i = 0; i < periodicUpdateActions.Count; i++)
		{
			periodicUpdateActions[i].TimeTitleNext -= CashedDeltaTime;
			if (periodicUpdateActions[i].TimeTitleNext <= 0)
			{
				periodicUpdateActions[i].TimeTitleNext = periodicUpdateActions[i].TimeDelayPreUpdate;
				periodicUpdateActions[i].Action();
			}
		}
	}


	private void FixedUpdate()
	{
		for (int i = fixedUpdateActions.Count; i >= 0; i--)
		{
			if (i < fixedUpdateActions.Count)
			{
				fixedUpdateActions[i].Invoke();
			}
		}
	}

	private void LateUpdate()
	{
		for (int i = lateUpdateActions.Count; i >= 0; i--)
		{
			if (i < lateUpdateActions.Count)
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

	public class TimedUpdate
	{
		public float TimeDelayPreUpdate = 0;
		public float TimeTitleNext = 0;
		public Action Action;

		public void SetUp(Action InAction, float InTimeDelayPreUpdate)
		{
			Action = InAction;
			TimeDelayPreUpdate = InTimeDelayPreUpdate;
			TimeTitleNext = InTimeDelayPreUpdate;
		}

		public void Pool()
		{
			TimeDelayPreUpdate = 0;
			TimeTitleNext = 0;
			Action = null;
			UpdateManager.instance.pooledTimedUpdates.Add(this);
		}
	}

	[ContextMenu("List Updates")]
	private void ListUpdates()
	{
		DebugLog(updateActions);
		DebugLog(fixedUpdateActions);
		DebugLog(lateUpdateActions);

		void DebugLog(List<Action> type)
		{
			var updates = new Dictionary<String, int>();

			foreach (var update in type)
			{
				if (updates.ContainsKey($"{update.Method.DeclaringType?.Name} {update.Method.Name}") == false)
				{
					updates.Add($"{update.Method.DeclaringType?.Name} {update.Method.Name}", 1);
				}
				else
				{
					updates[$"{update.Method.DeclaringType?.Name} {update.Method.Name}"]++;
				}
			}

			var updateString = new StringBuilder();

			updateString.AppendLine(nameof(type));

			foreach (var update in updates)
			{
				updateString.AppendLine($"Name: {update.Key} Amount: {update.Value}");
			}

			Debug.LogError(updateString.ToString());
		}

		var periodicUpdate = new Dictionary<string, int>();

		foreach (var update in periodicUpdateActions)
		{
			if (periodicUpdate.ContainsKey($"{update.Action.Method.DeclaringType?.Name} {update.Action.Method.Name}") == false)
			{
				periodicUpdate.Add($"{update.Action.Method.DeclaringType?.Name} {update.Action.Method.Name}", 1);
			}
			else
			{
				periodicUpdate[$"{update.Action.Method.DeclaringType?.Name} {update.Action.Method.Name}"]++;
			}
		}

		var stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(nameof(periodicUpdateActions));

		foreach (var update in periodicUpdate)
		{
			stringBuilder.AppendLine($"Name: {update.Key} Amount: {update.Value}");
		}

		Debug.LogError(stringBuilder.ToString());
	}
}

public enum CallbackType : byte
{
	UPDATE,
	FIXED_UPDATE,
	LATE_UPDATE,
	PERIODIC_UPDATE,
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