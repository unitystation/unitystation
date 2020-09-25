using System.Collections;
using UnityEngine;
using Mirror;

namespace Items.Scrolls.TeleportScroll
{
	public class ScrollOfTeleportation : Scroll
	{
		private const float TELEPORT_TRAVEL_TIME = 0;
		private const float TELEPORT_ANIMATE_TIME = 0.8f;
		private const float TELEPORT_ANIMATE_X_SCALE = 0.4f;
		private const float TELEPORT_ANIMATE_Y_SCALE = 1.1f;
		private const float TELEPORT_ANIMATE_HEIGHT = 1.5f;

		private HasNetworkTabItem netTab;

		private bool isBusy = false;

		// We sync the teleporting player so we can play animations locally.
		[SyncVar(hook = nameof(SyncPlayer))]
		private GameObject teleportingPlayer;

		private Transform playerSprite;

		/// <summary>
		/// When set true, plays teleport begin animation on the client. False: plays teleport end animation.
		/// </summary>
		[SyncVar(hook = nameof(SyncAnimation))]
		private bool syncAnimation = false; 

		protected override void Awake()
		{
			base.Awake();
			netTab = GetComponent<HasNetworkTabItem>();
		}

		public override bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (base.WillInteract(interaction, side) == false) return false;

			// If charges remain, return false to allow the HasNetworkTabItem component to take over.
			return !HasCharges;
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
				StartCoroutine(AnimateOpacity(-0.1f));
			}
			else
			{
				AnimateTeleportEnd();
				StartCoroutine(AnimateOpacity(0.1f));
			}
		}

		public void TeleportTo(TeleportDestination destination)
		{
			teleportingPlayer = netTab.LastInteractedPlayer();

			if (!HasChargesRemaining(teleportingPlayer)) return;

			if (isBusy)
			{
				Chat.AddExamineMsgFromServer(teleportingPlayer, $"You are already teleporting!");
				return;
			}

			Transform spawnTransform = PlayerSpawn.GetSpawnForJob((JobType)destination);
			StartCoroutine(RunTeleportSequence(spawnTransform));

			ChargesRemaining--;
		}

		private IEnumerator RunTeleportSequence(Transform spawnTransform)
		{
			isBusy = true;
			syncAnimation = true;
			yield return WaitFor.Seconds(TELEPORT_ANIMATE_TIME + TELEPORT_TRAVEL_TIME);

			teleportingPlayer.GetComponent<PlayerSync>().SetPosition(spawnTransform.position, true);

			syncAnimation = false;
			yield return WaitFor.Seconds(TELEPORT_ANIMATE_TIME);
			isBusy = false;
		}

		private void AnimateTeleportBegin()
		{
			playerSprite.LeanScaleX(TELEPORT_ANIMATE_X_SCALE, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanScaleY(TELEPORT_ANIMATE_Y_SCALE, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanMoveLocalY(TELEPORT_ANIMATE_HEIGHT, TELEPORT_ANIMATE_TIME);
		}

		private void AnimateTeleportEnd()
		{
			playerSprite.LeanScaleX(1, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanScaleY(1, TELEPORT_ANIMATE_TIME);
			playerSprite.LeanMoveLocalY(0, TELEPORT_ANIMATE_TIME);
		}

		private IEnumerator AnimateOpacity(float amountPerGrade)
		{
			SpriteHandler[] spriteHandlers = teleportingPlayer.GetComponentsInChildren<SpriteHandler>();

			float lapsedTime = 0;
			while (lapsedTime < TELEPORT_ANIMATE_TIME)
			{
				foreach (var handler in spriteHandlers)
				{
					var newColor = handler.GetColor();
					newColor.a = Mathf.Clamp(newColor.a + amountPerGrade, 0, 1);
					handler.SetColor(newColor);
				}

				yield return WaitFor.Seconds(0.1f);
				lapsedTime += 0.1f;
			}
		}
	}

	// Use the JobType's spawnpoint as the destination.
	public enum TeleportDestination
	{
		Commons = JobType.ASSISTANT,
		Atmospherics = JobType.ATMOSTECH,
		Cargo = JobType.CARGOTECH,
		Medbay = JobType.DOCTOR,
		Engineering = JobType.ENGINEER,
		Science = JobType.SCIENTIST,
		Security = JobType.SECURITY_OFFICER
	}
}
