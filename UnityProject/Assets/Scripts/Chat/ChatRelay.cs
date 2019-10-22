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
		if (chatEvent.channels == ChatChannel.Local || chatEvent.channels == ChatChannel.Combat
		                                            || chatEvent.channels == ChatChannel.Action)
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

			if (channels.HasFlag(ChatChannel.Combat) || channels.HasFlag(ChatChannel.Local) ||
			    channels.HasFlag(ChatChannel.System) || channels.HasFlag(ChatChannel.Examine) ||
			    channels.HasFlag(ChatChannel.Action))
			{
				if (!channels.HasFlag(ChatChannel.Binary))
				{
					UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.modifiers, chatEvent.message, chatEvent.messageOthers,
						chatEvent.originator, chatEvent.speaker);
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
				UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.modifiers, chatEvent.message, chatEvent.messageOthers,
					chatEvent.originator, chatEvent.speaker);
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

		if (PlayerManager.LocalPlayerScript == null)
		{
			channels = ChatChannel.OOC;
		}

		if (channels != ChatChannel.None)
		{
			GameObject chatEntry = Instantiate(ChatUI.Instance.chatEntryPrefab, Vector3.zero, Quaternion.identity);
			Text text = chatEntry.GetComponent<Text>();
			text.text = message;
			chatEntry.transform.SetParent(ChatUI.Instance.content, false);
			chatEntry.transform.localScale = Vector3.one;
		}
	}
}