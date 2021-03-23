using UnityEngine;
using Items.PDA;

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
			if (SyndicateOpConsole.Instance.TcReserve > 0) WithdrawTeleCrystals(interaction);
		}

		public void WithdrawTeleCrystals(HandApply interaction)
		{
			if (interaction.UsedObject == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"It seems to have {SyndicateOpConsole.Instance.TcReserve} telecrystals in reserve");
				return;
			}
			PDALogic pdaComp = interaction.UsedObject.GetComponent<PDALogic>();
			if (pdaComp != null && pdaComp.IsUplinkLocked == false)
			{
				int tc = Mathf.FloorToInt(SyndicateOpConsole.Instance.Operatives.Count / SyndicateOpConsole.Instance.TcIncrement);
				pdaComp.UplinkTC += tc;
				SyndicateOpConsole.Instance.TcReserve -= tc;
			}
		}
	
		public string Examine(Vector3 vector)
		{
			return $"It seems to have {SyndicateOpConsole.Instance.TcReserve} telecrystals in reserve";
		}
	}
}