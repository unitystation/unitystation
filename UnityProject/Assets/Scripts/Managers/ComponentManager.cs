using System.Collections.Generic;
using Logs;
using Tilemaps.Behaviours.Layers;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shared.Managers;

public class ComponentManager : SingletonManager<ComponentManager>
{
	public static Dictionary<GameObject, UniversalObjectPhysics> ObjectToPhysics = new Dictionary<GameObject, UniversalObjectPhysics>();
	public static Dictionary<GameObject, CommonComponents> ObjectToCommonComponent = new Dictionary<GameObject, CommonComponents>();



	public static bool TryGetCommonComponent(GameObject gameObject, out CommonComponents commonComponents)
	{
		if (gameObject == null)
		{
			commonComponents = null;
			return false;
		}
		if (ObjectToCommonComponent.TryGetValue(gameObject, out commonComponents))
		{
			return true;
		}

		if (gameObject.TryGetComponent<CommonComponents>(out commonComponents) == false)
		{
			return false;
		}
		ObjectToCommonComponent[gameObject] = commonComponents;
		return true;
	}

	public static bool TryGetUniversalObjectPhysics(GameObject gameObject, out UniversalObjectPhysics UOP)
	{
		if (ObjectToPhysics.TryGetValue(gameObject, out UOP))
		{
			return true;
		}

		if (gameObject.TryGetComponent<UniversalObjectPhysics>(out UOP))
		{

		}
		else
		{
			//Don't need to search if ghost as they dont have UOP
			if(gameObject.TryGetComponent<GhostMove>(out _)) return false;

			//Don't need to search if NetworkedMatrix as they dont have UOP
			if(gameObject.TryGetComponent<NetworkedMatrix>(out _)) return false;

			UOP = gameObject.GetComponentInParent<UniversalObjectPhysics>(); //No try get components in parent : ( : P
			if (UOP == null)
			{
				Loggy.LogError($"Unable to find UniversalObjectPhysics on {gameObject.name}");
				return false;
			}
		}

		ObjectToPhysics[gameObject] = UOP;
		return true;
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnRoundRestart;
		EventManager.AddHandler(Event.RoundEnded, OnRoundEnded);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnRoundRestart;
		EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnded);
	}

	private void OnRoundRestart(Scene oldScene, Scene newScene)
	{
		ObjectToPhysics.Clear();
		ObjectToCommonComponent.Clear();
	}

	private void OnRoundEnded()
	{
		ObjectToPhysics.Clear();
		ObjectToCommonComponent.Clear();
	}
}
