using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using Newtonsoft.Json;

namespace UI.Core.Alerts
{
	public class BodyAlertManager : NetworkBehaviour, IClientPlayerLeaveBody, IClientPlayerTransferProcess
	{
		//This is a json list of integers that correspond to record indexes in AlertSOs.Instance.AllAlertSOs
		//this is code from our lead developer btw
		[SyncVar(hook = nameof(SyncActiTheons))] public string PresentAlertsJson = "[]";

		public void Awake()
		{
			SyncActiTheons( "[]",  "[]");
		}

		public void RegisterAlert(AlertSO AlertSO, bool noDuplicates = true)
		{
			var List = JsonConvert.DeserializeObject<List<int>>(PresentAlertsJson); //TODO Make this more optimal sometime
			var alertToAdd = AlertSO.GetIndexed();
			if (noDuplicates && List.Contains(alertToAdd) == false)
			{
				List.Add(AlertSO.GetIndexed());
			}
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

		public List<AlertSO> GetPresentAlerts()
		{
			var presentAlerts = new List<AlertSO>();
			var list = JsonConvert.DeserializeObject<List<int>>(PresentAlertsJson);
			foreach (var newAlert in list)
			{
				try
				{
					if (AlertSOs.Instance.AllAlertSOs[newAlert] != null)
					{
						presentAlerts.Add(AlertSOs.Instance.AllAlertSOs[newAlert]);
					}
				}
				catch (Exception e)
				{
					Loggy.Error($"(Max): This fucked up shit is broken, who would have thought?: {e}");
				}
			}

			return presentAlerts;
		}
	}
}
