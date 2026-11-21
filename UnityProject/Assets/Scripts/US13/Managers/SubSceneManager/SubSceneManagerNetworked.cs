using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace US13.Managers.SubSceneManager
{
	public class SubSceneManagerNetworked : NetworkBehaviour
	{
		public struct LoadingProgressState
		{
			public bool Active;
			public string Stage;
			public string Context;
			public int Completed;
			public int Total;
		}

		[SyncVar] public bool ScenesInitialLoadingComplete = false;

		[SyncVar(hook = nameof(OnLoadingProgressChanged))]
		public LoadingProgressState LoadingProgress;

		private LoadingProgressState pendingProgress;
		private bool hasPendingProgress;
		private float nextProgressSendTime;
		private float nextProgressDisplayTime;
		private const float ProgressInterval = 0.1f;

		public readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

		public SubSceneManager SubSceneManager;

		public void ReportLoadingProgress(string stage, string context, int completed, int total)
		{
			if (isServer == false) return;
			pendingProgress = new LoadingProgressState
			{
				Active = true, Stage = stage, Context = context, Completed = completed, Total = total
			};
			hasPendingProgress = true;
			if (LoadingProgress.Active == false || stage != LoadingProgress.Stage || context != LoadingProgress.Context)
			{
				PublishPendingProgress();
			}
		}

		public void FinishLoadingProgress()
		{
			if (!isServer) return;
			hasPendingProgress = false;
			// A direct default assignment so that Mirror doesn't bitch about InitLocals.
			LoadingProgress = new LoadingProgressState
			{
				Active = false,
				Stage = string.Empty,
				Context = string.Empty,
				Completed = 0,
				Total = 0
			};
		}

		private void PublishPendingProgress()
		{
			LoadingProgress = pendingProgress;
			hasPendingProgress = false;
			nextProgressSendTime = Time.unscaledTime + ProgressInterval;
		}

		private void LateUpdate()
		{
			if (isServer)
			{
				if (hasPendingProgress && Time.unscaledTime >= nextProgressSendTime) PublishPendingProgress();
			}
			else if (isClient && LoadingProgress.Active && Time.unscaledTime >= nextProgressDisplayTime)
			{
				// Replay when the lobby UI becomes available, including after a late join.
				DisplayServerProgress();
			}
		}

		private void OnLoadingProgressChanged(LoadingProgressState previous, LoadingProgressState current)
		{
			if (isServer == false && current.Active) DisplayServerProgress();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (isServer == false && LoadingProgress.Active) DisplayServerProgress();
		}

		private void DisplayServerProgress()
		{
			SubsceneLoadTimer.Display(LoadingProgress.Stage, LoadingProgress.Context,
				LoadingProgress.Completed, LoadingProgress.Total, serverProgress: true);
			nextProgressDisplayTime = Time.unscaledTime + ProgressInterval;
		}

		//Note: This is the first thing ever that gets called on the server after managers finish setting up.
		//This is where life blooms.
		public override void OnStartServer()
		{
			NetworkServer.observerSceneList.Clear();
			// Determine a Main station subscene and away site
			Destroy(GameManager.Instance.GameMode);
			GameManager.Instance.GameMode = null;
			GameManager.Instance.ChooseGameMode();
			SubSceneManager.RoundStartServerLoadSequence().Forget(e => Logs.Loggy.Error(e.ToString()));
			base.OnStartServer();
		}

	}
}
