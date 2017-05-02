using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using PlayGroup;

namespace UI
{
    public class ControlChat: MonoBehaviour
    {
        public GameObject chatInputWindow;
        public InputField usernameInput;
        public RectTransform ChatPanel;
        // set in inspector (to enable/disable panel)

        public InputField InputFieldChat;
        public Text CurrentChannelText;
        public Scrollbar scrollBar;

        public bool isChatFocus = false;

        public bool ShowState = true;

        private string userIdInput = "";

        public void Start()
        {
            chatInputWindow.SetActive(false); 
        }
			
        public void Update()
        {
			if (!chatInputWindow.activeInHierarchy && Input.GetKey(KeyCode.T)) {
				chatInputWindow.SetActive(true);
				isChatFocus = true;
				EventSystem.current.SetSelectedGameObject(InputFieldChat.gameObject, null);
				InputFieldChat.OnPointerClick(new PointerEventData(EventSystem.current));
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon(true);
			}
            if (isChatFocus)
            {
                if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                {
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendChatMessage(InputFieldChat.text, true);
                    this.InputFieldChat.text = "";
                    CloseChatWindow();
                }
            }
        }
            
        public void OnClickSend()
        {
            if (this.InputFieldChat != null)
            {
                SoundManager.Play("Click01");
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendChatMessage(InputFieldChat.text, true);
                this.InputFieldChat.text = "";
                CloseChatWindow();
            }
        }

        public void OnChatCancel()
        {
            SoundManager.Play("Click01");
            this.InputFieldChat.text = "";
            CloseChatWindow();
        }

        void CloseChatWindow()
		{
            isChatFocus = false;
            chatInputWindow.SetActive(false);
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon(false);
        }

        public void ReportToChannel(string reportText)
        {
			//TODO Reporting msgs
//            StringBuilder txt = new StringBuilder(reportText + "\r\n");
        }
    }
}
