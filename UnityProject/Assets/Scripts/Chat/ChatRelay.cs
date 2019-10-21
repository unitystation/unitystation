using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

/// <summary>
/// ChatRelay is only to be used internally via Chat.cs
/// Do not change any protection levels in this script
/// </summary>
public class ChatRelay : NetworkBehaviour
{
	public static ChatRelay Instance;

	private ChatChannel namelessChannels;
	public List<ChatEvent> ChatLog { get; } = new List<ChatEvent>();

	private void Awake()
	{
		//ensures the static instance is cleaned up after scene changes:
		if (Instance == null)
		{
			Instance = this;
			Chat.RegisterChatRelay(Instance, AddToChatLogServer, AddToChatLogClient);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public void Start()
	{
		namelessChannels = ChatChannel.Examine | ChatChannel.Local | ChatChannel.None | ChatChannel.System |
		                   ChatChannel.Combat;
	}

	public string GetChannelColor(ChatChannel channel)
	{
		if (channel.HasFlag(ChatChannel.OOC)) return "386aff";
		if (channel.HasFlag(ChatChannel.Ghost)) return "386aff";
		if (channel.HasFlag(ChatChannel.Binary)) return "ff00ff";
		if (channel.HasFlag(ChatChannel.Supply)) return "a8732b";
		if (channel.HasFlag(ChatChannel.CentComm)) return "686868";
		if (channel.HasFlag(ChatChannel.Command)) return "204090";
		if (channel.HasFlag(ChatChannel.Common)) return "008000";
		if (channel.HasFlag(ChatChannel.Engineering)) return "fb5613";
		if (channel.HasFlag(ChatChannel.Medical)) return "337296";
		if (channel.HasFlag(ChatChannel.Science)) return "993399";
		if (channel.HasFlag(ChatChannel.Security)) return "a30000";
		if (channel.HasFlag(ChatChannel.Service)) return "6eaa2c";
		if (channel.HasFlag(ChatChannel.Local)) return "white";
		return "white";

		//Leaving values here incase nameless channels need them in the future
		/*
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
		*/
	}

	[Server]
	private void AddToChatLogServer(ChatEvent chatEvent)
	{
		PropagateChatToClients(chatEvent);
	}

	[Server]
	private void PropagateChatToClients(ChatEvent chatEvent)
	{
		List<ConnectedPlayer> players;
		if (chatEvent.matrix != MatrixInfo.Invalid)
		{
			//get players only on provided matrix
			players = PlayerList.Instance.GetPlayersOnMatrix(chatEvent.matrix);
		}
		else
		{
			players = PlayerList.Instance.AllPlayers;
		}

		//Local chat range checks:
		if (chatEvent.channels == ChatChannel.Local || chatEvent.channels == ChatChannel.Combat)
		{
			//			var speaker = PlayerList.Instance.Get(chatEvent.speaker);
			RaycastHit2D hit;
			LayerMask layerMask = LayerMask.GetMask("Walls", "Door Closed");
			for (int i = 0; i < players.Count(); i++)
			{
				if (Vector2.Distance(chatEvent.position, //speaker.GameObject.transform.position,
					    players[i].GameObject.transform.position) > 14f)
				{
					//Player in the list is too far away for local chat, remove them:
					players.Remove(players[i]);
				}
				else
				{
					//within range, but check if they are in another room or hiding behind a wall
					if (Physics2D.Linecast(chatEvent.position, //speaker.GameObject.transform.position,
						players[i].GameObject.transform.position, layerMask))
					{
						//if it hit a wall remove that player
						players.Remove(players[i]);
					}
				}
			}

			//Get NPCs in vicinity
			var npcMask = LayerMask.GetMask("NPC");
			var npcs = Physics2D.OverlapCircleAll(chatEvent.position, 14f, npcMask);
			foreach (Collider2D coll in npcs)
			{
				if (!Physics2D.Linecast(chatEvent.position,
					coll.transform.position, layerMask))
				{
					//NPC is in hearing range, pass the message on:
					var mobAi = coll.GetComponent<MobAI>();
					if (mobAi != null)
					{
						mobAi.LocalChatReceived(chatEvent);
					}
				}
			}
		}

		for (var i = 0; i < players.Count; i++)
		{
			ChatChannel channels = chatEvent.channels;

			if (channels.HasFlag(ChatChannel.None) || channels.HasFlag(ChatChannel.Combat) ||
			    channels.HasFlag(ChatChannel.System) || channels.HasFlag(ChatChannel.Examine)
			    || channels.HasFlag(ChatChannel.Local))
			{
				if (!channels.HasFlag(ChatChannel.Binary))
				{
					UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.message, chatEvent.speaker);
					continue;
				}
			}

			if (players[i].Script == null)
			{
				channels &= ChatChannel.OOC;
			}
			else
			{
				channels &= players[i].Script.GetAvailableChannelsMask(false);
			}

			//if the mask ends up being a big fat 0 then don't do anything
			if (channels != ChatChannel.None)
			{
				UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.message, chatEvent.speaker);
			}
		}

		if (RconManager.Instance != null)
		{
			string name = "";
			if ((namelessChannels & chatEvent.channels) != chatEvent.channels)
			{
				name = "<b>[" + chatEvent.channels + "]</b> ";
			}

			RconManager.AddChatLog(name + chatEvent.message);
		}
	}

	[Client]
	private void AddToChatLogClient(string message, ChatChannel channels)
	{
		Debug.Log(message + " " + channels);
		Debug.Log("TODO! STILL NEED TO ADD SPEAKER NAME AND COLOR FORMATTING ON CLIENT SIDE!");
		UpdateClientChat(message, channels);
	}

	[Client]
	private void UpdateClientChat(string message, ChatChannel channels)
	{
		if (UIManager.Instance.ttsToggle)
		{
			//Text to Speech:
			var ttsString = Regex.Replace(message, @"<[^>]*>", String.Empty);
			//message only atm
			if (ttsString.Contains(":"))
			{
				string saysString = ":";
				var messageString = ttsString.Substring(ttsString.IndexOf(saysString) + saysString.Length);
				MaryTTS.Instance.Synthesize(messageString);
			}
		}

		if (channels == ChatChannel.None)
		{
			return;
		}

		ChatChannel checkChannels;
		if (PlayerManager.LocalPlayerScript == null)
		{
			checkChannels = ChatChannel.OOC;
		}
		else
		{
			checkChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask(false);
		}

		if ((checkChannels & channels) == channels)
		{
			GameObject chatEntry = Instantiate(ChatUI.Instance.chatEntryPrefab, Vector3.zero, Quaternion.identity);
			Text text = chatEntry.GetComponent<Text>();
			text.text = message;
			chatEntry.transform.SetParent(ChatUI.Instance.content, false);
			chatEntry.transform.localScale = Vector3.one;
		}
	}
}