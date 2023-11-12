using System.Collections;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Logs;

namespace Systems.Spells.Wizard
{
	public class TeleportSpell : NetworkBehaviour
	{
		private const float TELEPORT_TRAVEL_TIME = 0;
		private const float TELEPORT_ANIMATE_TIME = 0.8f;
		private const float TELEPORT_ANIMATE_X_SCALE = 0.4f;
		private const float TELEPORT_ANIMATE_Y_SCALE = 1.1f;
		private const float TELEPORT_ANIMATE_HEIGHT = 1.5f;

		public bool IsBusy { get; private set; } = false;

		// We sync the teleporting player so we can play animations locally.
		[SyncVar(hook = nameof(SyncPlayer))]
		private NetworkIdentity IDteleportingPlayer;

		private GameObject teleportingPlayer
		{
			get => IDteleportingPlayer.OrNull()?.gameObject;
			set => SyncPlayer(IDteleportingPlayer, value.NetWorkIdentity());
		}


		private Transform playerSprite;

		[SerializeField] private AddressableAudioSource TeleportDisappear = null;

		[SerializeField] private AddressableAudioSource TeleportAppear = null;

		/// <summary>
		/// When set true, plays teleport begin animation on the client. False: plays teleport end animation.
		/// </summary>
		[SyncVar(hook = nameof(SyncAnimation))]
		private bool syncAnimation = false;

		public void ServerTeleportWizard(GameObject playerToTeleport, Vector3Int toWorldPos)
		{
			teleportingPlayer = playerToTeleport;

			StartCoroutine(RunTeleportSequence(toWorldPos));
		}

		private IEnumerator RunTeleportSequence(Vector3Int toWorldPos)
		{
			PlayerInfo player = teleportingPlayer.Player();

			IsBusy = true;
			syncAnimation = true;
			SoundManager.PlayNetworkedAtPos(TeleportDisappear, player.Script.WorldPos);
			yield return WaitFor.Seconds(TELEPORT_ANIMATE_TIME + TELEPORT_TRAVEL_TIME);

			player.Script.PlayerSync.AppearAtWorldPositionServer(toWorldPos);

			syncAnimation = false;
			SoundManager.PlayNetworkedAtPos(TeleportAppear, player.Script.WorldPos);
			yield return WaitFor.Seconds(TELEPORT_ANIMATE_TIME);
			IsBusy = false;
		}

		private void SyncPlayer(NetworkIdentity oldPlayer, NetworkIdentity newPlayer)
		{
			IDteleportingPlayer = newPlayer;
			if (teleportingPlayer == null) return; //might be setting to null idk
 			playerSprite = teleportingPlayer.transform.Find("Sprites");

			if (playerSprite == null)
			{
				Loggy.LogError($"Couldn't find child GameObject 'Sprites' on {teleportingPlayer}. Has the hierarchy changed?", Category.Spells);
			}
		}

		private void SyncAnimation(bool oldState, bool newState)
		{
			syncAnimation = newState;
			if (syncAnimation)
			{
				AnimateTeleportBegin();
			}
			else
			{
				AnimateTeleportEnd();
			}
		}

		private void AnimateTeleportBegin()
		{
			playerSprite.LeanScaleX(TELEPORT_ANIMATE_X_SCALE, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanScaleY(TELEPORT_ANIMATE_Y_SCALE, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanMoveLocalY(TELEPORT_ANIMATE_HEIGHT, TELEPORT_ANIMATE_TIME);
			AnimateOpacity(0, TELEPORT_ANIMATE_TIME);
		}

		private void AnimateTeleportEnd()
		{
			playerSprite.LeanScaleX(1, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanScaleY(1, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanMoveLocalY(0, TELEPORT_ANIMATE_TIME);
			AnimateOpacity(1, TELEPORT_ANIMATE_TIME);
		}

		private void AnimateOpacity(float alpha, float time)
		{
			SpriteHandler[] spriteHandlers = teleportingPlayer.GetComponentsInChildren<SpriteHandler>();

			foreach (var handler in spriteHandlers)
			{
				handler.gameObject.LeanAlpha(alpha, time);
			}
		}
	}
}
