using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;
using System.Text;
using Logs;
using Random = UnityEngine.Random;

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

	private readonly List<Action> preCameraUpdateActions = new List<Action>();
	private readonly List<Action> updateActions = new List<Action>();
	private readonly List<Action> fixedUpdateActions = new List<Action>();
	private readonly List<Action> lateUpdateActions = new List<Action>();

	public int preCameraUpdateActionsCount => preCameraUpdateActions.Count;
	public int updateActionsCount => updateActions.Count;
	public int fixedUpdateActionsCount => fixedUpdateActions.Count;
	public int lateUpdateActionsCount => lateUpdateActions.Count;
	public int periodicUpdateActionsCount => periodicUpdateActions.Count;
	public int soundUpdatesCount => soundUpdates.Count;
	public int thinkShotActionsCount => thinkShotActions.Count;




	private Action cameraFollowUpate = null;

	private List<TimedUpdate> periodicUpdateActions = new List<TimedUpdate>();
	private List<TimedUpdate> soundUpdates = new List<TimedUpdate>();
	private readonly List<TimedUpdate> thinkShotActions = new List<TimedUpdate>();

	private Queue<Tuple<CallbackType, Action>> threadSafeAddQueue = new Queue<Tuple<CallbackType, Action>>();
	private Queue<Tuple<Action, float>> threadSafeAddPeriodicQueue = new Queue<Tuple<Action, float>>();
	private Queue<Tuple<CallbackType, Action>> threadSafeRemoveQueue = new Queue<Tuple<CallbackType, Action>>();

	private static int NumberOfUpdatesAdded = 0;

	public List<TimedUpdate> pooledTimedUpdates = new List<TimedUpdate>();

	[Tooltip("For the editor to show more detailed logging in the profiler")]
	public bool Profile = false;

	public bool MidInvokeCalls {get; private set;}
	public Action LastInvokedAction {get; private set;}

	public static int CurrentFrameCashed = 0;

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

	public void Clear()
	{
		Debug.Log("removed " + CleanupUtil.RidListOfSoonToBeDeadElements(updateActions, u => u.Target as MonoBehaviour) + " messed up events in UpdateManager.updateActions");
		Debug.Log("removed " + CleanupUtil.RidListOfSoonToBeDeadElements(fixedUpdateActions, u => u.Target as MonoBehaviour) + " messed up events in UpdateManager.fixedUpdateActions");
		Debug.Log("removed " + CleanupUtil.RidListOfSoonToBeDeadElements(lateUpdateActions, u => u.Target as MonoBehaviour) + " messed up events in UpdateManager.lateUpdateActions");
		Debug.Log("removed " + CleanupUtil.RidListOfSoonToBeDeadElements(periodicUpdateActions, u => u.Action.Target as MonoBehaviour) + " messed up events in UpdateManager.periodicUpdateActions");
		Debug.Log("removed " + (CleanupUtil.RidListOfSoonToBeDeadElements(pooledTimedUpdates, u => u?.Action?.Target as MonoBehaviour) + CleanupUtil.RidListOfDeadElements(pooledTimedUpdates, u => (MonoBehaviour)u?.Action?.Target)) + " messed up events in UpdateManager.pooledTimedUpdates");
		Debug.Log("removed " + CleanupUtil.RidListOfSoonToBeDeadElements(preCameraUpdateActions, u => u.Target as MonoBehaviour) + " messed up events in UpdateManager.postCameraUpdateActions");
		Debug.Log("removed " + (CleanupUtil.RidListOfSoonToBeDeadElements(soundUpdates, u => u?.Action?.Target as MonoBehaviour) + CleanupUtil.RidListOfDeadElements(soundUpdates, u => (MonoBehaviour)u?.Action?.Target)) + " messed up events in UpdateManager.soundUpdates");
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

	/// <summary>
	/// A variant of the Add method that allows you to add a periodic update action with a time interval. Also used for sound updates.
	/// Functions will be invoked every {timeInterval} seconds when added through here.
	/// If you want to add a function that runs once after {timeInterval} seconds, use the ThinkShot method.
	/// </summary>
	/// <param name="action">the function that will be invoked after some time.</param>
	/// <param name="timeInterval">time in seconds until action is invoked. This time is slightly offset by 0.01 seconds to spread functions across multiple frames, instead of spamming all functions at once.</param>
	/// <param name="offsetUpdate">Does this contribute to the time update offset? Setting this to false is not recommended.</param>
	/// <param name="CallbackType">Default: PeriodicUpdate. Setting this to anything other than PERIODIC_UPDATE or Think will turn the action into a sound update.</param>
	public static void Add(Action action, float timeInterval, bool offsetUpdate = true, CallbackType CallbackType = CallbackType.PERIODIC_UPDATE)
	{
		if (action == null || Instance == null)
		{
			Loggy.Error("Trying to add a null action to the update manager, or Instance is not avaliable.");
			return;
		}
		if (CallbackType == CallbackType.THINK)
		{
			ThinkShot(action, timeInterval);
			return;
		}
		if (CallbackType == CallbackType.PERIODIC_UPDATE)
		{
			foreach (var x in Instance.periodicUpdateActions)
			{
				if (x.Action == action) return;
			}
			TimedUpdate timedUpdate = Instance.GetTimedUpdates();
			timedUpdate.SetUp(action, timeInterval, false);
			//Bod: It has a delay that it adds when first updating so,
			//if there's a bunch of objects Spawned on the first Frame they don't all update the same time
			//It should be fine if we skip this, but it needs this offset so it doesn't invoke all functions on the same frame, and cause stuttering.
			if (offsetUpdate)
			{
				timedUpdate.TimeTitleNext += NumberOfUpdatesAdded * 0.01f;
			}
			Instance.periodicUpdateActions.Add(timedUpdate);
		}
		else
		{
			foreach (var x in Instance.soundUpdates)
			{
				if (x.Action == action) return;
			}
			TimedUpdate timedUpdate = Instance.GetTimedUpdates();
			timedUpdate.SetUp(action, timeInterval, false);
			if (offsetUpdate)
			{
				timedUpdate.TimeTitleNext += NumberOfUpdatesAdded * 0.01f;
			}

			Instance.soundUpdates.Add(timedUpdate);
		}
		TickUpNumberOfUpdatesAdded();
	}

	private static void TickUpNumberOfUpdatesAdded()
	{
		NumberOfUpdatesAdded++;
		if (NumberOfUpdatesAdded > 500)
		{
			NumberOfUpdatesAdded = 0; //So the delay can't be too big
		}
	}

	/// <summary>
	/// Calls an action only once after a certain time interval. No update time offset is applied here.
	/// </summary>
	/// <param name="action">The function that will be invoked after a set period of time defined in time interval.</param>
	/// <param name="timeInterval">Time until action is invoked in seconds. Setting this below or equals 0 will cause timeInterval to run randomly between 0.1 and 1 seconds.</param>
	public static void ThinkShot(Action action, float timeInterval)
	{
		foreach (var thinkShot in Instance.thinkShotActions)
		{
			if (thinkShot.Action == action) return;
		}
		if (timeInterval <= 0)
		{
			timeInterval = Random.Range(0.1f, 1f);
		}
		TimedUpdate timedUpdate = Instance.GetTimedUpdates();
		timedUpdate.SetUp(action, timeInterval, true);
		Instance.thinkShotActions.Add(timedUpdate);
	}

	/// <summary>
	/// Calls an action only once after a random amount of time in seconds. No update time offset is applied here.
	/// </summary>
	/// <param name="action">The function that will be invoked after a set period of time defined in time interval.</param>
	/// <param name="minTimeInterval">The minimum Time before an action is invoked. Setting this below or equals 0 will cause timeInterval to be at a minimum of 1 second.</param>
	/// <param name="maxTimeInterval">The maximum Time before an action is invoked. Setting this below or equals 0 will cause timeInterval to be at a minimum of 2 seconds.</param>
	public static void ThinkShotRandomTime(Action action, float minTimeInterval = 0, float maxTimeInterval = 0)
	{
		if (Instance.thinkShotActions.Any(x => x.Action == action)) return;
		if (minTimeInterval <= 0 || maxTimeInterval <= 0)
		{
			minTimeInterval = 1f;
			maxTimeInterval = 2f;
		}
		TimedUpdate timedUpdate = Instance.GetTimedUpdates();
		timedUpdate.SetUp(action, Random.Range(minTimeInterval, maxTimeInterval), true);
		Instance.thinkShotActions.Add(timedUpdate);
	}

	public static void SetCameraUpdate(Action CameraFollowUpate)
	{
		if (instance != null)
		{
			instance.cameraFollowUpate = CameraFollowUpate;
		}
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
			TimedUpdate removingAction = null;
			foreach (var periodicUpdateAction in Instance.periodicUpdateActions)
			{
				if (periodicUpdateAction.Action == action)
				{
					removingAction = periodicUpdateAction;
				}
			}
			if (removingAction != null)
			{
				removingAction.Pool();
				Instance.periodicUpdateActions.Remove(removingAction);

			}
			return;
		}

		if (type == CallbackType.SOUND_UPDATE)
		{
			TimedUpdate RemovingAction = null;
			foreach (var periodicUpdateAction in Instance.soundUpdates)
			{
				if (periodicUpdateAction.Action == action)
				{
					RemovingAction = periodicUpdateAction;
				}
			}
			if (RemovingAction != null)
			{
				RemovingAction.Pool();
				Instance.soundUpdates.Remove(RemovingAction);

			}
			return;
		}

		if (type == CallbackType.EARLY_UPDATE)
		{
			Instance.preCameraUpdateActions.Remove(action);
			return;
		}

		if (type == CallbackType.THINK)
		{
			Loggy.Error("Cannot remove think shot actions manually. Let them die naturally when their time is up to process.");
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

	private void AddCallbackInternal(CallbackType type, Action action, int priority = 0)
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

		if (type == CallbackType.EARLY_UPDATE)
		{
			Instance.preCameraUpdateActions.Add(action);
			return;
		}

		if (type == CallbackType.THINK)
		{
			ThinkShot(action, 0);
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
		CurrentFrameCashed = Time.frameCount;

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
		MidInvokeCalls = true;

		for (int i = preCameraUpdateActions.Count - 1; i >= 0; i--)
        {
            if (i >= preCameraUpdateActions.Count) continue;
            var callingAction = preCameraUpdateActions[i];
            if (Profile)
            {
                Profiler.BeginSample(callingAction.Method?.ReflectedType?.FullName);
            }

            LastInvokedAction = callingAction;
            try
            {
                callingAction.Invoke();
            }
            catch (Exception e)
            {
                Loggy.Error(e.ToString());
            }

            if (Profile)
            {
                Profiler.EndSample();
            }
        }

		LastInvokedAction = cameraFollowUpate;
		cameraFollowUpate?.Invoke();

		for (int i = updateActions.Count; i >= 0; i--)
		{
			if (i < updateActions.Count)
			{
				var callingAction = updateActions[i];
				if (Profile)
				{
					Profiler.BeginSample(callingAction.Method?.ReflectedType?.FullName);
				}

				LastInvokedAction = callingAction;
				try
				{
					callingAction.Invoke();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}

				if (Profile)
				{
					Profiler.EndSample();
				}
			}
		}
		MidInvokeCalls = false;


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

	public void PostCameraRemove()
	{

	}


	/// <summary>
	///  Used to do increment the Time on Periodic updates to know when to Call them
	/// </summary>
	private void ProcessDelayUpdate()
	{
		MidInvokeCalls = true;
		int n = 0;
		for (int i = 0; i < periodicUpdateActions.Count; i++)
		{
			var periodicCall = periodicUpdateActions[i];
			periodicCall.TimeTitleNext -= CashedDeltaTime;
			if (periodicCall.TimeTitleNext <= 0)
			{
				n++;
				LastInvokedAction = periodicCall.Action;
				periodicCall.TimeTitleNext = periodicCall.TimeDelayPreUpdate + periodicCall.TimeTitleNext + (0.0001f *n);
				try
				{
					periodicCall.Action();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}
		}
		MidInvokeCalls = false;
		MidInvokeCalls = true;
		n = 0;
		for (int i = 0; i < soundUpdates.Count; i++)
		{
			var periodicCall = soundUpdates[i];
			periodicCall.TimeTitleNext -= CashedDeltaTime;
			if (periodicCall.TimeTitleNext <= 0)
			{
				n++;
				LastInvokedAction = periodicCall.Action;
				periodicCall.TimeTitleNext = periodicCall.TimeDelayPreUpdate + periodicCall.TimeTitleNext+ (0.0001f *n);
				try
				{
					periodicCall.Action();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}
		}
		MidInvokeCalls = false;
		MidInvokeCalls = true;
		n = 0;
		for (int i = 0; i < thinkShotActions.Count; i++)
		{
			var thinkShotAction = thinkShotActions[i];
			thinkShotAction.TimeTitleNext -= CashedDeltaTime;
			if (thinkShotAction.TimeTitleNext <= 0)
			{
				n++;
				LastInvokedAction = thinkShotAction.Action;
				try
				{
					thinkShotAction.Action();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
				thinkShotAction.Pool();
				thinkShotActions.RemoveAt(i);
				i--;
			}
		}
		MidInvokeCalls = false;
	}

	private void FixedUpdate()
	{
		MidInvokeCalls = true;
		for (int i = fixedUpdateActions.Count; i >= 0; i--)
		{
			if (i < fixedUpdateActions.Count)
			{
				LastInvokedAction = fixedUpdateActions[i];
				try
				{
					fixedUpdateActions[i].Invoke();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}
		}
		MidInvokeCalls = false;
	}

	private void LateUpdate()
	{
		MidInvokeCalls = true;
		for (int i = lateUpdateActions.Count; i >= 0; i--)
		{
			if (i < lateUpdateActions.Count)
			{
				LastInvokedAction = lateUpdateActions[i];
				try
				{
					lateUpdateActions[i].Invoke();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}
		}

		MidInvokeCalls = false;
	}

	private void OnDestroy()
	{
		if (instance == this)
			instance = null;
	}

	public class TimedUpdate
	{
		public bool OneShot = false;
		public float TimeDelayPreUpdate = 0;
		public float TimeTitleNext = 0;
		public Action Action;

		public void SetUp(Action inAction, float inTimeDelayPreUpdate, bool isOneShot)
		{
			Action = inAction;
			TimeDelayPreUpdate = inTimeDelayPreUpdate;
			TimeTitleNext = inTimeDelayPreUpdate;
			OneShot = isOneShot;
		}

		public void Pool()
		{
			TimeDelayPreUpdate = 0;
			TimeTitleNext = 0;
			Action = null;
			OneShot = false;
			UpdateManager.instance.pooledTimedUpdates.Add(this);
		}
	}

	[ContextMenu("List Updates")]
	private void ListUpdates()
	{
		DebugLog(updateActions);
		DebugLog(fixedUpdateActions);
		DebugLog(lateUpdateActions);
		DebugLog(preCameraUpdateActions);

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

		foreach (var update in soundUpdates)
		{
			if (periodicUpdate.ContainsKey($"S {update.Action.Method.DeclaringType?.Name} {update.Action.Method.Name}") == false)
			{
				periodicUpdate.Add($"S  {update.Action.Method.DeclaringType?.Name} {update.Action.Method.Name}", 1);
			}
			else
			{
				periodicUpdate[$"S {update.Action.Method.DeclaringType?.Name} {update.Action.Method.Name}"]++;
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
	SOUND_UPDATE,
	EARLY_UPDATE,
	THINK,
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
