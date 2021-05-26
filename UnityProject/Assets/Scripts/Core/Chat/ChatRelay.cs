using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using Systems.MobAIs;
using System.Text.RegularExpressions;
using Messages.Server;

/// <summary>
/// ChatRelay is only to be used internally via Chat.cs
/// Do not change any protection levels in this script
/// </summary>
public class ChatRelay : NetworkBehaviour
{
	public static ChatRelay Instance;

	private ChatChannel namelessChannels;
	private LayerMask layerMask;
	private LayerMask npcMask;

	private RconManager rconManager;

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
		layerMask = LayerMask.GetMask( "Door Closed");
		npcMask = LayerMask.GetMask("NPC");

		rconManager = RconManager.Instance;
	}

	[Server]
	public void PropagateChatToClients(ChatEvent chatEvent)
	{
		List<ConnectedPlayer> players = PlayerList.Instance.AllPlayers;

		//Local chat range checks:
		if (chatEvent.channels.HasFlag(ChatChannel.Local)
				|| chatEvent.channels.HasFlag(ChatChannel.Combat)
				|| chatEvent.channels.HasFlag(ChatChannel.Action))
		{
			for (int i = players.Count - 1; i >= 0; i--)
			{
				if (players[i].Script == null)
				{
					//joined viewer, don't message them
					players.RemoveAt(i);
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

				var playerPosition = players[i].GameObject.AssumedWorldPosServer();
				if (Vector2.Distance(chatEvent.position, playerPosition) > 14f)
				{
					//Player in the list is too far away for local chat, remove them:
					players.RemoveAt(i);
				}
				else
				{
					//within range, but check if they are in another room or hiding behind a wall
					if (MatrixManager.Linecast(chatEvent.position, LayerTypeSelection.Walls
						 , layerMask,playerPosition).ItHit)
					{
						//if it hit a wall remove that player
						players.RemoveAt(i);
					}
				}
			}

			//Get NPCs in vicinity
			var npcs = Physics2D.OverlapCircleAll(chatEvent.position, 14f, npcMask);
			foreach (Collider2D coll in npcs)
			{
				var npcPosition = coll.gameObject.AssumedWorldPosServer();
				if (MatrixManager.Linecast(chatEvent.position,LayerTypeSelection.Walls,
					 layerMask,npcPosition).ItHit ==false)
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
						chatEvent.originator, chatEvent.speaker, chatEvent.stripTags);

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
					chatEvent.originator, chatEvent.speaker, chatEvent.stripTags);
			}
		}

		if (rconManager != null)
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
	public void AddAdminPrivMessageToClient(string message)
	{
		trySendingTTS(message);

		ChatUI.Instance.AddAdminPrivEntry(message);
	}

	[Client]
	public void AddMentorPrivMessageToClient(string message)
	{
		trySendingTTS(message);

		ChatUI.Instance.AddMentorPrivEntry(message);
	}

	[Client]
	public void UpdateClientChat(string message, ChatChannel channels, bool isOriginator, GameObject recipient)
	{
		if (string.IsNullOrEmpty(message)) return;

		trySendingTTS(message);

		if (PlayerManager.LocalPlayerScript == null)
		{
			channels = ChatChannel.OOC;
		}

		if (channels != ChatChannel.None)
		{
			// replace action messages with chat bubble
			if(channels.HasFlag(ChatChannel.Combat) || channels.HasFlag(ChatChannel.Action) || channels.HasFlag(ChatChannel.Examine))
			{
				if(isOriginator)
				{
					ChatBubbleManager.Instance.ShowAction(Regex.Replace(message, "<.*?>", string.Empty), recipient);
				}
			}

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
