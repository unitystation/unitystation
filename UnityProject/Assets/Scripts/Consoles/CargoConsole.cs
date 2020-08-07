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
	private List<JobType> allowedTypes = null;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side) &&
		       Validations.HasComponent<IDCard>(interaction.HandObject);


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
