using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UI.Objects.Cargo;

namespace Objects.Cargo
{
	public class CargoConsole : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		public bool CorrectID;
		public bool Emagged;

		public GUI_Cargo cargoGUI;

		[SerializeField]
		private List<JobType> allowedTypes = null;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
				return false;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id))
				return true;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag))
				return true;
			return false;
		}

		/// <summary>
		/// Resets the ID to false
		/// </summary>
		[Server]
		public void ResetID()
		{
			CorrectID = false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id))
			{
				CheckID(interaction.HandSlot.Item.GetComponent<IDCard>().JobType, interaction.Performer);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag))
			{
				//"You adjust the supply console's routing and receiver spectrum, unlocking special supplies and contraband.
				Emagged = true;
				if (cargoGUI)
				{
					cargoGUI.pageCart.UpdateTab();
				}
			}
		}

		[Server]
		private void CheckID(JobType usedID, GameObject playeref)
		{
			if (cargoGUI == null)
				return;
			foreach (var aJob in allowedTypes.Where(aJob => usedID == aJob))
			{
				CorrectID = true;
				cargoGUI.pageCart.UpdateTab();
				break;
			}

			var denyString = "the console denies your ID";
			if (CorrectID)
			{
				denyString = "the console accepts your ID";
			}
			Chat.AddActionMsgToChat(playeref, $"You swipe your ID through the supply console's ID slot, {denyString}",
				$"{playeref.ExpensiveName()} swiped their ID through the supply console's ID slot");

		}
	}
}
