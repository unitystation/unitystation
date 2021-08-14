using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using NaughtyAttributes;

namespace UI.Action
{
	public class ItemActionButton : NetworkBehaviour, IServerActionGUI, IClientInventoryMove
	{
		[Tooltip("The button action data SO this component should use.")]
		[SerializeField]
		private ActionData actionData = default;

		[Tooltip("The slots in which this gameObject should display the action button.")]
		[SerializeField]
		private NamedSlotFlagged allowedSlots = (NamedSlotFlagged)~0; // Everything

		[Tooltip("Whether this action button should obtain sprite from a SpriteHandler")]
		[SerializeField]
		private bool useSpriteHandler = true;

		[Tooltip("The spriteHandler that manages the item's sprite. Applied to the action button after the action is called.")]
		[SerializeField, ShowIf(nameof(useSpriteHandler))]
		private SpriteHandler spriteHandler = default;

		private Pickupable pickupable;

		public ActionData ActionData => actionData;
		public event System.Action ServerActionClicked;
		public event System.Action ClientActionClicked;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();

			if(useSpriteHandler)
			{
				spriteHandler.OnSpriteDataSOChanged += SpriteHandlerSOChanged;
			}
		}

		private void SpriteHandlerSOChanged(SpriteDataSO obj)
		{
			UIActionManager.SetSpriteSO(this, spriteHandler.GetCurrentSpriteSO(), false, spriteHandler.Palette);
		}

		public void CallActionClient()
		{
			if (Validations.CanInteract(PlayerManager.LocalPlayerScript, NetworkSide.Client, allowSoftCrit: true))
			{
				ClientActionClicked?.Invoke();
				UpdateButtonSprite(false);
			}
		}

		public void CallActionServer(ConnectedPlayer SentByPlayer)
		{
			if (Validations.CanInteract(SentByPlayer.Script , NetworkSide.Server, true))
			{
				ServerActionClicked?.Invoke();
				UpdateButtonSprite(true);
			}
		}

		public void OnInventoryMoveClient(ClientInventoryMove info)
		{
			bool shouldShow = ShouldShowButton(info);
			ClientSetActionButtonVisibility(shouldShow);

			if (PlayerManager.LocalPlayerScript == null || PlayerManager.LocalPlayerScript.playerHealth == null) return;
		}

		public void ClientSetActionButtonVisibility(bool isVisible)
		{
			if (isVisible)
			{
				ClientShowActionButton();
			}
			else
			{
				ClientHideActionButton();
			}
		}

		public void ClientShowActionButton()
		{
			UIActionManager.ToggleLocal(this, true);
			UpdateButtonSprite(false);
		}

		public void ClientHideActionButton()
		{
			UIActionManager.ToggleLocal(this, false);
		}

		private bool ShouldShowButton(ClientInventoryMove info)
		{
			// Check if we are in an inventory
			if (info.ClientInventoryMoveType == ClientInventoryMoveType.Removed) return false;
			// ... we are not on a player
			if (pickupable.ItemSlot.Player == null) return false;
			// ... not the client that owns this object
			if (pickupable.ItemSlot.LocalUISlot == null) return false;
			// ... item is not in an allowed slot
			if (pickupable.ItemSlot.NamedSlot != null)
			{
				if (!allowedSlots.HasFlag(ItemSlot.GetFlaggedSlot(pickupable.ItemSlot.NamedSlot.Value))) return false;
			}


			return true;
		}

		private void OnDeath()
		{
			ClientHideActionButton();
		}

		private void UpdateButtonSprite(bool isServer)
		{
			if (useSpriteHandler)
			{
				UIActionManager.SetSpriteSO(this, spriteHandler.GetCurrentSpriteSO(), networked: isServer, palette: spriteHandler.Palette );
			}
		}
	}
}
