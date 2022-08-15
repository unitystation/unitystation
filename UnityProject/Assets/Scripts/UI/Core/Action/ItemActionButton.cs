using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using NaughtyAttributes;

namespace UI.Action
{
	public class ItemActionButton : NetworkBehaviour, IServerActionGUI, IServerInventoryMove, IOnPlayerLeaveBody, IOnPlayerTransfer
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

		public Mind PreviouslyOn;

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.ToPlayer != null)
			{
				var showAlert = ShowAlert();

				if (showAlert == false && PreviouslyOn != null)
				{
					UIActionManager.ToggleServer(PreviouslyOn, this, false);
					PreviouslyOn = null;
				}

				if (showAlert && PreviouslyOn == null)
				{
					UIActionManager.ToggleServer(info.ToPlayer.PlayerScript.mind, this, true);
					UIActionManager.SetServerSpriteSO(this, spriteHandler.GetCurrentSpriteSO(), spriteHandler.Palette);
					PreviouslyOn = info.ToPlayer.PlayerScript.mind;
				}
			}
			else if (PreviouslyOn != null)
			{
				UIActionManager.ToggleServer(PreviouslyOn, this, false);
				PreviouslyOn = null;
			}
		}

		private bool ShowAlert()
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

		private void UpdateButtonSprite(bool isServer)
		{
			if (useSpriteHandler)
			{
				UIActionManager.SetServerSpriteSO(this, spriteHandler.GetCurrentSpriteSO(), palette: spriteHandler.Palette );
			}
		}

		public void OnPlayerLeaveBody(Mind mind)
		{
			UIActionManager.ToggleServer(mind, this, false);
			PreviouslyOn = null;
		}

		public void OnPlayerTransfer(Mind mind)
		{
			if (ShowAlert())
			{
				UIActionManager.ToggleServer(mind, this, true);
			}

			PreviouslyOn = mind;
		}
	}
}
