using System;
using System.Collections;
using System.Linq;
using Logs;
using UnityEngine;
using UI.Core.NetUI;
using Managers;
using Objects.Wallmounts;
using Objects.Command;
using Strings;
using Systems.Clearance;

namespace UI.Objects.Command
{
	public class GUI_Comms : NetTab
	{
		[SerializeField]
		private NetPageSwitcher mainSwitcher = null;
		[SerializeField]
		private NetPage menuPage = null;

		[SerializeField]
		private NetPageSwitcher captainOnlySwitcher = null;
		[SerializeField]
		private NetPage noCaptainAccessPage = null;
		[SerializeField]
		private NetPage captainAccessPage = null;

		[SerializeField]
		private NetText_label idLabel = null;
		[SerializeField]
		private NetText_label shuttleStatusLabel = null;
		[SerializeField]
		private NetText_label shuttleTimerLabel = null;
		[SerializeField]
		private NetText_label shuttleCallResultLabel = null;
		[SerializeField]
		private NetText_label shuttleCallButtonLabel = null;
		[SerializeField]
		private NetSpriteImage statusImage = null;
		[SerializeField]
		private NetText_label CurrentAlertLevelLabel = null;
		[SerializeField]
		private NetText_label NewAlertLevelLabel = null;
		[SerializeField]
		private NetText_label AlertErrorLabel = null;

		private CommsConsole console;
		private EscapeShuttle shuttle;
		private Coroutine callResultHandle;

		private CentComm.AlertLevel LocalAlertLevel = CentComm.AlertLevel.Green;

		protected override void InitServer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StartCoroutine(WaitForProvider());
			}
		}

		private IEnumerator WaitForProvider()
		{
			string FormatTime(int timerSeconds)
			{
				if (shuttle.Status == EscapeShuttleStatus.DockedCentcom ||
					shuttle.Status == EscapeShuttleStatus.DockedStation)
				{
					return string.Empty;
				}

				return "ETA: " + TimeSpan.FromSeconds(timerSeconds).ToString("mm\\:ss");
			}

			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			console = Provider.GetComponentInChildren<CommsConsole>();

			//starting up, setting appropriate labels
			ProcessIdChange(console.IdCard);
			console.OnServerIDCardChanged.AddListener(ProcessIdChange);
			shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			shuttleStatusLabel.MasterSetValue(shuttle.Status.ToString());
			statusImage.SetSprite((int)shuttle.Status);
			shuttle.OnShuttleUpdate.AddListener(status =>
			{
				statusImage.SetSprite((int)shuttle.Status);
				shuttleStatusLabel.MasterSetValue(status.ToString());
			});

			shuttleTimerLabel.MasterSetValue(FormatTime(shuttle.CurrentTimerSeconds));
			shuttle.OnTimerUpdate.AddListener(timerSeconds =>
		   {
			   shuttleTimerLabel.MasterSetValue(FormatTime(timerSeconds));
		   });

			RefreshCallButtonText();

			Loggy.Log(nameof(WaitForProvider), Category.Shuttles);
		}

		private void ProcessIdChange(IDCard newId = null)
		{
			UpdateIdTexts();

			if (newId != null || IsAIInteracting())
			{
				LogIn();
			}
			else
			{
				LogOut();
			}
		}

		public void CallOrRecallShuttle(string text)
		{
			text = Chat.StripTags(text);

			Loggy.Log(nameof(CallOrRecallShuttle), Category.Shuttles);

			bool isRecall = shuttle.Status == EscapeShuttleStatus.OnRouteStation;



			string callResult;
			bool ok;

			if (isRecall)
			{
				ok = shuttle.RecallShuttle(out callResult);
				if (ok)
				{
					CentComm.MakeShuttleRecallAnnouncement(text);
					RefreshCallButtonText();
				}
			}
			else
			{
				if (text.Trim().Length < 10)
				{
					callResult = "You must provide a reason when calling shuttle!";
					ok = false;
				}
				else
				{
					ok = shuttle.CallShuttle(out callResult);
					if (ok)
					{
						CentComm.MakeShuttleCallAnnouncement(shuttle.InitialTimerSeconds, text);
						RefreshCallButtonText();
					}
				}
			}
			Loggy.Log(callResult, Category.Round);

			this.RestartCoroutine(ShowSubmitResult(callResult), ref callResultHandle);

			if (ok)
			{
				OpenMenu();
			}
		}

		private void RefreshCallButtonText()
		{
			shuttleCallButtonLabel.MasterSetValue(shuttle.Status == EscapeShuttleStatus.OnRouteStation ? "Recall Emergency Shuttle" : "Call Emergency Shuttle");
		}

		private IEnumerator ShowSubmitResult(string callResult)
		{
			shuttleCallResultLabel.MasterSetValue(callResult);
			yield return WaitFor.Seconds(3);
			shuttleCallResultLabel.MasterSetValue(String.Empty);
		}

		public void SetStatusDisplay(string text)
		{
			text = Chat.StripTags(text);

			Loggy.Log(nameof(SetStatusDisplay), Category.Shuttles);
			GameManager.Instance.CentComm.UpdateStatusDisplay(StatusDisplayChannel.Command, text.Substring(0, Mathf.Min(text.Length, 50)));
			OpenMenu();
		}

		public void MakeAnAnnouncement(string text)
		{
			text = Chat.StripTags(text);
			var language = Peepers.Count == 0 ? null : Peepers.ElementAt(0).Script.MobLanguages.CurrentLanguage;

			Loggy.Log(nameof(MakeAnAnnouncement), Category.Shuttles);
			if (text.Length > 200)
			{
				CentComm.MakeAnnouncement(ChatTemplates.CaptainAnnounce, text.Substring(0, 200), CentComm.UpdateSound.Announce, language);
			}
			else
			{
				CentComm.MakeAnnouncement(ChatTemplates.CaptainAnnounce, text, CentComm.UpdateSound.Announce, language);
			}
			OpenMenu();
		}

		public void UpdateAlertLevelLabels()
		{
			CurrentAlertLevelLabel.MasterSetValue(GameManager.Instance.CentComm.CurrentAlertLevel.ToString().ToUpper());
			NewAlertLevelLabel.MasterSetValue(LocalAlertLevel.ToString().ToUpper());
		}

		public void ChangeAlertLevel()
		{
			if (GameManager.Instance.RoundTime < GameManager.Instance.CentComm.lastAlertChange.AddMinutes(
				GameManager.Instance.CentComm.coolDownAlertChange))
			{
				StartCoroutine(DisplayAlertErrorMessage("Error: recent alert level change detected!"));
				return;
			}

			Loggy.Log(nameof(ChangeAlertLevel), Category.Shuttles);
			GameManager.Instance.CentComm.lastAlertChange = GameManager.Instance.RoundTime;
			GameManager.Instance.CentComm.ChangeAlertLevel(LocalAlertLevel);

			OpenMenu();
		}

		private IEnumerator DisplayAlertErrorMessage(string text)
		{
			AlertErrorLabel.MasterSetValue(text);
			for (int _i = 0; _i < 5; _i++)
			{
				yield return WaitFor.Seconds(1);
				AlertErrorLabel.MasterSetValue("");
				yield return WaitFor.Seconds(1);
				AlertErrorLabel.MasterSetValue(text);
			}
			AlertErrorLabel.MasterSetValue("");
			yield break;
		}

		public void SelectAlertLevel(string levelName)
		{
			//TODO require 2 ID's to change to red level
			LocalAlertLevel =
				(CentComm.AlertLevel)Enum.Parse(typeof(CentComm.AlertLevel), levelName);
		}

		public void RequestNukeCodes()
		{
			//todo
			Loggy.Log(nameof(RequestNukeCodes), Category.Shuttles);
		}

		public void RemoveId(PlayerInfo player)
		{
			if (console.IdCard && IsAIInteracting() == false)
			{
				console.ServerRemoveIDCard(player);
			}
			CloseTab();
		}

		public void UpdateIdTexts()
		{
			var idCard = console.IdCard;
			if (idCard != null)
			{
				idLabel.MasterSetValue($"{idCard.RegisteredName}, {idCard.GetJobTitle()}");
				return;
			}

			if (IsAIInteracting())
			{
				idLabel.MasterSetValue("AI Control");
				return;
			}

			idLabel.MasterSetValue("<No ID inserted>");
		}

		public void LogIn()
		{
			var AI = IsAIInteracting();
			if (console.IdCard == null && AI == false) return;

			if (AI)
			{
				captainOnlySwitcher.SetActivePage(captainAccessPage);
				OpenMenu();
				return;
			}


			if (console.Restricted.HasClearance(console.IdCard.ClearanceSource) == false)
			{
				idLabel.MasterSetValue(idLabel.Value + " (No access)");
				return;

			}

			var clearanceList = ((IClearanceSource)console.IdCard.ClearanceSource).GetCurrentClearance;
			var isCaptain = clearanceList.Contains(Clearance.Captain);
			captainOnlySwitcher.SetActivePage(isCaptain ? captainAccessPage : noCaptainAccessPage);

			OpenMenu();
		}

		public void LogOut()
		{
			mainSwitcher.SetActivePage(mainSwitcher.DefaultPage);
			UpdateIdTexts();
		}

		public void OpenMenu()
		{
			mainSwitcher.SetActivePage(menuPage);
		}
	}
}
