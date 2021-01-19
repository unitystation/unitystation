using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// TODO: check if hands are dismembered
public class Hands : MonoBehaviour
{
	[Header("Gameobject references")]
	/// <summary>
	/// active when left hand is used
	/// </summary>
	public GameObject leftHandSelector;
	/// <summary>
	/// active when right hand is used
	/// </summary>
	public GameObject rightHandSelector;
	[SerializeField] private Image leftHandImage = default;
	[SerializeField] private Image rightHandImage = default;
	[SerializeField] private RectTransform handsArrowRectR = default;
	[SerializeField] private RectTransform handsArrowRectL = default;
	// public Transform rightHand;
	// public Transform leftHand;

	[Header("Asset references")]
	/// <summary>
	/// sprite that will be displayed in leftHandImage or rightHandImage (depending on which hand is used) when hand is used
	/// </summary>
	[SerializeField] private Sprite usedHandSprite = default;
	/// <summary>
	/// sprite that will be displayed in leftHandImage or rightHandImage (depending on which hand is used) when hand isn't used
	/// </summary>
	[SerializeField] private Sprite unusedHandSprite = default;
	/// <summary>
	/// Active slot
	/// </summary>
	public UI_ItemSlot CurrentSlot => IsRight ? RightHand : LeftHand;
	/// <summary>
	/// Non active slot
	/// </summary>
	public UI_ItemSlot OtherSlot => IsRight ? LeftHand : RightHand;
	public UI_ItemSlot LeftHand =>
		PlayerManager.LocalPlayerScript?.ItemStorage?.GetNamedItemSlot(NamedSlot.leftHand)?.LocalUISlot;
	public UI_ItemSlot RightHand =>
		PlayerManager.LocalPlayerScript?.ItemStorage?.GetNamedItemSlot(NamedSlot.rightHand)?.LocalUISlot;
	/// <summary>
	/// True iff right hand is active hand
	/// </summary>
	public bool IsRight { get; private set; }
	public bool UsingBothHands { get; private set; }
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
				if (right != IsRight || UsingBothHands)
				{
					hasSwitchedHands = true;
				}
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(NamedSlot.rightHand);
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = NamedSlot.rightHand;

				rightHandImage.sprite = usedHandSprite;
				leftHandImage.sprite = unusedHandSprite;
			}
			else
			{
				if (right != IsRight || UsingBothHands)
				{
					hasSwitchedHands = true;
				}
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(NamedSlot.leftHand);
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = NamedSlot.leftHand;

				rightHandImage.sprite = unusedHandSprite;
				leftHandImage.sprite = usedHandSprite;
			}
			
			// If player was using both hands - flip images back
			if (UsingBothHands)
			{
				UsingBothHands = false;
				FlipHandsNArrows();
			}

			// activate correct selector
			rightHandSelector.SetActive(right);
			leftHandSelector.SetActive(!right);

			IsRight = right;
		}
	}

	/// <summary>
	/// OnClick listener for "use_both_hands_button"
	/// </summary>
	public void UseBothHands()
	{
		if (isValidPlayer())
		{
			UsingBothHands = !UsingBothHands;
			if (UsingBothHands)
			{
				hasSwitchedHands = true;

				FlipHandsNArrows();

				// TODO: use 2 hands
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(NamedSlot.rightHand);
				PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = NamedSlot.rightHand;

				rightHandImage.sprite = usedHandSprite;
				leftHandImage.sprite = usedHandSprite;

				// activate correct selector
				rightHandSelector.SetActive(UsingBothHands);
				leftHandSelector.SetActive(UsingBothHands);
			}
			else
			{
				FlipHandsNArrows();
				SetHand(IsRight);
			}
		}
	}

	/// <summary>
	/// Flip hand & hand arrow sprites
	/// </summary>
	private void FlipHandsNArrows()
	{
		Vector3 localScaleRight = rightHandImage.transform.localScale;
		Vector3 localScaleLeft = leftHandImage.transform.localScale;

		int mult = UsingBothHands ? 1 : -1;
		// flip hands
		rightHandImage.transform.localScale = new Vector3(mult * -Mathf.Abs(localScaleRight.x), localScaleRight.y, localScaleRight.z);
		leftHandImage.transform.localScale = new Vector3(mult * Mathf.Abs(localScaleLeft.x), localScaleLeft.y, localScaleLeft.z);

		// flip arrows
		Vector3 arrowScale = new Vector3(mult * -Mathf.Abs(handsArrowRectR.localScale.x), handsArrowRectR.localScale.y, handsArrowRectR.localScale.z);
		handsArrowRectL.localScale = arrowScale;
		handsArrowRectR.localScale = arrowScale;
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
				if (CurrentSlot.Item == null)
				{
					if (itemSlot.Item != null)
					{
						Inventory.ClientRequestTransfer(itemSlot.ItemSlot, CurrentSlot.ItemSlot);
						return true;
					}
				}
				else
				{
					if (itemSlot.Item == null)
					{
						Inventory.ClientRequestTransfer(CurrentSlot.ItemSlot, itemSlot.ItemSlot);
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

		if (!isValidPlayer())
		{
			return;
		}

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
		if (!isValidPlayer())
		{
			return;
		}

		// Is there an item to equip?
		if (CurrentSlot.Item == null)
		{
			return;
		}

		//This checks which UI slot the item can be equiped to and swaps it there
		//Try to equip the item into the appropriate slot
		var bestSlot = BestSlotForTrait.Instance.GetBestSlot(CurrentSlot.Item, PlayerManager.LocalPlayerScript.ItemStorage);
		if (bestSlot == null)
		{
			Chat.AddExamineMsg(PlayerManager.LocalPlayerScript.gameObject, "There is no available slot for that");
			return;
		}

		SwapItem(bestSlot.LocalUISlot);
	}

	/// <summary>
	/// Check if the player is allowed to interact with objects
	/// </summary>
	/// <returns>True if they can, false if they cannot</returns>
	private bool isValidPlayer()
	{
		if (PlayerManager.LocalPlayerScript == null) return false;

		// TODO tidy up this if statement once it's working correctly
		if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
				PlayerManager.LocalPlayerScript.IsGhost)
		{
			Logger.Log("Invalid player, cannot perform action!", Category.UI);
			return false;
		}

		return true;
	}
}