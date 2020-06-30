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

	public void ChangeMap()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdChangeNextMap(ServerData.UserID, PlayerList.Instance.AdminToken, nextMapDropDown.options[nextMapDropDown.value].text);
	}

	public override void OnPageRefresh(AdminPageRefreshData adminPageData)
	{
		base.OnPageRefresh(adminPageData);
		GenerateDropDownOptions(adminPageData);
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
			optionData[i].text = adminPageData.nextMap;
			nextMapDropDown.value = i;
			return;
		}
	}
}
