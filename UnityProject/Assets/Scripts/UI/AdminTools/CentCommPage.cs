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
			var action = PlayerManager.LocalPlayerScript.playerNetworkActions;

			if (action == null) return;

			action.CmdSendCentCommAnnouncement(DatabaseAPI.ServerData.UserID, PlayerList.Instance.AdminToken, text);

            adminTools.ShowMainPage();
        }

        public void SendCentCommReport()
        {
            string text = CentCommInputBox.text;
			var action = PlayerManager.LocalPlayerScript.playerNetworkActions;

			if (action == null) return;

			action.CmdSendCentCommReport(DatabaseAPI.ServerData.UserID, PlayerList.Instance.AdminToken, text);

            adminTools.ShowMainPage();
        }
    }
}

