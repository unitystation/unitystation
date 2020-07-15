using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatabaseAPI;
using UnityEngine.UI;
using InGameEvents;
using AdminTools;
using AdminCommands;

public class EventsManagerPage : AdminPage
{
	[SerializeField]
	private Dropdown nextDropDown = null;

	[SerializeField]
	private Dropdown eventTypeDropDown = null;

	[SerializeField]
	private Button triggerEvent = null;

	[SerializeField]
	private Toggle isFakeToggle = null;

	[SerializeField]
	private Toggle announceToggle = null;

	[SerializeField]
	private Toggle randomEventToggle = null;

	private bool generated = false;

	public void TriggerEvent()
	{
		if (!InGameEventType.TryParse(eventTypeDropDown.options[eventTypeDropDown.value].text, out InGameEventType eventType)) return;

		var index = nextDropDown.value;

		if (eventType == InGameEventType.Random)
		{
			index = 0;
		}

		ServerCommandVersionFourMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, index, isFakeToggle.isOn, announceToggle.isOn, eventType, "CmdTriggerGameEvent");
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
		GenerateDropDownOptionsEventType();
	}

	private void GenerateDropDownOptionsEventType()
	{
		if (!generated)
		{
			generated = true;
			EventTypeOptions();
		}

		if (!InGameEventType.TryParse(eventTypeDropDown.options[eventTypeDropDown.value].text, out InGameEventType eventType)) return;

		GenerateDropDownOptionsEventList(eventType);

	}

	private void EventTypeOptions()
	{
		//generate the drop down options:
		var optionData = new List<Dropdown.OptionData>();

		foreach (var eventTypeInList in InGameEventsManager.Instance.EnumListCache )
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = eventTypeInList
			});
		}

		eventTypeDropDown.options = optionData;
	}

	private void GenerateDropDownOptionsEventList(InGameEventType eventType)
	{
		//generate the drop down options:
		var optionData = new List<Dropdown.OptionData>();

		//Add random entry:
		optionData.Add(new Dropdown.OptionData
		{
			text = "Random"
		});

		var list = InGameEventsManager.Instance.GetListFromEnum(eventType);

		if (list == null)
		{
			nextDropDown.options = optionData;
			return;
		}

		foreach (var eventInList in list)
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = eventInList.EventName
			});
		}

		nextDropDown.options = optionData;
	}
}
