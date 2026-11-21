using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Logs;
using Mirror;
using UnityEditor;
using UnityEngine.SceneManagement;
using US13.Managers.AddressableManager;
using US13.UI.Systems.PreRound;
using Util;
using WebSocketSharp;

//The scene list on the server
namespace US13.Managers.SubSceneManager
{
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

		private GameManager gameManager => GameManager.Instance;

		public async UniTask RoundStartServerLoadSequence()
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			SubSceneManagerNetworked.ScenesInitialLoadingComplete = false;
			ServerInitialLoadingComplete = false;
			SubsystemMatrixQueueInit.InitializedAll = false;

			ConnectionLoadedRecord.Clear(); //New round
			var loadTimer = new SubsceneLoadTimer();
			//calculate load time:
			loadTimer.MaxLoadTime = 20f + (gameManager.GameMode.AsteroidList.Asteroids.Count * 10f);
			loadTimer.IncrementLoadBar("Preparing..");

			Loggy.Info(" waiting for addressables To load ");
			while (AddressableCatalogueManager.FinishLoaded == false)
			{
				await UniTask.NextFrame(cancellationToken: cancellationToken);
			}

			Loggy.Info(" Loading space ");
			await ServerLoadSpaceScene(loadTimer);

			//Choose and load a mainstation
			Loggy.Info(" Loading main station ");
			await ServerLoadMainStation(loadTimer);

			Loggy.Info(" Loading CentCom ");
			//Load CentCom Scene:
			await ServerLoadCentCom(loadTimer);

			if (gameManager.QuickLoad == false)
			{
				Loggy.Info(" Loading Asteroids ");
				//Load Asteroids:
				await ServerLoadAsteroids(loadTimer);
				Loggy.Info(" Loading AwaySite ");
				//Load away site:
				await ServerLoadAwaySite(loadTimer);


				//Load Additional Scenes:

				Loggy.Info(" Loading AdditionalScenes ");
				await ServerLoadAdditionalScenes(loadTimer);
			}

			SubSceneManagerNetworked.netIdentity.isDirty = true;
			EventManager.Broadcast(Event.ReadyToInitialiseMatrices, false);
			SubSceneManagerNetworked.ScenesInitialLoadingComplete = true;

			Loggy.Info(" waiting for MatrixManager.IsInitialized");
			while (MatrixManager.MatrixManager.IsInitialized == false)
			{
				await UniTask.NextFrame(cancellationToken: cancellationToken);
			}


			Loggy.Info(" Triggering for SubsystemMatrixQueueInit.InitAllSystems");
			loadTimer.IncrementLoadBar("Loading Subsystems..");
			await SubsystemMatrixQueueInit.InitAllSystems(cancellationToken);

			Loggy.Info(" waiting for SubsystemMatrixQueueInit.InitializedAll");
			while (SubsystemMatrixQueueInit.InitializedAll == false)
			{
				await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);
			}

			EventManager.Broadcast(Event.ScenesLoadedServer, false);
			Loggy.Info($"Server has loaded {serverChosenAwaySite} away site", Category.Round);
			ServerInitialLoadingComplete = true;
			GUI_PreRoundWindow.Instance?.HideLoadingArea();
		}

		//Load the space scene on the server
		async UniTask ServerLoadSpaceScene(SubsceneLoadTimer loadTimer)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			loadTimer.IncrementLoadBar($"Loading the void of time and space");
			await LoadSubScene("AdditionalScenes/EmptySpaceYesEmptySpace.json", loadTimer, default, SceneType.Space);
		}

		//Choose and load a main station on the server
		async UniTask ServerLoadMainStation(SubsceneLoadTimer loadTimer)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			var prevEditorScene = GetEditorPrevScene();

			if (AdminForcedMainStation is not "Random")
			{
				serverChosenMainStation = AdminForcedMainStation;
			}
			else if (prevEditorScene.Contains("StartUp") == false && prevEditorScene.Contains("Lobby") == false && (prevEditorScene != "") &&
			         prevEditorScene.Contains("Online") == false &&
			         GameData.Instance.DoNotLoadEditorPreviousScene == false) //TODO Game data option!!!!
			{
				serverChosenMainStation = prevEditorScene;
			}
			else
			{
				serverChosenMainStation = gameManager.GameMode.MainStations.GetRandomMainStation();
				Loggy.Info($"[SubSceneManager.SceneList] - Server has choosen {serverChosenMainStation} as main station. " +
				           $"Previous admin forced: {AdminForcedMainStation}", Category.Round);
			}

			//Reset map selector
			AdminForcedMainStation = "Random";

			loadTimer.IncrementLoadBar($"Loading {Path.GetFileName(serverChosenMainStation).Replace(".json", "")}");
			//load main station
			await LoadSubScene(serverChosenMainStation, loadTimer, default, SceneType.MainStation);
			MainStationLoaded = true;
		}

		//Load all the asteroids on the server
		async UniTask ServerLoadAsteroids(SubsceneLoadTimer loadTimer)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (gameManager.GameMode.MainStations.AsteroidListOverride != null &&
			    gameManager.GameMode.MainStations.AsteroidListOverride.Asteroids.Count > 0)
			{
				//Use the override asteroid list for this main station
				loadTimer.IncrementLoadBar("Loading Asteroids");
				await LoadAstroids(gameManager.GameMode.MainStations.AsteroidListOverride.Asteroids);
				return;
			}
			if (gameManager.GameMode.AsteroidList == null || gameManager.GameMode.AsteroidList.Asteroids.Count <= 0)
			{
				return;
			}
			loadTimer.IncrementLoadBar("Loading Asteroids");
			await LoadAstroids(gameManager.GameMode.AsteroidList.Asteroids);
			return;

			async UniTask LoadAstroids(List<string> asteroids)
			{
				foreach (var asteroid in asteroids)
				{
					Loggy.Info($" Loading Asteroid {asteroid} ");
					await LoadSubScene(asteroid, loadTimer, default, SceneType.Asteroid);
				}
			}
		}

		async UniTask ServerLoadCentCom(SubsceneLoadTimer loadTimer)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (gameManager.GameMode.AdditionalSceneList == null)
			{
				Loggy.Warning("This game mode has no additional scene list assigned! This may be intentional for non-standard SS13 game modes.", Category.Round);
				return;
			}
			loadTimer.IncrementLoadBar("Loading CentCom");

			//CENTCOM
			foreach (var centComData in gameManager.GameMode.AdditionalSceneList.CentComScenes)
			{
				if (centComData.DependentScene == null || centComData.CentComSceneName == null) continue;

				if (centComData.DependentScene != serverChosenMainStation) continue;

				await LoadSubScene(centComData.CentComSceneName, loadTimer, default,
					SceneType.AdditionalScenes);
				return;
			}

			var pickedMap = gameManager.GameMode.AdditionalSceneList.defaultCentComScenes.PickRandom();
			if (string.IsNullOrEmpty(pickedMap)) return;
			//If no special CentCom load default.
			await LoadSubScene(pickedMap, loadTimer, default, SceneType.AdditionalScenes);
		}

		//Load all the asteroids on the server
		async UniTask ServerLoadAdditionalScenes(SubsceneLoadTimer loadTimer)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (gameManager.GameMode.AdditionalSceneList == null)
			{
				Loggy.Warning("This game mode has no additional scene list assigned! This may be intentional for non-standard SS13 game modes.", Category.Round);
				return;
			}
			if (gameManager.QuickLoad)
			{
				Loggy.Info("Quickload detected. Skipping additional scenes..", Category.Round);
				return;
			}

			loadTimer.IncrementLoadBar("Loading Additional Scenes");
			foreach (var additionalScene in gameManager.GameMode.AdditionalSceneList.AdditionalScenes)
			{
				//LAVALAND
				//only spawn if game config allows
				if (additionalScene == "LavaLand" && !GameConfigManager.GameConfig.SpawnLavaLand &&
				    !AdminAllowLavaland)
				{
					continue;
				}

				if (additionalScene == "LavaLand" && !GameConfigManager.GameConfig.SpawnLavaLand)
				{
					//reset back to false for the next round if false before.
					AdminAllowLavaland = false;
				}
				else if (additionalScene == "LavaLand")
				{
					AdminAllowLavaland = true;
				}

				await LoadSubScene(additionalScene, loadTimer, default, SceneType.AdditionalScenes);
			}
		}

		//Load the away site on the server
		async UniTask ServerLoadAwaySite(SubsceneLoadTimer loadTimer)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (gameManager.GameMode.AwayWorldList == null)
			{
				Loggy.Warning("This game mode has no away world list assigned! This may be intentional for non-standard SS13 game modes.", Category.Round);
				return;
			}
			if (gameManager.QuickLoad)
			{
				Loggy.Info("Quickload detected. Skipping away sites scenes..", Category.Round);
				return;
			}

			serverChosenAwaySite = AdminForcedAwaySite == "Random" ? gameManager.GameMode.AwayWorldList.GetRandomAwaySite() : AdminForcedAwaySite;
			AdminForcedAwaySite = "Random";

			loadTimer.IncrementLoadBar("Loading Away Site");
			if (serverChosenAwaySite.IsNullOrEmpty() == false)
			{
				await LoadSubScene(serverChosenAwaySite, loadTimer);
				AwaySiteLoaded = true;
			}
		}

		#region GameMode Unique Scenes

		public async UniTask LoadSyndicate()
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (SyndicateLoaded) return;
			var pickedMap = gameManager.GameMode.AdditionalSceneList.defaultSyndicateScenes.PickRandom();

			foreach (var syndicateData in gameManager.GameMode.AdditionalSceneList.SyndicateScenes)
			{
				if (syndicateData.DependentScene == null || syndicateData.SyndicateSceneName == null)
					continue;
				if (syndicateData.DependentScene != serverChosenMainStation)
					continue;

				pickedMap = syndicateData.SyndicateSceneName;
				break;
			}


			await LoadSubScene(pickedMap);
			SyndicateScene = SceneManager.GetSceneByName(pickedMap);
			SyndicateLoaded = true;

			await TryWaitClients(pickedMap);
		}

		public async UniTask LoadWizard()
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (WizardLoaded) return;

			string pickedScene = gameManager.GameMode.AdditionalSceneList.WizardScenes.PickRandom();

			await LoadSubScene(pickedScene);

			WizardLoaded = true;
			await TryWaitClients(pickedScene);
		}

		public async UniTask TryWaitClients(string SceneName)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			float Seconds = 0;
			bool OneClearFrame = false;
			while (Seconds < 10) //So hacked clients can't Mess up the round
			{
				bool Loading = false;
				foreach (var Info in MatrixManager.MatrixManager.Instance.ActiveMatricesList)
				{
					lock (Info.Matrix.MetaTileMap.QueuedChanges)
					{
						if (Info.Matrix.MetaTileMap.QueuedChanges.Count > 0)
						{
							Loading = true;
						}
					}
				}

				if (Loading == false)
				{
					if (OneClearFrame == false)
					{
						OneClearFrame = true;
					}
					else
					{
						return;
					}
				}

				await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f), cancellationToken: cancellationToken);
				Seconds += 0.25f;
			}
		}

		#endregion

		public static string GetEditorPrevScene()
		{
			var prevEditorScene = string.Empty;
#if UNITY_EDITOR
			prevEditorScene = EditorPrefs.GetString("SelectedMap", "");

			if (string.IsNullOrEmpty(prevEditorScene))
			{
				prevEditorScene = EditorPrefs.GetString("prevEditorScene", prevEditorScene);
				if (prevEditorScene == "OnlineScene")
				{
					prevEditorScene = string.Empty;
				}
			}
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
}