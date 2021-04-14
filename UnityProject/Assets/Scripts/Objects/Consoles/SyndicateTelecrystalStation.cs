using UnityEngine;
using Items.PDA;
using System;

namespace SyndicateOps
{
	class SyndicateTelecrystalStation : MonoBehaviour,  ICheckedInteractable<HandApply>, IExaminable
	{
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
			if (interaction.UsedObject == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"It seems to have {SyndicateOpConsole.Instance.TcReserve} telecrystals in reserve");
				return;
			}
			PDALogic pdaComp = interaction.UsedObject.GetComponent<PDALogic>();
			if (pdaComp != null)
			{
				if (pdaComp.IsUplinkLocked == false)
				{

					int tc = Mathf.FloorToInt(SyndicateOpConsole.Instance.Operatives.Count / SyndicateOpConsole.Instance.TcIncrement);
					//this is to prevent tc being unobtainable when the value above is bigger then the amount of tc left within the reserves
					tc = Math.Min(tc, SyndicateOpConsole.Instance.TcReserve);
					pdaComp.UplinkTC += tc;
					SyndicateOpConsole.Instance.TcReserve -= tc;
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
