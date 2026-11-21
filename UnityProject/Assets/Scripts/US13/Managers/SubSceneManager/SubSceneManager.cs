using System.Threading;
using US13.UI.Systems.PreRound;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using UnityEngine.SceneManagement;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.MapSaver;
using US13.Messages.Client;

namespace US13.Managers.SubSceneManager
{
	public partial class SubSceneManager : MonoBehaviour
	{
		private CancellationTokenSource loadingCancellation;
		private CancellationToken LoadingToken
		{
			get
			{
				if (loadingCancellation == null) loadingCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
				return loadingCancellation.Token;
			}
		}

		public GameObject MatrixPrefab;
		public GameObject NetworkedMatrixPrefab;

		private static SubSceneManager subSceneManager;
		public static SubSceneManager Instance;

		public ScenesSyncList loadedScenesList => SubSceneManagerNetworked.loadedScenesList;
		public MainStationListSO MainStationList => GameManager.Instance.GameMode.MainStations;
		public AwayWorldListSO AwayWorlds => GameManager.Instance.GameMode.AwayWorldList;

		public bool AwaySiteLoaded { get; private set; }

		public bool IsMaintRooms
		{

			get { return serverChosenAwaySite == "AwaySites/Backrooms.json"; }
		}

		public bool MainStationLoaded { get; private set; }

		public bool SyndicateLoaded { get; private set; }
		public Scene SyndicateScene { get; private set; }
		public bool WizardLoaded { get; private set; }

		public SubSceneManagerNetworked SubSceneManagerNetworked;

		void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void OnEnable()
		{
			UpdateManager.UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			EventManager.AddHandler(Event.RoundEnded, RoundEnded);
		}

		private void OnDisable()
		{
			UpdateManager.UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			EventManager.RemoveHandler(Event.RoundEnded, RoundEnded);
		}

		private void OnDestroy()
		{
			CancelLoading();
		}

		private void CancelLoading()
		{
			loadingCancellation?.Cancel();
			loadingCancellation?.Dispose();
			loadingCancellation = null;
		}

		/// <summary>
		/// So the client isn't loading scenes while server is Loading a new round
		/// </summary>
		private void RoundEnded()
		{
			CancelLoading();
			ClientIsFullyDoneLoadingOnSubsceneManager = false;
			clientIsLoadingSubscene = false;
			ClientObserver.Clear();
			var finish = ClientSideFinishAction;
			ClientSideFinishAction = null;
			finish?.Invoke();
			ServerInitialLoadingComplete = false;
		}

		private void UpdateMe()
		{
			MonitorServerSceneListOnClient();
		}

		/// <summary>
		/// General subscene loader
		/// </summary>
		/// <param name="sceneName"></param>
		/// <returns></returns>
		async UniTask LoadSubScene(string sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true, SceneType sceneType = SceneType.HiddenScene)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (AccessFile.Exists(sceneName, true, FolderType.Maps, false))
			{
				if (CustomNetworkManager.IsServer) //Client loading is handled automatically
				{

					loadTimer?.Report("Waiting for prefabs", sceneName);
					while (CustomNetworkManager.AllPrefabsLoadedSt == false)
					{
						await UniTask.NextFrame(cancellationToken: cancellationToken);
					}

					loadTimer?.Report("Reading map", sceneName);
					await UniTask.NextFrame(cancellationToken: cancellationToken);
					string json = AccessFile.Load(sceneName, FolderType.Maps);

					loadTimer?.Report("Deserializing map", sceneName);
					await UniTask.NextFrame(cancellationToken: cancellationToken);

					// Deserialize the JSON content to a MapData object
					MapSaver.MapSaver.MapData mapData = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(json);
					await MapLoader.ServerLoadMap(Vector3.zero, Vector3.zero,mapData, sceneType, progress: p => loadTimer?.Report(p.Stage, p.Context, p.Completed, p.Total), cancellationToken: cancellationToken);
				}
			}
			else
			{
				await LoadUnityScene(sceneName, loadTimer, HandlSynchronising, sceneType);
			}

			Loggy.Info($"Finished loading {sceneName}");
		}

		private async UniTask LoadUnityScene(string sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true, SceneType sceneType = SceneType.HiddenScene)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			if (CustomNetworkManager.IsServer == false)
			{
				if (clientLoadedSubScenes.Any(x => x.SceneName == sceneName))
				{
					Loggy.Info($"Scene already loaded client {sceneName}");
					return;
				}
			}
			ConnectionLoadedRecord[sceneName] = new HashSet<int>();
			AsyncOperation AO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
			Loggy.Info($"AO Handle Generated for {sceneName}");
			if (AO != null)
			{
				Loggy.Info($"Waiting for AO.isDone {sceneName}");
				try
				{
					while (AO.isDone == false)
					{
						loadTimer?.Report("Loading Unity scene", sceneName, Mathf.RoundToInt(Mathf.Clamp01(AO.progress / 0.9f) * 100), 100);
						await UniTask.NextFrame(cancellationToken: cancellationToken);
					}
				}
				catch (OperationCanceledException)
				{
					// Unity cannot cancel a scene operation; finish it before releasing its scene.
					await AO;
					var scene = SceneManager.GetSceneByName(sceneName);

					if (scene.IsValid() == false)
					{
						scene = SceneManager.GetSceneByPath(sceneName);
					}
					if (scene.IsValid() && scene.isLoaded)
					{
						await SceneManager.UnloadSceneAsync(scene);
					}
					throw;
				}

				Loggy.Info($"Finished waiting for AO.isDone {sceneName}");
				if (loadTimer != null) loadTimer.IncrementLoadBar();
				if (CustomNetworkManager.IsServer)
				{
					Loggy.Info($"SpawnObjects + RequestObserverRefresh {sceneName}");
					NetworkServer.SpawnObjects();

					while (NetworkClient.connection != null && NetworkClient.connection.isAuthenticated == false) //Needed so that if Authentication takes time, server instance does not disconnect itself.
					{
						await UniTask.NextFrame(cancellationToken: cancellationToken);
					}
					RequestObserverRefresh.Send(sceneName);
				}
				else
				{
					if (HandlSynchronising)
					{
						NetworkClient.PrepareToSpawnSceneObjects();
						await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: cancellationToken);
						RequestObserverRefresh.Send(sceneName);
					}
				}

				if (CustomNetworkManager.IsServer)
				{
					Loggy.Info($"SloadedScenesList.add {sceneName}");
					loadedScenesList.Add(new SceneInfo
					{
						SceneName = sceneName,
						SceneType = sceneType
					});
					SubSceneManagerNetworked.netIdentity.isDirty = true;
				}

			}
			else
			{
				Loggy.Error($"was unable to find scene for {sceneName} Skipping");
			}
		}

		public static void ProcessObserverRefreshReq(PlayerInfo connectedPlayer, Scene sceneContext)
		{
			if (connectedPlayer.Connection == null) return;
			if (ConnectionLoadedRecord.ContainsKey(sceneContext.name) == false)
			{
				ConnectionLoadedRecord[sceneContext.name] = new HashSet<int>();
			}

			ConnectionLoadedRecord[sceneContext.name].Add(connectedPlayer.Connection.connectionId);

			Instance.AddObserverToAllObjects(connectedPlayer.Connection, sceneContext);
		}
	}

	public enum SceneType
	{
		MainStation,
		AwaySite,
		Asteroid,
		AdditionalScenes,
		Space,
		HiddenScene
	}

	[System.Serializable]
	public struct SceneInfo : IEquatable<SceneInfo>
	{
		public string SceneName;
		public SceneType SceneType;

		public bool Equals(SceneInfo other)
		{
			return SceneName == other.SceneName && SceneType == other.SceneType;
		}

		public override bool Equals(object obj)
		{
			return obj is SceneInfo other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((SceneName != null ? SceneName.GetHashCode() : 0) * 397) ^ (int) SceneType;
			}
		}
	}

	[System.Serializable]
	public class ScenesSyncList : SyncList<SceneInfo>
	{
	}

	public class SubsceneLoadTimer
	{
		public float MaxLoadTime;
		public void IncrementLoadBar(string text = "") => Report("Preparing scenes", text);
		public void Report(string stage, string context, int completed = 0, int total = 0)
		{
			string detail = total > 0 ?
				$"{stage}: {completed:N0} / {total:N0}"
				: stage == "Tile changes remaining" ?
					$"{stage}: {completed:N0}"
					: stage;
			GUI_PreRoundWindow.Instance?.LoadingArea?.UpdateLoadingBar("Loading Scenes", context,
				total > 0 ? Mathf.Clamp01((float)completed / total) : 0f, detail);
		}
	}
}
