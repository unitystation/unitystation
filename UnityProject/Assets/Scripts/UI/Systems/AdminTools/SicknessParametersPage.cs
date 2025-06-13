using AdminCommands;
using InGameEvents;
using System;
using System.Collections.Generic;
using HealthV2.Sickness;
using Newtonsoft.Json;
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

		[SerializeField]
		private InputField DiseaseStrengthInput = null;

		private int index;
		private bool fakeEvent;
		private bool announceEvent;
		private InGameEventType eventType;

		public void Awake()
		{
			sicknessDropdown.ClearOptions();

			List<Dropdown.OptionData> optionDatas = new List<Dropdown.OptionData>();

			foreach (CureManager.CureableSickness sicknesss in CureManager.Instance.CureableSicknesses)
			{
				optionDatas.Add(new Dropdown.OptionData(sicknesss.Sickness.Name));
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
			if (Int32.TryParse(NumberOfPlayerInput.textComponent.text, out var numberResult) == false) return;
			if (Int32.TryParse(DiseaseStrengthInput.textComponent.text, out var strengthResult) == false) return;

			SicknessEventParameters eventParameters = new SicknessEventParameters();

			eventParameters.PlayerToInfect = numberResult;
			eventParameters.Strength = strengthResult;

			eventParameters.SicknessIndex = sicknessDropdown.value;

			AdminCommandsManager.Instance.CmdTriggerGameEvent(
					index, fakeEvent, announceEvent, eventType, JsonConvert.SerializeObject(eventParameters));

			// We hide the panel
			gameObject.SetActive(false);
		}
	}
}
