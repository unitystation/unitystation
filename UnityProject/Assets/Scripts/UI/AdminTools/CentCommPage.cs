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

	        ServerCommandVersionTwoMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, text, "CmdSendCentCommAnnouncement");

	        adminTools.ShowMainPage();
        }

        public void SendCentCommReportButtonClick()
        {
	        adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to send a REPORT?", SendCentCommReport, gameObject);
        }

        private void SendCentCommReport()
        {
	        var text = CentCommInputBox.text;

	        ServerCommandVersionTwoMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, text, "CmdSendCentCommReport");

	        adminTools.ShowMainPage();
        }

        public void CallShuttleButtonClick()
        {
	        adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to CALL the emergency shuttle?", CallShuttle, gameObject);
        }

        private void CallShuttle()
        {
	        var text = CentCommInputBox.text;

	        ServerCommandVersionTwoMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, text, "CmdCallShuttle");
        }

        public void RecallShuttleButtonClick()
        {
	        adminTools.areYouSurePage.SetAreYouSurePage("Are you sure you want to RECALL the emergency shuttle?", RecallShuttle, gameObject);
        }

        private void RecallShuttle()
        {
	        var text = CentCommInputBox.text;

	        ServerCommandVersionTwoMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, text, "CmdRecallShuttle");
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

	        ServerCommandVersionFiveMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, toggleBool, "CmdSendBlockShuttleCall");
        }

        public void ToggleRecallShuttle()
        {
	        var toggleBool = recallBlockToggle.isOn;

	        currentData.blockRecall = toggleBool;

	        ServerCommandVersionFiveMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, toggleBool,"CmdSendBlockShuttleRecall");
        }
    }
}

