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
		CheckID(interaction.HandSlot.Item.GetComponent<IDCard>().JobType,interaction.Performer);
	}

	[Server]
	private void CheckID(JobType usedID, GameObject playeref)
	{
		// Null checks people, always need them in the weirdest of places
		if (associatedTab == null) return;
		foreach (var aJob in allowedTypes.Where(aJob => usedID == aJob))
		{
			correctID = true;
			associatedTab.UpdateId();
			break;
		}
		// Not "optimized" for readability
		if (correctID)
		{
			Chat.AddActionMsgToChat(playeref, "You swipe your ID through the supply console's ID slot, t" +
			                                  "he console accepts your ID",
				"swiped their ID through the supply console's ID slot");
		}
		else
		{
			Chat.AddActionMsgToChat(playeref, "You swipe your ID through the supply console's ID slot, " +
			                                  "the console denies your ID",
				$"{playeref.ExpensiveName()} swiped their ID through the supply console's ID slot");

		}

	}


	public void NetTabRef(GameObject netTab)
	{
		associatedTab = netTab.GetComponent<GUI_Cargo>();
	}


}
