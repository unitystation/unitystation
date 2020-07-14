using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AdminTools
{
    public class CentCommPage : AdminPage
    {
        [SerializeField] InputFieldFocus CentCommInputBox = null;

        public void SendCentCommAnnouncement()
        {

            string text = CentCommInputBox.text;

            if(!AdminCommandsManager.Instance.hasAuthority) return;

            AdminCommandsManager.Instance.CmdSendCentCommAnnouncement(DatabaseAPI.ServerData.UserID, PlayerList.Instance.AdminToken, text);

            adminTools.ShowMainPage();
        }

        public void SendCentCommReport()
        {
            string text = CentCommInputBox.text;

            if(!AdminCommandsManager.Instance.hasAuthority) return;

            AdminCommandsManager.Instance.CmdSendCentCommReport(DatabaseAPI.ServerData.UserID, PlayerList.Instance.AdminToken, text);

            adminTools.ShowMainPage();
        }
    }
}

