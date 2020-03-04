using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AdminTools
{
    public class CentCommPage : AdminPage
    {
        [SerializeField] InputFieldFocus AnnouncementText;

        public void SendCentCommAnnouncement()
        {
            
            string text = AnnouncementText.text;
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendCentCommAnnouncement(DatabaseAPI.ServerData.UserID, 
                                                                                            PlayerList.Instance.AdminToken,
                                                                                            text);
            
            adminTools.ShowMainPage();
        }
    }
}

