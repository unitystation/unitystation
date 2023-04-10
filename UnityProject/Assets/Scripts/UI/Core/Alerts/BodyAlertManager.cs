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
			var oldList = JsonConvert.DeserializeObject<List<int>>(OldData);
			var newList = JsonConvert.DeserializeObject<List<int>>(NewData);

			// Unregister alerts that were present in the old list but not in the new list
			foreach (var oldAlert in oldList)
			{
				if (!newList.Contains(oldAlert))
				{
					ClientAlertManager.Instance.UnRegisterAlert(AlertSOs.Instance.AllAlertSOs[oldAlert]);
				}
			}

			// Register alerts that are present in the new list but not in the old list
			foreach (var newAlert in newList)
			{
				if (!oldList.Contains(newAlert))
				{
					ClientAlertManager.Instance.RegisterAlert(AlertSOs.Instance.AllAlertSOs[newAlert]);
				}
			}
		}
	}


	public void ClientOnPlayerLeaveBody()
	{
		ClientAlertManager.Instance.UnRegisterAlertALL();
	}

	public void ClientOnPlayerTransferProcess()
	{
		var List = JsonConvert.DeserializeObject<List<int>>(PresentAlertsJson);
		ClientAlertManager.Instance.UnRegisterAlertALL(); //TODO Suboptimal but easy

		foreach (var NewAlert in List)
		{
			ClientAlertManager.Instance.RegisterAlert(AlertSOs.Instance.AllAlertSOs[NewAlert]);
		}
	}
}
