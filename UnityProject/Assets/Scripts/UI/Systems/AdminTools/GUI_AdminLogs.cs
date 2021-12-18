using System;
using System.Collections;
using System.Collections.Generic;
using Systems.GameLogs;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LogType = Systems.GameLogs.LogType;

namespace UI.Systems.AdminTools
{
	public class GUI_AdminLogs : MonoBehaviour
	{
		[SerializeField] private GameObject logTemplate;
		[SerializeField] private GameObject logObjectList;
		[SerializeField] private TMP_Dropdown dropDown;


		private void OnEnable()
		{
			SwitchLogs();
		}

		public void SwitchLogs()
		{
			if (dropDown.captionText.text.Contains("General"))
			{
				LoadLogs(LogType.General);
				return;
			}
			if (dropDown.captionText.text.Contains("Combat"))
			{
				LoadLogs(LogType.Combat);
				return;
			}
			if (dropDown.captionText.text.Contains("Admin"))
			{
				LoadLogs(LogType.Admin);
				return;
			}
			if (dropDown.captionText.text.Contains("Antag"))
			{
				LoadLogs(LogType.Antag);
				return;
			}
			if (dropDown.captionText.text.Contains("Interaction"))
			{
				LoadLogs(LogType.Interactions);
				return;
			}
			if (dropDown.captionText.text.Contains("Explosion"))
			{
				LoadLogs(LogType.Explosion);
				return;
			}
			if (dropDown.captionText.text.Contains("Chat"))
			{
				LoadLogs(LogType.Chat);
				return;
			}
		}

		public void CloseUI()
		{
			this.SetActive(false);
		}


		private void LoadLogs(LogType type)
		{
			foreach (var logObj in logObjectList.GetComponentsInChildren<LogUI>())
			{
				Destroy(logObj.gameObject);
			}
			string[] logs = GameLogs.Instance.ReteriveAllLogsOfType(type);
			foreach (var logLine in logs)
			{
				GameObject newLogObject = Instantiate(logTemplate);
				var logscript = newLogObject.GetComponent<LogUI>();
				logscript.Text.text = logLine;
				newLogObject.transform.SetParent(logObjectList.transform, false);
				newLogObject.SetActive(true);
			}
		}
	}
}
