using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatabaseAPI;
using UnityEngine.UI;
using AdminTools;

public class RoundManagerPage : AdminPage
{
	[SerializeField]
	private Dropdown nextMapDropDown = null;

	[SerializeField]
	private Dropdown nextAwaySiteDropDown = null;

	[SerializeField]
	private Toggle lavaLandToggle = null;

	public void ChangeMap()
	{
		if(!AdminCommandsManager.Instance.netIdentity.hasAuthority) return;

		AdminCommandsManager.Instance.CmdChangeNextMap(ServerData.UserID, PlayerList.Instance.AdminToken, nextMapDropDown.options[nextMapDropDown.value].text);
	}

	public void ChangeAwaySite()
	{
		if(!AdminCommandsManager.Instance.netIdentity.hasAuthority) return;

		AdminCommandsManager.Instance.CmdChangeAwaySite(ServerData.UserID, PlayerList.Instance.AdminToken, nextAwaySiteDropDown.options[nextAwaySiteDropDown.value].text);
	}

	public void StartRoundButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to START the round?", StartRound);
	}

	private void StartRound()
	{
		if(!AdminCommandsManager.Instance.netIdentity.hasAuthority) return;

		AdminCommandsManager.Instance.CmdStartRound(ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void EndRoundButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to END the round?", EndRound);
	}

	private void EndRound()
	{
		if(!AdminCommandsManager.Instance.netIdentity.hasAuthority) return;

		AdminCommandsManager.Instance.CmdEndRound(ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void CallShuttleButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to CALL the emergency shuttle?", CallShuttle);
	}

	private void CallShuttle()
	{
		if(!AdminCommandsManager.Instance.netIdentity.hasAuthority) return;

		AdminCommandsManager.Instance.CmdCallShuttle(ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void RecallShuttleButtonClick()
	{
		adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to RECALL the emergency shuttle?", RecallShuttle);
	}

	private void RecallShuttle()
	{
		if(!AdminCommandsManager.Instance.netIdentity.hasAuthority) return;

		AdminCommandsManager.Instance.CmdRecallShuttle(ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void ToggleLavaLand()
	{
		currentData.allowLavaLand = lavaLandToggle.isOn;
		RequestLavaLandToggle.Send(ServerData.UserID, PlayerList.Instance.AdminToken, lavaLandToggle.isOn);
	}

	public override void OnPageRefresh(AdminPageRefreshData adminPageData)
	{
		base.OnPageRefresh(adminPageData);
		lavaLandToggle.isOn = adminPageData.allowLavaLand;
		GenerateDropDownOptions(adminPageData);
		GenerateDropDownOptionsAwaySite(adminPageData);
	}

	private void GenerateDropDownOptions(AdminPageRefreshData adminPageData)
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
}
