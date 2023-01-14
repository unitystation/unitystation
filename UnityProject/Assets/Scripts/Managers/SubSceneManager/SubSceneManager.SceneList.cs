using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

//The scene list on the server
public partial class SubSceneManager
{
	private string serverChosenAwaySite;
	private string serverChosenMainStation;

	public static string ServerChosenMainStation => Instance.serverChosenMainStation;

	public static string AdminForcedMainStation;
	public static string AdminForcedAwaySite = "Random";
	public static bool AdminAllowLavaland;

	public AssetReference SpaceSceneRef;

	private List<string> sceneNames;

	private IEnumerator RoundStartServerLoadSequence()
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

		//(Max): this wait with magic unexplained number is stopping the game from wetting the bed after loading scenes.
		//Why does waiting 0.1 seconds prevent the game breaking and not starting the round start timer and systems initialising properly? I have no clue.
		//Add to this counter here for every hour spent trying to understand why this breaks: 2
		yield return WaitFor.Seconds(0.1f);
		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		EventManager.Broadcast( Event.ScenesLoadedServer);
		Logger.Log($"Server has loaded {serverChosenAwaySite} away site", Category.Round);
	}

	//Load the space scene on the server
	private IEnumerator ServerLoadSpaceScene(SubsceneLoadTimer loadTimer)
	{
		loadTimer.IncrementLoadBar($"Loading the void of time and space");
		yield return StartCoroutine(LoadSubScene(SpaceSceneRef, loadTimer));
		netIdentity.isDirty = true;
	}

	//Choose and load a main station on the server
	private IEnumerator ServerLoadMainStation(SubsceneLoadTimer loadTimer)
	{
		serverChosenMainStation = AdminForcedMainStation ?? allmainstationmaps.PickRandom().Key;

		//Reset map selector
		AdminForcedMainStation = null;

		loadTimer.IncrementLoadBar($"Loading {serverChosenMainStation}");
		//load main station
		yield return StartCoroutine(LoadSubScene(serverChosenMainStation, loadTimer, true, SceneType.MainStation));
		netIdentity.isDirty = true;
		Chat.AddGameWideSystemMsgToChat($"Server loaded station: {serverChosenMainStation}");
		MainStationLoaded = true;
	}

	//Load all the asteroids on the server
	private IEnumerator ServerLoadAsteroids(SubsceneLoadTimer loadTimer)
	{
		loadTimer.IncrementLoadBar("Loading Asteroids");

		foreach (var asteroid in asteroidList.Asteroids)
		{
			yield return StartCoroutine(LoadSubScene(asteroid, loadTimer, true, SceneType.Asteroid));
			netIdentity.isDirty = true;
		}
	}

	private IEnumerator ServerLoadCentCom(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad)
		{
			yield return null;
		}
		loadTimer.IncrementLoadBar("Loading CentCom");

		//CENTCOM
		yield return StartCoroutine(LoadSubScene(additionalSceneList.CentComScenes.PickRandom().CentComSceneName, loadTimer, true, SceneType.AdditionalScenes));
		netIdentity.isDirty = true;
		var pickedMap = additionalSceneList.defaultCentComScenes.PickRandom();
		if (pickedMap == null || pickedMap.IsValid() == false) yield break;
		//If no special CentCom load default.
		yield return StartCoroutine(LoadSubScene(pickedMap, loadTimer, true, SceneType.AdditionalScenes));
		netIdentity.isDirty = true;
	}

	//Load all the asteroids on the server
	private IEnumerator ServerLoadAdditionalScenes(SubsceneLoadTimer loadTimer)
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

			yield return StartCoroutine(LoadSubScene(additionalScene, loadTimer, true, SceneType.AdditionalScenes));
			netIdentity.isDirty = true;
		}
	}

	//Load the away site on the server
	private IEnumerator ServerLoadAwaySite(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad) yield break;
		yield return WaitFor.EndOfFrame;

		//Load the away site
		loadTimer.IncrementLoadBar("Loading Away Site");
		if (serverChosenAwaySite == null) yield break;
		//TODO: Bring back admin forced specific away sites.
		yield return StartCoroutine(LoadSubScene(awayWorldList.AwayWorlds.PickRandom(), loadTimer, true, SceneType.AwaySite));
		AwaySiteLoaded = true;
		netIdentity.isDirty = true;
	}

	#region GameMode Unique Scenes

	public IEnumerator LoadSyndicate()
	{
		if (SyndicateLoaded) yield break;
		var pickedMap = additionalSceneList.defaultSyndicateScenes.PickRandom();
		yield return StartCoroutine(LoadSubScene(pickedMap, null, true, SceneType.AdditionalScenes));
		netIdentity.isDirty = true;
		SyndicateScene = SceneManager.GetSceneByName(pickedMap.ToString());
		SyndicateLoaded = true;
	}

	public IEnumerator LoadWizard()
	{
		if (WizardLoaded) yield break;

		var pickedScene = additionalSceneList.WizardScenes.PickRandom();

		yield return StartCoroutine(LoadSubScene(pickedScene, null, true, SceneType.AdditionalScenes));
		netIdentity.isDirty = true;

		WizardLoaded = true;
	}

	#endregion

	/// <summary>
	/// Add a new scene to a specific connections observable list
	/// </summary>
	private void AddObservableSceneToConnection(NetworkConnectionToClient conn, Scene sceneContext)
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
