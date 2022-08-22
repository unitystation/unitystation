using System.Collections.Generic;
using MiniGames;
using Mirror;
using UnityEngine;

namespace Objects.Closets
{
	public class AbandonedCrates : ClosetControl, IMiniGame
	{

		[SerializeField] private MiniGameResultTracker miniGameTracker;
		[SerializeField] private List<MiniGameModule> miniGameModules;
		[SyncVar] private int currentMiniGameIndex = -1;

		public override void OnSpawnServer(SpawnInfo info)
		{
			base.OnSpawnServer(info);
			SetupMiniGames();
		}

		private void SetupMiniGames()
		{
			if(miniGameTracker == null) return;
			if (miniGameModules.Count == 0)
			{
				Logger.LogError("[ClosetControl/MiniGames] - Found MiniGame tracker but no minigames found!");
				return;
			}
			currentMiniGameIndex = Random.Range(0, miniGameModules.Count - 1);
			miniGameModules[currentMiniGameIndex].Setup(miniGameTracker);
			SetLock(Lock.Locked);
		}

		protected override void InteractionChecks(PositionalHandApply interaction)
		{
			if (miniGameTracker != null)
			{
				if(doorState == Door.Opened || lockState == Lock.Unlocked) return;
				if (currentMiniGameIndex == -1)
				{
					Logger.LogError("[ClosetControl/MiniGames] - Found MiniGameTracker but no minigame is assigned!");
					return;
				}
				RpcStartMiniGameForTargetClient(interaction.PerformerPlayerScript.connectionToServer);
				return;
			}
			base.InteractionChecks(interaction);
		}


		[TargetRpc]
		private void RpcStartMiniGameForTargetClient(NetworkConnection target)
		{
			StartGame();
		}

		[Command(requiresAuthority = false)]
		public void CmdGameEnd(bool result)
		{
			GameEnd(result);
		}

		public void StartGame()
		{
			miniGameModules[currentMiniGameIndex].StartMiniGame();
		}

		public void GameEnd(bool hasWon)
		{
			if (hasWon == false)
			{
				_ = SoundManager.PlayNetworkedAtPosAsync(CommonSounds.Instance.AccessDenied,
					gameObject.AssumedWorldPosServer());
				return;
			}
			SetLock(Lock.Unlocked);
			SetDoor(Door.Opened);
		}

		[Command(requiresAuthority = false)]
		public void ServerCheckResult()
		{

		}

	}
}