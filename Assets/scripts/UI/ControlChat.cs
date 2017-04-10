using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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
        private readonly Dictionary<string, Toggle> channelToggles = new Dictionary<string, Toggle>();

        public bool ShowState = true;

        private string userIdInput = "";

        public void Start()
        {
            chatInputWindow.SetActive(false);

            //			ChatPanel.gameObject.SetActive(false);
           
        }

        //FIXME: The left over guts from the old Photon Chat Controller
        //FIXME: Develop new chat system over uNet

        public void Update()
        {

//            if(chatClient != null) {
//                if(chatClient.CanChat && !GameData.IsInGame) {
//                    //TODO: Remove this when a better transition handler is implemented 
//            
//                }
//            }

//            if(chatClient != null) {
//                if(Input.GetKeyDown(KeyCode.T) && !isChatFocus && chatClient.CanChat) {
//
//                    if(!chatInputWindow.activeSelf) {
//                        chatInputWindow.SetActive(true);
//                    }
//
//                    isChatFocus = true;
//                    EventSystem.current.SetSelectedGameObject(InputFieldChat.gameObject, null);
//                    InputFieldChat.OnPointerClick(new PointerEventData(EventSystem.current));
//                }
//            }

            if (isChatFocus)
            {

                if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                {
//                    SendChatMessage(this.InputFieldChat.text);
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
//                SendChatMessage(this.InputFieldChat.text);
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

        }

        public void ReportToChannel(string reportText)
        {

            StringBuilder txt = new StringBuilder(reportText + "\r\n");


//            this.CurrentChannelText.text += txt.ToString();

        }

    }
}
