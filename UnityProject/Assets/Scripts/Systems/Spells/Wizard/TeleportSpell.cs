using System.Collections;
using UnityEngine;
using Mirror;
using AddressableReferences;

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
		private GameObject teleportingPlayer;

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
			ConnectedPlayer player = teleportingPlayer.Player();

			IsBusy = true;
			syncAnimation = true;
			SoundManager.PlayNetworkedAtPos(TeleportDisappear, player.Script.WorldPos);
			yield return WaitFor.Seconds(TELEPORT_ANIMATE_TIME + TELEPORT_TRAVEL_TIME);

			player.Script.PlayerSync.SetPosition(toWorldPos, true);

			syncAnimation = false;
			SoundManager.PlayNetworkedAtPos(TeleportAppear, player.Script.WorldPos);
			yield return WaitFor.Seconds(TELEPORT_ANIMATE_TIME);
			IsBusy = false;
		}

		private void SyncPlayer(GameObject oldPlayer, GameObject newPlayer)
		{
			teleportingPlayer = newPlayer;
			playerSprite = teleportingPlayer.transform.Find("Sprites");

			if (playerSprite == null)
			{
				Logger.LogError($"Couldn't find child GameObject 'Sprites' on {teleportingPlayer}. Has the hierarchy changed?");
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
