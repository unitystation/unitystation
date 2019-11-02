using UnityEngine;

public class Hands : MonoBehaviour
{
	public Transform selector;
	public RectTransform selectorRect;
	public Transform rightHand;
	public Transform leftHand;
	/// <summary>
	/// Active slot
	/// </summary>
	public UI_ItemSlot CurrentSlot => IsRight ? RightHand : LeftHand;
	/// <summary>
	/// Non active slot
	/// </summary>
	public UI_ItemSlot OtherSlot => IsRight ? LeftHand : RightHand;
	public UI_ItemSlot LeftHand =>
		PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.leftHand).LocalUISlot;
	public UI_ItemSlot RightHand =>
		PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.rightHand).LocalUISlot;
	/// <summary>
	/// True iff right hand is active hand
	/// </summary>
	public bool IsRight { get; private set; }
	public bool hasSwitchedHands;

	private void Start()
	{
		IsRight = true;
		hasSwitchedHands = false;
	}

	/// <summary>
	/// Action to swap hands
	/// </summary>
	public void Swap()
	{
		if (isValidPlayer())
		{
			SetHand(!IsRight);
		}
	}

	/// <summary>
	/// Sets the current active hand (true for right, false for left)
	/// </summary>
	public void SetHand(bool right)
	{
		if (isValidPlayer())
		{
			if (right)
			{
				if (right != IsRight)
				{
					hasSwitchedHands = true;
				}
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(NamedSlot.rightHand);
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = NamedSlot.rightHand;
				selector.SetParent(rightHand, false);
			}
			else
			{
				if (right != IsRight)
				{
					hasSwitchedHands = true;
				}
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(NamedSlot.leftHand);
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = NamedSlot.leftHand;
				selector.SetParent(leftHand, false);
			}

			IsRight = right;
			var pos = selectorRect.anchoredPosition;
			pos.x = 0f;
			selectorRect.anchoredPosition = pos;
			selector.SetAsFirstSibling();
		}
	}

	/// <summary>
	/// Swap the item in the current slot to itemSlot
	/// </summary>
	public bool SwapItem(UI_ItemSlot itemSlot)
	{
		if (isValidPlayer())
		{
			if (CurrentSlot != itemSlot)
			{
				var pna = PlayerManager.LocalPlayerScript.playerNetworkActions;

				if (CurrentSlot.Item == null)
				{
					if(itemSlot.Item != null)
					{
						RequestInventoryTransferMessage.Send(itemSlot.ItemSlot, CurrentSlot.ItemSlot);
						return true;
					}
				}
				else
				{
					if(itemSlot.Item == null)
					{
						RequestInventoryTransferMessage.Send(CurrentSlot.ItemSlot, itemSlot.ItemSlot);
						return true;
					}
				}
			}
		}
		return false;
	}

	/// <summary>
	/// General function to activate the item's UIInteract
	/// This is the same as clicking the item with the same item's hand
	/// </summary>
	public void Activate()
	{
		// Is there an item in the active hand?
		if (CurrentSlot.Item == null)
		{
			return;
		}
		CurrentSlot.TryItemInteract();
	}

	/// <summary>
	/// General function to try to equip the item in the active hand
	/// </summary>
	public void Equip()
	{
		// Is the player allowed to interact? (not a ghost)
		if(!isValidPlayer())
		{
			return;
		}

		// Is there an item to equip?
		if(CurrentSlot.Item == null)
		{
			Logger.Log("!CurrentSlot.IsFull");
			return;
		}

		//This checks which UI slot the item can be equiped to and swaps it there
		//Try to equip the item into the appropriate slot
		if (!SwapItem(CurrentSlot))
		{
			//If we couldn't equip the item into it's primary slot, try the pockets!
			if(!SwapItem(PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.storage01).LocalUISlot))
			{
				//We couldn't equip the item in pocket 1. Try pocket2!
				//This swap fails if both pockets are full, do nothing if fail
				SwapItem(PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.storage02).LocalUISlot);
			}
		}
	}

	/// <summary>
	/// Check if the player is allowed to interact with objects
	/// </summary>
	/// <returns>True if they can, false if they cannot</returns>
	private bool isValidPlayer()
	{
		if (PlayerManager.LocalPlayerScript != null)
		{
			// TODO tidy up this if statement once it's working correctly
			if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.IsGhost)
			{
				Logger.Log("Invalid player, cannot perform action!", Category.UI);
				return false;
			}
		}
		return true;
	}

}
