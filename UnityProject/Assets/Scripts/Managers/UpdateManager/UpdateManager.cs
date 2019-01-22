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

	public static UpdateManager Instance
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

	public void Add(Action updatable)
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
		SceneManager.activeSceneChanged += SceneChanged;
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