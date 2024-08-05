using System.Threading;
using AdminTools;
using Core.Admin.Logs;
using Messages.Client.Admin;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.AdminTools.AdminLogs
{
	public class AdminLogEntryUI : MonoBehaviour
	{
		[SerializeField] private Button gibButton = null;
		[SerializeField] private Button teleportButton = null;
		[SerializeField] private TMP_Text logInfo = null;
		[SerializeField] private TMP_Text logTime = null;
		private LogEntry entry = null;
		public string LogText => entry?.Log;

		public void Setup(LogEntry newEntry)
		{
			entry = newEntry;
			logInfo.text = entry.Log;
			logTime.text = newEntry.LogTime.ToLocalTime().ToLongTimeString();
			gameObject.SetActive(true);
			//gibButton.onClick.AddListener(GibRequest);
			//teleportButton.onClick.AddListener(TeleportTo);
		}

		private void GibRequest()
		{
			AdminPlayerAlertActions.Send(
				PlayerAlertActions.Gibbed, entry.LogTime.ToLongTimeString(), AdminLogsManager.GetPerpIdFromString(entry.Perpetrator), PlayerList.Instance.AdminToken);
		}

		public void TeleportTo()
		{
			if (PlayerManager.LocalPlayerScript == null) return;

			var spawned =
				CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

			var target = spawned[AdminLogsManager.GetPerpIdFromString(entry.Perpetrator)];
			if (target == null) return;

			if (PlayerManager.LocalPlayerScript.IsGhost == false)
			{
				teleportButton.interactable = false;
				PlayerManager.LocalMindScript.CmdAGhost();
			}
			PlayerManager.LocalPlayerObject.GetComponent<GhostMove>()
				.CMDSetServerPosition(target.transform.position);
		}
	}
}