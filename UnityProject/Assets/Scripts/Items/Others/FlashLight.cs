using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using NaughtyAttributes;

namespace Items.Others
{
	[RequireComponent(typeof(ItemLightControl))]
	public class FlashLight : NetworkBehaviour, IServerActionGUI, IClientInventoryMove,
			ICheckedInteractable<HandActivate>
	{
		[SerializeField]
		private SpriteHandler spriteHandler = default;

		[SerializeField]
		[Tooltip("Should this flashlight have a relevant action button?")]
		private bool hasActionButton = true;

		[SerializeField, ShowIf(nameof(hasActionButton))]
		[Tooltip("Assign the action button this flashlight should use.")]
		private ActionData flashlightAction = default;

		// The light the flashlight has access to
		private ItemLightControl lightControl;
		private Pickupable pickupable;

		protected bool IsOn => lightControl.IsOn;
		protected int SpriteIndex => IsOn ? 1 : 0;
		public ActionData ActionData => flashlightAction;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			lightControl = GetComponent<ItemLightControl>();
		}

		/// <summary>
		/// Checks to make sure the player is conscious and stuff
		/// </summary>
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		/// <summary>
		/// Toggles the light on and off depending on the "IsOn" bool from the "ItemLightControl" component
		/// </summary>
		public void ServerPerformInteraction(HandActivate interaction)
		{
			ToggleLight();
		}

		public void OnInventoryMoveClient(ClientInventoryMove info)
		{
			if (!hasActionButton) return;

			bool shouldShowButton = pickupable.ItemSlot != null &&
					pickupable.ItemSlot.Player != null &&
					info.ClientInventoryMoveType == ClientInventoryMoveType.Added;

			if (!shouldShowButton)
			{
				UIActionManager.ToggleLocal(this, false);
				return;
			}

			// If the slot the item is a slot of the client's.
			if (pickupable.ItemSlot.LocalUISlot != null)
			{
				UIActionManager.ToggleLocal(this, true);
				UIActionManager.SetSpriteSO(this, spriteHandler.GetCurrentSpriteSO(), false);
			}
		}

		/// <summary>
		/// The action button calls this when clicked.
		/// </summary>
		/// <param name="SentByPlayer"></param>
		public void CallActionServer(ConnectedPlayer SentByPlayer)
		{
			ToggleLight();
		}

		public void CallActionClient() { }

		protected virtual void ToggleLight()
		{
			lightControl.Toggle(!lightControl.IsOn);
			spriteHandler.ChangeSprite(SpriteIndex);
			UIActionManager.SetSpriteSO(this, spriteHandler.GetCurrentSpriteSO());
		}
	}
}
