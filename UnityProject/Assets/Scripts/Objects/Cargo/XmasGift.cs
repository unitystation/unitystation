using System;
using System.Collections;
using System.Collections.Generic;
using Items.Cargo.Wrapping;
using UnityEngine;


namespace Items.Cargo
{
	public class XmasGift : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private WrappedItem gift;

		private void Awake()
		{
			gift = GetComponent<WrappedItem>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			gift.ServerPerformInteraction(interaction);
		}
	}
}

