using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabStatus : GUI_CargoTab
{
	[SerializeField]
	private Text creditsText = null;
	[SerializeField]
	private Text shuttleStatusText = null;

	public override void OnTabOpened()
	{
		CargoManager.Instance.OnCreditsUpdate += UpdateTab;
		CargoManager.Instance.OnShuttleUpdate += UpdateTab;
		UpdateTab();
	}

	public override void OnTabClosed()
	{
		CargoManager.Instance.OnCreditsUpdate -= UpdateTab;
		CargoManager.Instance.OnShuttleUpdate -= UpdateTab;
	}

	public override void UpdateTab()
	{
		creditsText.text = CargoManager.Instance.Credits.ToString();
		shuttleStatusText.text = CargoManager.Instance.ShuttleStatus.ToString();
	}
}
