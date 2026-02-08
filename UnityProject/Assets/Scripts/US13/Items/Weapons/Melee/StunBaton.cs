using Mirror;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Managers;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Weapons.Melee
{
	[RequireComponent(typeof(Pickupable))]
	public class StunBaton : NetworkBehaviour, IPredictedInteractable<HandActivate>
	{
		public SpriteHandler spriteHandler;

		/// <summary>
		/// Sound played when turning this baton on/off
		/// </summary>
		[SerializeField] private AddressableAudioSource soundToggle = null;

		/// <summary>
		/// Sprite to be shown when the baton is on
		/// </summary>
		[SerializeField]
		private Sprite spriteActive = null;

		/// <summary>
		/// Sprite to be shown when the baton is off
		/// </summary>
		[SerializeField]
		private Sprite spriteInactive = null;

		[SyncVar(hook = nameof(UpdateState))]
		public bool active;

		public bool isActive => active;

		public void ToggleState()
		{
			UpdateState(active, !active);
		}

		private void UpdateState(bool oldValue, bool newState)
		{
			active = newState;

			UpdateSprite();
		}

		private void UpdateSprite()
		{
			if (active)
			{
				spriteHandler?.SetSpriteNonNetworked(spriteActive);
			}
			else
			{
				spriteHandler?.SetSpriteNonNetworked(spriteInactive);
			}
		}

		public void ClientPredictInteraction(HandActivate interaction)
		{
			ToggleState();
		}

		public void ServerRollbackClient(HandActivate interaction)
		{
			UpdateState(active, active);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			SoundManager.PlayNetworkedAtPos(soundToggle, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);
			ToggleState();
		}
	}
}