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
    public class ControlChat : MonoBehaviour
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

        private List<ChatEvent> _localEvents = new List<ChatEvent>();
        public void AddChatEvent(ChatEvent chatEvent)
        {
            _localEvents.Add(chatEvent);
            ChatRelay.Instance.RefreshLog();
        }

        public List<ChatEvent> GetChatEvents()
        {
            return _localEvents;
        }

        public void Start()
        {
            chatInputWindow.SetActive(false);
        }

        public void Update()
        {
            if (!chatInputWindow.activeInHierarchy && Input.GetKey(KeyCode.T) && GameData.IsInGame)
            {
                chatInputWindow.SetActive(true);
                isChatFocus = true;
                EventSystem.current.SetSelectedGameObject(InputFieldChat.gameObject, null);
                InputFieldChat.OnPointerClick(new PointerEventData(EventSystem.current));
            }
            if (isChatFocus)
            {
                if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendChatMessage(InputFieldChat.text, true);
                    if (this.InputFieldChat.text != "")
                        PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon(true);
                    this.InputFieldChat.text = "";
                    CloseChatWindow();
                }
            }
        }

        public void OnClickSend()
        {
            if (!string.IsNullOrEmpty(this.InputFieldChat.text))
            {
                SoundManager.Play("Click01");
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendChatMessage(InputFieldChat.text, true);
                if (this.InputFieldChat.text != "")
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon(true);
                this.InputFieldChat.text = "";
            }
            CloseChatWindow();
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

        //Called from the server only
        public void ReportToChannel(string reportText)
        {
            string txt = "<color=green>" + reportText + "</color>";
            ChatRelay.Instance.chatlog.Add(new ChatEvent(txt));
        }
    }
}