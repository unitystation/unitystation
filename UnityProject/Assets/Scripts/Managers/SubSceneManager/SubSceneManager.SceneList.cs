using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEngine.SceneManagement;
using WebSocketSharp;
using UnityEngine;
using UnityEngine.AddressableAssets;

//The scene list on the server
public partial class SubSceneManager
{
	private AssetReference serverChosenAwaySite;
	private AssetReference serverChosenMainStation;

	public static AssetReference ServerChosenMainStation => Instance.serverChosenMainStation;

	public static string AdminForcedMainStation = "Random";
	public static string AdminForcedAwaySite = "Random";
	public static bool AdminAllowLavaland;

	public AssetReference SpaceSceneRef;

	private Dictionary<AssetReference, string> sceneNames = new Dictionary<AssetReference, string>();

	IEnumerator RoundStartServerLoadSequence()
	{
		var loadTimer = new SubsceneLoadTimer();
		//calculate load time:
		loadTimer.MaxLoadTime = 20f + (asteroidList.Asteroids.Count * 10f);
		loadTimer.IncrementLoadBar("Preparing..");

		while (AddressableCatalogueManager.FinishLoaded == false)
		{
			yield return null;
		}

		yield return StartCoroutine(ServerLoadSpaceScene(loadTimer));

		//Choose and load a mainstation
		yield return StartCoroutine(ServerLoadMainStation(loadTimer));

		if (GameManager.Instance.QuickLoad == false)
		{
			//Load Asteroids:
			yield return StartCoroutine(ServerLoadAsteroids(loadTimer));
			//Load away site:
			yield return StartCoroutine(ServerLoadAwaySite(loadTimer));
			//Load CentCom Scene:
			yield return StartCoroutine(ServerLoadCentCom(loadTimer));
			//Load Additional Scenes:
			yield return StartCoroutine(ServerLoadAdditionalScenes(loadTimer));
		}

		netIdentity.isDirty = true;

		yield return WaitFor.Seconds(0.1f);
		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		EventManager.Broadcast( Event.ScenesLoadedServer, false);
		Logger.Log($"Server has loaded {serverChosenAwaySite} away site", Category.Round);
		foreach (var s in sceneNames.Values)
		{
			Debug.Log(s);
		}
	}

	//Load the space scene on the server
	IEnumerator ServerLoadSpaceScene(SubsceneLoadTimer loadTimer)
	{
		loadTimer.IncrementLoadBar($"Loading the void of time and space");
		yield return StartCoroutine(LoadSubScene(SpaceSceneRef, loadTimer));
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = sceneNames[SpaceSceneRef],
			SceneKey = SpaceSceneRef.AssetGUID,
			SceneType = SceneType.Space
		});
		netIdentity.isDirty = true;
	}

	//Choose and load a main station on the server
	IEnumerator ServerLoadMainStation(SubsceneLoadTimer loadTimer)
	{
		MainStationLoaded = true;
		//Auto scene load stuff in editor:
		switch (AdminForcedMainStation)
		{
			case "Random":
				serverChosenMainStation = mainStationList.MainStations.PickRandom();
				break;
			default:
				//serverChosenMainStation = AdminForcedMainStation;
				break;
		}

		//Reset map selector
		AdminForcedMainStation = "Random";

		loadTimer.IncrementLoadBar($"Loading {serverChosenMainStation}");
		//load main station
		yield return StartCoroutine(LoadSubScene(serverChosenMainStation, loadTimer));
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = sceneNames[serverChosenMainStation],
			SceneKey = serverChosenMainStation.AssetGUID,
			SceneType = SceneType.MainStation
		});
		netIdentity.isDirty = true;
		Chat.AddGameWideSystemMsgToChat($"Server loaded station: {sceneNames[serverChosenMainStation]}");
	}

	//Load all the asteroids on the server
	IEnumerator ServerLoadAsteroids(SubsceneLoadTimer loadTimer)
	{
		loadTimer.IncrementLoadBar("Loading Asteroids");

		foreach (var asteroid in asteroidList.Asteroids)
		{
			yield return StartCoroutine(LoadSubScene(asteroid, loadTimer));

			loadedScenesList.Add(new SceneInfo
			{
				SceneName = sceneNames[asteroid],
				SceneKey = asteroid.AssetGUID,
				SceneType = SceneType.Asteroid
			});
			netIdentity.isDirty = true;
		}
	}

	IEnumerator ServerLoadCentCom(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad)
		{
			yield return null;
		}
		loadTimer.IncrementLoadBar("Loading CentCom");

		//CENTCOM
		foreach (var centComData in additionalSceneList.CentComScenes)
		{
			if (centComData.DependentScene == null || centComData.CentComSceneName == null) continue;

			if (centComData.DependentScene != serverChosenMainStation) continue;

			yield return StartCoroutine(LoadSubScene(centComData.CentComSceneName, loadTimer));

			loadedScenesList.Add(new SceneInfo
			{
				SceneName = sceneNames[centComData.CentComSceneName],
				SceneKey = centComData.CentComSceneName.AssetGUID,
				SceneType = SceneType.AdditionalScenes
			});
			netIdentity.isDirty = true;
			yield break;
		}

		var pickedMap = additionalSceneList.defaultCentComScenes.PickRandom();

		if (pickedMap == null || pickedMap.IsValid() == false) yield break;

		//If no special CentCom load default.
		yield return StartCoroutine(LoadSubScene(pickedMap, loadTimer));

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = sceneNames[pickedMap],
			SceneKey = pickedMap.AssetGUID,
			SceneType = SceneType.AdditionalScenes
		});
		netIdentity.isDirty = true;
	}

	//Load all the asteroids on the server
	IEnumerator ServerLoadAdditionalScenes(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad)
		{
			yield return null;
		}

		loadTimer.IncrementLoadBar("Loading Additional Scenes");
		foreach (var additionalScene in additionalSceneList.AdditionalScenes)
		{
			//LAVALAND
			//only spawn if game config allows
			if (additionalScene.ToString() == "LavaLand" && !GameConfig.GameConfigManager.GameConfig.SpawnLavaLand && !AdminAllowLavaland)
			{
				continue;
			}

			if (additionalScene.ToString() == "LavaLand" && !GameConfig.GameConfigManager.GameConfig.SpawnLavaLand)
			{
				//reset back to false for the next round if false before.
				AdminAllowLavaland = false;
			}
			else if (additionalScene.ToString() == "LavaLand")
			{
				AdminAllowLavaland = true;
			}

			yield return StartCoroutine(LoadSubScene(additionalScene, loadTimer));

			loadedScenesList.Add(new SceneInfo
			{
				SceneName = sceneNames[additionalScene],
				SceneKey = additionalScene.AssetGUID,
				SceneType = SceneType.AdditionalScenes
			});
			netIdentity.isDirty = true;
		}
	}

	//Load the away site on the server
	IEnumerator ServerLoadAwaySite(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad) yield break;
		yield return WaitFor.EndOfFrame;

		//Load the away site
		serverChosenAwaySite = awayWorldList.AwayWorlds.PickRandom();

		loadTimer.IncrementLoadBar("Loading Away Site");
		if (serverChosenAwaySite == null) yield break;
		yield return StartCoroutine(LoadSubScene(serverChosenAwaySite, loadTimer));
		AwaySiteLoaded = true;
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = sceneNames[serverChosenAwaySite],
			SceneKey = serverChosenAwaySite.AssetGUID,
			SceneType = SceneType.HiddenScene
		});
		netIdentity.isDirty = true;
	}

	#region GameMode Unique Scenes

	public IEnumerator LoadSyndicate()
	{
		if (SyndicateLoaded) yield break;
		var pickedMap = additionalSceneList.defaultSyndicateScenes.PickRandom();
		yield return StartCoroutine(LoadSubScene(pickedMap));

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = sceneNames[pickedMap],
			SceneKey = pickedMap.AssetGUID,
			SceneType = SceneType.HiddenScene
		});
		netIdentity.isDirty = true;

		SyndicateScene = SceneManager.GetSceneByName(pickedMap.ToString());
		SyndicateLoaded = true;
	}

	public IEnumerator LoadWizard()
	{
		if (WizardLoaded) yield break;

		var pickedScene = additionalSceneList.WizardScenes.PickRandom();

		yield return StartCoroutine(LoadSubScene(pickedScene));

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = sceneNames[pickedScene],
			SceneKey = pickedScene.AssetGUID,
			SceneType = SceneType.HiddenScene
		});
		netIdentity.isDirty = true;

		WizardLoaded = true;
	}

	#endregion

	/// <summary>
	/// Add a new scene to a specific connections observable list
	/// </summary>
	void AddObservableSceneToConnection(NetworkConnectionToClient conn, Scene sceneContext)
	{
		if (!NetworkServer.observerSceneList.ContainsKey(conn))
		{
			AddNewObserverScenePermissions(conn);
		}

		if (!NetworkServer.observerSceneList[conn].Contains(sceneContext))
		{
			NetworkServer.observerSceneList[conn].Add(sceneContext);
		}
	}
}
