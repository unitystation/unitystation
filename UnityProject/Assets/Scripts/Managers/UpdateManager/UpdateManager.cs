using System.Collections.Generic;
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

	//List of all the objects to override UpdateMe method in Update
	private readonly List<ManagedNetworkBehaviour> regularUpdate = new List<ManagedNetworkBehaviour>();
	private readonly List<Action> regularUpdateAction = new List<Action>();

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
		if (!regularUpdate.Contains(updatable))
		{
			regularUpdate.Add(updatable);
		}
	}

	public void Add(Action updatable)
	{
		if (!regularUpdateAction.Contains(updatable))
		{
			regularUpdateAction.Add(updatable);
		}
	}

	public void Remove(ManagedNetworkBehaviour updatable)
	{
		if (regularUpdate.Contains(updatable)) {
			regularUpdate.Remove(updatable);
		}
	}

	public void Remove(Action updatable)
	{
		if (regularUpdateAction.Contains(updatable))
		{
			regularUpdateAction.Remove(updatable);
		}
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

	// Reset the references when the scene is changed
	private void Reset()
	{
		regularUpdate.Clear();
	}

	private void Update()
	{
		for (int i = 0; i < regularUpdate.Count; i++)
		{
			regularUpdate[i].UpdateMe();
			regularUpdate[i].FixedUpdateMe();
			regularUpdate[i].LateUpdateMe();
		}

		for(int i = 0; i < regularUpdateAction.Count; i++)
		{
			regularUpdateAction[i].Invoke();
		}
	}
}