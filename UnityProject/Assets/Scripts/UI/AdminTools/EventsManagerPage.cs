using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatabaseAPI;
using UnityEngine.UI;
using InGameEvents;
using AdminTools;
using AdminCommands;
using Assets.Scripts.UI.AdminTools;
using System.Linq;

public class EventsManagerPage : AdminPage
{
	[SerializeField]
	private Dropdown nextDropDown = null;

	[SerializeField]
	private Dropdown eventTypeDropDown = null;

	[SerializeField]
	private Button triggerEvent = null; // TODO: this is unused and is creating a compiler warning.

	[SerializeField]
	private Toggle isFakeToggle = null;

	[SerializeField]
	private Toggle announceToggle = null;

	[SerializeField]
	private Toggle randomEventToggle = null;

	/// <summary>
	/// The pages to show for specifying extra parameters for an event type.
	/// </summary>
	private EventParameterPages eventsParametersPages = null;

	private bool generated = false;

	public void Awake()
	{
		eventsParametersPages = GetComponent<EventParameterPages>();
	}

	public void TriggerEvent()
	{
		if (!InGameEventType.TryParse(eventTypeDropDown.options[eventTypeDropDown.value].text, out InGameEventType eventType)) return;

		var index = nextDropDown.value;

		if (eventType == InGameEventType.Random)
		{
			index = 0;
		}

		if (index != 0) // Index 0 (Random Event) will never have a parameter page
		{
			// Instead of triggering the event right away, if we have an extra parameter page, we show it
			List<EventScriptBase> listEvents = InGameEventsManager.Instance.GetListFromEnum(eventType);
			if (listEvents[index - 1].parametersPageType != ParametersPageType.None)
			{
				GameObject parameterPage = eventsParametersPages.eventParameterPages.FirstOrDefault(p => p.ParametersPageType == listEvents[index - 1].parametersPageType).ParameterPage;

				if (parameterPage)
				{
					parameterPage.SetActive(true);
					parameterPage.GetComponent<SicknessParametersPage>().SetBasicEventParameters(index, isFakeToggle.isOn, announceToggle.isOn, InGameEventType.Fun);
					return;
				}
			}
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
