using System.Collections.Generic;
using Logs;
using MiniGames;
using UnityEngine;
using Random = UnityEngine.Random;
using MiniGames.MiniGameModules;

namespace Objects.Closets
{
	public class AbandonedCrates : MonoBehaviour, IServerSpawn, ICheckedInteractable<HandApply>
	{

		[SerializeField] private MiniGameResultTracker miniGameTracker;
		[SerializeField] private List<MiniGameModule> miniGameModules;
		[SerializeField] private ClosetControl control;
		[SerializeField] private HasNetworkTab netTab = null;

		private int currentMiniGameIndex = -1;


		private void Awake()
		{
			if (control == null) control = GetComponent<ClosetControl>();
		}


		public void OnSpawnServer(SpawnInfo info)
		{
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

			currentMiniGameIndex = Random.Range(0, miniGameModules.Count - 1);

			if(miniGameModules[currentMiniGameIndex] as ReflectionGolfModule != null) netTab.NetTabType = NetTabType.ReflectionGolf;

			miniGameModules[currentMiniGameIndex].Setup(miniGameTracker, gameObject);
			control.SetLock(ClosetControl.Lock.Locked);
		}

		public void StartGame()
		{
			miniGameModules[currentMiniGameIndex].StartMiniGame();
		}

		public void GameWin()
		{
			control.SetLock(ClosetControl.Lock.Unlocked);
			control.SetDoor(ClosetControl.Door.Opened);
		}

		public void GameLoss()
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(CommonSounds.Instance.AccessDenied,
				gameObject.AssumedWorldPosServer());
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (miniGameTracker == null || control == null ||control.IsOpen) return false;
			if (currentMiniGameIndex == -1)
			{
				Loggy.LogError("[MiniGames/AbandonedCrates] - Found MiniGameTracker but no minigame is assigned!");
				return false;
			}
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			miniGameTracker.OnStartGame?.Invoke();
		}
	}
}