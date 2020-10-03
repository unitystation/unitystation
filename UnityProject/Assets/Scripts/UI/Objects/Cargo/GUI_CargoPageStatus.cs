using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoPageStatus : GUI_CargoPage
{
	[SerializeField]
	private NetLabel creditsText = null;
	[SerializeField]
	private NetLabel shuttleButtonText = null;
	[SerializeField]
	private NetLabel messageText = null;
	[SerializeField]
	private NetColorChanger statusCargoImage = null;
	[SerializeField]
	private NetColorChanger statusTransitImage = null;
	[SerializeField]
	private NetColorChanger statusCentcomImage = null;
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

		if (cm.ShuttleStatus == ShuttleStatus.OnRouteCentcom ||
			cm.ShuttleStatus == ShuttleStatus.OnRouteStation)
		{
			if (cm.CurrentFlyTime > 0)
			{
				string min = Mathf.FloorToInt(cm.CurrentFlyTime / 60).ToString();
				string sec = (cm.CurrentFlyTime % 60).ToString();
				sec = sec.Length >= 10 ? sec : "0" + sec;

				shuttleButtonText.SetValueServer(min + ":" + sec);
			}
			else
			{
				shuttleButtonText.SetValueServer("ARRIVING");
			}
			SetShuttleStatus(statusTransitImage);
		}
		else
		{
			if (cm.ShuttleStatus == ShuttleStatus.DockedStation)
			{
				SetShuttleStatus(statusCargoImage);
			}
			else
			{
				SetShuttleStatus(statusCentcomImage);
			}
			shuttleButtonText.SetValueServer("SEND");
		}

		messageText.SetValueServer(cm.CentcomMessage);
		creditsText.SetValueServer(cm.Credits.ToString());
	}

	//Current shuttle status is displayed like a switch - only one is active
	private void SetShuttleStatus(NetColorChanger objToSwitch)
	{
		statusCargoImage.SetValueServer(Color.black);
		statusTransitImage.SetValueServer(Color.black);
		statusCentcomImage.SetValueServer(Color.black);

		objToSwitch.SetValueServer(Color.white);
	}
}
