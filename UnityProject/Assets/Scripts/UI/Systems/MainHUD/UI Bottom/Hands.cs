using UnityEngine;
using UnityEngine.UI;
using HealthV2;
using Items.Implants.Organs;
using Logs;

namespace Player
{
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
		/// Active slot
		/// </summary>
		public UI_ItemSlot CurrentSlot;// => PlayerManager.LocalPlayerScript.ItemStorage.GetActiveHandSlot();

		/// <summary>
		/// True iff right hand is active hand
		/// </summary>
		public bool UsingBothHands { get; private set; }

		/// <summary>
		/// Sets the current active hand (true for right, false for left)
		/// </summary>
		public void SetHand(NamedSlot namedSlot, GameObject gamebodypPart)
		{
			if (!IsValidPlayer()) return;
			var Slot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetNamedItemSlot(gamebodypPart, namedSlot);
			if (Slot == null) return;

			var bodyPartUISlots = gamebodypPart.GetComponent<BodyPartUISlots>();
			if (UIManager.Instance.UI_SlotManager.BodyPartToSlot.ContainsKey(bodyPartUISlots.InterfaceGetInstanceID) == false) return;



			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSetActiveHand(gamebodypPart.NetId(), namedSlot);
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.activeHand = gamebodypPart;
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CurrentActiveHand = namedSlot;

			// If player was using both hands - flip images back
			if (UsingBothHands)
			{
				UsingBothHands = false;
				FlipHandsNArrows();
			}
		}

		/// <summary>
		/// OnClick listener for "use_both_hands_button"
		/// </summary>
		public void UseBothHands()
		{
			if (IsValidPlayer())
			{
				UsingBothHands = !UsingBothHands;
				if (UsingBothHands)
				{
					FlipHandsNArrows();

					// TODO: use 2 hands
					// PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(NamedSlot.rightHand);
					// PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = NamedSlot.rightHand;

					rightHandImage.sprite = usedHandSprite;
					leftHandImage.sprite = usedHandSprite;

					// activate correct selector
					rightHandSelector.SetActive(UsingBothHands);
					leftHandSelector.SetActive(UsingBothHands);
				}
				else
				{
					FlipHandsNArrows();
					//SetHand(IsRight);
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
			if (IsValidPlayer())
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

			if (!IsValidPlayer())
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
			if (!IsValidPlayer())
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
			var bestSlot = BestSlotForTrait.Instance.GetBestSlot(CurrentSlot.Item, PlayerManager.LocalPlayerScript.DynamicItemStorage);
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
		public static bool IsValidPlayer()
		{
			if (PlayerManager.LocalPlayerScript == null) return false;

			// TODO tidy up this if statement once it's working correctly
			if (!PlayerManager.LocalPlayerScript.playerMove.AllowInput ||
					PlayerManager.LocalPlayerScript.IsNormal == false)
			{
				Loggy.Log("Invalid player, cannot perform action!", Category.Interaction);
				return false;
			}

			return true;
		}
	}
}
