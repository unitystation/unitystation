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
    public Color binaryCol; // "#ff00ff";
    public Color supplyCol; // "#a8732b"
	public Color centCommCol; // "#686868"
    public Color commandCol; // "#204090"
    public Color commonCol; // "#008000"
    public Color engineeringCol; //"#fb5613"
	public Color examineCol; //"black"
    public Color localCol; // "black"
    public Color medicalCol; // "#337296"
    public Color noneCol; // ""
    public Color OOCcol; // "#386aff"
    public Color scienceCol; // "#993399"
    public Color securityCol; // "#a30000"
    public Color serviceCol; // "#6eaa2c"
    public Color syndicateCol; // "#6d3f40"
    public Color systemCol; // "#dd5555"
    public Color ghostCol; // "#386aff"
    public Color combatCol; // "#dd0000"

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
			{ChatChannel.Binary, ColorUtility.ToHtmlStringRGB(binaryCol) },
			{ChatChannel.Supply, ColorUtility.ToHtmlStringRGB(supplyCol)},
			{ChatChannel.CentComm, ColorUtility.ToHtmlStringRGB(centCommCol)},
			{ChatChannel.Command, ColorUtility.ToHtmlStringRGB(commandCol)},
			{ChatChannel.Common, ColorUtility.ToHtmlStringRGB(commonCol)},
			{ChatChannel.Engineering, ColorUtility.ToHtmlStringRGB(engineeringCol)},
			{ChatChannel.Examine, ColorUtility.ToHtmlStringRGB(examineCol)},
			{ChatChannel.Local, ColorUtility.ToHtmlStringRGB(localCol)},
			{ChatChannel.Medical, ColorUtility.ToHtmlStringRGB(medicalCol)},
			{ChatChannel.None, ColorUtility.ToHtmlStringRGB(noneCol)},
			{ChatChannel.OOC, ColorUtility.ToHtmlStringRGB(OOCcol)},
			{ChatChannel.Science, ColorUtility.ToHtmlStringRGB(scienceCol)},
			{ChatChannel.Security, ColorUtility.ToHtmlStringRGB(securityCol)},
			{ChatChannel.Service, ColorUtility.ToHtmlStringRGB(serviceCol)},
			{ChatChannel.Syndicate, ColorUtility.ToHtmlStringRGB(syndicateCol)},
			{ChatChannel.System, ColorUtility.ToHtmlStringRGB(systemCol)},
			{ChatChannel.Ghost, ColorUtility.ToHtmlStringRGB(ghostCol)},
			{ChatChannel.Combat, ColorUtility.ToHtmlStringRGB(combatCol)}
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