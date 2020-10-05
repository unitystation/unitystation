using AdminCommands;
using DatabaseAPI;
using Health.Sickness;
using InGameEvents;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class SicknessParametersPage: AdminPage
	{
		[SerializeField]
		private Dropdown sicknessDropdown = null;

		[SerializeField]
		private InputField NumberOfPlayerInput = null;

		private int index;
		private bool fakeEvent;
		private bool announceEvent;
		private InGameEventType eventType;

		public void Awake()
		{
			sicknessDropdown.ClearOptions();

			List<Dropdown.OptionData> optionDatas = new List<Dropdown.OptionData>();

			foreach (Sickness sickness in SicknessManager.Instance.Sicknesses)
			{
				optionDatas.Add(new Dropdown.OptionData(sickness.SicknessName));
			}

			sicknessDropdown.AddOptions(optionDatas);
		}

		public void SetBasicEventParameters(int index, bool isFake, bool announce, InGameEventType eventType)
		{
			this.index = index;
			fakeEvent = isFake;
			announceEvent = announce;
			this.eventType = eventType;
		}

		public void StartInfection()
		{
			SicknessEventParameters eventParameters = new SicknessEventParameters();
			eventParameters.PlayerToInfect = Convert.ToInt32(NumberOfPlayerInput.textComponent.text);
			eventParameters.SicknessIndex = sicknessDropdown.value;

			ServerCommandVersionFourMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, index, fakeEvent, announceEvent, eventType, "CmdTriggerGameEvent", eventParameters);

			// We hide the panel
			gameObject.SetActive(false);
		}
	}
}
