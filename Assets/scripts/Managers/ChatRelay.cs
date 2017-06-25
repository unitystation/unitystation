using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using System.Linq;

public class ChatRelay : NetworkBehaviour
{

    public static ChatRelay chatRelay;
    public ChatEventList chatlog = new ChatEventList();

    public static ChatRelay Instance
    {
        get
        {
            if (!chatRelay)
            {
                chatRelay = FindObjectOfType<ChatRelay>();
            }
            return chatRelay;
        }
    }

    public override void OnStartClient()
    {
        chatlog.Callback = RefreshChatLog;
        base.OnStartClient();
    }

    void RefreshChatLog(SyncListStruct<ChatEvent>.Operation op, int index)
    {
        RefreshLog();

    }

    public void RefreshLog()
    {
        UIManager.Chat.CurrentChannelText.text = "";
        List<ChatEvent> chatEvents = new List<ChatEvent>();
        chatEvents.AddRange(chatlog);
        chatEvents.AddRange(UIManager.Chat.GetChatEvents());

        foreach (ChatEvent chatline in chatEvents.OrderBy(c => c.timestamp))
        {
            string curList = UIManager.Chat.CurrentChannelText.text;
            UIManager.Chat.CurrentChannelText.text = curList + chatline.message + "\r\n";
        }
    }
}
