using System.Collections.Generic;
using Logs;
using MiniGames;
using UnityEngine;
using Random = UnityEngine.Random;
using MiniGames.MiniGameModules;
using Mirror;

namespace Objects.Closets
{
	public class AbandonedCrates : NetworkBehaviour
	{

		[SerializeField] private MiniGameResultTracker miniGameTracker;
		[SerializeField] private List<MiniGameModule> miniGameModules;
		[SerializeField] private ClosetControl control;
		[SerializeField] private HasNetworkTab netTab = null;

		[SyncVar(hook = nameof(SyncMiniGame))] private int currentMiniGameIndex = 0;
		private bool hasCompletedPuzzle = false;


		private void Awake()
		{
			if (control == null) control = GetComponent<ClosetControl>();

			miniGameTracker.OnGameWon.AddListener(GameWin);
			miniGameTracker.OnGameLoss.AddListener(GameLoss);

			SetupMiniGames();
		}

		public void SyncMiniGame(int oldValue, int newValue)
		{
			if (CustomNetworkManager.IsServer == true) return;
			if (oldValue == newValue) return;

			currentMiniGameIndex = newValue;

			SetupMiniGames();
		}

		private void SetupMiniGames()
		{
			if (miniGameTracker == null) return;
			if (miniGameModules.Count == 0)
			{
				Loggy.LogError("[MiniGames/AbandonedCrates] - Found MiniGame tracker but no minigames found!");
				return;
			}

			if (CustomNetworkManager.IsServer == true)
			{
				control.SetLock(ClosetControl.Lock.Locked);
				currentMiniGameIndex = Random.Range(0, miniGameModules.Count - 1);

			}

			if (currentMiniGameIndex < 0) return;

			if (miniGameModules[currentMiniGameIndex] is ReflectionGolfModule) netTab.NetTabType = NetTabType.ReflectionGolf;

			miniGameModules[currentMiniGameIndex].Setup(miniGameTracker, gameObject);

			miniGameTracker.StartGame();
		}

		public void GameWin()
		{
			netTab.NetTabType = NetTabType.None;
			hasCompletedPuzzle = true;
			miniGameTracker.OnGameWon.RemoveListener(GameWin);

			if (CustomNetworkManager.IsServer == false) return;

			control.SetLock(ClosetControl.Lock.Unlocked);
		}

		public void GameLoss()
		{
			netTab.NetTabType = NetTabType.None;
			miniGameTracker.OnGameLoss.RemoveListener(GameLoss);

			if (CustomNetworkManager.IsServer ==false) return;

			_ = SoundManager.PlayNetworkedAtPosAsync(CommonSounds.Instance.AccessDenied,
				gameObject.AssumedWorldPosServer());
		}
	}
}