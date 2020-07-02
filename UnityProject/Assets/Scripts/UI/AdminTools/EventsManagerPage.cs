using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatabaseAPI;
using UnityEngine.UI;
using InGameEvents;
using AdminTools;

public class EventsManagerPage : AdminPage
{
	[SerializeField]
	private Dropdown nextDropDown = null;

	[SerializeField]
	private Button triggerEvent = null;

	[SerializeField]
	private Toggle isFakeToggle = null;

	[SerializeField]
	private Toggle announceToggle = null;

	[SerializeField]
	private Toggle randomEventToggle = null;

	public void TriggerEvent()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTriggerGameEvent(ServerData.UserID, PlayerList.Instance.AdminToken, nextDropDown.value, isFakeToggle.isOn, announceToggle.isOn);
	}

	public void ToggleRandomEvents()
	{
		currentData.randomEventsAllowed = randomEventToggle.isOn;
		RequestRandomEventAllowedChange.Send(ServerData.UserID, PlayerList.Instance.AdminToken, randomEventToggle.isOn);
	}

	public override void OnPageRefresh(AdminPageRefreshData adminPageData)
	{
		base.OnPageRefresh(adminPageData);
		randomEventToggle.isOn = adminPageData.randomEventsAllowed;
	}

	public void GenerateDropDownOptions()
	{
		//generate the drop down options:
		var optionData = new List<Dropdown.OptionData>();

		//Add random entry:
		optionData.Add(new Dropdown.OptionData
		{
			text = "Random"
		});

		foreach (var eventInList in InGameEventsManager.Instance.ListOfFunEventScripts)
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = eventInList.EventName
			});
		}

		nextDropDown.options = optionData;
	}
}
