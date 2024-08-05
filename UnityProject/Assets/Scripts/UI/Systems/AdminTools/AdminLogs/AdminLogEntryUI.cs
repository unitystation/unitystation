using System.Collections.Generic;
using Core.Admin.Logs;
using Messages.Client.Admin;
using Mirror;
using TMPro;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Systems.AdminTools.AdminLogs
{
	public class AdminLogEntryUI : MonoBehaviour, IHoverTooltip, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] private Button gibButton = null;
		[SerializeField] private Button teleportButton = null;
		[SerializeField] private TMP_Text logInfo = null;
		[SerializeField] private TMP_Text logTime = null;
		private LogEntry entry = null;
		public LogEntry StoredLogEntry => entry;
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

		public string HoverTip()
		{
			return $"Perpetrator: {entry.Perpetrator}\n" +
			       $"Time: {entry.LogTime.ToLongTimeString()} UTC\n" +
			       $"Category: {entry.Category}\n" +
			       $"Severity: {entry.LogImportance}\n";
		}

		public string CustomTitle()
		{
			return "Log Entry";
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return null;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			UIManager.SetHoverToolTip = gameObject;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			UIManager.SetHoverToolTip = null;
		}
	}
}