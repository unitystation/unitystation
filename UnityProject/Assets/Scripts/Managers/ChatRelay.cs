using System;
using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class ChatRelay : NetworkBehaviour
{
	public static ChatRelay chatRelay;

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

	//Example on how to get all of the available voices: (to remind me for next part)
	//  SpObjectTokenCategory tokenCat = new SpObjectTokenCategory();
	//  tokenCat.SetId(SpeechLib.SpeechStringConstants.SpeechCategoryVoices, false);
	//ISpeechObjectTokens tokens = tokenCat.EnumerateTokens(null, null);

	//  int n = 0;
	//foreach (SpObjectToken item in tokens)
	//{
	//		GUILayout.Label( "Voice"+ n +" ---> "+ item.GetDescription(0));
	//	    n ++;
	//}

	public static ChatRelay Instance {
		get {
			if (!chatRelay) {
				chatRelay = FindObjectOfType<ChatRelay>();
			}
			return chatRelay;
		}
	}

	public List<ChatEvent> ChatLog { get; } = new List<ChatEvent>();

	public void Start()
	{
		namelessChannels = ChatChannel.Examine | ChatChannel.Local | ChatChannel.None | ChatChannel.System | ChatChannel.Combat;
	}

	//Get it every time so that colors can be adjusted from inspector (to tweak asthetics)
	public string GetCannelColor(ChatChannel channel)
	{
		var chatColors = new Dictionary<ChatChannel, Color>
		 {
			{ChatChannel.Binary, binaryCol},
			{ChatChannel.Supply, supplyCol},
			{ChatChannel.CentComm, centCommCol},
			{ChatChannel.Command, commandCol},
			{ChatChannel.Common, commonCol},
			{ChatChannel.Engineering, engineeringCol},
			{ChatChannel.Examine, examineCol},
			{ChatChannel.Local, localCol},
			{ChatChannel.Medical, medicalCol},
			{ChatChannel.None, noneCol},
			{ChatChannel.OOC, OOCcol},
			{ChatChannel.Science, scienceCol},
			{ChatChannel.Security, securityCol},
			{ChatChannel.Service, serviceCol},
			{ChatChannel.Syndicate, syndicateCol},
			{ChatChannel.System, systemCol},
			{ChatChannel.Ghost, ghostCol},
			{ChatChannel.Combat, combatCol}
		};

		return ColorUtility.ToHtmlStringRGB(chatColors[channel]);
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
	}

	[Client]
	private void UpdateClientChat(string message, ChatChannel channels)
	{
        //Text to Speech:
        var ttsString = Regex.Replace(message, @"<[^>]*>", String.Empty);
        //message only atm
        if (ttsString.Contains("says:")){
            string saysString = "says:";
            var messageString = ttsString.Substring(ttsString.IndexOf(saysString) + saysString.Length);
            GoogleCloudTTS.Instance.Synthesize(messageString);
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
            string colorMessage = "<color=#" + GetCannelColor(channels) + ">" + name + message + "</color>";
            GameObject chatEntry = Instantiate(ControlChat.Instance.chatEntryPrefab, Vector3.zero, Quaternion.identity);
            Text text = chatEntry.GetComponent<Text>();
            text.text = colorMessage;
            chatEntry.transform.parent = ControlChat.Instance.content;
            chatEntry.transform.localScale = Vector3.one;
        }
    }
}