using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AdminTools
{
    public class CentCommPage : AdminPage
    {
        [SerializeField] InputFieldFocus CentCommInputBox;

        public void SendCentCommAnnouncement()
        {
            
            string text = CentCommInputBox.text;
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendCentCommAnnouncement(DatabaseAPI.ServerData.UserID, 
                                                                                            PlayerList.Instance.AdminToken,
                                                                                            text);
            
            adminTools.ShowMainPage();
        }

        public void SendCentCommReport()
        {
            string text = CentCommInputBox.text;
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendCentCommReport(DatabaseAPI.ServerData.UserID, 
                                                                                        PlayerList.Instance.AdminToken,
                                                                                        text);
            
            adminTools.ShowMainPage();
        }
    }
}

