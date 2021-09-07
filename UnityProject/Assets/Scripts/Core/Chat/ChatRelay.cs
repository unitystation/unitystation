using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using Systems.MobAIs;
using System.Text.RegularExpressions;
using Systems.Ai;
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
		Loudness loud = chatEvent.VoiceLevel;

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

				if (players[i].Script.gameObject == chatEvent.originator)
				{
					//Always send the originator chat to themselves
					continue;
				}

				if (players[i].Script.IsGhost && players[i].Script.IsPlayerSemiGhost == false)
				{
					//send all to ghosts
					continue;
				}

				if (chatEvent.position == TransformState.HiddenPos)
				{
					//show messages with no provided position to everyone
					continue;
				}

				//Send chat to PlayerChatLocation pos, usually just the player object but for AI is its vessel
				var playerPosition = players[i].Script.PlayerChatLocation.OrNull()?.AssumedWorldPosServer()
					?? players[i].Script.gameObject.AssumedWorldPosServer();

				//Do player position to originator distance check
				if (DistanceCheck(playerPosition) == false)
				{
					//Distance check failed so if we are Ai, then try send action and combat messages to their camera location
					//as well as if possible
					if (chatEvent.channels.HasFlag(ChatChannel.Local) == false &&
					    players[i].Script.PlayerState == PlayerScript.PlayerStates.Ai &&
					    players[i].Script.TryGetComponent<AiPlayer>(out var aiPlayer) &&
					    aiPlayer.IsCarded == false)
					{
						playerPosition = players[i].Script.gameObject.AssumedWorldPosServer();

						//Check camera pos
						if (DistanceCheck(playerPosition))
						{
							//Camera can see player, allow Ai to see action/combat messages
							continue;
						}
					}

					//Player failed distance checks remove them
					players.RemoveAt(i);
				}

				bool DistanceCheck(Vector3 playerPos)
				{
					//TODO maybe change this to (chatEvent.position - playerPos).sqrMagnitude > 196f to avoid square root for performance?
					if (Vector2.Distance(chatEvent.position, playerPos) > 14f)
					{
						//Player in the list is too far away for local chat, remove them:
						return false;
					}

					//Within range, but check if they are in another room or hiding behind a wall
					if (MatrixManager.Linecast(chatEvent.position, LayerTypeSelection.Walls,
						layerMask, playerPos).ItHit)
					{
						//If it hit a wall remove that player
						return false;
					}

					//Player can see the position
					return true;
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
				//Binary check here to avoid speaking in local when speaking on binary
				if (!channels.HasFlag(ChatChannel.Binary) || (players[i].Script.IsGhost && players[i].Script.IsPlayerSemiGhost == false))
				{
					UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.modifiers, chatEvent.message, loud, chatEvent.messageOthers,
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
				UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.modifiers, chatEvent.message, loud, chatEvent.messageOthers,
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
	public void UpdateClientChat(string message, ChatChannel channels, bool isOriginator, GameObject recipient, Loudness loudness)
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
