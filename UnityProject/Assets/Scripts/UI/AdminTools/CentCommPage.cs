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
            Logger.Log( nameof(SendCentCommAnnouncement), Category.NetUI );
            CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text);
            adminTools.ShowMainPage();
        }
    }
}

