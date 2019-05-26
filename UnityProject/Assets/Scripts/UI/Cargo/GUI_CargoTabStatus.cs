using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabStatus : GUI_CargoTab
{
	[SerializeField]
	private NetLabel creditsText = null;
	[SerializeField]
	private NetLabel shuttleStatusText = null;

	public override void Init()
	{
		CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateTab);
		CargoManager.Instance.OnShuttleUpdate.AddListener(UpdateTab);
	}

	public override void OpenTab()
	{
		UpdateTab();
	}

	private void UpdateTab()
	{
		Debug.Log("status updated");

		Debug.Log(CargoManager.Instance.Credits.ToString());
		Debug.Log(CargoManager.Instance.ShuttleStatus.ToString());

		creditsText.SetValue = CargoManager.Instance.Credits.ToString();
		shuttleStatusText.SetValue = CargoManager.Instance.ShuttleStatus.ToString();
	}
}
