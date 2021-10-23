using System.Collections;
using Mirror;
using UnityEditor;
using UnityEngine.SceneManagement;
using WebSocketSharp;
using UnityEngine;

//The scene list on the server
public partial class SubSceneManager
{
	private string serverChosenAwaySite = "loading";
	private string serverChosenMainStation = "loading";

	public static string ServerChosenMainStation => Instance.serverChosenMainStation;

	public static string AdminForcedMainStation = "Random";
	public static string AdminForcedAwaySite = "Random";
	public static bool AdminAllowLavaland;

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
	}

	//Choose and load a main station on the server
	IEnumerator ServerLoadMainStation(SubsceneLoadTimer loadTimer)
	{
		MainStationLoaded = true;
		//Auto scene load stuff in editor:
		var prevEditorScene = GetEditorPrevScene();
		if ((prevEditorScene != "") && AdminForcedMainStation == "Random")
		{
			serverChosenMainStation = prevEditorScene;
		}
		else if(AdminForcedMainStation == "Random")
		{
			serverChosenMainStation = mainStationList.GetRandomMainStation();
		}
		else
		{
			serverChosenMainStation = AdminForcedMainStation;
		}

		//Reset map selector
		AdminForcedMainStation = "Random";

		loadTimer.IncrementLoadBar($"Loading {serverChosenMainStation}");
		//load main station
		yield return StartCoroutine(LoadSubScene(serverChosenMainStation, loadTimer));
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = serverChosenMainStation,
			SceneType = SceneType.MainStation
		});
		netIdentity.isDirty = true;
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
				SceneName = asteroid,
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
				SceneName = centComData.CentComSceneName,
				SceneType = SceneType.AdditionalScenes
			});
			netIdentity.isDirty = true;
			yield break;
		}

		var pickedMap = additionalSceneList.defaultCentComScenes.PickRandom();

		//If no special CentCom load default.
		yield return StartCoroutine(LoadSubScene(pickedMap, loadTimer));

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = pickedMap,
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
			if (additionalScene == "LavaLand" && !GameConfig.GameConfigManager.GameConfig.SpawnLavaLand && !AdminAllowLavaland)
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

			yield return StartCoroutine(LoadSubScene(additionalScene, loadTimer));

			loadedScenesList.Add(new SceneInfo
			{
				SceneName = additionalScene,
				SceneType = SceneType.AdditionalScenes
			});
			netIdentity.isDirty = true;
		}
	}

	//Load the away site on the server
	IEnumerator ServerLoadAwaySite(SubsceneLoadTimer loadTimer)
	{
		if (GameManager.Instance.QuickLoad)
		{
			yield return null;
		}
		var prevEditorScene = GetEditorPrevScene();
		//Load the away site
		if (awayWorldList.AwayWorlds.Contains(prevEditorScene) && AdminForcedAwaySite == "Random")
		{
			serverChosenAwaySite = prevEditorScene;
		}
		else if(AdminForcedAwaySite == "Random")
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
			loadedScenesList.Add(new SceneInfo
			{
				SceneName = serverChosenAwaySite,
				SceneType = SceneType.AwaySite
			});
			netIdentity.isDirty = true;
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

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = pickedMap,
			SceneType = SceneType.AdditionalScenes
		});
		netIdentity.isDirty = true;

		SyndicateScene = SceneManager.GetSceneByName(pickedMap);
		SyndicateLoaded = true;
	}

	public IEnumerator LoadWizard()
	{
		if (WizardLoaded) yield break;

		string pickedScene = additionalSceneList.WizardScenes.PickRandom();

		yield return StartCoroutine(LoadSubScene(pickedScene));

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = pickedScene,
			SceneType = SceneType.AdditionalScenes
		});
		netIdentity.isDirty = true;

		WizardLoaded = true;
	}

	#endregion

	string GetEditorPrevScene()
	{
		var prevEditorScene = "";
#if UNITY_EDITOR
		if (EditorPrefs.HasKey("prevEditorScene"))
		{
			if (!string.IsNullOrEmpty(EditorPrefs.GetString("prevEditorScene")))
			{
				prevEditorScene = EditorPrefs.GetString("prevEditorScene");
			}
		}
#endif
		return prevEditorScene;
	}

	/// <summary>
	/// Add a new scene to a specific connections observable list
	/// </summary>
	void AddObservableSceneToConnection(NetworkConnection conn, Scene sceneContext)
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
