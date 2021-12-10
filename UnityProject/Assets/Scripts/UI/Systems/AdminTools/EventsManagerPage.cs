using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InGameEvents;
using AdminTools;
using AdminCommands;
using Assets.Scripts.UI.AdminTools;
using System.Linq;
using Messages.Client.Admin;


public class EventsManagerPage : AdminPage
{
	[SerializeField]
	private Dropdown nextDropDown = null;

	[SerializeField]
	private Dropdown eventTypeDropDown = null;

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


	private DateTime stationTimeHolder;
	private DateTime stationTimeSnapshot;

	public void TriggerEvent()
	{
		// Pull time from game manager and put it into a private holder variable which gets cleared on timeout and allows the button to be pressed again. Current WAIT time set to 5 secs
		
		stationTimeHolder = GameManager.Instance.stationTime;

		if (stationTimeHolder < (stationTimeSnapshot))
   		{
            // Tells all admins to wait X seconds, this is based on round time so if the server stutters loading an event it 
            // will take it into account effectivly stopping any sort of spam.
			Chat.AddExamineMsgToClient($"Please wait {Mathf.Round((float)stationTimeSnapshot.Subtract(stationTimeHolder).TotalSeconds)} seconds before trying to generate another event.");
            return;
    	}

		stationTimeSnapshot = stationTimeHolder.AddSeconds(5);

		if (!InGameEventType.TryParse(eventTypeDropDown.options[eventTypeDropDown.value].text,
			out InGameEventType eventType)) return;

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
				GameObject parameterPage = eventsParametersPages.eventParameterPages
					.FirstOrDefault(p => p.ParametersPageType == listEvents[index - 1].parametersPageType)
					.ParameterPage;
				if (parameterPage)
				{
					parameterPage.SetActive(true);
					parameterPage.GetComponent<SicknessParametersPage>().SetBasicEventParameters(index,
						isFakeToggle.isOn, announceToggle.isOn, InGameEventType.Fun);
					return;
				}
			}
		}

		AdminCommandsManager.Instance.CmdTriggerGameEvent(index, isFakeToggle.isOn, announceToggle.isOn, eventType, null);
	}

	public void ToggleRandomEvents()
	{
		currentData.randomEventsAllowed = randomEventToggle.isOn;
		RequestRandomEventAllowedChange.Send(randomEventToggle.isOn);
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
