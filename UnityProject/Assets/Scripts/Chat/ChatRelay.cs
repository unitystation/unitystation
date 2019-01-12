using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class ChatRelay : NetworkBehaviour
{
	public static ChatRelay Instance;

	private ChatChannel namelessChannels;
	public List<ChatEvent> ChatLog { get; } = new List<ChatEvent>();

	private void Awake()
	{
		//ensures the static instance is cleaned up after scene changes:
		if(Instance == null){
			Instance = this;
		} else {
			Destroy(gameObject);
		}
	}

	public void Start()
	{
		namelessChannels = ChatChannel.Examine | ChatChannel.Local | ChatChannel.None | ChatChannel.System | ChatChannel.Combat;
	}

	//Get it every time so that colors can be adjusted from inspector (to tweak asthetics)
	public string GetCannelColor(ChatChannel channel)
	{
		var chatColors = new Dictionary<ChatChannel, String>
		 {
			{ChatChannel.Binary, "ff00ff"},
			{ChatChannel.Supply, "a8732b"},
			{ChatChannel.CentComm, "686868"},
			{ChatChannel.Command, "204090"},
			{ChatChannel.Common, "008000"},
			{ChatChannel.Engineering, "fb5613"},
			{ChatChannel.Examine, "white"},
			{ChatChannel.Local, "white"},
			{ChatChannel.Medical, "337296"},
			{ChatChannel.None, ""},
			{ChatChannel.OOC, "386aff"},
			{ChatChannel.Science, "993399"},
			{ChatChannel.Security, "a30000"},
			{ChatChannel.Service, "6eaa2c"},
			{ChatChannel.Syndicate, "6d3f40"},
			{ChatChannel.System, "dd5555"},
			{ChatChannel.Ghost, "386aff"},
			{ChatChannel.Combat, "dd0000"}
		};

		return chatColors[channel];
	}

	[Server]
	public void AddToChatLogServer(ChatEvent chatEvent)
	{
		PropagateChatToClients(chatEvent);
	}

	[Client]
	public void AddToChatLogClient(string message, ChatChannel channels)
	{
		UpdateClientChat(message, channels);
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
			for (int i = 0; i < players.Count(); i++) {
				if (Vector2.Distance(chatEvent.position,//speaker.GameObject.transform.position,
									players[i].GameObject.transform.position) > 14f) {
					//Player in the list is too far away for local chat, remove them:
					players.Remove(players[i]);
				} else {
					//within range, but check if they are in another room or hiding behind a wall
					if (Physics2D.Linecast(chatEvent.position,//speaker.GameObject.transform.position, 
										  players[i].GameObject.transform.position, layerMask)) {
						//if it hit a wall remove that player
						players.Remove(players[i]);
					}
				}
			}
		}

		for (var i = 0; i < players.Count; i++) {
			var playerScript = players[i].GameObject.GetComponent<PlayerScript>();
			ChatChannel channels = playerScript.GetAvailableChannelsMask(false) & chatEvent.channels;
			UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.message);
		}

		if(RconManager.Instance != null){
			string name = "";
			if ((namelessChannels & chatEvent.channels) != chatEvent.channels) {
				name = "<b>[" + chatEvent.channels + "]</b> ";
			}
			RconManager.AddChatLog(name + chatEvent.message);
		}
	}

	[Client]
	private void UpdateClientChat(string message, ChatChannel channels)
	{
		if (UIManager.Instance.ttsToggle) {
			//Text to Speech:
			var ttsString = Regex.Replace(message, @"<[^>]*>", String.Empty);
			//message only atm
			if (ttsString.Contains(":")) {
				string saysString = ":";
				var messageString = ttsString.Substring(ttsString.IndexOf(saysString) + saysString.Length);
				MaryTTS.Instance.Synthesize(messageString);
				// GoogleCloudTTS.Instance.Synthesize(messageString);
			}
		}

        ChatEvent chatEvent = new ChatEvent(message, channels, true);

		if (channels == ChatChannel.None) {
			return;
		}

		string name = "";
		if ((namelessChannels & channels) != channels) {
			name = "<b>[" + channels + "]</b> ";
		}

		if ((PlayerManager.LocalPlayerScript.GetAvailableChannelsMask(false) & channels) == channels && (chatEvent.channels & channels) == channels) {
            //Chatevent UI entry:
			//FIXME at the moment all chat entries are white because of the new system, its a WIP
			//string colorMessage = "<color=#" + GetCannelColor(channels) + ">" + name + message + "</color>";
            string colorMessage = "<color=white>" + name + message + "</color>";
            GameObject chatEntry = Instantiate(ControlChat.Instance.chatEntryPrefab, Vector3.zero, Quaternion.identity);
            Text text = chatEntry.GetComponent<Text>();
            text.text = colorMessage;
			chatEntry.transform.SetParent(ControlChat.Instance.content, false);
            chatEntry.transform.localScale = Vector3.one;
        }
    }
}