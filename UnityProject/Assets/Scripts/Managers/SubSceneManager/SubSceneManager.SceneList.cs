using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Managers;
using Mirror;
using UnityEditor;
using UnityEngine.SceneManagement;
using WebSocketSharp;
using UnityEngine;

//The scene list on the server
public partial class SubSceneManager
{
	public bool ServerInitialLoadingComplete { get; private set; } = false;
	private string serverChosenAwaySite = "loading";
	private string serverChosenMainStation = "loading";

	public static string ServerChosenMainStation => Instance.serverChosenMainStation;

	public static string AdminForcedMainStation = "Random";
	public static string AdminForcedAwaySite = "Random";
	public static bool AdminAllowLavaland;

	public static Dictionary<string, HashSet<int>> ConnectionLoadedRecord = new Dictionary<string, HashSet<int>>();

	public IEnumerator RoundStartServerLoadSequence()
	{
		SubSceneManagerNetworked.ScenesInitialLoadingComplete = false;
		ServerInitialLoadingComplete = false;
		SubsystemMatrixQueueInit.InitializedAll = false;

		ConnectionLoadedRecord.Clear(); //New round
		var loadTimer = new SubsceneLoadTimer();
		//calculate load time:
		loadTimer.MaxLoadTime = 20f + (asteroidList.Asteroids.Count * 10f);
		loadTimer.IncrementLoadBar("Preparing..");

		Loggy.Log(" waiting for addressables To load ");
		while (AddressableCatalogueManager.FinishLoaded == false)
		{
			yield return null;
		}

		Loggy.Log(" Loading space ");
		yield return StartCoroutine(ServerLoadSpaceScene(loadTimer));

		//Choose and load a mainstation
		Loggy.Log(" Loading main station ");
		yield return StartCoroutine(ServerLoadMainStation(loadTimer));

		if (GameManager.Instance.QuickLoad == false)
		{
			Loggy.Log(" Loading Asteroids ");
			//Load Asteroids:
			yield return StartCoroutine(ServerLoadAsteroids(loadTimer));
			Loggy.Log(" Loading AwaySite ");
			//Load away site:
			yield return StartCoroutine(ServerLoadAwaySite(loadTimer));

			Loggy.Log(" Loading CentCom ");
			//Load CentCom Scene:
			yield return StartCoroutine(ServerLoadCentCom(loadTimer));
			//Load Additional Scenes:

			Loggy.Log(" Loading AdditionalScenes ");
			yield return StartCoroutine(ServerLoadAdditionalScenes(loadTimer));
		}

		SubSceneManagerNetworked.netIdentity.isDirty = true;
		EventManager.Broadcast(Event.ReadyToInitialiseMatrices, false);
		SubSceneManagerNetworked.ScenesInitialLoadingComplete = true;

		Loggy.Log(" waiting for MatrixManager.IsInitialized");
		while (MatrixManager.IsInitialized == false)
		{
			yield return null;
		}


		Loggy.Log(" Triggering for SubsystemMatrixQueueInit.InitAllSystems");
		loadTimer.IncrementLoadBar("Loading Subsystems..");
		yield return SubsystemMatrixQueueInit.InitAllSystems();

		Loggy.Log(" waiting for SubsystemMatrixQueueInit.InitializedAll");
		while (SubsystemMatrixQueueInit.InitializedAll == false)
		{
			yield return WaitFor.Seconds(1f);
		}

		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		EventManager.Broadcast(Event.ScenesLoadedServer, false);
		Loggy.Log($"Server has loaded {serverChosenAwaySite} away site", Category.Round);
		ServerInitialLoadingComplete = true;
	}

	//Load the space scene on the server
	IEnumerator ServerLoadSpaceScene(SubsceneLoadTimer loadTimer)
	{
		loadTimer.IncrementLoadBar($"Loading the void of time and space");
		yield return StartCoroutine(LoadSubScene("SpaceScene", loadTimer, default, SceneType.Space));
	}

	//Choose and load a main station on the server
	IEnumerator ServerLoadMainStation(SubsceneLoadTimer loadTimer)
	{
		var prevEditorScene = GetEditorPrevScene();

		if (AdminForcedMainStation is not "Random")
		{
			serverChosenMainStation = AdminForcedMainStation;
		}
		else if (prevEditorScene.Contains("Lobby") == false && (prevEditorScene != "") &&
		         prevEditorScene.Contains("Online") == false &&
		         GameData.Instance.DoNotLoadEditorPreviousScene == false) //TODO Game data option!!!!
		{
			serverChosenMainStation = prevEditorScene;
		}
		else
		{
			serverChosenMainStation = mainStationList.GetRandomMainStation();
		}

		//Reset map selector
		AdminForcedMainStation = "Random";

		loadTimer.IncrementLoadBar($"Loading {serverChosenMainStation}");
		//load main station
		yield return StartCoroutine(LoadSubScene(serverChosenMainStation, loadTimer, default, SceneType.MainStation));
		MainStationLoaded = true;
	}

	//Load all the asteroids on the server
	IEnumerator ServerLoadAsteroids(SubsceneLoadTimer loadTimer)
	{
		loadTimer.IncrementLoadBar("Loading Asteroids");

		foreach (var asteroid in asteroidList.Asteroids)
		{
			Loggy.Log($" Loading Asteroid {asteroid} ");
			yield return StartCoroutine(LoadSubScene(asteroid, loadTimer, default, SceneType.Asteroid));
		}
	}

	IEnumerator ServerLoadCentCom(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad)
		{
			yield break;
		}

		loadTimer.IncrementLoadBar("Loading CentCom");

		//CENTCOM
		foreach (var centComData in additionalSceneList.CentComScenes)
		{
			if (centComData.DependentScene == null || centComData.CentComSceneName == null) continue;

			if (centComData.DependentScene != serverChosenMainStation) continue;

			yield return StartCoroutine(LoadSubScene(centComData.CentComSceneName, loadTimer, default,
				SceneType.AdditionalScenes));
			yield break;
		}

		var pickedMap = additionalSceneList.defaultCentComScenes.PickRandom();
		if (string.IsNullOrEmpty(pickedMap)) yield break;
		//If no special CentCom load default.
		yield return StartCoroutine(LoadSubScene(pickedMap, loadTimer, default, SceneType.AdditionalScenes));
	}

	//Load all the asteroids on the server
	IEnumerator ServerLoadAdditionalScenes(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad)
		{
			yield break;
		}

		loadTimer.IncrementLoadBar("Loading Additional Scenes");
		foreach (var additionalScene in additionalSceneList.AdditionalScenes)
		{
			//LAVALAND
			//only spawn if game config allows
			if (additionalScene == "LavaLand" && !GameConfig.GameConfigManager.GameConfig.SpawnLavaLand &&
			    !AdminAllowLavaland)
			{
				continue;
			}

			if (additionalScene == "LavaLand" && !GameConfig.GameConfigManager.GameConfig.SpawnLavaLand)
			{
				//reset back to false for the next round if false before.
				AdminAllowLavaland = false;
			}
			else if (additionalScene == "LavaLand")
			{
				AdminAllowLavaland = true;
			}

			yield return StartCoroutine(LoadSubScene(additionalScene, loadTimer, default, SceneType.AdditionalScenes));
		}
	}

	//Load the away site on the server
	IEnumerator ServerLoadAwaySite(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad)
		{
			yield break;
		}

		if (AdminForcedAwaySite == "Random")
		{
			serverChosenAwaySite = awayWorldList.GetRandomAwaySite();
		}
		else
		{
			serverChosenAwaySite = AdminForcedAwaySite;
		}

		AdminForcedAwaySite = "Random";

		loadTimer.IncrementLoadBar("Loading Away Site");
		if (serverChosenAwaySite.IsNullOrEmpty() == false)
		{
			yield return StartCoroutine(LoadSubScene(serverChosenAwaySite, loadTimer));
			AwaySiteLoaded = true;
		}
	}

	#region GameMode Unique Scenes

	public IEnumerator LoadSyndicate()
	{
		if (SyndicateLoaded) yield break;
		var pickedMap = additionalSceneList.defaultSyndicateScenes.PickRandom();

		foreach (var syndicateData in additionalSceneList.SyndicateScenes)
		{
			if (syndicateData.DependentScene == null || syndicateData.SyndicateSceneName == null)
				continue;
			if (syndicateData.DependentScene != serverChosenMainStation)
				continue;

			pickedMap = syndicateData.SyndicateSceneName;
			break;
		}


		yield return StartCoroutine(LoadSubScene(pickedMap));
		SyndicateScene = SceneManager.GetSceneByName(pickedMap);
		SyndicateLoaded = true;

		yield return TryWaitClients(pickedMap);
	}

	public IEnumerator LoadWizard()
	{
		if (WizardLoaded) yield break;

		string pickedScene = additionalSceneList.WizardScenes.PickRandom();

		yield return StartCoroutine(LoadSubScene(pickedScene));

		WizardLoaded = true;
		yield return TryWaitClients(pickedScene);
	}

	public IEnumerator TryWaitClients(string SceneName)
	{
		int Clients = NetworkServer.connections.Values.Count();

		float Seconds = 0;
		while (ConnectionLoadedRecord[SceneName].Count < Clients &&
		       Seconds < 10) //So hacked clients can't Mess up the round
		{
			yield return WaitFor.Seconds(0.25f);
			Seconds += 0.25f;
		}
	}

	#endregion

	public static string GetEditorPrevScene()
	{
		var prevEditorScene = string.Empty;
#if UNITY_EDITOR
		prevEditorScene = EditorPrefs.GetString("prevEditorScene", prevEditorScene);
#endif
		return prevEditorScene;
	}

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