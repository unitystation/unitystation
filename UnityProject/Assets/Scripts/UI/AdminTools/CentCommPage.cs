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
			var action = PlayerManager.LocalPlayerScript;

			if (action == null) return;

			action.playerNetworkActions.CmdSendCentCommAnnouncement(DatabaseAPI.ServerData.UserID, PlayerList.Instance.AdminToken, text);

            adminTools.ShowMainPage();
        }

        public void SendCentCommReport()
        {
            string text = CentCommInputBox.text;
			var action = PlayerManager.LocalPlayerScript;

			if (action == null) return;

			action.playerNetworkActions.CmdSendCentCommReport(DatabaseAPI.ServerData.UserID, PlayerList.Instance.AdminToken, text);

            adminTools.ShowMainPage();
        }
    }
}

