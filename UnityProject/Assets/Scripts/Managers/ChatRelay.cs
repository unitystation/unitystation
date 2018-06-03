using System;
using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine.Networking;
using UnityEngine;

public class ChatRelay : NetworkBehaviour
{
	public static ChatRelay chatRelay;

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

	public List<ChatEvent> ChatLog { get; } = new List<ChatEvent>();

	public void Start()
	{
		chatColors = new Dictionary<ChatChannel, string>
		{
			{ChatChannel.Binary, "#ff00ff"},
			{ChatChannel.Supply, "#a8732b"},
			{ChatChannel.CentComm, "#686868"},
			{ChatChannel.Command, "#204090"},
			{ChatChannel.Common, "#008000"},
			{ChatChannel.Engineering, "#fb5613"},
			{ChatChannel.Examine, "black"},
			{ChatChannel.Local, "black"},
			{ChatChannel.Medical, "#337296"},
			{ChatChannel.None, ""},
			{ChatChannel.OOC, "#386aff"},
			{ChatChannel.Science, "#993399"},
			{ChatChannel.Security, "#a30000"},
			{ChatChannel.Service, "#6eaa2c"},
			{ChatChannel.Syndicate, "#6d3f40"},
			{ChatChannel.System, "#dd5555"},
			{ChatChannel.Ghost, "#386aff"},
			{ChatChannel.Combat, "#dd0000"}
		};
		namelessChannels = ChatChannel.Examine | ChatChannel.Local | ChatChannel.None | ChatChannel.System | ChatChannel.Combat;
		
		RefreshLog();
	}

	[Server]
	public void AddToChatLogServer(ChatEvent chatEvent)
	{
		PropagateChatToClients(chatEvent);
		RefreshLog();
	}

	[Client]
	public void AddToChatLogClient(string message, ChatChannel channels)
	{
		UpdateClientChat(message, channels);
		RefreshLog();
	}

	[Server]
	private void PropagateChatToClients(ChatEvent chatEvent)
	{
		var players = PlayerList.Instance.InGamePlayers;

		//Local chat range checks:
		if (chatEvent.channels == ChatChannel.Local || chatEvent.channels == ChatChannel.Combat) {
//			var speaker = PlayerList.Instance.Get(chatEvent.speaker);
			RaycastHit2D hit;
			LayerMask layerMask = 1 << 9; //Walls layer
			for (int i = 0; i < players.Count(); i++){
				if(Vector2.Distance(chatEvent.position,//speaker.GameObject.transform.position,
				                    players[i].GameObject.transform.position) > 14f){
					//Player in the list is too far away for local chat, remove them:
					players.Remove(players[i]);
				} else {
					//within range, but check if they are in another room or hiding behind a wall
					if(Physics2D.Linecast(chatEvent.position,//speaker.GameObject.transform.position, 
					                      players[i].GameObject.transform.position, layerMask)){
						//if it hit a wall remove that player
						players.Remove(players[i]);
					}
				}
			}
		}

		for ( var i = 0; i < players.Count; i++ )
		{
			var playerScript = players[i].GameObject.GetComponent<PlayerScript>();
			ChatChannel channels = playerScript.GetAvailableChannelsMask( false ) & chatEvent.channels;
			UpdateChatMessage.Send( players[i].GameObject, channels, chatEvent.message );
		}
	}

	[Client]
	private void UpdateClientChat(string message, ChatChannel channels)
	{
		ChatEvent chatEvent = new ChatEvent(message, channels, true);
		ChatLog.Add(chatEvent);
	}

	public void RefreshLog()
	{
		UIManager.Chat.CurrentChannelText.text = "";
		List<ChatEvent> chatEvents = new List<ChatEvent>();
		chatEvents.AddRange(ChatLog);
		chatEvents.AddRange(UIManager.Chat.GetChatEvents());

		string curList = UIManager.Chat.CurrentChannelText.text;

		foreach (ChatEvent chatline in chatEvents.OrderBy(c => c.timestamp))
		{
			string message = chatline.message;
			foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel)))
			{
				if (channel == ChatChannel.None)
				{
					continue;
				}

				string name = "";
				if ((namelessChannels & channel) != channel)
				{
					name = "<b>[" + channel + "]</b> ";
				}

				if ((PlayerManager.LocalPlayerScript.GetAvailableChannelsMask(false) & channel) == channel && (chatline.channels & channel) == channel)
				{
					string colorMessage = "<color=" + chatColors[channel] + ">" + name + message + "</color>";
					UIManager.Chat.CurrentChannelText.text = curList + "\r\n" + colorMessage;
					curList = UIManager.Chat.CurrentChannelText.text;
				}
			}
		}
	}
}