using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using PlayGroup;
using System.Linq;

public class ChatRelay : NetworkBehaviour
{

	public static ChatRelay chatRelay;
	private List<ChatEvent> chatlog = new List<ChatEvent>();

	private Dictionary<ChatChannel, string> chatColors;
	private ChatChannel namelessChannels;

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
		chatColors = new Dictionary<ChatChannel, string>() {
			{ChatChannel.Binary, "#ff00ff"},
			{ChatChannel.Cargo, "#a8732b"},
			{ChatChannel.CentComm, "#686868"},
			{ChatChannel.Command, "#204090"},
			{ChatChannel.Common, "#008000"},
			{ChatChannel.Engineering, "#fb5613"},
			{ChatChannel.Examine, "black"},
			{ChatChannel.Local, "#999999"},
			{ChatChannel.Medical, "#337296"},
			{ChatChannel.None, ""},
			{ChatChannel.OOC, "#386aff"},
			{ChatChannel.Science, "#993399"},
			{ChatChannel.Security, "#a30000"},
			{ChatChannel.Service, "#6eaa2c"},
			{ChatChannel.Syndicate, "#6d3f40"},
			{ChatChannel.System, "#dd5555"}
		};
		namelessChannels = (ChatChannel.Examine | ChatChannel.Local | ChatChannel.None | ChatChannel.System);
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

		string curList = UIManager.Chat.CurrentChannelText.text;

		foreach (ChatEvent chatline in chatEvents.OrderBy(c => c.timestamp)) {
			string message = chatline.message;
			foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel))) {
				if (channel == ChatChannel.None) {
					continue;
				}

				string name = "";
				if((namelessChannels & channel) != channel) {
					name = "<b>[" + channel.ToString() + "]</b> ";
				}

				if ((PlayerManager.LocalPlayerScript.GetAvailableChannels(false) & channel) == channel && (chatline.channels & channel) == channel) {
					string colorMessage = "<color=" + chatColors[channel] + ">" + name + message + "</color>";
					UIManager.Chat.CurrentChannelText.text = curList + colorMessage + "\r\n";
					curList = UIManager.Chat.CurrentChannelText.text;
				}
			}
		}
    }
}
