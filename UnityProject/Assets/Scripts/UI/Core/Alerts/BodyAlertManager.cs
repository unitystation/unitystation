using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;


public class BodyAlertManager : NetworkBehaviour, IClientPlayerLeaveBody, IClientPlayerTransferProcess
{
	[SyncVar(hook = nameof(SyncActiTheons))] public string PresentAlertsJson = "[]";


	public void Awake()
	{
		SyncActiTheons( "[]",  "[]");
	}


	public void RegisterAlert(AlertSO AlertSO)
	{
		var List = JsonConvert.DeserializeObject<List<int>>(PresentAlertsJson); //TODO Make this more optimal sometime
		List.Add(AlertSO.GetIndexed());
		SyncActiTheons(PresentAlertsJson, PresentAlertsJson = JsonConvert.SerializeObject(List));
	}

	public void UnRegisterAlert(AlertSO AlertSO)
	{
		var List = JsonConvert.DeserializeObject<List<int>>(PresentAlertsJson); //TODO Make this more optimal sometime
		List.Remove(AlertSO.GetIndexed());
		SyncActiTheons(PresentAlertsJson, PresentAlertsJson = JsonConvert.SerializeObject(List));
	}

	public void SyncActiTheons(string OldData, string NewData)
	{
		PresentAlertsJson = NewData;
		if (isOwned && PlayerManager.LocalPlayerObject == this.gameObject)
		{
			var List = JsonConvert.DeserializeObject<List<int>>(PresentAlertsJson);
			UIManager.Instance.ClientAlertManager.UnRegisterAlertALL(); //TODO Suboptimal but easy

			foreach (var NewAlert in List)
			{
				UIManager.Instance.ClientAlertManager.RegisterAlert(AlertSOs.Instance.AllAlertSOs[NewAlert]);
			}
		}
	}


	public void ClientOnPlayerLeaveBody()
	{
		UIManager.Instance.ClientAlertManager.UnRegisterAlertALL();
	}

	public void ClientOnPlayerTransferProcess()
	{
		var List = JsonConvert.DeserializeObject<List<int>>(PresentAlertsJson);
		UIManager.Instance.ClientAlertManager.UnRegisterAlertALL(); //TODO Suboptimal but easy

		foreach (var NewAlert in List)
		{
			UIManager.Instance.ClientAlertManager.RegisterAlert(AlertSOs.Instance.AllAlertSOs[NewAlert]);
		}
	}
}
