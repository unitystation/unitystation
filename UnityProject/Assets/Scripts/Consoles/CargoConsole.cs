using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class CargoConsole : NetworkBehaviour,ICheckedInteractable<HandApply>
{
	private bool correctID;
	public bool CorrectID => correctID;

	private GUI_Cargo associatedTab;

	[SerializeField]
	private List<JobType> allowedTypes = new List<JobType>();

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;

		//interaction only works if using an ID card on console
		if (!Validations.HasComponent<IDCard>(interaction.HandObject))
			return false;

		return true;
	}

	/// <summary>
	/// Resets the ID to false
	/// </summary>
	[Server]
	public void ResetID()
	{
		correctID = false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		CheckID(interaction.HandSlot.Item.GetComponent<IDCard>().JobType);
	}

	private void CheckID(JobType usedID)
	{
		foreach (var aJob in allowedTypes.Where(aJob => usedID == aJob))
		{
			correctID = true;
			associatedTab.UpdateId();
			break;
		}
	}


	public void NetTabRef(GameObject netTab)
	{
		associatedTab = netTab.GetComponent<GUI_Cargo>();
	}


}
