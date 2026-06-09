using System;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.PDA;
using Util;

namespace US13.Objects.Consoles
{
	class SyndicateTelecrystalStation : MonoBehaviour,  ICheckedInteractable<HandApply>, IExaminable
	{
		private static int TransferAmount = 5;
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side)) return true;
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			WithdrawTeleCrystals(interaction);
		}

		public void WithdrawTeleCrystals(HandApply interaction)
		{
			if (SyndicateOpConsole.Instance.TcReserve == 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"There are no telecrystals in reserve");
				return;
			}
			if (interaction.UsedObject == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"There are {SyndicateOpConsole.Instance.TcReserve} telecrystals in reserve");
				return;
			}
			PDALogic pdaComp = interaction.UsedObject.GetComponent<PDALogic>();
			if (pdaComp != null)
			{
				if (pdaComp.IsUplinkLocked == false)
				{
					var amount = Math.Min(TransferAmount, SyndicateOpConsole.Instance.TcReserve);
					pdaComp.UplinkTC += amount;
					pdaComp.UpdateTCCountGui();
					SyndicateOpConsole.Instance.TcReserve -= amount;
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You successfully transfer {amount} telecrystals into the {interaction.TargetObject.ExpensiveName()}");
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"Your {interaction.TargetObject.ExpensiveName()} must be unlocked to transfer TC!");
				}
			}
		}

		public string Examine(Vector3 vector)
		{
			return $"It seems to have {SyndicateOpConsole.Instance.TcReserve} telecrystals in reserve";
		}
	}
}
