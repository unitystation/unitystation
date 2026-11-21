using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;
using US13.Managers.AddressableManager;
using US13.Managers.NetworkManagement;
using US13.Messages.Client;
using US13.UI.Systems.PreRound;

namespace US13.Managers.SubSceneManager
{
	public partial class SubSceneManager
	{
		public bool clientIsLoadingSubscene;
		public HashSet<SceneInfo> clientLoadedSubScenes = new HashSet<SceneInfo>();
		private float waitTime;
		private readonly float tickRate = 1f;
		public Dictionary<string, bool> ClientObserver = new Dictionary<string, bool>();
		public bool ClientIsFullyDoneLoadingOnSubsceneManager { get; private set; }
		private Action ClientSideFinishAction;

		private void MonitorServerSceneListOnClient()
		{
			if (CustomNetworkManager.IsServer || clientIsLoadingSubscene || !AddressableCatalogueManager.FinishLoaded) return;
			waitTime += Time.deltaTime;
			if (waitTime < tickRate) return;
			waitTime = 0;
			foreach (var scene in loadedScenesList)
			{
				if (clientLoadedSubScenes.Contains(scene)) continue;
				LoadClientSubScene(scene).Forget(e => Logs.Loggy.Error(e.ToString()));
				break;
			}
		}

		private async UniTask LoadClientSubScene(SceneInfo sceneInfo, bool synchronize = true, bool ownsLoadingFlag = true)
		{
			var cancellationToken = LoadingToken;
			if (ownsLoadingFlag) clientIsLoadingSubscene = true;
			try
			{
				await LoadSubScene(sceneInfo.SceneName, new SubsceneLoadTimer(), synchronize, sceneInfo.SceneType);
				cancellationToken.ThrowIfCancellationRequested();
				if (sceneInfo.SceneType == SceneType.MainStation) MainStationLoaded = true;
				clientLoadedSubScenes.Add(sceneInfo);
			}
			finally
			{
				if (ownsLoadingFlag && !cancellationToken.IsCancellationRequested) clientIsLoadingSubscene = false;
			}
		}

		public void LoadScenesFromServer(List<SceneInfo> scenes, string originalScene, Action onFinish)
		{
			LoadClientScenesFromServer(scenes, originalScene, onFinish).Forget(e => Logs.Loggy.Error(e.ToString()));
		}

		private async UniTask LoadClientScenesFromServer(List<SceneInfo> scenes, string originalScene, Action onFinish)
		{
			var cancellationToken = LoadingToken;
			ClientSideFinishAction = onFinish;
			ClientIsFullyDoneLoadingOnSubsceneManager = false;
			clientIsLoadingSubscene = true;
			var progress = new SubsceneLoadTimer();

			try
			{
				for (int i = 0; i < scenes.Count; i++)
				{
					progress.Report("Scenes completed", scenes[i].SceneName, i, scenes.Count);
					await LoadClientSubScene(scenes[i], false, false);
					cancellationToken.ThrowIfCancellationRequested();
					progress.Report("Scenes completed", scenes[i].SceneName, i + 1, scenes.Count);
				}

				progress.Report("Preparing scene objects", originalScene);
				NetworkClient.PrepareToSpawnSceneObjects();
				ClientObserver.Clear();

				foreach (var scene in scenes)
				{
					ClientObserver[scene.SceneName] = false;
				}
				RequestObserverRefresh.Send(originalScene);

				foreach (var scene in scenes)
				{
					await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: cancellationToken);
					RequestObserverRefresh.Send(scene.SceneName);
				}
				int attempts = 0;
				while (!ObserverOfAll() && attempts++ < 600)
				{
					progress.Report("Scene acknowledgements received", "Synchronizing objects", ClientObserver.Count(x => x.Value), ClientObserver.Count);
					await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: cancellationToken);
				}
				if (ObserverOfAll() == false)
				{
					progress.Report("Synchronization timed out", "Some scenes have not acknowledged their objects");
					Logs.Loggy.Error("Timed out waiting for scene observer acknowledgements.");
					return;
				}
				ClientIsFullyDoneLoadingOnSubsceneManager = true;
				ClientSideFinishAction = null;
				onFinish?.Invoke();
				cancellationToken.ThrowIfCancellationRequested();
				GUI_PreRoundWindow.Instance?.HideLoadingArea();
			}
			finally
			{
				if (cancellationToken.IsCancellationRequested == false)
				{
					clientIsLoadingSubscene = false;
					ClientObserver.Clear();
					ClientSideFinishAction = null;
				}
			}
		}

		private bool ObserverOfAll()
		{
			return ClientObserver.All(x => x.Value);
		}
	}
}
