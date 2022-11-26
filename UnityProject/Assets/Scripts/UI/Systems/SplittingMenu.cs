using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class SplittingMenu : MonoBehaviour
	{
		[SerializeField]
		private InputField amountInput;

		public static SplittingMenu Instance;
		private int AmountToTransfer;
		private ItemSlot currentSlot;
		private ItemSlot stackSlot;

		public void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		public void Enable()
		{
			this.SetActive(true);
		}

		public void Disable()
		{
			this.SetActive(false);
		}

		public void BtnConfirmation()
		{
			if (int.TryParse(amountInput.text, out AmountToTransfer))
			{
				currentSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();

				var leftHands = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.leftHand);
				foreach (var leftHand in leftHands)
				{
					if (leftHand != currentSlot && leftHand.ItemObject != null)
					{
						stackSlot = leftHand;
					}
				}

				var rightHands = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.rightHand);
				foreach (var rightHand in rightHands)
				{
					if (rightHand != currentSlot && rightHand.ItemObject != null)
					{
						stackSlot = rightHand;
					}
				}

				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSplitStack
					(
					stackSlot.ItemStorage.gameObject.NetId(),
					stackSlot.NamedSlot.GetValueOrDefault(NamedSlot.none),
					AmountToTransfer
					);

				Disable();
			}
			else
			{
				amountInput.text = "0";
			}
		}
	}
}
