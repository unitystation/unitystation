using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using AdminTools;
using AdminCommands;
using Managers;
using Messages.Client.Admin;
using Strings;
using Systems.Cargo;
using TMPro;


public class RoundManagerPage : AdminPage
{
	[SerializeField]
	private Dropdown nextMapDropDown = null;

	[SerializeField]
	private Dropdown nextAwaySiteDropDown = null;

	[SerializeField]
	private Toggle lavaLandToggle = null;

	[SerializeField]
	private Toggle cargoToggle = null;

	[SerializeField]
	private Toggle randomBountyToggle = null;

	[SerializeField]
	private Dropdown alertLevelDropDown = null;

	private List<string> alertLevelEnumCache = new List<string>();

	[SerializeField] private GameObject bountyManagerPanel;

	private void Start()
	{
		alertLevelEnumCache = Enum.GetNames(typeof(CentComm.AlertLevel)).ToList();
	}

	public void ChangeMap()
	{
		AdminCommandsManager.Instance.CmdChangeNextMap(nextMapDropDown.options[nextMapDropDown.value].text);
	}

	public void ChangeAwaySite()
	{
		AdminCommandsManager.Instance.CmdChangeAwaySite(nextAwaySiteDropDown.options[nextAwaySiteDropDown.value].text);
	}

	public void StartRoundButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to START the round?", StartRound, gameObject);
	}

	private void StartRound()
	{
		AdminCommandsManager.Instance.CmdStartRound();
	}

	public void EndRoundButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to END the round?", EndRound, gameObject);
	}

	private void EndRound()
	{
		AdminCommandsManager.Instance.CmdEndRound();
		adminTools.ClosePanel(); // We close the panel immediately after, so it is not open when new round starts.
	}

	public void ToggleLavaLand()
	{
		currentData.allowLavaLand = lavaLandToggle.isOn;
		RequestLavaLandToggle.Send(lavaLandToggle.isOn);
	}

	public void ChangeAlertLevel()
	{
		if (!CentComm.AlertLevel.TryParse(alertLevelDropDown.options[alertLevelDropDown.value].text, out CentComm.AlertLevel alertLevel)) return;

		AdminCommandsManager.Instance.CmdChangeAlertLevel(alertLevel);
	}

	public override void OnPageRefresh(AdminPageRefreshData adminPageData)
	{
		base.OnPageRefresh(adminPageData);
		lavaLandToggle.isOn = adminPageData.allowLavaLand;
		GenerateDropDownOptionsMap(adminPageData);
		GenerateDropDownOptionsAwaySite(adminPageData);
		GenerateDropDownOptionsAlertLevels(adminPageData);
	}

	private void GenerateDropDownOptionsMap(AdminPageRefreshData adminPageData)
	{
		//generate the drop down options:
		var optionData = new List<Dropdown.OptionData>();

		//Add random entry:
		optionData.Add(new Dropdown.OptionData
		{
			text = "Random"
		});

		foreach (var mapName in SubSceneManager.Instance.MainStationList.MainStations)
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = mapName.ToString()
			});
		}

		nextMapDropDown.options = optionData;

		for (var i = 0; i < optionData.Count; i++)
		{
			if (optionData[i].text == adminPageData.nextMap)
			{
				nextMapDropDown.value = i;
				return;
			}
		}
	}

	private void GenerateDropDownOptionsAwaySite(AdminPageRefreshData adminPageData)
	{
		//generate the drop down options:
		var optionData = new List<Dropdown.OptionData>();

		//Add random entry:
		optionData.Add(new Dropdown.OptionData
		{
			text = "Random"
		});

		foreach (var awaySiteName in SubSceneManager.Instance.awayWorldList.AwayWorlds)
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = awaySiteName.ToString()
			});
		}

		nextAwaySiteDropDown.options = optionData;

		for (var i = 0; i < optionData.Count; i++)
		{
			if (optionData[i].text == adminPageData.nextAwaySite)
			{
				nextAwaySiteDropDown.value = i;
				return;
			}
		}
	}

	public void ShowBountyManagerPanel()
	{
		bountyManagerPanel.SetActive(true);
	}

	private void GenerateDropDownOptionsAlertLevels(AdminPageRefreshData adminPageData)
	{
		//generate the drop down options:
		var optionData = new List<Dropdown.OptionData>();

		foreach (var alert in alertLevelEnumCache)
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = alert
			});
		}

		alertLevelDropDown.options = optionData;

		for (var i = 0; i < optionData.Count; i++)
		{
			if (optionData[i].text == adminPageData.alertLevel)
			{
				alertLevelDropDown.value = i;
				return;
			}
		}
	}

	public void ToggleCargo()
	{
		AdminCommandsManager.Instance.CmdChangeCargoConnectionStatus(cargoToggle.isOn);
	}

	public void ToggleRandomBounties()
	{
		AdminCommandsManager.Instance.CmdToggleCargoRandomBounty(randomBountyToggle.isOn);
	}
}
