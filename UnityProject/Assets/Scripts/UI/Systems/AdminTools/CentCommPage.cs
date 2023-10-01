using System.Collections.Generic;
using UnityEngine;
using DatabaseAPI;
using AdminCommands;
using Logs;
using UnityEngine.UI;


namespace AdminTools
{
	public class CentCommPage : AdminPage
	{
		[SerializeField] InputFieldFocus CentCommInputBox = null;

		[SerializeField]
		private Toggle callBlockToggle = null;

		[SerializeField]
		private Toggle recallBlockToggle = null;

		public void SendCentCommAnnouncementButtonClick()
		{
			adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to send an ANNOUNCEMENT?", SendCentCommAnnouncement, gameObject);
		}

		private void SendCentCommAnnouncement()
		{
			AdminCommandsManager.Instance.CmdSendCentCommAnnouncement(CentCommInputBox.text);
		}

		public void SendCentCommReportButtonClick()
		{
			adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to send a REPORT?", SendCentCommReport, gameObject);
		}

		private void SendCentCommReport()
		{
			AdminCommandsManager.Instance.CmdSendCentCommReport(CentCommInputBox.text);
		}

		public void CallShuttleButtonClick()
		{
			adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to CALL the emergency shuttle?", CallShuttle, gameObject);
		}

		private void CallShuttle()
		{
			AdminCommandsManager.Instance.CmdCallShuttle(CentCommInputBox.text);
		}

		public void RecallShuttleButtonClick()
		{
			adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to RECALL the emergency shuttle?", RecallShuttle, gameObject);
		}

		private void RecallShuttle()
		{
			AdminCommandsManager.Instance.CmdRecallShuttle(CentCommInputBox.text);
		}

		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			base.OnPageRefresh(adminPageData);
			callBlockToggle.isOn = adminPageData.blockCall;
			recallBlockToggle.isOn = adminPageData.blockRecall;
		}

		public void ToggleCallShuttle()
		{
			var toggleBool = callBlockToggle.isOn;

			currentData.blockCall = toggleBool;

			AdminCommandsManager.Instance.CmdSendBlockShuttleCall(toggleBool);
		}

		public void ToggleRecallShuttle()
		{
			var toggleBool = recallBlockToggle.isOn;

			currentData.blockRecall = toggleBool;

			AdminCommandsManager.Instance.CmdSendBlockShuttleRecall(toggleBool);
		}

		public void CreateERTBtn()
		{
			Loggy.LogWarning("Create ERT is not implemented.", Category.Admin);
		}

		public void CreateDeathSquadBtn()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
					"Are you sure you want to create a Death Squad? Intended for extreme cases of station dissidence.",
					CreateDeathSquad, gameObject);
		}

		private void CreateDeathSquad()
		{
			AdminCommandsManager.Instance.CmdCreateDeathSquad();
		}
	}
}
