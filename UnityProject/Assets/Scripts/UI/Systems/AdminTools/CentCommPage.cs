using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatabaseAPI;
using AdminCommands;
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

	        var text = CentCommInputBox.text;

	        AdminCommandsManager.Instance.CmdSendCentCommAnnouncement(ServerData.UserID, PlayerList.Instance.AdminToken, text);

	        adminTools.ShowMainPage();
        }

        public void SendCentCommReportButtonClick()
        {
	        adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to send a REPORT?", SendCentCommReport, gameObject);
        }

        private void SendCentCommReport()
        {
	        var text = CentCommInputBox.text;

	        AdminCommandsManager.Instance.CmdSendCentCommReport(ServerData.UserID, PlayerList.Instance.AdminToken, text);

	        adminTools.ShowMainPage();
        }

        public void CallShuttleButtonClick()
        {
	        adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to CALL the emergency shuttle?", CallShuttle, gameObject);
        }

        private void CallShuttle()
        {
	        var text = CentCommInputBox.text;

	        AdminCommandsManager.Instance.CmdCallShuttle(ServerData.UserID, PlayerList.Instance.AdminToken, text);
        }

        public void RecallShuttleButtonClick()
        {
	        adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to RECALL the emergency shuttle?", RecallShuttle, gameObject);
        }

        private void RecallShuttle()
        {
	        var text = CentCommInputBox.text;

	        AdminCommandsManager.Instance.CmdRecallShuttle(ServerData.UserID, PlayerList.Instance.AdminToken, text);
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

	        AdminCommandsManager.Instance.CmdSendBlockShuttleCall(ServerData.UserID, PlayerList.Instance.AdminToken, toggleBool);
        }

        public void ToggleRecallShuttle()
        {
	        var toggleBool = recallBlockToggle.isOn;

	        currentData.blockRecall = toggleBool;

	        AdminCommandsManager.Instance.CmdSendBlockShuttleRecall(ServerData.UserID, PlayerList.Instance.AdminToken, toggleBool);
        }

		public void CreateERTBtn()
		{
			Logger.LogWarning("Create ERT is not implemented.", Category.Admin);
		}

		public void CreateDeathSquadBtn()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
					"Are you sure you want to create a Death Squad? Intended for extreme cases of station dissidence.",
					CreateDeathSquad, gameObject);
		}

		private void CreateDeathSquad()
		{
			AdminCommandsManager.Instance.CmdCreateDeathSquad(ServerData.UserID, PlayerList.Instance.AdminToken);
		}
    }
}
