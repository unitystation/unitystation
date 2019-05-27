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
	private bool inited = false;

	public override void Init()
	{
		if (inited || !gameObject.activeInHierarchy)
		{
			return;
		}
		CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateTab);
		CargoManager.Instance.OnShuttleUpdate.AddListener(UpdateTab);
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
		creditsText.SetValue = CargoManager.Instance.Credits.ToString();
		shuttleStatusText.SetValue = CargoManager.Instance.ShuttleStatus.ToString();
	}
}
