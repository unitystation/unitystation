using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using System.Linq;

public class ChatRelay : NetworkBehaviour
{

	public static ChatRelay chatRelay;
	private List<ChatEvent> chatlog = new List<ChatEvent>();

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
		RefreshLog();
		base.OnStartClient();
	}

	public List<ChatEvent> ChatLog
	{
		get { return chatlog; }
	}

	public void AddToChatLog(ChatEvent message)
	{
		chatlog.Add(message);
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
