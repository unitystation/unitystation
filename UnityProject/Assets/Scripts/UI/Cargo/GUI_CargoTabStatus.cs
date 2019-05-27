using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabStatus : GUI_CargoTab
{
	[SerializeField]
	private NetLabel creditsText = null;
	[SerializeField]
	private NetLabel shuttleButtonText = null;
	[SerializeField]
	private NetLabel messageText = null;
	private bool inited = false;

	public override void Init()
	{
		if (inited || !gameObject.activeInHierarchy)
		{
			return;
		}
		CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateTab);
		CargoManager.Instance.OnShuttleUpdate.AddListener(UpdateTab);
		CargoManager.Instance.OnTimerUpdate.AddListener(UpdateTab);
	}

	public override void OpenTab()
	{
		if (!inited)
		{
			Init();
			inited = true;
		}
		UpdateTab();
	}

	private void UpdateTab()
	{
		CargoManager cm = CargoManager.Instance;

		if (cm.ShuttleStatus == CargoShuttleStatus.OnRouteCentcom ||
			cm.ShuttleStatus == CargoShuttleStatus.OnRouteStation)
		{
			if (cm.CurrentFlyTime > 0)
			{
				string min = Mathf.FloorToInt(cm.CurrentFlyTime / 60).ToString();
				string sec = (cm.CurrentFlyTime % 60).ToString();
				sec = sec.Length >= 10 ? sec : "0" + sec;

				shuttleButtonText.SetValue = min + ":" + sec;
			}
			else
			{
				shuttleButtonText.SetValue = "ARRIVING";
			}
		}
		else
		{
			shuttleButtonText.SetValue = "SEND";
		}

		messageText.SetValue = cm.CentcomMessage;
		creditsText.SetValue = cm.Credits.ToString();
	}
}
