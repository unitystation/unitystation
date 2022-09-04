using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using NaughtyAttributes;

namespace UI.Action
{
	public class ItemActionButton : NetworkBehaviour, IServerActionGUI, IItemInOutMovedPlayer
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
				if (isServer)
				{
					spriteHandler.OnSpriteDataSOChanged += so => SpriteHandlerSOChanged(so);
				}
			}
		}

		private void SpriteHandlerSOChanged(SpriteDataSO obj)
		{
			UIActionManager.SetServerSpriteSO(this, obj, spriteHandler.Palette);
		}

		public void CallActionClient()
		{
			if (Validations.CanInteract(PlayerManager.LocalPlayerScript, NetworkSide.Client, allowSoftCrit: true))
			{
				ClientActionClicked?.Invoke();
			}
		}

		public void CallActionServer(PlayerInfo playerInfo)
		{
			if (Validations.CanInteract(playerInfo.Script , NetworkSide.Server, true))
			{
				ServerActionClicked?.Invoke();
				UpdateButtonSprite(true);
			}
		}

		public Mind CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		public bool IsValidSetup(Mind player)
		{
			bool showAlert;
			if (pickupable.ItemSlot.NamedSlot == null)
			{
				showAlert = false;
			}
			else
			{
				showAlert = (allowedSlots.HasFlag(ItemSlot.GetFlaggedSlot(pickupable.ItemSlot.NamedSlot.Value)));
			}

			return showAlert;
		}

		void IItemInOutMovedPlayer.ChangingPlayer(Mind hideForPlayer, Mind showForPlayer)
		{
			if (hideForPlayer != null)
			{
				UIActionManager.ToggleServer(hideForPlayer, this, false);
			}

			if (showForPlayer != null)
			{
				UIActionManager.ToggleServer(showForPlayer, this, true);
				UIActionManager.SetServerSpriteSO(this, spriteHandler.GetCurrentSpriteSO(), spriteHandler.Palette);
			}
		}

		private void UpdateButtonSprite(bool isServer)
		{
			if (useSpriteHandler)
			{
				UIActionManager.SetServerSpriteSO(this, spriteHandler.GetCurrentSpriteSO(), palette: spriteHandler.Palette );
			}
		}

	}
}
