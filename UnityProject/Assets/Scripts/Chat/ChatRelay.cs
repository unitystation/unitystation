﻿using System;
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
	private LayerMask layerMask;
	private LayerMask npcMask;


	/// <summary>
	/// The char indicating that the following text is speech.
	/// For example: Player says, [Character goes here]"ALL CLOWNS MUST SUFFER"
	/// </summary>
	private char saysChar = ' '; // This is U+200A, a hair space.

	private void Awake()
	{
		//ensures the static instance is cleaned up after scene changes:
		if (Instance == null)
		{
			Instance = this;
			Chat.RegisterChatRelay(Instance, AddToChatLogServer, AddToChatLogClient, AddPrivMessageToClient);
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
		layerMask = LayerMask.GetMask("Walls", "Door Closed");
		npcMask = LayerMask.GetMask("NPC");
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
			for (int i = 0; i < players.Count; i++)
			{
				if (players[i].Script == null)
				{
					//joined viewer, don't message them
					players.Remove(players[i]);
					continue;
				}

				if (players[i].Script.IsGhost)
				{
					//send all to ghosts
					continue;
				}

				if (chatEvent.position == TransformState.HiddenPos)
				{
					//show messages with no provided position to everyone
					continue;
				}

				if (Vector2.Distance(chatEvent.position,
						(Vector3)players[i].Script.WorldPos) > 14f)
				{
					//Player in the list is too far away for local chat, remove them:
					players.Remove(players[i]);
				}
				else
				{
					//within range, but check if they are in another room or hiding behind a wall
					if (Physics2D.Linecast(chatEvent.position,
						(Vector3)players[i].Script.WorldPos, layerMask))
					{
						//if it hit a wall remove that player
						players.Remove(players[i]);
					}
				}
			}

			//Get NPCs in vicinity
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
				if (!channels.HasFlag(ChatChannel.Binary) || players[i].Script.IsGhost)
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
	private void AddPrivMessageToClient(string message, string adminId)
	{
		trySendingTTS(message);

		ChatUI.Instance.AddAdminPrivEntry(message, adminId);
	}

	[Client]
	private void UpdateClientChat(string message, ChatChannel channels)
	{
		if (string.IsNullOrEmpty(message)) return;

		trySendingTTS(message);

		if (PlayerManager.LocalPlayerScript == null)
		{
			channels = ChatChannel.OOC;
		}

		if (channels != ChatChannel.None)
		{
			ChatUI.Instance.AddChatEntry(message);
		}
	}

	/// <summary>
	/// Sends a message to TTS to vocalize.
	/// They are required to contain the saysChar.
	/// Messages must also contain at least one letter from the alphabet.
	/// </summary>
	/// <param name="message">The message to try to vocalize.</param>
	private void trySendingTTS(string message)
	{
		if (UIManager.Instance.ttsToggle)
		{
			message = Regex.Replace(message, @"<[^>]*>", String.Empty); // Style tags
			int saysCharIndex = message.IndexOf(saysChar);
			if (saysCharIndex != -1)
			{
				string messageAfterSaysChar = message.Substring(message.IndexOf(saysChar) + 1);
				if (messageAfterSaysChar.Length > 0 && messageAfterSaysChar.Any(char.IsLetter))
				{
					MaryTTS.Instance.Synthesize(messageAfterSaysChar);
				}
			}
		}
	}
}