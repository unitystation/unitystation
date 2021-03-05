using UnityEngine;
using Managers;
using Antagonists;
using Mirror;
using System.Collections.Generic;
using Items.PDA;

class SyndicateTelecrystalStation : MonoBehaviour,  ICheckedInteractable<HandApply>, IExaminable
{
	private int tcPerOperative = 0;
	public int TcReserve => SyndicateOpConsole.Instance.TcReserve;

	private List<SpawnedAntag> operatives;

	private void Awake()
	{
	   SyndicateOpConsole.Instance.OnTimerExpired += RecieveTeleCrystals;
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side)) return true;
		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (TcReserve > 0) WithdrawTeleCrystals(interaction);
	}

	public void RecieveTeleCrystals()
	{
		var antagplayers = AntagManager.Instance.CurrentAntags;

		foreach (var antag in antagplayers )
		{
			if (antag.Antagonist.AntagJobType == JobType.SYNDICATE)
			{
				operatives.Add(antag);
			}
		}

		tcPerOperative = TcReserve / operatives.Count;
	}

	public void WithdrawTeleCrystals(HandApply interaction)
	{
		PDALogic pdaComp = interaction.UsedObject.GetComponent<PDALogic>();
		if (pdaComp != null && pdaComp.IsUplinkLocked == false)
		{
			foreach (var op in operatives)
			{
				if (op.Owner == interaction.PerformerPlayerScript.mind)
				{
					operatives.Remove(op);
					pdaComp.UplinkTC += tcPerOperative;
					SyndicateOpConsole.Instance.TcReserve -= tcPerOperative;
				}
			}
		}
	}

	public string Examine(Vector3 vector)
	{
		var examine = $"It seems to have {TcReserve} telecrystals in reserve, {tcPerOperative} per Operative";
		return examine;
	}
}