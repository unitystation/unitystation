using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Chemistry;

namespace UI
{
	public class GrenadeTimerMenu : MonoBehaviour //copypasta of splitting menu
	{
		[SerializeField]
		private InputField TimerInput;

		public static GrenadeTimerMenu Instance;
		[NonSerialized]
		public GameObject Grenade;
		private int newTimerLength;

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
			bool isInHands = false;
			if (int.TryParse(TimerInput.text, out newTimerLength))
			{
				var leftHands = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.leftHand);
				foreach (var leftHand in leftHands)
				{
					if (leftHand.ItemObject == Grenade)
					{
						isInHands = true;
					}
				}

				var rightHands = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.rightHand);
				foreach (var rightHand in rightHands)
				{
					if (rightHand.ItemObject == Grenade)
					{
						isInHands = true;
					}
				}

				newTimerLength = newTimerLength < 0 ? 0 : newTimerLength;
				newTimerLength = newTimerLength > 1000000 ? 1000000 : newTimerLength;
				if (isInHands) Grenade.GetComponent<ChemGrenade>().TimerLength = newTimerLength;

				Disable();
			}
			else
			{
				TimerInput.text = "0";
			}
		}
	}
}
