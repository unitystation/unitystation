using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatabaseAPI;
using UnityEngine.UI;
using AdminTools;
using AdminCommands;
using Managers;
using Messages.Client.Admin;

public class RoundManagerPage : AdminPage
{
	[SerializeField]
	private Dropdown nextMapDropDown = null;

	[SerializeField]
	private Dropdown nextAwaySiteDropDown = null;

	[SerializeField]
	private Toggle lavaLandToggle = null;

	[SerializeField]
	private Dropdown alertLevelDropDown = null;

	private List<string> alertLevelEnumCache = new List<string>();

	private void Start()
	{
		alertLevelEnumCache = Enum.GetNames(typeof(CentComm.AlertLevel)).ToList();
	}

	public void ChangeMap()
	{
		AdminCommandsManager.Instance.CmdChangeNextMap(ServerData.UserID, PlayerList.Instance.AdminToken, nextMapDropDown.options[nextMapDropDown.value].text);
	}

	public void ChangeAwaySite()
	{
		AdminCommandsManager.Instance.CmdChangeAwaySite(ServerData.UserID, PlayerList.Instance.AdminToken, nextAwaySiteDropDown.options[nextAwaySiteDropDown.value].text);
	}

	public void StartRoundButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to START the round?", StartRound, gameObject);
	}

	private void StartRound()
	{
		AdminCommandsManager.Instance.CmdStartRound(ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void EndRoundButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to END the round?", EndRound, gameObject);
	}

	private void EndRound()
	{
		AdminCommandsManager.Instance.CmdEndRound(ServerData.UserID, PlayerList.Instance.AdminToken);
		adminTools.ClosePanel(); // We close the panel immediately after, so it is not open when new round starts.
	}

	public void ToggleLavaLand()
	{
		currentData.allowLavaLand = lavaLandToggle.isOn;
		RequestLavaLandToggle.Send(ServerData.UserID, PlayerList.Instance.AdminToken, lavaLandToggle.isOn);
	}

	public void ChangeAlertLevel()
	{
		if (!CentComm.AlertLevel.TryParse(alertLevelDropDown.options[alertLevelDropDown.value].text, out CentComm.AlertLevel alertLevel)) return;

		AdminCommandsManager.Instance.CmdChangeAlertLevel(ServerData.UserID, PlayerList.Instance.AdminToken, alertLevel);
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
		var optionData = new List<Dropdown.OptionData>
		{
			//Add random entry:
			new Dropdown.OptionData
			{
				text = "Random"
			}
		};

		foreach (var mapName in SubSceneManager.Instance.MainStationList.MainStations)
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = mapName
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
		var optionData = new List<Dropdown.OptionData>
		{
			//Add random entry:
			new Dropdown.OptionData
			{
				text = "Random"
			}
		};

		foreach (var awaySiteName in SubSceneManager.Instance.awayWorldList.AwayWorlds)
		{
			optionData.Add(new Dropdown.OptionData
			{
				text = awaySiteName
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
}
