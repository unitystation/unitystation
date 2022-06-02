using System.Collections;
using System.Collections.Generic;
using Managers;
using Messages.Server.SpritesMessages;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ComponentManager : SingletonManager<ComponentManager>
{
	public static Dictionary<GameObject, UniversalObjectPhysics> ObjectToPhysics = new Dictionary<GameObject, UniversalObjectPhysics>();
	//TODO in future maybe reference a Component that is on every prefab that handles all Components on it

	public override void Awake()
	{
		base.Awake();

	}

	public static bool TryGetUniversalObjectPhysics(GameObject gameObject, out UniversalObjectPhysics UOP)
	{

		if (ObjectToPhysics.TryGetValue(gameObject, out UOP))
		{
			return true;
		}
		else
		{

			if ( gameObject.TryGetComponent<UniversalObjectPhysics>(out UOP))
			{

			}
			else
			{
				UOP = gameObject.GetComponentInParent<UniversalObjectPhysics>(); //No try get components in parent : ( : P
				if (UOP == null)
				{
					Logger.LogError($"Unable to find UniversalObjectPhysics on {gameObject.name}");
					return false;
				}
			}

			ObjectToPhysics[gameObject] = UOP;
			return true;
		}

	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnRoundRestart;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnRoundRestart;
	}

	void OnRoundRestart(Scene oldScene, Scene newScene)
	{
		ObjectToPhysics.Clear();
	}
}
