using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
///     Handles the update methods for in game objects
///     Handling the updates from a single point decreases cpu time
///     and increases performance
/// </summary>
public class UpdateManager : MonoBehaviour
{
	private static UpdateManager updateManager;

	private event Action UpdateMe;
	private event Action FixedUpdateMe;
	private event Action LateUpdateMe;
	private event Action UpdateAction;

<<<<<<< HEAD
	// TODO: Obsolete, remove when no longer used.
	public static UpdateManager Instance { get { return instance; } }

	private class NamedAction
=======
	public static UpdateManager Instance
>>>>>>> parent of eef9bd6cb... refactored update manager
	{
		get
		{
			if (updateManager == null)
			{
				updateManager = FindObjectOfType<UpdateManager>();
			}
			return updateManager;
		}
	}

	public void Add(ManagedNetworkBehaviour updatable)
	{
		if (updatable.GetType().GetMethod(nameof(ManagedNetworkBehaviour.UpdateMe))?.DeclaringType == updatable.GetType())
		{ //Avoiding duplicates:
			UpdateMe -= updatable.UpdateMe;
			UpdateMe += updatable.UpdateMe;
		}

		if (updatable.GetType().GetMethod(nameof(ManagedNetworkBehaviour.FixedUpdateMe))?.DeclaringType == updatable.GetType())
		{
			FixedUpdateMe -= updatable.FixedUpdateMe;
			FixedUpdateMe += updatable.FixedUpdateMe;
		}

		if (updatable.GetType().GetMethod(nameof(ManagedNetworkBehaviour.LateUpdateMe))?.DeclaringType == updatable.GetType())
		{
			LateUpdateMe -= updatable.LateUpdateMe;
			LateUpdateMe += updatable.LateUpdateMe;
		}
	}

<<<<<<< HEAD
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
=======
	public void Add(Action updatable)
>>>>>>> parent of eef9bd6cb... refactored update manager
	{
		UpdateAction -= updatable;
		UpdateAction += updatable;
	}

	public void Remove(ManagedNetworkBehaviour updatable)
	{
		UpdateMe -= updatable.UpdateMe;
		FixedUpdateMe -= updatable.FixedUpdateMe;
		LateUpdateMe -= updatable.LateUpdateMe;
	}

	public void Remove(Action updatable)
	{
		UpdateAction -= updatable;
	}

	private void OnEnable()
	{
<<<<<<< HEAD
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
=======
		SceneManager.activeSceneChanged += SceneChanged;
>>>>>>> parent of eef9bd6cb... refactored update manager
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= SceneChanged;
	}

	private void SceneChanged(Scene prevScene, Scene newScene)
	{
		Reset();
	}

	private void Reset()
	{
		UpdateMe = null;
		FixedUpdateMe = null;
		LateUpdateMe = null;
		UpdateAction = null;
	}

	private void Update()
	{
		UpdateMe?.Invoke();
		UpdateAction?.Invoke();
	}

	private void FixedUpdate()
	{
		FixedUpdateMe?.Invoke();
	}

	private void LateUpdate()
	{
		LateUpdateMe?.Invoke();
	}
}